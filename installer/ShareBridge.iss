; Inno Setup Script for Share Bridge
; Produces a standalone, single-file setup installer: ShareBridge-Setup-v1.0.1.exe

#define MyAppName "Share Bridge"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "aadilahammad86"
#define MyAppURL "https://github.com/aadilahammad86/ShareBridge"
#define MyAppExeName "ShareBridge.exe"
#define MyAppSourceDir "..\src\ShareBridge\bin\Release\net10.0-windows10.0.26100.0\win-x64"

[Setup]
AppId={{5E58309A-FD1C-4B6F-92DF-6A59972352B1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\ShareBridge
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\
OutputBaseFilename=ShareBridge-Setup-v{#MyAppVersion}
SetupIconFile=..\src\ShareBridge\Assets\AppIcon.ico
UninstallDisplayIcon={app}\Assets\AppIcon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog commandline
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "contextmenu"; Description: "Add Share Bridge to Windows File Explorer context menu"; GroupDescription: "Shell Integration:"; Flags: checkedonce

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\AppIcon.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Assets\AppIcon.ico"; Tasks: desktopicon

[Registry]
; Cascading Context Menu in File Explorer for All Files (System-Wide)
Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Share Bridge"; Flags: uninsdeletekey; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\Assets\AppIcon.ico"; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge"; ValueType: string; ValueName: "SubCommands"; ValueData: ""; Tasks: contextmenu

Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\01_Telegram"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Send via Telegram"; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\01_Telegram\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --target telegram ""%1"""; Tasks: contextmenu

Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\02_Clipboard"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Copy to Clipboard"; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\02_Clipboard"; ValueType: string; ValueName: "Icon"; ValueData: "shell32.dll,134"; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\02_Clipboard\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --target clipboard ""%1"""; Tasks: contextmenu

Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\03_WhatsApp"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Send via WhatsApp"; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\03_WhatsApp\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --target whatsapp ""%1"""; Tasks: contextmenu

Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\04_Gmail"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Send via Gmail"; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\04_Gmail\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" --target gmail ""%1"""; Tasks: contextmenu

Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\05_Picker"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Open Share Bridge..."; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\05_Picker"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\Assets\AppIcon.ico"; Tasks: contextmenu
Root: HKLM; Subkey: "Software\Classes\*\shell\ShareBridge\shell\05_Picker\command"; ValueType: string; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: contextmenu

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
