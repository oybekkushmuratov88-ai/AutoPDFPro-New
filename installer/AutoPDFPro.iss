; AutoPDF Pro — Inno Setup script (https://jrsoftware.org/isinfo.php)
; Build the solution first (build\build.ps1), then compile this file with ISCC.exe:
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\AutoPDFPro.iss
; Output: dist\AutoPDFPro-2.0.0-Setup.exe

#define AppName "AutoPDF Pro"
#define AppVersion "2.0.0"
#define BundleSource "..\artifacts\AutoPDFPro.bundle"

[Setup]
AppId={{F3C1626A-8269-442B-A3AC-1C878B083C04}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=AutoPDF Pro
DefaultDirName={commonpf64}\Autodesk\ApplicationPlugins\AutoPDFPro.bundle
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=AutoPDFPro-{#AppVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#AppName} (AutoCAD 2024)
CloseApplications=yes
CloseApplicationsFilter=acad.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
Source: "{#BundleSource}\PackageContents.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BundleSource}\Contents\Windows\2024\*.dll"; DestDir: "{app}\Contents\Windows\2024"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}\Contents\Help"; Flags: ignoreversion isreadme

[InstallDelete]
; Remove files of a previous version of this bundle.
Type: filesandordirs; Name: "{app}\Contents"

[Code]
function IsAutoCadRunning(): Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if Exec(ExpandConstant('{cmd}'), '/C tasklist /FI "IMAGENAME eq acad.exe" | find /I "acad.exe" >NUL', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Result := (ResultCode = 0);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  while IsAutoCadRunning() do
  begin
    if MsgBox('AutoCAD ishlayapti. Davom etish uchun AutoCAD ni yoping.' + #13#10 +
              'AutoCAD is running. Please close it to continue.', mbError, MB_RETRYCANCEL) = IDCANCEL then
    begin
      Result := False;
      Exit;
    end;
  end;
end;
