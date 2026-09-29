;===========================================================================================
; AutoPDF Pro — single-file installer (NSIS 3, https://nsis.sourceforge.io)
;
; Build the plug-in first (it assembles artifacts\AutoPDFPro.bundle), then compile this script:
;   Windows:  "C:\Program Files (x86)\NSIS\makensis.exe" installer\AutoPDFPro.nsi
;   Linux:    makensis installer/AutoPDFPro.nsi
; build\build.ps1 and build/build.sh do both. Output: dist\AutoPDFPro-<version>-Setup.exe
;
; What Setup does:
;   * language dialog (O‘zbekcha / Русский / English); on a first installation the choice also
;     becomes the plug-in's interface language
;   * needs 64-bit Windows and administrator rights; warns when AutoCAD 2024 is not installed
;   * waits until AutoCAD is closed (AutoCAD locks the DLLs)
;   * moves other AutoPDF bundles (the old plug-in, per-user copies) to
;     %LOCALAPPDATA%\AutoPDFPro\Backup\<date-time> so that AUTOPDF is defined only once
;   * installs %ProgramFiles%\Autodesk\ApplicationPlugins\AutoPDFPro.bundle (trusted by SECURELOAD,
;     loaded by every AutoCAD 2024 user of the computer)
;   * registers an uninstaller under "Apps" / "Programs and Features"
;
; Silent install:  AutoPDFPro-<version>-Setup.exe /S     Silent uninstall:  Uninstall.exe /S
;===========================================================================================

Unicode true
ManifestDPIAware true
SetCompressor /SOLID lzma
RequestExecutionLevel admin

!ifndef VERSION
  !define VERSION "2.2.0"
!endif
!define PRODUCT "AutoPDF Pro"
!define AUTHOR "Kushmuratov Oybek"
!define COPYRIGHT_YEAR "2026"
!define BUNDLE_NAME "AutoPDFPro.bundle"
; UpgradeCode of bundle\AutoPDFPro.bundle\PackageContents.xml: identifies this product's bundle
!define UPGRADE_CODE "1199BA7E-5919-42A7-A870-A8B38B9554FC"
!define REG_KEY "Software\AutoPDFPro"
!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoPDFPro"
!ifndef BUNDLE_DIR
  !define BUNDLE_DIR "..\artifacts\${BUNDLE_NAME}"
!endif
!ifndef OUTFILE
  !define OUTFILE "..\dist\AutoPDFPro-${VERSION}-Setup.exe"
!endif

Name "${PRODUCT} ${VERSION}"
OutFile "${OUTFILE}"
InstallDir "$PROGRAMFILES64\Autodesk\ApplicationPlugins\${BUNDLE_NAME}"
BrandingText "${PRODUCT} ${VERSION} · ${AUTHOR}"
ShowInstDetails hide
ShowUninstDetails hide

!include MUI2.nsh
!include LogicLib.nsh
!include FileFunc.nsh
!include x64.nsh

Var BackupDir

;-------------------------------------------------------------------------------------------
; Modern UI

!define MUI_ICON "art\AutoPDFPro.ico"
!define MUI_UNICON "art\AutoPDFPro.ico"
!define MUI_WELCOMEFINISHPAGE_BITMAP "art\welcome.bmp"
!define MUI_HEADERIMAGE
!define MUI_HEADERIMAGE_RIGHT
!define MUI_HEADERIMAGE_BITMAP "art\header.bmp"
!define MUI_ABORTWARNING
!define MUI_UNABORTWARNING
!define MUI_FINISHPAGE_NOREBOOTSUPPORT
!define MUI_CUSTOMFUNCTION_GUIINIT GuiInit

; The language of the last installation is reused by the uninstaller and by an update.
!define MUI_LANGDLL_REGISTRY_ROOT "HKLM"
!define MUI_LANGDLL_REGISTRY_KEY "${REG_KEY}"
!define MUI_LANGDLL_REGISTRY_VALUENAME "InstallerLanguage"
!define MUI_LANGDLL_ALLLANGUAGES
!define MUI_LANGDLL_WINDOWTITLE "${PRODUCT} ${VERSION}"
!define MUI_LANGDLL_INFO "Tilni tanlang · Выберите язык · Select a language"

!define MUI_WELCOMEPAGE_TEXT "$(WelcomeText)"
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE WelcomeLeave
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_TEXT "$(FinishText)"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

; NSIS's own Uzbek translation has broken characters; installer\nsis holds a corrected one.
!insertmacro MUI_LANGUAGEEX "nsis" "Uzbek"
!insertmacro MUI_LANGUAGE "Russian"
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_RESERVEFILE_LANGDLL

;-------------------------------------------------------------------------------------------
; Texts

LangString WelcomeText ${LANG_UZBEK} "Ushbu dastur kompyuteringizga $(^NameDA) plaginini o‘rnatadi. Plagin AutoCAD 2024 chizmasidagi listlarni (ramkalarni) avtomatik topadi va har birini alohida PDF faylga saqlaydi.$\r$\n$\r$\nDavom etishdan oldin AutoCAD ni yoping.$\r$\n$\r$\nMuallif: ${AUTHOR}$\r$\n$\r$\n$_CLICK"
LangString WelcomeText ${LANG_RUSSIAN} "Программа установит плагин $(^NameDA). Плагин автоматически находит листы (рамки) в чертеже AutoCAD 2024 и сохраняет каждый в отдельный PDF-файл.$\r$\n$\r$\nПеред продолжением закройте AutoCAD.$\r$\n$\r$\nАвтор: ${AUTHOR}$\r$\n$\r$\n$_CLICK"
LangString WelcomeText ${LANG_ENGLISH} "Setup will install the $(^NameDA) plug-in. It finds the sheets (frames) of an AutoCAD 2024 drawing automatically and saves each one as a separate PDF file.$\r$\n$\r$\nPlease close AutoCAD before you continue.$\r$\n$\r$\nAuthor: ${AUTHOR}$\r$\n$\r$\n$_CLICK"

LangString FinishText ${LANG_UZBEK} "$(^NameDA) kompyuteringizga o‘rnatildi.$\r$\n$\r$\nQanday foydalaniladi:$\r$\n   1. AutoCAD 2024 ni ishga tushiring.$\r$\n   2. AUTOPDF buyrug‘ini kiriting (Enter).$\r$\n$\r$\nInterfeys tilini plaginning «Sozlamalar» oynasida o‘zgartirish mumkin."
LangString FinishText ${LANG_RUSSIAN} "$(^NameDA) установлен на компьютер.$\r$\n$\r$\nКак пользоваться:$\r$\n   1. Запустите AutoCAD 2024.$\r$\n   2. Введите команду AUTOPDF (Enter).$\r$\n$\r$\nЯзык интерфейса можно изменить в окне «Настройки» плагина."
LangString FinishText ${LANG_ENGLISH} "$(^NameDA) has been installed.$\r$\n$\r$\nHow to use it:$\r$\n   1. Start AutoCAD 2024.$\r$\n   2. Type the command AUTOPDF (Enter).$\r$\n$\r$\nThe interface language can be changed in the plug-in's Settings window."

; PluginSettings.Language values (src\AutoPDF.Core\Settings\PluginSettings.cs)
LangString PluginLanguage ${LANG_UZBEK} "Uzbek"
LangString PluginLanguage ${LANG_RUSSIAN} "Russian"
LangString PluginLanguage ${LANG_ENGLISH} "English"

LangString MsgNeed64Bit ${LANG_UZBEK} "$(^NameDA) faqat 64 bitli Windows da ishlaydi (AutoCAD 2024 kabi)."
LangString MsgNeed64Bit ${LANG_RUSSIAN} "$(^NameDA) работает только в 64-разрядной Windows (как и AutoCAD 2024)."
LangString MsgNeed64Bit ${LANG_ENGLISH} "$(^NameDA) requires 64-bit Windows (like AutoCAD 2024)."

LangString MsgNeedAdmin ${LANG_UZBEK} "O‘rnatish uchun administrator huquqlari kerak.$\r$\n$\r$\nO‘rnatish faylini sichqonchaning o‘ng tugmasi bilan bosing va «Запуск от имени администратора» (Run as administrator) ni tanlang."
LangString MsgNeedAdmin ${LANG_RUSSIAN} "Для установки нужны права администратора.$\r$\n$\r$\nЩёлкните файл установки правой кнопкой мыши и выберите «Запуск от имени администратора»."
LangString MsgNeedAdmin ${LANG_ENGLISH} "Administrator rights are required.$\r$\n$\r$\nRight-click the Setup file and choose $\"Run as administrator$\"."

LangString MsgNoAutoCAD ${LANG_UZBEK} "Bu kompyuterda AutoCAD 2024 topilmadi.$\r$\n$\r$\n$(^NameDA) faqat AutoCAD 2024 da ishlaydi. Baribir o‘rnatilsinmi?"
LangString MsgNoAutoCAD ${LANG_RUSSIAN} "AutoCAD 2024 на этом компьютере не найден.$\r$\n$\r$\n$(^NameDA) работает только в AutoCAD 2024. Всё равно установить?"
LangString MsgNoAutoCAD ${LANG_ENGLISH} "AutoCAD 2024 was not found on this computer.$\r$\n$\r$\n$(^NameDA) works only with AutoCAD 2024. Install anyway?"

LangString MsgAutoCADRunning ${LANG_UZBEK} "AutoCAD ishlab turibdi.$\r$\n$\r$\nChizmalarni saqlang, AutoCAD ni yoping va qayta urinib ko‘ring."
LangString MsgAutoCADRunning ${LANG_RUSSIAN} "AutoCAD запущен.$\r$\n$\r$\nСохраните чертежи, закройте AutoCAD и нажмите «Повторить»."
LangString MsgAutoCADRunning ${LANG_ENGLISH} "AutoCAD is running.$\r$\n$\r$\nSave your drawings, close AutoCAD and click Retry."

LangString DetAutoCADRunning ${LANG_UZBEK} "AutoCAD yopilmadi — jarayon to‘xtatildi."
LangString DetAutoCADRunning ${LANG_RUSSIAN} "AutoCAD не закрыт — операция прервана."
LangString DetAutoCADRunning ${LANG_ENGLISH} "AutoCAD was not closed — cancelled."

LangString DetOldMoved ${LANG_UZBEK} "Eski versiya zaxiraga ko‘chirildi: "
LangString DetOldMoved ${LANG_RUSSIAN} "Старая версия перемещена в резервную папку: "
LangString DetOldMoved ${LANG_ENGLISH} "Older version moved to the backup folder: "

LangString MsgOldFailed ${LANG_UZBEK} "Eski AutoPDF versiyasini ko‘chirib bo‘lmadi. Uni qo‘lda o‘chiring, aks holda AUTOPDF buyrug‘i ikki marta aniqlanadi:"
LangString MsgOldFailed ${LANG_RUSSIAN} "Не удалось переместить старую версию AutoPDF. Удалите её вручную, иначе команда AUTOPDF будет определена дважды:"
LangString MsgOldFailed ${LANG_ENGLISH} "An older AutoPDF version could not be moved. Please delete it manually, otherwise the AUTOPDF command is defined twice:"

LangString MsgPurge ${LANG_UZBEK} "AutoPDF Pro sozlamalari, jurnallari va eski versiyalar zaxirasi ham o‘chirilsinmi?"
LangString MsgPurge ${LANG_RUSSIAN} "Удалить также настройки, журналы и резервные копии старых версий AutoPDF Pro?"
LangString MsgPurge ${LANG_ENGLISH} "Also delete the AutoPDF Pro settings, logs and backups of older versions?"

;-------------------------------------------------------------------------------------------
; File properties of Setup.exe

VIProductVersion "${VERSION}.0"
VIFileVersion "${VERSION}.0"
VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "${PRODUCT}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductVersion" "${VERSION}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "${VERSION}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "${PRODUCT} ${VERSION} Setup (AutoCAD 2024 plug-in)"
VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "${AUTHOR}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "© ${COPYRIGHT_YEAR} ${AUTHOR}. All rights reserved."
VIAddVersionKey /LANG=${LANG_ENGLISH} "OriginalFilename" "AutoPDFPro-${VERSION}-Setup.exe"

;-------------------------------------------------------------------------------------------
; Helpers shared by Setup and the uninstaller.
;
; Calling convention: arguments are pushed in order, results are popped, and every register a
; helper uses is restored.

!macro SHARED_FUNCTIONS UN
; Push haystack, push needle, call, pop: 1 when the needle occurs in the haystack (case-insensitive).
Function ${UN}StrContains
  Exch $R1
  Exch
  Exch $R0
  Push $R2
  Push $R3
  Push $R4
  StrLen $R3 $R1
  StrCpy $R2 0
  ${Do}
    StrCpy $R4 $R0 $R3 $R2
    ${If} $R4 == ""
      StrCpy $R2 0
      ${ExitDo}
    ${EndIf}
    ${If} $R4 == $R1
      StrCpy $R2 1
      ${ExitDo}
    ${EndIf}
    IntOp $R2 $R2 + 1
  ${Loop}
  StrCpy $R0 $R2
  Pop $R4
  Pop $R3
  Pop $R2
  Exch
  Pop $R1
  Exch $R0
FunctionEnd

; Call, pop: 1 when AutoCAD is running (acad.exe: AutoCAD and every AutoCAD 2024 based product)
; or when the installed plug-in DLL is locked, else 0. AutoCAD locks the plug-in's DLLs.
Function ${UN}IsAutoCADRunning
  Push $0
  Push $1
  ; With the filter tasklist prints only acad.exe lines, or a (localized) "no tasks" message.
  nsExec::ExecToStack '"$SYSDIR\tasklist.exe" /NH /FO CSV /FI "IMAGENAME eq acad.exe"'
  Pop $0
  Pop $0
  Push $0
  Push "acad.exe"
  Call ${UN}StrContains
  Pop $1
  ${If} $1 = 0
  ${AndIf} ${FileExists} "$INSTDIR\Contents\Windows\2024\AutoPDF.AutoCAD.dll"
    ClearErrors
    FileOpen $0 "$INSTDIR\Contents\Windows\2024\AutoPDF.AutoCAD.dll" a
    ${If} ${Errors}
      StrCpy $1 1
    ${Else}
      FileClose $0
    ${EndIf}
  ${EndIf}
  StrCpy $0 $1
  Pop $1
  Exch $0
FunctionEnd

; Call, pop: "ok" once AutoCAD is closed, "cancel" when the user gives up.
Function ${UN}WaitForAutoCAD
  Push $0
  ${Do}
    Call ${UN}IsAutoCADRunning
    Pop $0
    ${If} $0 = 0
      StrCpy $0 "ok"
      ${ExitDo}
    ${EndIf}
    ${If} ${Cmd} `MessageBox MB_ICONEXCLAMATION|MB_RETRYCANCEL "$(MsgAutoCADRunning)" /SD IDCANCEL IDCANCEL`
      StrCpy $0 "cancel"
      ${ExitDo}
    ${EndIf}
  ${Loop}
  Exch $0
FunctionEnd
!macroend
!insertmacro SHARED_FUNCTIONS ""
!insertmacro SHARED_FUNCTIONS "un."

;-------------------------------------------------------------------------------------------
; Other AutoPDF bundles

; Push file, push text, call, pop: 1 when the file (ANSI/UTF-8 or UTF-16LE) contains the text.
Function FileContains
  Exch $R1
  Exch
  Exch $R0
  Push $R2
  Push $R3
  Push $R4
  Push $R5
  StrCpy $R5 0
  ClearErrors
  FileOpen $R2 $R0 r
  ${IfNot} ${Errors}
    StrCpy $R4 0
    FileReadByte $R2 $R3
    ${If} $R3 = 255
      FileReadByte $R2 $R3
      ${If} $R3 = 254
        StrCpy $R4 1
      ${EndIf}
    ${EndIf}
    ${If} $R4 = 0
      FileSeek $R2 0 SET
    ${EndIf}
    ${Do}
      ClearErrors
      ${If} $R4 = 1
        FileReadUTF16LE $R2 $R3
      ${Else}
        FileRead $R2 $R3
      ${EndIf}
      ${If} ${Errors}
        ${ExitDo}
      ${EndIf}
      Push $R3
      Push $R1
      Call StrContains
      Pop $R5
    ${LoopUntil} $R5 = 1
    FileClose $R2
  ${EndIf}
  StrCpy $R0 $R5
  Pop $R5
  Pop $R4
  Pop $R3
  Pop $R2
  Exch
  Pop $R1
  Exch $R0
FunctionEnd

; Push a bundle folder, push a backup folder, call: moves the bundle into the backup folder.
Function MoveToBackup
  Exch $R1
  Exch
  Exch $R0
  Push $R2
  ${GetFileName} $R0 $R2
  CreateDirectory $R1
  ClearErrors
  Rename $R0 "$R1\$R2"
  ${If} ${Errors}
    ; another drive or a locked file: copy, then delete
    ClearErrors
    CopyFiles /SILENT $R0 $R1
    ${IfNot} ${Errors}
      RMDir /r $R0
    ${EndIf}
  ${EndIf}
  ${If} ${FileExists} "$R0\PackageContents.xml"
    DetailPrint "$(MsgOldFailed) $R0"
    MessageBox MB_ICONEXCLAMATION|MB_OK "$(MsgOldFailed)$\r$\n$\r$\n$R0" /SD IDOK
  ${Else}
    DetailPrint "$(DetOldMoved)$R0 -> $R1"
  ${EndIf}
  Pop $R2
  Pop $R0
  Pop $R1
FunctionEnd

; Push an ApplicationPlugins folder, push a name for its backup subfolder, call: every AutoPDF bundle
; in the folder, except this installation's own bundle (updated in place), is moved to
; $BackupDir\<name>, so AutoCAD never loads two AUTOPDF commands.
Function BackupOtherBundles
  Exch $R1
  Exch
  Exch $R0
  Push $R2
  Push $R3
  Push $R4
  Push $R5
  ${If} ${FileExists} "$R0\*.bundle"
    FindFirst $R2 $R3 "$R0\*.bundle"
    ${DoWhile} $R3 != ""
      StrCpy $R4 "$R0\$R3"
      ${If} ${FileExists} "$R4\PackageContents.xml"
        Push "$R4\PackageContents.xml"
        Push "AutoPDF"
        Call FileContains
        Pop $R5
        ${If} $R5 = 1
          StrCpy $R5 0
          ${If} $R4 == $INSTDIR
            Push "$R4\PackageContents.xml"
            Push "${UPGRADE_CODE}"
            Call FileContains
            Pop $R5
          ${EndIf}
          ${If} $R5 = 0
            Push $R4
            Push "$BackupDir\$R1"
            Call MoveToBackup
          ${EndIf}
        ${EndIf}
      ${EndIf}
      FindNext $R2 $R3
    ${Loop}
    FindClose $R2
  ${EndIf}
  Pop $R5
  Pop $R4
  Pop $R3
  Pop $R2
  Pop $R0
  Pop $R1
FunctionEnd

;-------------------------------------------------------------------------------------------
; Installer

Function .onInit
  SetRegView 64

  ; The dialog preselects O‘zbekcha, or the language of the previous installation.
  StrCpy $LANGUAGE ${LANG_UZBEK}
  !insertmacro MUI_LANGDLL_DISPLAY

  ; A silent installation has no GUI, so its checks run here (their messages are not shown).
  ${If} ${Silent}
    Call StartupChecks
  ${EndIf}
FunctionEnd

; Language strings follow the chosen language only after .onInit has returned, therefore the
; interactive checks run when the GUI starts.
Function GuiInit
  Call StartupChecks
FunctionEnd

Function StartupChecks
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP|MB_OK "$(MsgNeed64Bit)" /SD IDOK
    SetErrorLevel 3
    Quit
  ${EndIf}

  UserInfo::GetAccountType
  Pop $0
  ${If} $0 != "Admin"
    MessageBox MB_ICONSTOP|MB_OK "$(MsgNeedAdmin)" /SD IDOK
    SetErrorLevel 740
    Quit
  ${EndIf}

  ; AutoCAD 2024 is release R24.3; AutoCAD and its 2024 verticals register below this key.
  ClearErrors
  EnumRegKey $0 HKLM "SOFTWARE\Autodesk\AutoCAD\R24.3" 0
  ${If} ${Errors}
  ${OrIf} $0 == ""
    ${If} ${Cmd} `MessageBox MB_ICONEXCLAMATION|MB_YESNO|MB_DEFBUTTON2 "$(MsgNoAutoCAD)" /SD IDYES IDNO`
      SetErrorLevel 1
      Quit
    ${EndIf}
  ${EndIf}
FunctionEnd

Function WelcomeLeave
  Call WaitForAutoCAD
  Pop $0
  ${If} $0 != "ok"
    Abort
  ${EndIf}
FunctionEnd

Section "AutoPDF Pro" SecMain
  SectionIn RO

  Call WaitForAutoCAD
  Pop $0
  ${If} $0 != "ok"
    DetailPrint "$(DetAutoCADRunning)"
    Abort
  ${EndIf}

  ; Other AutoPDF bundles -> %LOCALAPPDATA%\AutoPDFPro\Backup\<date-time>
  SetShellVarContext current
  ${GetTime} "" "L" $0 $1 $2 $3 $4 $5 $6
  StrCpy $BackupDir "$LOCALAPPDATA\AutoPDFPro\Backup\$2$1$0-$4$5$6"
  Push "$PROGRAMFILES64\Autodesk\ApplicationPlugins"
  Push "ProgramFiles"
  Call BackupOtherBundles
  SetShellVarContext all
  Push "$APPDATA\Autodesk\ApplicationPlugins"
  SetShellVarContext current
  Push "ProgramData"
  Call BackupOtherBundles
  Push "$APPDATA\Autodesk\ApplicationPlugins"
  Push "AppData"
  Call BackupOtherBundles

  ; Files of an earlier installation of this bundle
  RMDir /r "$INSTDIR\Contents"

  SetOutPath "$INSTDIR"
  File "${BUNDLE_DIR}\PackageContents.xml"
  SetOutPath "$INSTDIR\Contents\Windows\2024"
  File "${BUNDLE_DIR}\Contents\Windows\2024\AutoPDF.AutoCAD.dll"
  File "${BUNDLE_DIR}\Contents\Windows\2024\AutoPDF.Core.dll"
  File "${BUNDLE_DIR}\Contents\Windows\2024\AutoPDF.UI.dll"
  File "${BUNDLE_DIR}\Contents\Windows\2024\PdfSharp.dll"
  SetOutPath "$INSTDIR"

  WriteUninstaller "$INSTDIR\Uninstall.exe"

  WriteRegStr HKLM "${REG_KEY}" "Version" "${VERSION}"
  WriteRegStr HKLM "${REG_KEY}" "InstallDir" "$INSTDIR"

  WriteRegStr HKLM "${UNINST_KEY}" "DisplayName" "${PRODUCT} (AutoCAD 2024)"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "${UNINST_KEY}" "Publisher" "${AUTHOR}"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayIcon" "$INSTDIR\Uninstall.exe,0"
  WriteRegStr HKLM "${UNINST_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKLM "${UNINST_KEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegStr HKLM "${UNINST_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINST_KEY}" "Comments" "AUTOPDF: AutoCAD 2024 sheets -> PDF"
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoRepair" 1
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKLM "${UNINST_KEY}" "EstimatedSize" "$0"

  ; First installation: the plug-in starts in the language chosen for Setup.
  ; An existing settings file (the user's own choices) is never touched.
  SetShellVarContext current
  ${IfNot} ${FileExists} "$APPDATA\AutoPDFPro\settings.xml"
    CreateDirectory "$APPDATA\AutoPDFPro"
    ClearErrors
    FileOpen $0 "$APPDATA\AutoPDFPro\settings.xml" w
    ${IfNot} ${Errors}
      FileWrite $0 '<?xml version="1.0" encoding="utf-8"?>$\r$\n<AutoPdfSettings>$\r$\n  <Language>$(PluginLanguage)</Language>$\r$\n</AutoPdfSettings>$\r$\n'
      FileClose $0
    ${EndIf}
  ${EndIf}
SectionEnd

;-------------------------------------------------------------------------------------------
; Uninstaller

Function un.onInit
  SetRegView 64
  !insertmacro MUI_UNGETLANGUAGE
FunctionEnd

Section "Uninstall"
  Call un.WaitForAutoCAD
  Pop $0
  ${If} $0 != "ok"
    DetailPrint "$(DetAutoCADRunning)"
    Abort
  ${EndIf}

  ; Only this bundle's own files; the folder itself goes when it is empty.
  Delete "$INSTDIR\Contents\Windows\2024\AutoPDF.AutoCAD.dll"
  Delete "$INSTDIR\Contents\Windows\2024\AutoPDF.Core.dll"
  Delete "$INSTDIR\Contents\Windows\2024\AutoPDF.UI.dll"
  Delete "$INSTDIR\Contents\Windows\2024\PdfSharp.dll"
  RMDir "$INSTDIR\Contents\Windows\2024"
  RMDir "$INSTDIR\Contents\Windows"
  RMDir "$INSTDIR\Contents"
  Delete "$INSTDIR\PackageContents.xml"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"

  DeleteRegKey HKLM "${UNINST_KEY}"
  DeleteRegKey HKLM "${REG_KEY}"

  SetShellVarContext current
  ${If} ${FileExists} "$APPDATA\AutoPDFPro\*.*"
  ${OrIf} ${FileExists} "$LOCALAPPDATA\AutoPDFPro\*.*"
    ${If} ${Cmd} `MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "$(MsgPurge)" /SD IDNO IDYES`
      RMDir /r "$APPDATA\AutoPDFPro"
      RMDir /r "$LOCALAPPDATA\AutoPDFPro"
    ${EndIf}
  ${EndIf}
SectionEnd
