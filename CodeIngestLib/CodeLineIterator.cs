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
using JetBrains.Annotations;

namespace CodeIngestLib;

/// <summary>
/// Iterates through code lines from a StreamReader, providing filtered content based on specified options.
/// Can optionally strip comments and import statements while processing each line.
/// </summary>
internal class CodeLineIterator
{
    private readonly StreamReader m_reader;
    private readonly bool m_stripComments;
    private readonly bool m_stripImports;

    public CodeLineIterator([NotNull] StreamReader reader, bool stripComments, bool stripImports)
    {
        m_reader = reader ?? throw new ArgumentNullException(nameof(reader));
        m_stripComments = stripComments;
        m_stripImports = stripImports;
    }

    public IEnumerable<string> GetLines()
    {
        var inBlockComment = false;
        while (m_reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            line = line.Trim();

            if (m_stripImports && (line.StartsWith("using") || line.StartsWith("#include") || line.StartsWith("#pragma") || line.StartsWith("namespace") || line.StartsWith("import") || line.StartsWith("from ")))
                continue;

            if (m_stripComments)
            {
                if (line.StartsWith("//") || line.StartsWith("# "))
                    continue;

                if (line.StartsWith("/*") && line.EndsWith("*/"))
                    continue;
                
                // Strip single-line comments mid-line.
                var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                if (commentIndex > 0)
                    line = line[..commentIndex].Trim();

                // Strip comments in the middle of a line.
                var commentStart = line.IndexOf("/*", StringComparison.Ordinal);
                var commentEnd = line.IndexOf("*/", StringComparison.Ordinal);
                while (commentStart >= 0 && commentEnd >= commentStart)
                {
                    line = (line[..commentStart] + line[(commentEnd + 2)..]).Trim();
                    
                    commentStart = line.IndexOf("/*", StringComparison.Ordinal);
                    commentEnd = line.IndexOf("*/", StringComparison.Ordinal);
                }
                
                if (inBlockComment)
                {
                    if (!line.Contains("*/"))
                        continue; // We're in a comment block - Skip this line.
                    
                    // We're in a block comment, and this line ends with a comment block.
                    inBlockComment = false;
                    line = line[(line.IndexOf("*/", StringComparison.Ordinal) + 2)..].Trim();
                }

                if (line.Contains("/*"))
                {
                    inBlockComment = true;
                    continue;
                }
            }

            yield return line;
        }
    }
}