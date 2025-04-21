namespace CodeIngest;

static class Program
{
    private static void Main(string[] args)
    {
        // Getting directory argument from the command line (Report error if none found).
        if (args.Length < 2 || args.Any(a => a is "-h" or "--help" or "/?"))
        {
            ShowUsage();
            return;
        }

        var outputFile = new FileInfo(args[^1]);
        var patterns = new List<string>();
        var directories = new List<DirectoryInfo>();

        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Contains('*'))
                patterns.AddRange(args[i].Split(';', StringSplitOptions.RemoveEmptyEntries));
            else
                directories.Add(new DirectoryInfo(args[i]));
        }

        if (directories.Count == 0)
            directories.Add(new DirectoryInfo("."));

        if (patterns.Count == 0)
            patterns.Add("*.cs");

        // Recurse directory to find all source files.
        var sourceFiles = directories
            .Where(d => d.Exists)
            .SelectMany(d => patterns.SelectMany(p =>
            {
                try
                {
                    return d.GetFiles(p, SearchOption.AllDirectories);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to read directory {d.FullName}: {ex.Message}");
                    return [];
                }
            }))
            .Where(f => !ShouldSkipFile(f))
            .ToDictionary(o => o.Name, o => File.ReadLines(o.FullName));
        
        if (sourceFiles.Count == 0)
        {
            Console.WriteLine("No matching files found. Check your filters or directory paths.");
            return;
        }

        // Write header.
        using (var fileStream = outputFile.Open(FileMode.Create))
        using (var writer = new StreamWriter(fileStream))
        {
            {
                writer.WriteLine("// CodeIngest Source Dump - A CLI tool that merges and processes code files for GPT reviews.");
                writer.WriteLine("// Notes: Some code content may have been removed.");

                // Combine files into a single output file.
                foreach (var kvp in sourceFiles)
                {
                    var lines = kvp.Value.ToList(); // Force evaluation to count
                    var padWidth = lines.Count.ToString().Length;

                    writer.WriteLine($"// File: {kvp.Key}");

                    var lineNumber = 1;
                    foreach (var line in lines)
                    {
                        if (ShouldIncludeSourceLine(line))
                            writer.WriteLine($"{lineNumber.ToString().PadLeft(padWidth)}|{GetCodeLine(line).Trim()}");

                        lineNumber++;
                    }
                }
            }
        }
        
        // Report summary.
        Console.WriteLine("CodeIngest completed successfully.");
        Console.WriteLine($"Processed {sourceFiles.Count:N0} files, producing {outputFile.Length:N0} bytes.");
    }

    private static void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  CodeIngest [<directory> ...] [*.ext1;*.ext2] <output.code>");
        Console.WriteLine();
        Console.WriteLine("See:");
        Console.WriteLine("  https://github.com/deanthecoder/CodeIngest");
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

    private static bool ShouldSkipFile(FileInfo f) =>
        new[]
            {
                "resx", ".g.", ".designer.", "\\obj\\", "/obj/", "\\bin\\", "/bin/", "assemblyinfo.cs", "/.", "\\."
            }
            .Any(o => f.FullName.Contains(o, StringComparison.OrdinalIgnoreCase));

    private static bool ShouldIncludeSourceLine(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;
        if (s.StartsWith("using"))
            return false;
        
        var trimmed = s.Trim();
        if (trimmed.StartsWith("//"))
            return false;
        if (trimmed.StartsWith("namespace"))
            return false;
        return true;
    }

    private static string GetCodeLine(string line)
    {
        // Strip all comments.
        var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
        return commentIndex >= 0 ? line[..commentIndex] : line;
    }
}