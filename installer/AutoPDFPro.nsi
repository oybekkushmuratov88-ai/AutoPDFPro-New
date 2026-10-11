;===========================================================================================
; AutoPDF Pro — single-file installer (NSIS 3, https://nsis.sourceforge.io)
;
; Build the plug-ins first (they stage artifacts\AutoPDFPro.bundle and artifacts\ZWCAD\<year>), then
; compile this script:
;   Windows:  "C:\Program Files (x86)\NSIS\makensis.exe" installer\AutoPDFPro.nsi
;   Linux:    makensis installer/AutoPDFPro.nsi
; build\build.ps1 and build/build.sh do both. Output: dist\AutoPDFPro-<version>-Setup.exe
;
; One Setup, every supported CAD application — each gets the assembly built for it:
;   AutoCAD 2021–2024 (R24, .NET Framework 4.8) and 2025–2026 (R25, .NET 8)
;       → %ProgramFiles%\Autodesk\ApplicationPlugins\AutoPDFPro.bundle; PackageContents.xml has one
;         component per release family, so AutoCAD's autoloader loads only the matching build.
;   ZWCAD 2024 / 2025 / 2026 (.NET Framework; ZWCAD does not read Autodesk bundles)
;       → %ProgramFiles%\AutoPDF Pro\ZWCAD\<year>\AutoPDF.ZWCAD.<year>.dll, registered for loading at
;         start-up under HKLM and HKCU\SOFTWARE\ZWSOFT\ZWCAD\<year>\<language>\Applications\AutoPDFPro
;         (LOADER, LOADCTRLS=2, MANAGED=1).
; The components page preselects the applications found on this computer.
;
; Also: language dialog (O‘zbekcha / Русский / English, becomes the plug-in language on a first
; install); 64-bit Windows and administrator rights required; waits until AutoCAD/ZWCAD are closed;
; moves other AutoPDF bundles to %LOCALAPPDATA%\AutoPDFPro\Backup\<date-time> so that AUTOPDF is defined
; once; registers an uninstaller under "Apps". Registration and the administrator's approval happen on
; the first AUTOPDF (not in Setup).
;
; Silent install:  AutoPDFPro-<version>-Setup.exe /S     Silent uninstall:  Uninstall.exe /S
;===========================================================================================

Unicode true
ManifestDPIAware true
SetCompressor /SOLID lzma
RequestExecutionLevel admin

!ifndef VERSION
  !define VERSION "3.0.1"
!endif
!define PRODUCT "AutoPDF Pro"
!define AUTHOR "Kushmuratov Oybek"
!define COPYRIGHT_YEAR "2026"
!define BUNDLE_NAME "AutoPDFPro.bundle"
!define BUNDLE_PATH "$PROGRAMFILES64\Autodesk\ApplicationPlugins\${BUNDLE_NAME}"
; UpgradeCode of bundle\AutoPDFPro.bundle\PackageContents.xml: identifies this product's bundle
!define UPGRADE_CODE "1199BA7E-5919-42A7-A870-A8B38B9554FC"
!define REG_KEY "Software\AutoPDFPro"
!define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoPDFPro"
!define ZW_ROOT "SOFTWARE\ZWSOFT\ZWCAD"
!define ZW_APP "AutoPDFPro"
!ifndef ARTIFACTS
  !define ARTIFACTS "..\artifacts"
!endif
!ifndef OUTFILE
  !define OUTFILE "..\dist\AutoPDFPro-${VERSION}-Setup.exe"
!endif

Name "${PRODUCT} ${VERSION}"
OutFile "${OUTFILE}"
InstallDir "$PROGRAMFILES64\AutoPDF Pro"
BrandingText "${PRODUCT} ${VERSION} · ${AUTHOR}"
ShowInstDetails hide
ShowUninstDetails hide

!include MUI2.nsh
!include LogicLib.nsh
!include FileFunc.nsh
!include Sections.nsh
!include x64.nsh

Var BackupDir
Var FoundCad

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

; The language of the last installation is reused by the uninstaller and preselected by an update.
!define MUI_LANGDLL_REGISTRY_ROOT "HKLM"
!define MUI_LANGDLL_REGISTRY_KEY "${REG_KEY}"
!define MUI_LANGDLL_REGISTRY_VALUENAME "InstallerLanguage"
!define MUI_LANGDLL_ALLLANGUAGES
!define MUI_LANGDLL_WINDOWTITLE "${PRODUCT} ${VERSION}"
!define MUI_LANGDLL_INFO "Tilni tanlang · Выберите язык · Select a language"
; Always ask for the language, even on an update.
!define MUI_LANGDLL_ALWAYSSHOW

!define MUI_WELCOMEPAGE_TEXT "$(WelcomeText)"
!define MUI_PAGE_CUSTOMFUNCTION_LEAVE WelcomeLeave
!insertmacro MUI_PAGE_WELCOME
!define MUI_PAGE_HEADER_TEXT "$(CompTitle)"
!define MUI_PAGE_HEADER_SUBTEXT "$(CompSubtitle)"
!define MUI_COMPONENTSPAGE_TEXT_TOP "$(CompTop)"
!insertmacro MUI_PAGE_COMPONENTS
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

LangString WelcomeText ${LANG_UZBEK} "Ushbu dastur kompyuteringizga $(^NameDA) plaginini o‘rnatadi. Plagin chizmadagi listlarni (ramkalarni) avtomatik topadi va har birini alohida PDF faylga saqlaydi.$\r$\n$\r$\nQo‘llab-quvvatlanadi: AutoCAD 2021–2026, ZWCAD 2024–2026.$\r$\n$\r$\nDavom etishdan oldin AutoCAD va ZWCAD ni yoping.$\r$\n$\r$\nMuallif: ${AUTHOR}$\r$\n$\r$\n$_CLICK"
LangString WelcomeText ${LANG_RUSSIAN} "Программа установит плагин $(^NameDA). Плагин автоматически находит листы (рамки) в чертеже и сохраняет каждый в отдельный PDF-файл.$\r$\n$\r$\nПоддерживаются: AutoCAD 2021–2026, ZWCAD 2024–2026.$\r$\n$\r$\nПеред продолжением закройте AutoCAD и ZWCAD.$\r$\n$\r$\nАвтор: ${AUTHOR}$\r$\n$\r$\n$_CLICK"
LangString WelcomeText ${LANG_ENGLISH} "Setup will install the $(^NameDA) plug-in. It finds the sheets (frames) of a drawing automatically and saves each one as a separate PDF file.$\r$\n$\r$\nSupported: AutoCAD 2021–2026, ZWCAD 2024–2026.$\r$\n$\r$\nPlease close AutoCAD and ZWCAD before you continue.$\r$\n$\r$\nAuthor: ${AUTHOR}$\r$\n$\r$\n$_CLICK"

LangString CompTitle ${LANG_UZBEK} "Dasturlarni tanlang"
LangString CompTitle ${LANG_RUSSIAN} "Выбор программ"
LangString CompTitle ${LANG_ENGLISH} "Choose the applications"
LangString CompSubtitle ${LANG_UZBEK} "Plagin qaysi CAD dasturlariga o‘rnatilsin?"
LangString CompSubtitle ${LANG_RUSSIAN} "В какие CAD-программы установить плагин?"
LangString CompSubtitle ${LANG_ENGLISH} "Which CAD applications should get the plug-in?"
LangString CompTop ${LANG_UZBEK} "Bu kompyuterda topilgan dasturlar belgilangan. Har bir dastur o‘ziga mos modulni oladi."
LangString CompTop ${LANG_RUSSIAN} "Отмечены программы, найденные на этом компьютере. Каждая получит свой модуль."
LangString CompTop ${LANG_ENGLISH} "The applications found on this computer are selected. Each gets the module built for it."

LangString DescAcad ${LANG_UZBEK} "AutoCAD 2021, 2022, 2023, 2024 (.NET Framework) va 2025, 2026 (.NET 8) — avtomatik yuklanadi."
LangString DescAcad ${LANG_RUSSIAN} "AutoCAD 2021, 2022, 2023, 2024 (.NET Framework) и 2025, 2026 (.NET 8) — загружается автоматически."
LangString DescAcad ${LANG_ENGLISH} "AutoCAD 2021, 2022, 2023, 2024 (.NET Framework) and 2025, 2026 (.NET 8) — loaded automatically."
LangString DescZw ${LANG_UZBEK} "ZWCAD uchun alohida modul; ZWCAD ishga tushganda yuklanadi."
LangString DescZw ${LANG_RUSSIAN} "Отдельный модуль для ZWCAD; загружается при запуске ZWCAD."
LangString DescZw ${LANG_ENGLISH} "A separate module for ZWCAD; loaded when ZWCAD starts."

LangString FinishText ${LANG_UZBEK} "$(^NameDA) o‘rnatildi.$\r$\n$\r$\nQanday foydalaniladi:$\r$\n   1. AutoCAD yoki ZWCAD ni ishga tushiring.$\r$\n   2. AUTOPDF buyrug‘ini kiriting (Enter).$\r$\n   3. Birinchi marta ism va e-pochtangizni kiriting — so‘rov administratorga boradi. Ruxsat berilgach (xat keladi) PDF yaratish ishlaydi.$\r$\n$\r$\nLitsenziya holati: AUTOPDFLICENSE buyrug‘i."
LangString FinishText ${LANG_RUSSIAN} "$(^NameDA) установлен.$\r$\n$\r$\nКак пользоваться:$\r$\n   1. Запустите AutoCAD или ZWCAD.$\r$\n   2. Введите команду AUTOPDF (Enter).$\r$\n   3. При первом запуске укажите имя и e-mail — запрос уйдёт администратору. После одобрения (придёт письмо) создание PDF заработает.$\r$\n$\r$\nСтатус лицензии: команда AUTOPDFLICENSE."
LangString FinishText ${LANG_ENGLISH} "$(^NameDA) has been installed.$\r$\n$\r$\nHow to use it:$\r$\n   1. Start AutoCAD or ZWCAD.$\r$\n   2. Type the command AUTOPDF (Enter).$\r$\n   3. The first time, enter your name and e-mail — the request goes to the administrator. Once approved (you get an e-mail), PDF creation works.$\r$\n$\r$\nLicence status: the AUTOPDFLICENSE command."

; PluginSettings.Language values (src\AutoPDF.Core\Settings\PluginSettings.cs)
LangString PluginLanguage ${LANG_UZBEK} "Uzbek"
LangString PluginLanguage ${LANG_RUSSIAN} "Russian"
LangString PluginLanguage ${LANG_ENGLISH} "English"

LangString MsgNeed64Bit ${LANG_UZBEK} "$(^NameDA) faqat 64 bitli Windows da ishlaydi."
LangString MsgNeed64Bit ${LANG_RUSSIAN} "$(^NameDA) работает только в 64-разрядной Windows."
LangString MsgNeed64Bit ${LANG_ENGLISH} "$(^NameDA) requires 64-bit Windows."

LangString MsgNeedAdmin ${LANG_UZBEK} "O‘rnatish uchun administrator huquqlari kerak.$\r$\n$\r$\nO‘rnatish faylini sichqonchaning o‘ng tugmasi bilan bosing va «Запуск от имени администратора» (Run as administrator) ni tanlang."
LangString MsgNeedAdmin ${LANG_RUSSIAN} "Для установки нужны права администратора.$\r$\n$\r$\nЩёлкните файл установки правой кнопкой мыши и выберите «Запуск от имени администратора»."
LangString MsgNeedAdmin ${LANG_ENGLISH} "Administrator rights are required.$\r$\n$\r$\nRight-click the Setup file and choose $\"Run as administrator$\"."

LangString MsgNoCad ${LANG_UZBEK} "Bu kompyuterda AutoCAD 2021–2026 yoki ZWCAD 2024–2026 topilmadi.$\r$\n$\r$\nBaribir o‘rnatilsinmi? (Keyingi oynada dasturlarni o‘zingiz tanlaysiz.)"
LangString MsgNoCad ${LANG_RUSSIAN} "На этом компьютере не найдены AutoCAD 2021–2026 или ZWCAD 2024–2026.$\r$\n$\r$\nВсё равно установить? (Программы можно выбрать на следующей странице.)"
LangString MsgNoCad ${LANG_ENGLISH} "Neither AutoCAD 2021–2026 nor ZWCAD 2024–2026 was found on this computer.$\r$\n$\r$\nInstall anyway? (You choose the applications on the next page.)"

LangString MsgCadRunning ${LANG_UZBEK} "AutoCAD yoki ZWCAD ishlab turibdi.$\r$\n$\r$\nChizmalarni saqlang, dasturni yoping va qayta urinib ko‘ring."
LangString MsgCadRunning ${LANG_RUSSIAN} "AutoCAD или ZWCAD запущен.$\r$\n$\r$\nСохраните чертежи, закройте программу и нажмите «Повторить»."
LangString MsgCadRunning ${LANG_ENGLISH} "AutoCAD or ZWCAD is running.$\r$\n$\r$\nSave your drawings, close the application and click Retry."

LangString DetCadRunning ${LANG_UZBEK} "AutoCAD/ZWCAD yopilmadi — jarayon to‘xtatildi."
LangString DetCadRunning ${LANG_RUSSIAN} "AutoCAD/ZWCAD не закрыт — операция прервана."
LangString DetCadRunning ${LANG_ENGLISH} "AutoCAD/ZWCAD was not closed — cancelled."

LangString DetOldMoved ${LANG_UZBEK} "Eski versiya zaxiraga ko‘chirildi: "
LangString DetOldMoved ${LANG_RUSSIAN} "Старая версия перемещена в резервную папку: "
LangString DetOldMoved ${LANG_ENGLISH} "Older version moved to the backup folder: "

LangString DetZwRegistered ${LANG_UZBEK} "ZWCAD uchun ro‘yxatga olindi: "
LangString DetZwRegistered ${LANG_RUSSIAN} "Зарегистрировано для ZWCAD: "
LangString DetZwRegistered ${LANG_ENGLISH} "Registered for ZWCAD: "

LangString MsgOldFailed ${LANG_UZBEK} "Eski AutoPDF versiyasini ko‘chirib bo‘lmadi. Uni qo‘lda o‘chiring, aks holda AUTOPDF buyrug‘i ikki marta aniqlanadi:"
LangString MsgOldFailed ${LANG_RUSSIAN} "Не удалось переместить старую версию AutoPDF. Удалите её вручную, иначе команда AUTOPDF будет определена дважды:"
LangString MsgOldFailed ${LANG_ENGLISH} "An older AutoPDF version could not be moved. Please delete it manually, otherwise the AUTOPDF command is defined twice:"

LangString MsgPurge ${LANG_UZBEK} "AutoPDF Pro sozlamalari, jurnallari, litsenziya holati va eski versiyalar zaxirasi ham o‘chirilsinmi?$\r$\n$\r$\n(Litsenziya o‘chirilsa, qayta o‘rnatgandan keyin yana administrator ruxsati kerak bo‘ladi.)"
LangString MsgPurge ${LANG_RUSSIAN} "Удалить также настройки, журналы, состояние лицензии и резервные копии старых версий AutoPDF Pro?$\r$\n$\r$\n(Без сохранённой лицензии после переустановки снова потребуется одобрение администратора.)"
LangString MsgPurge ${LANG_ENGLISH} "Also delete the AutoPDF Pro settings, logs, licence state and backups of older versions?$\r$\n$\r$\n(Without the stored licence, a reinstall needs the administrator's approval again.)"

;-------------------------------------------------------------------------------------------
; File properties of Setup.exe

VIProductVersion "${VERSION}.0"
VIFileVersion "${VERSION}.0"
VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductName" "${PRODUCT}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "ProductVersion" "${VERSION}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileVersion" "${VERSION}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "FileDescription" "${PRODUCT} ${VERSION} Setup (AutoCAD 2021-2026, ZWCAD 2024-2026)"
VIAddVersionKey /LANG=${LANG_ENGLISH} "CompanyName" "${AUTHOR}"
VIAddVersionKey /LANG=${LANG_ENGLISH} "LegalCopyright" "© ${COPYRIGHT_YEAR} ${AUTHOR}. All rights reserved."
VIAddVersionKey /LANG=${LANG_ENGLISH} "OriginalFilename" "AutoPDFPro-${VERSION}-Setup.exe"

;-------------------------------------------------------------------------------------------
; Helpers shared by Setup and the uninstaller.
;
; Calling convention: arguments are pushed in order, results are popped, and every register a
; helper uses is restored.

!macro LOCK_CHECK UN FILE
  Push "${FILE}"
  Call ${UN}IsFileLocked
  Pop $0
  IntOp $1 $1 | $0
!macroend

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

; Push an image name, call, pop: 1 when a process with that image name runs.
Function ${UN}IsProcessRunning
  Exch $R0
  Push $R1
  ; With the filter tasklist prints only matching lines, or a (localized) "no tasks" message.
  nsExec::ExecToStack '"$SYSDIR\tasklist.exe" /NH /FO CSV /FI "IMAGENAME eq $R0"'
  Pop $R1
  Pop $R1
  Push $R1
  Push $R0
  Call ${UN}StrContains
  Pop $R0
  Pop $R1
  Exch $R0
FunctionEnd

; Push a file, call, pop: 1 when the file exists and cannot be opened for writing (a CAD has loaded it).
Function ${UN}IsFileLocked
  Exch $R0
  Push $R1
  ${If} ${FileExists} $R0
    ClearErrors
    FileOpen $R1 $R0 a
    ${If} ${Errors}
      StrCpy $R0 1
    ${Else}
      FileClose $R1
      StrCpy $R0 0
    ${EndIf}
  ${Else}
    StrCpy $R0 0
  ${EndIf}
  Pop $R1
  Exch $R0
FunctionEnd

; Call, pop: 1 when AutoCAD (acad.exe, also its verticals) or ZWCAD runs, or one of our DLLs is locked.
Function ${UN}IsCadRunning
  Push $0
  Push $1
  StrCpy $1 0
  Push "acad.exe"
  Call ${UN}IsProcessRunning
  Pop $0
  IntOp $1 $1 | $0
  Push "ZWCAD.exe"
  Call ${UN}IsProcessRunning
  Pop $0
  IntOp $1 $1 | $0
  !insertmacro LOCK_CHECK "${UN}" "${BUNDLE_PATH}\Contents\Windows\R24\AutoPDF.AutoCAD.dll"
  !insertmacro LOCK_CHECK "${UN}" "${BUNDLE_PATH}\Contents\Windows\R25\AutoPDF.AutoCAD.R25.dll"
  !insertmacro LOCK_CHECK "${UN}" "${BUNDLE_PATH}\Contents\Windows\2024\AutoPDF.AutoCAD.dll"
  !insertmacro LOCK_CHECK "${UN}" "$INSTDIR\ZWCAD\2024\AutoPDF.ZWCAD.2024.dll"
  !insertmacro LOCK_CHECK "${UN}" "$INSTDIR\ZWCAD\2025\AutoPDF.ZWCAD.2025.dll"
  !insertmacro LOCK_CHECK "${UN}" "$INSTDIR\ZWCAD\2026\AutoPDF.ZWCAD.2026.dll"
  StrCpy $0 $1
  Pop $1
  Exch $0
FunctionEnd

; Call, pop: "ok" once AutoCAD/ZWCAD are closed, "cancel" when the user gives up.
Function ${UN}WaitForCad
  Push $0
  ${Do}
    Call ${UN}IsCadRunning
    Pop $0
    ${If} $0 = 0
      StrCpy $0 "ok"
      ${ExitDo}
    ${EndIf}
    ${If} ${Cmd} `MessageBox MB_ICONEXCLAMATION|MB_RETRYCANCEL "$(MsgCadRunning)" /SD IDCANCEL IDCANCEL`
      StrCpy $0 "cancel"
      ${ExitDo}
    ${EndIf}
  ${Loop}
  Exch $0
FunctionEnd

; Push a ZWCAD year, call: removes our start-up registration from every language profile (HKLM + HKCU).
Function ${UN}UnregisterZwcad
  Exch $R0
  Push $R1
  Push $R2
  StrCpy $R1 0
  ${Do}
    ClearErrors
    EnumRegKey $R2 HKLM "${ZW_ROOT}\$R0" $R1
    ${If} ${Errors}
    ${OrIf} $R2 == ""
      ${ExitDo}
    ${EndIf}
    DeleteRegKey HKLM "${ZW_ROOT}\$R0\$R2\Applications\${ZW_APP}"
    DeleteRegKey /ifempty HKLM "${ZW_ROOT}\$R0\$R2\Applications"
    IntOp $R1 $R1 + 1
  ${Loop}
  StrCpy $R1 0
  ${Do}
    ClearErrors
    EnumRegKey $R2 HKCU "${ZW_ROOT}\$R0" $R1
    ${If} ${Errors}
    ${OrIf} $R2 == ""
      ${ExitDo}
    ${EndIf}
    DeleteRegKey HKCU "${ZW_ROOT}\$R0\$R2\Applications\${ZW_APP}"
    DeleteRegKey /ifempty HKCU "${ZW_ROOT}\$R0\$R2\Applications"
    IntOp $R1 $R1 + 1
  ${Loop}
  Pop $R2
  Pop $R1
  Pop $R0
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
; in the folder, except this product's own bundle (replaced in place), is moved to $BackupDir\<name>,
; so AutoCAD never loads two AUTOPDF commands.
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
          ${If} $R4 == "${BUNDLE_PATH}"
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
; Host detection and ZWCAD registration

; Push a registry path below HKLM, call, pop: 1 when it exists and has at least one subkey.
Function HasKey
  Exch $R0
  Push $R1
  ClearErrors
  EnumRegKey $R1 HKLM $R0 0
  ${If} ${Errors}
  ${OrIf} $R1 == ""
    StrCpy $R0 0
  ${Else}
    StrCpy $R0 1
  ${EndIf}
  Pop $R1
  Exch $R0
FunctionEnd

!macro ZW_WRITE ROOT KEY
  WriteRegStr ${ROOT} "${KEY}" "DESCRIPTION" "${PRODUCT} ${VERSION} (AUTOPDF)"
  WriteRegStr ${ROOT} "${KEY}" "LOADER" "$R1"
  WriteRegDWORD ${ROOT} "${KEY}" "LOADCTRLS" 2
  WriteRegDWORD ${ROOT} "${KEY}" "MANAGED" 1
!macroend

; Push a ZWCAD year, push the DLL path, call: registers the DLL for loading at start-up in every
; language profile of that ZWCAD (HKLM for all users, HKCU for the current one).
Function RegisterZwcad
  Exch $R1
  Exch
  Exch $R0
  Push $R2
  Push $R3
  StrCpy $R2 0
  ${Do}
    ClearErrors
    EnumRegKey $R3 HKLM "${ZW_ROOT}\$R0" $R2
    ${If} ${Errors}
    ${OrIf} $R3 == ""
      ${ExitDo}
    ${EndIf}
    !insertmacro ZW_WRITE HKLM "${ZW_ROOT}\$R0\$R3\Applications\${ZW_APP}"
    !insertmacro ZW_WRITE HKCU "${ZW_ROOT}\$R0\$R3\Applications\${ZW_APP}"
    DetailPrint "$(DetZwRegistered)ZWCAD $R0 ($R3)"
    IntOp $R2 $R2 + 1
  ${Loop}
  Pop $R3
  Pop $R2
  Pop $R0
  Pop $R1
FunctionEnd

!macro ACAD_RELEASE RELEASE
  Push "SOFTWARE\Autodesk\AutoCAD\${RELEASE}"
  Call HasKey
  Pop $0
  IntOp $1 $1 | $0
!macroend

!macro ZW_DETECT YEAR SECTION
  Push "${ZW_ROOT}\${YEAR}"
  Call HasKey
  Pop $0
  ${If} $0 = 0
    !insertmacro UnselectSection ${SECTION}
  ${Else}
    StrCpy $FoundCad 1
  ${EndIf}
!macroend

;-------------------------------------------------------------------------------------------
; Installer sections

Section "-Prepare" SecPrepare
  Call WaitForCad
  Pop $0
  ${If} $0 != "ok"
    DetailPrint "$(DetCadRunning)"
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

  ; This product's earlier files (2.x kept its uninstaller inside the bundle) and ZWCAD registrations.
  RMDir /r "${BUNDLE_PATH}"
  RMDir /r "$INSTDIR\ZWCAD"
  Push "2024"
  Call UnregisterZwcad
  Push "2025"
  Call UnregisterZwcad
  Push "2026"
  Call UnregisterZwcad
SectionEnd

Section "AutoCAD 2021–2026" SecAcad
  SetOutPath "${BUNDLE_PATH}"
  File "${ARTIFACTS}\${BUNDLE_NAME}\PackageContents.xml"
  SetOutPath "${BUNDLE_PATH}\Contents\Windows\R24"
  File "${ARTIFACTS}\${BUNDLE_NAME}\Contents\Windows\R24\*.dll"
  SetOutPath "${BUNDLE_PATH}\Contents\Windows\R25"
  File "${ARTIFACTS}\${BUNDLE_NAME}\Contents\Windows\R25\*.dll"
SectionEnd

!macro ZW_SECTION YEAR SECTION_ID
Section "ZWCAD ${YEAR}" ${SECTION_ID}
  SetOutPath "$INSTDIR\ZWCAD\${YEAR}"
  File "${ARTIFACTS}\ZWCAD\${YEAR}\*.dll"
  Push "${YEAR}"
  Push "$INSTDIR\ZWCAD\${YEAR}\AutoPDF.ZWCAD.${YEAR}.dll"
  Call RegisterZwcad
SectionEnd
!macroend
!insertmacro ZW_SECTION "2024" SecZw2024
!insertmacro ZW_SECTION "2025" SecZw2025
!insertmacro ZW_SECTION "2026" SecZw2026

Section "-Finish" SecFinish
  SetOutPath "$INSTDIR"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  WriteRegStr HKLM "${REG_KEY}" "Version" "${VERSION}"
  WriteRegStr HKLM "${REG_KEY}" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${REG_KEY}" "BundleDir" "${BUNDLE_PATH}"
  ; 2.x stored the installer-time registration here; registration now lives in the plug-in.
  DeleteRegValue HKLM "${REG_KEY}" "Registered"
  DeleteRegValue HKLM "${REG_KEY}" "RegName"
  DeleteRegValue HKLM "${REG_KEY}" "RegEmail"
  DeleteRegValue HKLM "${REG_KEY}" "RegPhone"
  DeleteRegValue HKLM "${REG_KEY}" "RegDelivered"
  DeleteRegValue HKLM "${REG_KEY}" "RegAtUtc"

  WriteRegStr HKLM "${UNINST_KEY}" "DisplayName" "${PRODUCT} (AutoCAD, ZWCAD)"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "${UNINST_KEY}" "Publisher" "${AUTHOR}"
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayIcon" "$INSTDIR\Uninstall.exe,0"
  WriteRegStr HKLM "${UNINST_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKLM "${UNINST_KEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegStr HKLM "${UNINST_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKLM "${UNINST_KEY}" "Comments" "AUTOPDF: sheets -> PDF for AutoCAD 2021-2026 and ZWCAD 2024-2026"
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoRepair" 1
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  StrCpy $3 0
  ${If} ${FileExists} "${BUNDLE_PATH}\*.*"
    ${GetSize} "${BUNDLE_PATH}" "/S=0K" $3 $1 $2
  ${EndIf}
  IntOp $0 $0 + $3
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

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecAcad} "$(DescAcad)"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecZw2024} "$(DescZw)"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecZw2025} "$(DescZw)"
  !insertmacro MUI_DESCRIPTION_TEXT ${SecZw2026} "$(DescZw)"
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;-------------------------------------------------------------------------------------------
; Installer functions (after the sections, which they reference)

