; MonitorianNeo インストーラ（Inno Setup 6）
;
; ビルド方法:
;   dotnet publish MonitorianNeo\Monitorian\Monitorian.csproj -c Release -r win-x64 ^
;     --self-contained true -p:PublishSingleFile=true ^
;     -p:IncludeNativeLibrariesForSelfExtract=true -o dist\publish
;   ISCC.exe /DAppVersion=0.1.0 installer\MonitorianNeo.iss
;
; 出力: dist\MonitorianNeo-<version>-setup.exe

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

#define AppName "MonitorianNeo"
#define AppPublisher "hashimotok-delien"
#define AppUrl "https://github.com/hashimotok-delien/MonitorianNeo"
#define AppExeName "MonitorianNeo.exe"

[Setup]
; AppId は製品ごとに固定する（変更すると別製品として扱われ、上書きインストールされない）
AppId={{7F3A5C1E-9B62-4E48-93D0-5A8C2B6E4F17}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\MonitorianNeo\Installer\Resources\License.rtf
OutputDir=..\dist
OutputBaseFilename={#AppName}-{#AppVersion}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; 管理者権限なしでもインストールできる（既定はユーザー領域、必要なら管理者向けに切り替え可能）
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=yes
SetupIconFile=..\MonitorianNeo\Monitorian\Resources\Icons\AppIcon.ico

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\dist\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; アンインストール時に起動中のプロセスを終了させる
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM {#AppExeName}"; Flags: runhidden skipifdoesntexist
