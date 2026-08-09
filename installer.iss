#ifndef MyAppVersion
#define MyAppVersion "1.1.0-Alpha"
#endif
#define MyAppName "TeamSpeak Overlay Pro"
#define MyAppPublisher "SergeyIvanovPro"
#define MyAppURL "https://github.com/texport/teamspeak-overlay"
#define MyAppExeName "TeamSpeakOverlay.exe"

[Setup]
AppId={{D374D871-3D28-44A4-B97F-99F5E68C1C82}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\TeamSpeakOverlay
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
SetupIconFile=app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=bin\Release\net8.0-windows
OutputBaseFilename=TeamSpeakOverlay-v1.1.0-Alpha-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
