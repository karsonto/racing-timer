; 比赛计时系统 - Inno Setup 脚本
; 请确保已安装 Inno Setup: https://jrsoftware.org/isinfo.php

#define MyAppName "比赛计时系统"
#define MyAppVersion "1.0.3"
#define MyAppPublisher "Race Timer Pro"
#define MyAppExeName "Timer.exe"
#define MyAppURL "https://github.com/karsonto/racing-timer"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\RaceTimerPro
DefaultGroupName={#MyAppName}
OutputDir=installer
OutputBaseFilename=RaceTimerPro_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
WizardStyle=modern
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加图标:"

[Files]
Source: "Timer\Timer\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Dirs]
Name: "{app}\data"; Permissions: users-modify

[UninstallDelete]
Type: filesandordirs; Name: "{app}\data"

