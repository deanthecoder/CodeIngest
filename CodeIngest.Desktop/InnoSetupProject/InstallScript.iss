#define MyAppName     "Code Ingest"
#define MyAppExeName  "CodeIngest.Desktop.exe"

[Setup]
AppId={{CA3E4B28-314A-451F-ED15-8777E28DBDE4}
AppName={#MyAppName}
AppVersion=1.0
AppPublisher=Dean Edis
AppPublisherURL=https://github.com/deanthecoder/CodeIngest
DefaultDirName={commonpf}\CodeIngest
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
SourceDir=..\CodeIngest.Desktop\
OutputDir=..\InnoSetupProject\
OutputBaseFilename={#MyAppName} Installer

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net9.0\publish\win-x64\*.*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
