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
using System.Linq;
using System.Threading.Tasks;
using CodeIngestLib;
using DTC.Core;
using DTC.Core.Extensions;
using DTC.Core.UI;
using DTC.Core.ViewModels;
using Material.Icons;

namespace CodeIngest.Desktop;

public class MainViewModel : ViewModelBase
{
    private readonly IDialogService m_dialogService;
    private readonly ActionConsolidator m_folderSelectionConsolidator;
    private FolderTreeRoot m_root;
    private ProgressToken m_backgroundRefreshProgress;
    private bool m_isCSharp = Settings.Instance.IsCSharp;
    private bool m_isCpp = Settings.Instance.IsCpp;
    private bool m_isPython = Settings.Instance.IsPython;
    private bool m_isJavaScript = Settings.Instance.IsJavaScript;
    private bool m_isCustomFilter = Settings.Instance.IsCustomFilter;
    private bool m_excludeImports = Settings.Instance.ExcludeImports;
    private bool m_useFullPaths = Settings.Instance.UseFullPaths;
    private bool m_excludeComments = Settings.Instance.ExcludeComments;
    private string m_customFileFilter = Settings.Instance.CustomFileFilter;
    private int? m_previewFileCount;
    private long? m_previewFileSize;
    private bool m_isGeneratingPreview;

    public FolderTreeRoot Root
    {
        get => m_root;
        set
        {
            if (!SetField(ref m_root, value) || value == null)
                return;
            Settings.Instance.RootFolder = m_root.Root.Clone();
            m_root.SelectionChanged += OnFolderSelectionChanged;
            InvalidatePreviewStats();
        }
    }
    
    public bool IsCSharp
    {
        get => m_isCSharp;
        set
        {
            if (!SetField(ref m_isCSharp, value))
                return;
            Settings.Instance.IsCSharp = value;
            InvalidatePreviewStats();
        }
    }

    public bool IsCpp
    {
        get => m_isCpp;
        set
        {
            if (!SetField(ref m_isCpp, value))
                return;
            Settings.Instance.IsCpp = value;
            InvalidatePreviewStats();
        }
    }
    
    public bool IsPython
    {
        get => m_isPython;
        set
        {
            if (!SetField(ref m_isPython, value))
                return;
            Settings.Instance.IsPython = value;
            InvalidatePreviewStats();
        }
    }

    public bool IsJavaScript
    {
        get => m_isJavaScript;
        set
        {
            if (!SetField(ref m_isJavaScript, value))
                return;
            Settings.Instance.IsJavaScript = value;
            InvalidatePreviewStats();
        }
    }
    
    public bool IsCustomFilter
    {
        get => m_isCustomFilter;
        set
        {
            if (!SetField(ref m_isCustomFilter, value))
                return;
            Settings.Instance.IsCustomFilter = value;
            InvalidatePreviewStats();
        }
    }

    public string CustomFileFilter
    {
        get => m_customFileFilter.StringOrDefault("*.*");
        set
        {
            if (!SetField(ref m_customFileFilter, value))
                return;
            Settings.Instance.CustomFileFilter = value;
            InvalidatePreviewStats();
        }
    }

    public bool ExcludeImports
    {
        get => m_excludeImports;
        set
        {
            if (!SetField(ref m_excludeImports, value))
                return;
            Settings.Instance.ExcludeImports = value;
            InvalidatePreviewStats();
        }
    }

    public bool ExcludeComments
    {
        get => m_excludeComments;
        set
        {
            if (!SetField(ref m_excludeComments, value))
                return;
            Settings.Instance.ExcludeComments = value;
            InvalidatePreviewStats();
        }
    }

    public bool UseFullPaths
    {
        get => m_useFullPaths;
        set
        {
            if (!SetField(ref m_useFullPaths, value))
                return;
            Settings.Instance.UseFullPaths = value;
            InvalidatePreviewStats();
        }
    }

    public int? PreviewFileCount
    {
        get => m_previewFileCount;
        set => SetField(ref m_previewFileCount, value);
    }

    public long? PreviewFileSize
    {
        get => m_previewFileSize;
        set
        {
            if (!SetField(ref m_previewFileSize, value))
                return;
            OnPropertyChanged(nameof(PreviewTokenCount));
            OnPropertyChanged(nameof(PreviewTokenRisk));
        }
    }

    public int PreviewTokenCount => (int)((PreviewFileSize ?? 0) / 3.8);
    public double PreviewTokenRisk => (double)PreviewTokenCount / 128_000;

    public bool IsGeneratingPreview
    {
        get => m_isGeneratingPreview;
        set => SetField(ref m_isGeneratingPreview, value);
    }

