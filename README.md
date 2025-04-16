# CodeIngest

**CodeIngest** is a cross-platform C# CLI tool that recursively scans a directory of `.cs` files, filters out noise (comments, using statements, namespaces), and generates a flattened source dump designed for GPT code review or large-scale source inspection.

## Features

- Cross-platform (.NET 6+)
- Strips comments, `using` directives, and `namespace` blocks
- Outputs a single readable `.cs` file with:
  - File headers
  - Line numbers
  - Cleaned source code
- Skips generated and irrelevant files (e.g. `.designer.cs`, `bin`, `obj`, `.resx`, etc.)

## Usage

```
CodeIngest.exe <source-folder> <output-file.cs>
```
