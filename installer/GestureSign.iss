#define AppName "GestureSign"
#ifndef AppVersion
#define AppVersion "8.1.0"
#endif
#ifndef ReleaseTag
#define ReleaseTag AppVersion
#endif
#ifndef SourceDir
#define SourceDir "..\bin\Release"
#endif
#ifndef OutputDir
#define OutputDir "..\artifacts"
#endif
#ifndef OutputBaseFilename
#define OutputBaseFilename "GestureSignSetup"
#endif

[Setup]
AppId={{FF1CDB16-BCD3-4A02-95E9-9C82FC9D91E9}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#ReleaseTag}
AppPublisher=GestureSign community fork
AppPublisherURL=https://github.com/CaFeZn/GestureSign
AppSupportURL=https://github.com/CaFeZn/GestureSign/issues
AppUpdatesURL=https://github.com/CaFeZn/GestureSign/releases
DefaultDirName={autopf}\GestureSign
DefaultGroupName=GestureSign
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile=..\GestureSign.ControlPanel\Resources\normal.ico
UninstallDisplayIcon={app}\GestureSign.ControlPanel.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible arm64
ArchitecturesInstallIn64BitMode=x64compatible arm64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Start GestureSign daemon when Windows starts"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\GestureSign Control Panel"; Filename: "{app}\GestureSign.ControlPanel.exe"
Name: "{group}\GestureSign Daemon"; Filename: "{app}\GestureSign.exe"
Name: "{group}\Uninstall GestureSign"; Filename: "{uninstallexe}"
Name: "{autodesktop}\GestureSign"; Filename: "{app}\GestureSign.ControlPanel.exe"; Tasks: desktopicon
Name: "{userstartup}\GestureSign"; Filename: "{app}\GestureSign.exe"; Tasks: startup

[Run]
Filename: "{app}\GestureSign.ControlPanel.exe"; Description: "{cm:LaunchProgram,GestureSign}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{cmd}"; Parameters: "/C taskkill /IM GestureSign.exe /F >NUL 2>NUL"; Flags: runhidden

[Code]
function IsDotNet48Installed(): Boolean;
var
  Release: Cardinal;
begin
  Result :=
    (RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040)) or
    (RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040));
end;

function InitializeSetup(): Boolean;
begin
  Result := IsDotNet48Installed();
  if not Result then
  begin
    MsgBox(
      'GestureSign requires Microsoft .NET Framework 4.8.' + #13#10 + #13#10 +
      'Install .NET Framework 4.8 first, then run this installer again:' + #13#10 +
      'https://dotnet.microsoft.com/download/dotnet-framework/net48',
      mbCriticalError,
      MB_OK);
  end;
end;