Function .onInit
  SetRegView 64

  ; The dialog preselects O‘zbekcha, or the language of the previous installation.
  StrCpy $LANGUAGE ${LANG_UZBEK}
  !insertmacro MUI_LANGDLL_DISPLAY

  Call SelectDetectedHosts

  ; A silent installation has no GUI, so its checks run here (their messages are not shown).
  ${If} ${Silent}
    Call StartupChecks
  ${EndIf}
FunctionEnd

; Preselects the components of the CAD applications found on this computer (all when none is found).
Function SelectDetectedHosts
  StrCpy $FoundCad 0
  StrCpy $1 0
  !insertmacro ACAD_RELEASE "R24.0"
  !insertmacro ACAD_RELEASE "R24.1"
  !insertmacro ACAD_RELEASE "R24.2"
  !insertmacro ACAD_RELEASE "R24.3"
  !insertmacro ACAD_RELEASE "R25.0"
  !insertmacro ACAD_RELEASE "R25.1"
  ${If} $1 = 0
    !insertmacro UnselectSection ${SecAcad}
  ${Else}
    StrCpy $FoundCad 1
  ${EndIf}
  !insertmacro ZW_DETECT "2024" ${SecZw2024}
  !insertmacro ZW_DETECT "2025" ${SecZw2025}
  !insertmacro ZW_DETECT "2026" ${SecZw2026}

  ${If} $FoundCad = 0
    ; Nothing found (e.g. the CAD is installed later): offer everything.
    !insertmacro SelectSection ${SecAcad}
    !insertmacro SelectSection ${SecZw2024}
    !insertmacro SelectSection ${SecZw2025}
    !insertmacro SelectSection ${SecZw2026}
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

  ${If} $FoundCad = 0
    ${If} ${Cmd} `MessageBox MB_ICONEXCLAMATION|MB_YESNO|MB_DEFBUTTON2 "$(MsgNoCad)" /SD IDYES IDNO`
      SetErrorLevel 1
      Quit
    ${EndIf}
  ${EndIf}
FunctionEnd

Function WelcomeLeave
  Call WaitForCad
  Pop $0
  ${If} $0 != "ok"
    Abort
  ${EndIf}
FunctionEnd

;-------------------------------------------------------------------------------------------
; Uninstaller

Function un.onInit
  SetRegView 64
  !insertmacro MUI_UNGETLANGUAGE
FunctionEnd

Section "Uninstall"
  Call un.WaitForCad
  Pop $0
  ${If} $0 != "ok"
    DetailPrint "$(DetCadRunning)"
    Abort
  ${EndIf}

  ; The AutoCAD bundle is this product's own folder (named after it): remove it completely.
  ${If} ${FileExists} "${BUNDLE_PATH}\PackageContents.xml"
    RMDir /r "${BUNDLE_PATH}"
  ${EndIf}

  Push "2024"
  Call un.UnregisterZwcad
  Push "2025"
  Call un.UnregisterZwcad
  Push "2026"
  Call un.UnregisterZwcad
  RMDir /r "$INSTDIR\ZWCAD"
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
