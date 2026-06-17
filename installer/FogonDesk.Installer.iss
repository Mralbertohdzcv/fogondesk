#define MyAppName "FogonDesk POS"
#define MyAppVersion "0.2.0"
#define MyAppPublisher "FogonDesk"
#define MyAppExeName "FogonDesk.Desktop.exe"

[Setup]
AppId={{17E10E0D-5298-479B-86F2-C930074D7185}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\FogonDesk POS
ArchitecturesAllowed=x86 x64compatible
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=FogonDeskSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UsePreviousAppDir=yes
SetupLogging=yes

[Files]
Source: "..\src\FogonDesk.Desktop\bin\x86\Release\net48\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "seed\fogondesk.db"; DestDir: "{localappdata}\FogonDesk\data"; DestName: "fogondesk.db"; Flags: ignoreversion
Source: "seed\fogondesk.db"; DestDir: "{app}\seed"; DestName: "fogondesk.db"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Iniciar {#MyAppName}"; Flags: nowait postinstall skipifsilent
