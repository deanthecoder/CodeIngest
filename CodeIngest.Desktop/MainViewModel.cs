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
using System.IO;
using System.Threading.Tasks;
using CodeIngestLib;
using CSharp.Core.Extensions;
using CSharp.Core.UI;
using CSharp.Core.ViewModels;
using Material.Icons;

namespace CodeIngest.Desktop;

public class MainViewModel : ViewModelBase
{
    private readonly IDialogService m_dialogService;
    private FolderTreeRoot m_root = new FolderTreeRoot(new DirectoryInfo(Environment.CurrentDirectory));

    private bool m_isCSharp = true;
    private bool m_isCppNoHeaders;
    private bool m_isCppWithHeaders;
    private bool m_includeMarkdown;
    private bool m_excludeImports = true;
    private bool m_useFullPaths;
    private bool m_excludeComments = true;

    public FolderTreeRoot Root
    {
        get => m_root;
        set => SetField(ref m_root, value);
    }

    public bool IsCSharp
    {
        get => m_isCSharp;
        set => SetField(ref m_isCSharp, value);
    }

    public bool IsCppNoHeaders
    {
        get => m_isCppNoHeaders;
        set => SetField(ref m_isCppNoHeaders, value);
    }

    public bool IsCppWithHeaders
    {
        get => m_isCppWithHeaders;
        set => SetField(ref m_isCppWithHeaders, value);
    }

    public bool ExcludeImports
    {
        get => m_excludeImports;
        set => SetField(ref m_excludeImports, value);
    }

    public bool ExcludeComments
    {
        get => m_excludeComments;
        set => SetField(ref m_excludeComments, value);
    }

    public bool IncludeMarkdown
    {
        get => m_includeMarkdown;
        set => SetField(ref m_includeMarkdown, value);
    }

    public bool UseFullPaths
    {
        get => m_useFullPaths;
        set => SetField(ref m_useFullPaths, value);
    }

    public async Task SelectRoot()
    {
        var rootFolder = await m_dialogService.SelectFolderAsync("Select a folder to scan for code.");
        if (rootFolder != null)
            Root = new FolderTreeRoot(rootFolder);
    }

    public MainViewModel(IDialogService dialogService = null)
    {
        m_dialogService = dialogService ?? DialogService.Instance;
    }

    public async Task RunIngest()
    {
        var selectedFolders = Root.GetSelectedItems();
        if (selectedFolders.Length == 0)
            return; // Nothing to do.

        string[] filterExtensions;
        if (IsCppNoHeaders)
            filterExtensions = [".cpp"];
        else if (IsCppWithHeaders)
            filterExtensions = [".cpp", ".h"];
        else
            filterExtensions = [".cs"];
        
        var nameSuggestion = selectedFolders[0].Name;
        var outputFile = await m_dialogService.ShowFileSaveAsync("Save output file as...", nameSuggestion, "Code File", filterExtensions);
        if (outputFile == null)
            return; // User cancelled.

        var options = new IngestOptions
        {
            ExcludeImports = ExcludeImports,
            UseFullPaths = UseFullPaths,
            StripComments = ExcludeComments
        };

        options.FilePatterns.Clear();
        if (IsCSharp)
        {
            options.FilePatterns.Add("*.cs");
        } else if (IsCppNoHeaders)
        {
            options.FilePatterns.Add("*.cpp");
        } else if (IsCppWithHeaders)
        {
            options.FilePatterns.Add("*.h");
            options.FilePatterns.Add("*.cpp");
        }
        
        if (IncludeMarkdown)
            options.FilePatterns.Add("*.md");
        
        var ingester = new Ingester(options);
        var result = ingester.Run(selectedFolders, outputFile);
        if (!result.HasValue)
        {
            m_dialogService.ShowMessage("Failed to generate code file.", "Please check your file permissions and try again.", MaterialIconKind.Error);
            return;
        }
        
        m_dialogService.ShowMessage("Code file generated successfully.", $@"{result.Value.FileCount:N0} files produced {result.Value.OutputBytes.ToSize()} of output.");
    }
}