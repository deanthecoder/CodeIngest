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
using CSharp.Core.Extensions;
using CSharp.Core.Settings;

namespace CodeIngest.Desktop;

public class Settings : UserSettingsBase
{
    public static Settings Instance { get; } = new Settings();

    public DirectoryInfo RootFolder
    {
        get => Get<DirectoryInfo>();
        set => Set(value);
    }
    
    public bool IsCSharp
    {
        get => Get<bool>();
        set => Set(value);
    }
    
    public bool IsCppNoHeaders
    {
        get => Get<bool>();
        set => Set(value);
    }
    
    public bool IsCppWithHeaders
    {
        get => Get<bool>();
        set => Set(value);
    }
    
    public bool ExcludeImports
    {
        get => Get<bool>();
        set => Set(value);
    }
    
    public bool ExcludeComments
    {
        get => Get<bool>();
        set => Set(value);
    }
    
    public bool IncludeMarkdown
    {
        get => Get<bool>();
        set => Set(value);
    }
    
    public bool UseFullPaths
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsPython
    {
        get => Get<bool>();
        set => Set(value);
    }

    public bool IsJavaScript
    {
        get => Get<bool>();
        set => Set(value);
    }

    protected override void ApplyDefaults()
    {
        RootFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).ToDir();
        IsCSharp = true;
        IsCppNoHeaders = false;
        IsCppWithHeaders = false;
        IsPython = false;
        IsJavaScript = false;
        ExcludeImports = true;
        ExcludeComments = true;
        IncludeMarkdown = false;
        UseFullPaths = false;
    }
}