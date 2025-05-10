// Code authored by Dean Edis (DeanTheCoder).
// Anyone is free to copy, modify, use, compile, or distribute this software,
// either in source code form or as a compiled binary, for any non-commercial
// purpose.
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
using CodeIngestLib;
using DTC.Core.Extensions;

namespace CodeIngestCmd;

internal static class Program
{
    private static void Main(string[] args)
    {
        var switches = args.Where(o => o.StartsWith('-')).ToArray();
        var arguments = args.Where(o => !o.StartsWith('-')).ToArray();

        if (args.Length < 2 || args.Any(a => a is "-h" or "--help" or "/?"))
        {
            ShowUsage();
            return;
        }

        var outputFile = new FileInfo(arguments[^1]);
        var patterns = new List<string>();
        var directories = new List<DirectoryInfo>();

        for (var i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i].Contains('*'))
                patterns.AddRange(arguments[i].Split(';', StringSplitOptions.RemoveEmptyEntries));
            else
                directories.Add(new DirectoryInfo(arguments[i]));
        }

        if (directories.Count == 0)
            directories.Add(new DirectoryInfo("."));

        if (patterns.Count == 0)
            patterns.AddIfUnique("*.cs");

        var options = new IngestOptions
        {
            UseFullPaths = switches.Any(o => o is "-full"),
            Verbose = switches.Any(o => o is "-v")
        };
        if (patterns.Count > 0)
        {
            options.FilePatterns.Clear();
            options.FilePatterns.AddRange(patterns);
        }
        
        var ingester = new Ingester(options);
        var result = ingester.Run(directories, outputFile);
        if (!result.HasValue)
        {
            Console.WriteLine("CodeIngest failed.");
            return;
        }
        
        Console.WriteLine("CodeIngest completed successfully.");
        Console.WriteLine($"Processed {result.Value.FileCount:N0} files, producing {result.Value.OutputBytes:N0} bytes.");
    }

    private static void ShowUsage()
    {
        Console.WriteLine("CodeIngest - A CLI tool that merges and processes code files for GPT reviews.");
        Console.WriteLine("             https://github.com/deanthecoder/CodeIngest");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  CodeIngest [-h] [-full] [-v] [<directory> ...] [*.ext1;*.ext2] <output.code>");
        Console.WriteLine();
        Console.WriteLine("Where:");
        Console.WriteLine("  -full           Optionally include full path names in the output.");
        Console.WriteLine("  -h              Show this help message.");
        Console.WriteLine("  -v              Verbose mode.");
        Console.WriteLine("  <directory>     One or more directories to search for source files.");
        Console.WriteLine("                  If not specified, the current working directory will be used.");
        Console.WriteLine("  *.ext1;...      One or more file extensions to include in the search.");
        Console.WriteLine("                  If not specified, *.cs will be used by default.");
        Console.WriteLine("  <output.code>   The output file to write the merged code to.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  CodeIngest MyProject Out.cs");
        Console.WriteLine("  CodeIngest Src1 Src2 *.cs;*.txt Dump.txt");
        Console.WriteLine("  CodeIngest *.cs;*.cpp SourceDump.code");
        Console.WriteLine();
        Console.WriteLine("Note:");
        Console.WriteLine("  - The output file will be overwritten if it already exists.");
        Console.WriteLine("  - If no directory is specified, the current directory is used.");
        Console.WriteLine("  - If no file filter is specified, *.cs is used by default.");
    }
}