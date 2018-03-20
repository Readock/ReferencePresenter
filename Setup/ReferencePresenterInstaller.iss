; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{0E1E7DCB-A174-401F-84F8-2DC675880131}
AppName=ReferencePresenter
AppVersion=1.0
;AppVerName=ReferencePresenter ReferencePresenter 1.0
DefaultDirName={pf}\ReferencePresenter
DefaultGroupName=ReferencePresenter
AllowNoIcons=yes
OutputBaseFilename=setup
Compression=lzma
SolidCompression=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked 
Name: "explorerintegration"; Description: "Creates open with dialog in explorer"; GroupDescription: "Windows Explorer integration:"; Flags: checkedonce          

[Files]
Source: "..\ReferencePresenter\bin\Release\ReferencePresenter.exe"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\ReferencePresenter"; Filename: "{app}\ReferencePresenter.exe"
Name: "{group}\{cm:UninstallProgram,ReferencePresenter}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\ReferencePresenter"; Filename: "{app}\ReferencePresenter.exe"; Tasks: desktopicon

[Registry]
Root: HKCR; Subkey: "*\shell\Open with ReferencePresenter"; Flags: uninsdeletekey; Tasks: explorerintegration
Root: HKCR; Subkey: "*\shell\Open with ReferencePresenter\command"; Flags: uninsdeletekey; Tasks: explorerintegration              
Root: HKCR; Subkey: "*\shell\Open with ReferencePresenter\command"; ValueType: string; ValueName: ""; ValueData: "{app}\ReferencePresenter.exe %1"; Tasks: explorerintegration
Root: HKCR; Subkey: "*\shell\Open with ReferencePresenter"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\ReferencePresenter.exe"; Tasks: explorerintegration


[Run]
Filename: "{app}\ReferencePresenter.exe"; Description: "{cm:LaunchProgram,ReferencePresenter}"; Flags: nowait postinstall skipifsilent

;[Code]
;begin
;  if IsTaskSelected('shellextension') then begin
;    RegWriteStringValue(HKEY_CLASSES_ROOT, '*\shell\OpenWithReferencePresenter\command', 'UserName', ExpandConstant('{sysuserinfoname}'));    
;  end;
;end;