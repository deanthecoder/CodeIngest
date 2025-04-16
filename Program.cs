namespace CodeIngest;

internal static class Program
{
    private static void Main(string[] args)
    {
        // Getting directory argument from the command line (Report error if none found).
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: CodeIngest <directory> <output.cs>");
            return;
        }
        
        var sourceDirectory = new DirectoryInfo(args[0]);
        
        // Check directory exists (Report error if not found).
        if (!sourceDirectory.Exists)
        {
            Console.WriteLine("Directory not found: " + sourceDirectory.FullName);
            return;
        }
        
        // Recurse directory to file all .cs files.
        var sourceFiles =
            sourceDirectory
                .GetFiles("*.cs", SearchOption.AllDirectories)
                .Where(f => !ShouldSkipFile(f))
                .ToDictionary(o => o.FullName, o => File.ReadLines(o.FullName).Where(ShouldIncludeSourceLine).Select(s => s.Trim()));
        
        // Write header.
        using var fileStream = new FileInfo(args[1]).Open(FileMode.Create);
        using var writer = new StreamWriter(fileStream);
        writer.WriteLine("// CodeIngest Source Dump - A CLI tool that merges and processes .cs files for GPT reviews.");
        writer.WriteLine("// Notes: Comments, namespaces, and using statements removed to reduce noise.");
        writer.WriteLine("// Language: C#");

        // Combine files into a single output file.
        foreach (var kvp in sourceFiles)
        {
            var lines = kvp.Value.ToList(); // Force evaluation to count
            var lineCount = lines.Count;
            var padWidth = lineCount.ToString().Length;

            writer.WriteLine($"// File: {kvp.Key} ({lineCount:N0} lines)");

            var lineNumber = 1;
            foreach (var line in lines)
                writer.WriteLine($"{lineNumber++.ToString().PadLeft(padWidth)} | {line}");
        }
        
        // Report summary.
        Console.WriteLine("CodeIngest completed successfully.");
        Console.WriteLine($"Processed {sourceFiles.Count:N0} files, producing {fileStream.Length:N0} bytes.");
    }

    private static bool ShouldSkipFile(FileInfo f) =>
        new[] {"resx", ".g.", ".designer.", "\\obj\\", "/obj/", "\\bin\\", "/bin/", "assemblyinfo.cs" }.Any(o => f.FullName.Contains(o, StringComparison.OrdinalIgnoreCase));

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
}