    public async Task SelectRoot()
    {
        var rootFolder = await m_dialogService.SelectFolderAsync("Select a folder to scan for code.");
        if (rootFolder == null)
            return;
        
        Root = new FolderTreeRoot(rootFolder);
    }

    public MainViewModel(IDialogService dialogService = null)
    {
        m_dialogService = dialogService ?? DialogService.Instance;

        m_folderSelectionConsolidator = new ActionConsolidator(() => _ = RefreshPredictedSizeAsync(), 2.0);
        Root = new FolderTreeRoot(Settings.Instance.RootFolder);
    }

    public async Task RunIngest()
    {
        var selectedFolders = Root.GetSelectedItems().ToArray();
        if (selectedFolders.Length == 0)
            return; // Nothing to do.

        var outputFile = await PromptForOutputFile(selectedFolders);
        if (outputFile == null)
            return; // User cancelled.

        var progress = new ProgressToken(true) { IsCancelSupported = true };
        (int FileCount, long OutputBytes)? result;
        using (m_dialogService.ShowBusy("Generating code...", progress))
        {
            var options = GetIngestOptions();
            var ingester = new Ingester(options);
            result = await Task.Run(() => ingester.Run(selectedFolders, outputFile, progress));
        }
        if (!result.HasValue)
        {
            m_dialogService.ShowMessage("Failed to generate code file.", "Please check your file permissions and try again.", MaterialIconKind.Error);
            return;
        }
        
        m_dialogService.ShowMessage("Code file generated successfully.", $"{result.Value.FileCount:N0} files produced {result.Value.OutputBytes.ToSize()} of output.");
    }

    private async Task<FileInfo> PromptForOutputFile(DirectoryInfo[] selectedFolders)
    {
        string[] saveFilter;
        if (IsCpp)
            saveFilter = [".cpp"];
        else if (IsPython)
            saveFilter = [".py"];
        else if (IsJavaScript)
            saveFilter = [".js"];
        else if (IsCustomFilter)
            saveFilter = [".txt"];
        else
            saveFilter = [".cs"];
        
        var nameSuggestion = selectedFolders[0].Name + "_Ingested";
        var outputFile = await m_dialogService.ShowFileSaveAsync("Save output file as...", nameSuggestion, "Code File", saveFilter);
        return outputFile;
    }

    private IngestOptions GetIngestOptions()
    {
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
        } else if (IsCpp)
        {
            if (!ExcludeImports)
                options.FilePatterns.Add("*.h");
            options.FilePatterns.Add("*.cpp");
            options.FilePatterns.Add("*.c");
        } else if (m_isJavaScript)
        {
            options.FilePatterns.Add("*.js");
        } else if (IsPython)
        {
            options.FilePatterns.Add("*.py");
        } else if (IsCustomFilter)
        {
            var filters = CustomFileFilter.StringOrDefault("*.*").Split(';').Select(o => o.Trim()).Where(IsValidFileFilter).ToArray();
            options.FilePatterns.AddRange(filters);
        }
        
        return options;
    }

    private static bool IsValidFileFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return false;

        // Disallow spaces
        if (filter.Contains(' '))
            return false;

        // Disallowed characters in file names (excluding * and ? which are valid in wildcards)
        if (filter.IndexOfAny((char[])['<', '>', ':', '"', '/', '\\', '|']) >= 0)
            return false;

        // Must contain * and .
        return filter.Contains('*') && filter.Contains('.');
    }
    
    private void OnFolderSelectionChanged(object sender, EventArgs e) =>
        InvalidatePreviewStats();

    private void InvalidatePreviewStats()
    {
        IsGeneratingPreview = true;
        PreviewFileCount = null;
        PreviewFileSize = null;
        m_folderSelectionConsolidator.Invoke();
    }

    private async Task RefreshPredictedSizeAsync()
    {
        try
        {
            m_backgroundRefreshProgress?.Cancel();
        
            var selectedFolders = Root.GetSelectedItems().ToArray();
            if (selectedFolders.Length == 0)
            {
                PreviewFileCount = 0;
                PreviewFileSize = 0;
                return; // Nothing to do.
            }
        
            var options = GetIngestOptions();
            if (options.FilePatterns.Count == 0)
            {
                PreviewFileCount = 0;
                PreviewFileSize = 0;
                return; // Nothing search.
            }
        
            var ingester = new Ingester(options);
            m_backgroundRefreshProgress = new ProgressToken(isCancelSupported: true);
        
            await Task.Run(() =>
            {
                var result = ingester.Run(selectedFolders, progress: m_backgroundRefreshProgress);
                if (!result.HasValue || m_backgroundRefreshProgress.CancelRequested)
                    return;
                PreviewFileCount = result.Value.FileCount;
                PreviewFileSize = result.Value.OutputBytes;
            });
        }
        finally
        {
            IsGeneratingPreview = false;
        }
    }
}