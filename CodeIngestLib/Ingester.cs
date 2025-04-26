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
using System.Text;

namespace CodeIngestCmd;

internal class Ingester
{
    private static string[] SymbolsToCollapse { get; } = new[]
    {
        "<", "<=", "=", "==", "=>", ">", "!=", "(", ")", "{", "}", "[", "]", "-", "+", "*", "&", "%", "/", "<<", ">>", ";", ",", "||", "|", ":", "?", "|"
    };

    public void Ingest(IngestOptions options)
    {
        var sourceFiles = options.Directories
            .Where(d => d.Exists)
            .SelectMany(d => options.Patterns.SelectMany(p =>
            {
                try
                {
                    return d.GetFiles(p, SearchOption.AllDirectories);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to read directory {d.FullName}: {ex.Message}");
                    return Array.Empty<FileInfo>();
                }
            }))
            .Where(f => !ShouldSkipFile(f))
            .ToDictionary(o => o.FullName, o => File.ReadLines(o.FullName));

        if (sourceFiles.Count == 0)
        {
            Console.WriteLine("No matching files found. Check your filters or directory paths.");
            return;
        }

        using (var fileStream = options.OutputFile.Open(FileMode.Create))
        using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
        {
            writer.NewLine = "\n";
            writer.WriteLine("// CodeIngest - A CLI tool that merges and processes code files for GPT reviews.");
            writer.WriteLine("// Notes: Some code content may have been removed.");

            foreach (var kvp in sourceFiles)
            {
                var lines = kvp.Value.ToList();
                var padWidth = lines.Count.ToString().Length;

                writer.WriteLine($"// File: {(options.UseFullPaths ? kvp.Key : Path.GetFileName(kvp.Key))}");

                var lineNumber = 1;
                foreach (var line in lines)
                {
                    if (ShouldIncludeSourceLine(line))
                        writer.WriteLine($"{lineNumber.ToString().PadLeft(padWidth)}|{GetCodeLine(line).Trim()}");

                    lineNumber++;
                }

                if (options.Verbose)
                    Console.WriteLine($"{kvp.Key} ({lines.Sum(o => o.Length):N0} characters -> {lines.Count:N0} lines)");
            }
        }

        Console.WriteLine("CodeIngest completed successfully.");
        Console.WriteLine($"Processed {sourceFiles.Count:N0} files, producing {options.OutputFile.Length:N0} bytes.");
    }

    private static bool ShouldSkipFile(FileInfo f) =>
        new[]
        {
            "resx", ".g.", ".designer.", "\\obj\\", "/obj/", "\\bin\\", "/bin/", "assemblyinfo.cs", "/.", "\\."
        }.Any(o => f.FullName.Contains(o, StringComparison.OrdinalIgnoreCase));

    private static bool ShouldIncludeSourceLine(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;
        if (s.StartsWith("using") || s.StartsWith("#include") || s.StartsWith("#pragma"))
            return false;

        var trimmed = s.Trim();
        if (trimmed.StartsWith("//"))
            return false;
        if (trimmed.StartsWith("/*") && trimmed.EndsWith("*/"))
            return false;
        if (trimmed.StartsWith("namespace"))
            return false;
        return true;
    }

    private static string GetCodeLine(string line)
    {
        var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
        if (commentIndex >= 0)
            line = line[..commentIndex];

        foreach (var expr in SymbolsToCollapse)
            line = line.Replace($"{expr} ", expr).Replace($" {expr}", expr);

        while (line.Contains("  "))
            line = line.Replace("  ", " ");

        return line;
    }
}