// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
//  purpose.
// 
// If you modify the code, please retain this copyright header,
// and consider contributing back to the repository or letting us know
// about your modifications. Your contributions are valued!
// 
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSharp.Core;
using CSharp.Core.Extensions;

namespace CodeIngestLib;

public class Ingester
{
    private readonly IngestOptions m_options;

    private static string[] SymbolsToCollapse { get; } = new[]
    {
        "<", "<=", "=", "==", "=>", ">", "!=", "(", ")", "{", "}", "[", "]", "-", "+", "*", "&", "%", "/", "<<", ">>", ";", ",", "||", "|", ":", "?", "|"
    };
    
    public Ingester(IngestOptions options)
    {
        m_options = options;
    }

    public (int FileCount, long OutputBytes)? Run(IEnumerable<DirectoryInfo> directories, FileInfo outputFile = null, ProgressToken progress = null)
    {
        var didError = false;
        var sourceFiles = directories
            .Where(d => d.Exists() && d.IsAccessible())
            .AsParallel()
            .SelectMany(d => m_options.FilePatterns.SelectMany(p =>
            {
                try
                {
                    return d.TryGetFiles(p, SearchOption.AllDirectories);
                }
                catch (Exception ex)
                {
                    didError = true;
                    Logger.Instance.Error($"Failed to read directory {d.FullName}: {ex.Message}");
                    return [];
                }
            }))
            .Where(f => !ShouldSkipFile(f))
            .ToArray();

        if (didError)
        {
            Logger.Instance.Error("Error collecting files.");
            return null;
        }
        
        if (sourceFiles.Length == 0)
        {
            Logger.Instance.Warn("No matching files found. Check your filters or directory paths.");
            return (0, 0);
        }

        // If caller isn't collecting output (Just wants size info), write output to a temp file.
        using var tempOutputFile = new TempFile();
        outputFile ??= tempOutputFile;
        
        using (var outputStream = (outputFile).Open(FileMode.Create))
        using (var writer = new StreamWriter(outputStream, Encoding.UTF8))
        {
            writer.NewLine = "\n";
            writer.WriteLine("// CodeIngest - A CLI tool that merges and processes code files for GPT reviews.");
            writer.WriteLine("// Notes: Some code content may have been removed.");

            for (var i = 0; i < sourceFiles.Length; i++)
            {
                var sourceFile = sourceFiles[i];
                if (progress != null)
                {
                    if (progress.CancelRequested)
                        break; // Caller requested cancellation.
                    progress.Progress = (int)(100.0 * (i + 1.0) / sourceFiles.Length); 
                }
                
                using var reader = new StreamReader(sourceFile.FullName, Encoding.UTF8);

                writer.WriteLine($"// File: {(m_options.UseFullPaths ? sourceFile.FullName : sourceFile.Name)}");

                var lineNumber = 1;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (ShouldIncludeSourceLine(line, m_options))
                        writer.WriteLine($"{lineNumber.ToString()}|{GetCodeLine(line).Trim()}");

                    lineNumber++;
                }

                if (m_options.Verbose)
                    Logger.Instance.Warn($"{sourceFile.FullName} processed ({lineNumber - 1:N0} lines)");
            }
        }

        return (sourceFiles.Length, outputFile.Length);
    }

    private static bool ShouldSkipFile(FileInfo f) =>
        new[]
        {
            "resx", ".g.", ".designer.", "\\obj\\", "/obj/", "\\bin\\", "/bin/", "assemblyinfo.cs", "/.", "\\."
        }.Any(o => f.FullName.Contains(o, StringComparison.OrdinalIgnoreCase));

    private static bool ShouldIncludeSourceLine(string s, IngestOptions options)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;
        var trimmed = s.Trim();

        if (options.ExcludeImports && s.StartsWith("using") || s.StartsWith("#include") || s.StartsWith("#pragma") || trimmed.StartsWith("namespace"))
            return false;

        if (options.StripComments)
        {
            if (trimmed.StartsWith("//"))
                return false;
            if (trimmed.StartsWith("/*") && trimmed.EndsWith("*/"))
                return false;
        }
        
        return true;
    }

    private static string GetCodeLine(string line)
    {
        var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
        if (commentIndex >= 0)
            line = line[..commentIndex];
        
        if (line.Contains('\t'))
            line = line.Replace('\t', ' ');
        if (!line.Contains(' '))
            return line;

        // Strip spaces around operators.
        foreach (var expr in SymbolsToCollapse)
        {
            if (line.Contains(expr))
                line = line.Replace($"{expr} ", expr).Replace($" {expr}", expr);
        }

        while (line.Contains("  "))
            line = line.Replace("  ", " ");

        return line;
    }
}