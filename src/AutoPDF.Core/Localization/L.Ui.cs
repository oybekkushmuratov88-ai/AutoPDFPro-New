namespace AutoPDF.Core.Localization
{
    /// <summary>Texts of the windows: main window, settings, preview, summary and message dialogs.</summary>
    public static partial class L
    {
        // ---- Main window: detection -------------------------------------------------------------
        public static string DetectionMethod => T("Aniqlash usuli", "Способ поиска листов", "Detection method");
        public static string DetectManual => T("Listlarni qo‘lda tanlash", "Выбрать листы вручную", "Select sheets manually");
        public static string DetectManualHint => T(
            "Ramkalarni o‘zingiz tanlaysiz (bittalab yoki ramka bilan o‘rab).",
            "Вы сами выбираете рамки (по одной или рамкой выбора).",
            "You pick the frames yourself (one by one or with a selection window).");
        public static string DetectBlockName => T("Blok nomi bo‘yicha topish", "Поиск по имени блока", "Find by block name");
        public static string DetectBlockNameHint => T(
            "Model fazosidagi shu nomli barcha bloklar list deb olinadi.",
            "Все блоки с этим именем в пространстве модели считаются листами.",
            "Every block with this name in model space is treated as a sheet.");
        public static string BlockNamesLabel => T(
            "Blok nomlari (vergul bilan, * va ? mumkin):",
            "Имена блоков (через запятую, можно * и ?):",
            "Block names (comma-separated, * and ? allowed):");
        public static string BlockNamesExample => T(
            "Masalan: Ramka_A3, Format*, SHTAMP?",
            "Например: Рамка_A3, Формат*, ШТАМП?",
            "For example: Frame_A3, Format*, TITLE?");
        public static string DetectSample => T("Namuna ramka bo‘yicha (butun chizma)", "По образцу рамки (весь чертёж)", "By sample frame (whole drawing)");
        public static string DetectSampleHint => T(
            "Bitta ramkani ko‘rsatasiz — chizmadagi xuddi shunday (shu qatlamdagi) barcha ramkalar topiladi.",
            "Укажите одну рамку — будут найдены все такие же рамки чертежа (на том же слое).",
            "Pick one frame — all similar frames in the drawing (on the same layer) are found.");
        public static string DetectSampleRange => T("Namuna ramka + diapazon (qisman tanlash)", "По образцу + область (частичный выбор)", "Sample frame + area (partial selection)");
        public static string DetectSampleRangeHint => T(
            "Bitta ramkani ko‘rsatasiz, so‘ng hududni tanlaysiz — faqat shu hududdagi o‘xshash ramkalar olinadi.",
            "Укажите рамку-образец, затем область — берутся только похожие рамки в этой области.",
            "Pick a sample frame, then an area — only similar frames inside that area are taken.");

        // ---- Main window: sorting, numbering, folder --------------------------------------------
        public static string SortDirection => T("Saralash yo‘nalishi", "Порядок нумерации", "Sorting direction");
        public static string SortLeftToRight => T("Chapdan o‘ngga", "Слева направо", "Left to right");
        public static string SortTopToBottom => T("Yuqoridan pastga", "Сверху вниз", "Top to bottom");
        public static string SortLeftToRightHint => T(
            "Qator-qator: yuqori qatordan boshlab, har qator chapdan o‘ngga",
            "По строкам: с верхней строки, каждая строка слева направо",
            "Row by row: top row first, each row left to right");
        public static string SortTopToBottomHint => T(
            "Ustun-ustun: chap ustundan boshlab, har ustun yuqoridan pastga",
            "По столбцам: с левого столбца, каждый столбец сверху вниз",
            "Column by column: left column first, each column top to bottom");
        public static string StartNumber => T("Boshlang‘ich raqam", "Начальный номер", "Starting number");
        public static string OutputFolder => T("Saqlash papkasi", "Папка сохранения", "Output folder");
        public static string FilesPreview(string first, string second) => F(T("Fayllar: {0}, {1} …", "Файлы: {0}, {1} …", "Files: {0}, {1} …"), first, second);
        public static string FilesRange(string first, string last) => F(T("Fayllar: {0} … {1}", "Файлы: {0} … {1}", "Files: {0} … {1}"), first, last);
        public static string FileSingle(string name) => T("Fayl: ", "Файл: ", "File: ") + name;

        // ---- Main window: table ----------------------------------------------------------------
        public static string FoundSheets(int count) => T("Topilgan listlar: ", "Найдено листов: ", "Sheets found: ") + count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static string CountReady(int n) => T("tayyor: ", "готово: ", "ready: ") + n;
        public static string CountCrop(int n) => T("kesish bilan: ", "с обрезкой: ", "with crop: ") + n;
        public static string CountWarnings(int n) => T("ogohlantirish: ", "предупреждений: ", "warnings: ") + n;
        public static string CountErrors(int n) => T("xato: ", "ошибок: ", "errors: ") + n;
        public const string ColNumber = "#";
        public static string ColBlock => T("Blok", "Блок", "Block");
        public static string ColWidth => T("Eni", "Ширина", "Width");
        public static string ColHeight => T("Bo‘yi", "Высота", "Height");
        public static string ColPaper => T("Qog‘oz", "Формат", "Paper");
        public static string ColStatus => T("Holat", "Статус", "Status");
        public static string TipFormat => T("Format", "Формат", "Format");
        public static string TipNonStandard => T("nostandart", "нестандартный", "non-standard");
        public static string TipPaper => T("Qog‘oz", "Бумага", "Paper");
        public static string TipScale => T("Masshtab", "Масштаб", "Scale");
        public static string TipLayer => T("Qatlam", "Слой", "Layer");
        public static string TipPlot => T("Plot", "Печать", "Plot");
        public static string TipFile => T("Fayl", "Файл", "File");

        // ---- Main window: buttons and messages -------------------------------------------------
        public static string Scan => T("Skanerlash", "Сканировать", "Scan");
        public static string Check => T("Tekshirish", "Проверить", "Check");
        public static string CreatePdf => T("PDF yaratish", "Создать PDF", "Create PDF");
        public static string Settings => T("Sozlamalar", "Настройки", "Settings");
        public static string ScanHint => T(
            "Tanlangan usul bo‘yicha chizmadan listlarni topish",
            "Найти листы на чертеже выбранным способом",
            "Find the sheets in the drawing with the chosen method");
        public static string CheckHint => T(
            "Listlarni chizmada raqamlab ko‘rsatish va qog‘oz/plotterni tekshirish",
            "Показать номера листов на чертеже и проверить бумагу/плоттер",
            "Show the sheet numbers on the drawing and check paper/plotter");
        public static string CreatePdfHint => T(
            "Har bir listni alohida PDF faylga chiqarish (01.pdf, 02.pdf …)",
            "Сохранить каждый лист в отдельный PDF (01.pdf, 02.pdf …)",
            "Export each sheet to its own PDF (01.pdf, 02.pdf …)");
        public static string SettingsHint => T(
            "Til, plotter, CTB, masshtab, qog‘oz o‘lchamlari va boshqa sozlamalar",
            "Язык, плоттер, CTB, масштаб, форматы бумаги и другие настройки",
            "Language, plotter, CTB, scale, paper sizes and more");
        public static string NoSheetsYet => T(
            "Listlar hali aniqlanmagan. Usulni tanlab «Skanerlash» tugmasini bosing.",
            "Листы ещё не найдены. Выберите способ и нажмите «Сканировать».",
            "No sheets detected yet. Choose a method and press “Scan”.");
        public static string NoSheetsFound => T("Hech qanday list topilmadi.", "Листы не найдены.", "No sheets found.");
        public static string ChooseFolderFirst => T("Avval saqlash papkasini tanlang.", "Сначала выберите папку сохранения.", "Choose the output folder first.");
        public static string FolderNotWritable => T("Papkaga yozib bo‘lmadi:", "Нет доступа на запись в папку:", "Cannot write to the folder:");
        public static string ScanFirst => T(
            "Avval listlarni aniqlang: usulni tanlab «Skanerlash» tugmasini bosing.",
            "Сначала найдите листы: выберите способ и нажмите «Сканировать».",
            "Detect the sheets first: choose a method and press “Scan”.");
        public static string FolderNotFound(string folder) => T("Papka topilmadi: ", "Папка не найдена: ", "Folder not found: ") + folder;
        public static string FileNotFound(string file) => T("Fayl topilmadi: ", "Файл не найден: ", "File not found: ") + file;
        public static string ExistingFilesQuestion(int count) => F(T(
            "Saqlash papkasida {0} ta PDF fayl allaqachon mavjud.\n\nHa — ustiga yozish\nYo‘q — yangi nom bilan saqlash (01_1.pdf)\nBekor qilish — eksportni to‘xtatish",
            "В папке сохранения уже есть PDF-файлы: {0}.\n\nДа — перезаписать\nНет — сохранить с новым именем (01_1.pdf)\nОтмена — остановить экспорт",
            "{0} PDF file(s) already exist in the output folder.\n\nYes — overwrite\nNo — save under a new name (01_1.pdf)\nCancel — stop the export"), count);

        // ---- Preview window ----------------------------------------------------------------
        public static string PreviewTitle => T("PDF ko‘rib chiqish", "Просмотр PDF", "PDF preview");
        public static string PreviewHeader(int index, int total, string file) => F(T("List {0} / {1} — {2}", "Лист {0} / {1} — {2}", "Sheet {0} / {1} — {2}"), index, total, file);
        public static string PreviewCancel => T("Bekor qilish", "Отмена", "Cancel");
        public static string PreviewConfirm => T("Tasdiqlash", "Подтвердить", "Confirm");
        public static string PreviewConfirmLast => T("Tasdiqlash (oxirgi list)", "Подтвердить (последний лист)", "Confirm (last sheet)");
        public static string PreviewConfirmAll => T("Tasdiqlash va qolganlarini avtomatik yaratish", "Подтвердить и создать остальные автоматически", "Confirm and create the rest automatically");
        public static string PreviewOpenExternal => T("Tashqi dasturda ochish", "Открыть во внешней программе", "Open in external viewer");
        public static string PreviewUnavailable => T(
            "Ko‘rish imkoni yo‘q — faylni tashqi dasturda oching.",
            "Предпросмотр недоступен — откройте файл во внешней программе.",
            "Preview not available — open the file in an external viewer.");
        public static string PreviewCancelHint => T(
            "Eksportni to‘xtatish (yaratilgan fayllar saqlanib qoladi)",
            "Остановить экспорт (созданные файлы сохранятся)",
            "Stop the export (files already created are kept)");
        public static string PreviewConfirmHint => T(
            "Keyingi listni yaratib, uni ham ko‘rsatish",
            "Создать следующий лист и тоже показать его",
            "Create the next sheet and preview it too");
        public static string PreviewConfirmAllHint => T(
            "Qolgan barcha listlarni ko‘rsatmasdan ketma-ket yaratish",
            "Создать все оставшиеся листы без просмотра",
            "Create all remaining sheets without previews");
        public static string PreviewTimeout => T(
            "PDF ko‘rinishini tayyorlash juda uzoq davom etdi.",
            "Подготовка предпросмотра заняла слишком много времени.",
            "Rendering the preview took too long.");
        public static string InfoTitle => T("Ma’lumot", "Сведения", "Details");
        public static string InfoFile => T("Fayl", "Файл", "File");
        public static string InfoExpectedPaper => T("Kutilgan qog‘oz", "Ожидаемый формат", "Expected paper");
        public static string InfoPdfPage => T("PDF sahifasi", "Страница PDF", "PDF page");
        public static string InfoFileSize => T("Fayl hajmi", "Размер файла", "File size");
        public static string InfoMethod => T("Usul", "Способ", "Method");
        public static string InfoPlot => T("Plot", "Печать", "Plot");
        public static string InfoChecks => T("Tekshiruv", "Проверка", "Checks");
        public const string MethodApi = "AutoCAD plot API";
        public static string MethodCommand => T("-PLOT buyrug‘i", "команда -PLOT", "-PLOT command");
        public static string CheckFileOk(string size) => F(T("✓ Fayl yaratildi va bo‘sh emas ({0})", "✓ Файл создан и не пустой ({0})", "✓ File created and not empty ({0})"), size);
        public static string CheckPageOk(object size) => T("✓ Sahifa o‘lchami to‘g‘ri: ", "✓ Размер страницы верный: ", "✓ Page size correct: ") + size;
        public static string CheckPageWarn(object size) => T("⚠ Sahifa o‘lchami: ", "⚠ Размер страницы: ", "⚠ Page size: ") + size;

        // ---- Settings window -----------------------------------------------------------------
        public static string SettingsTitle => T("Sozlamalar", "Настройки", "Settings");
        public static string SettingsSubtitle => T("Til, plot, aniqlash va eksport parametrlari", "Язык, печать, поиск листов и экспорт", "Language, plotting, detection and export");

        /// <summary>Always trilingual so a user who does not read the current language still finds it.</summary>
        public const string GroupLanguage = "Til · Язык · Language";
        public static string LanguageLabel => T("Interfeys tili:", "Язык интерфейса:", "Interface language:");
        public static string LanguageHint => T(
            "Saqlangandan so‘ng oyna tanlangan tilda qayta ochiladi.",
            "После сохранения окно откроется заново на выбранном языке.",
            "After saving, the window reopens in the selected language.");
        public static string GroupPlot => T("Chop etish (plot)", "Печать (плоттер)", "Plotting");
        public static string GroupDetection => T("Aniqlash", "Поиск листов", "Detection");
        public static string GroupExport => T("Eksport", "Экспорт", "Export");
        public static string GroupDiagnostics => T("Diagnostika", "Диагностика", "Diagnostics");
        public static string Plotter => T("Plotter (PC3):", "Плоттер (PC3):", "Plotter (PC3):");
        public static string CustomSizesPlotter => T("Maxsus o‘lchamlar plotteri (ixtiyoriy):", "Плоттер нестандартных форматов (необязательно):", "Custom-sizes plotter (optional):");
        public static string CustomSizesPlotterHint => T(
            "DWG To PDF.pc3 nusxasi, unga 630x297 kabi maxsus o‘lchamlar qo‘shilgan bo‘lsa — o‘sha o‘lchamlar aniq chiqariladi (README ga qarang).",
            "Копия DWG To PDF.pc3 с добавленными форматами (например, 630x297) — такие листы печатаются на точный формат (см. README).",
            "A copy of DWG To PDF.pc3 with added sizes such as 630x297 — those sheets are then plotted on the exact paper (see README).");
        public static string StyleTable => T("Plot uslubi jadvali:", "Таблица стилей печати:", "Plot style table:");
        public static string UseMonochrome => T(
            "Plot uslubi jadvalini qo‘llash (monochrome.ctb — oq-qora)",
            "Применять таблицу стилей (monochrome.ctb — чёрно-белая)",
            "Apply the plot style table (monochrome.ctb — black and white)");
        public static string PlotWithStyles => T("Plot uslublari bilan chop etish", "Печать со стилями печати", "Plot with plot styles");
        public static string PlotLineweights => T("Chiziq qalinliklarini chop etish", "Печать весов линий", "Plot lineweights");
        public static string ScaleLineweights => T("Chiziq qalinligini masshtablash", "Масштабировать веса линий", "Scale lineweights");
        public static string CenterPlot => T("Chizmani markazlashtirish", "Центрировать чертёж", "Center the plot");
        public static string AllowCrop => T(
            "Nostandart o‘lcham: kattaroq qog‘ozga chiqarib, PDF ni aniq o‘lchamga qirqish",
            "Нестандартный формат: печатать на большем листе и обрезать PDF до точного размера",
            "Non-standard size: plot on a larger sheet and crop the PDF to the exact size");
        public static string Scale => T("Masshtab:", "Масштаб:", "Scale:");
        public static string ScaleAuto => T("Avtomatik", "Автоматически", "Automatic");
        public static string ScaleHint => T(
            "Avtomatik: ramka o‘lchamidan aniqlanadi (1:1, 1:100 …).",
            "Автоматически: определяется по размеру рамки (1:1, 1:100 …).",
            "Automatic: detected from the frame size (1:1, 1:100 …).");
        public static string PlotMethod => T("Plot usuli:", "Способ печати:", "Plot method:");
        public static string PlotMethodAuto => T("Avtomatik (API, kerak bo‘lsa -PLOT)", "Автоматически (API, при ошибке -PLOT)", "Automatic (API, -PLOT on failure)");
        public static string PlotMethodApi => T("Faqat API (PlotEngine)", "Только API (PlotEngine)", "API only (PlotEngine)");
        public static string PlotMethodCmd => T("Faqat -PLOT buyrug‘i", "Только команда -PLOT", "-PLOT command only");
        public static string CustomFormats => T("Qo‘shimcha formatlar (mm):", "Дополнительные форматы (мм):", "Additional formats (mm):");
        public static string CustomFormatsHint => T("Masalan: 630x297; 891x420; 1260x594", "Например: 630x297; 891x420; 1260x594", "For example: 630x297; 891x420; 1260x594");
        public static string SnapTolerance => T("O‘lcham tolerantligi (mm):", "Допуск размера (мм):", "Size tolerance (mm):");
        public static string MinSheetSize => T("Minimal list tomoni (mm):", "Минимальная сторона листа (мм):", "Minimum sheet side (mm):");
        public static string SampleMatchLayer => T("Namuna: faqat shu qatlamdagi (sloy) ramkalar", "Образец: только рамки на том же слое", "Sample: only frames on the same layer");
        public static string SampleMatchSize => T("Namuna: faqat bir xil o‘lchamdagi ramkalar", "Образец: только рамки того же размера", "Sample: only frames of the same size");
        public static string FileNamePattern => T("Fayl nomi shabloni:", "Шаблон имени файла:", "File name pattern:");
        public static string FileNamePatternHint => T(
            "{N} – raqam (01, 02 …), {DWG} – chizma nomi, {FORMAT} – format (A4, A3x3 …)",
            "{N} – номер (01, 02 …), {DWG} – имя чертежа, {FORMAT} – формат (A4, A3x3 …)",
            "{N} – number (01, 02 …), {DWG} – drawing name, {FORMAT} – format (A4, A3x3 …)");
        public static string Overwrite => T("Fayl mavjud bo‘lsa:", "Если файл существует:", "If the file exists:");
        public static string OverwriteAsk => T("So‘rash", "Спросить", "Ask");
        public static string OverwriteReplace => T("Ustiga yozish", "Перезаписать", "Overwrite");
        public static string OverwriteRename => T("Yangi nom bilan (01_1.pdf)", "С новым именем (01_1.pdf)", "New name (01_1.pdf)");
        public static string OverwriteSkip => T("O‘tkazib yuborish", "Пропустить", "Skip");
        public static string OnError => T("Xatolik bo‘lganda:", "При ошибке:", "On error:");
        public static string OnErrorContinue => T("Davom etish (keyingi listga o‘tish)", "Продолжить (перейти к следующему листу)", "Continue (go to the next sheet)");
        public static string OnErrorStop => T("To‘xtatish", "Остановить", "Stop");
        public static string PageTolerance => T("PDF sahifa tolerantligi (mm):", "Допуск страницы PDF (мм):", "PDF page tolerance (mm):");
        public static string PreviewEachSheet => T(
            "Har bir PDF dan keyin ko‘rib chiqish (tasdiqlash oynasi)",
            "Показывать каждый PDF для подтверждения",
            "Preview each PDF for confirmation");
        public static string OpenFolderWhenDone => T("Tugagach papkani ochish", "Открыть папку по завершении", "Open the folder when done");
        public static string VerboseLog => T("Batafsil jurnal (log)", "Подробный журнал (лог)", "Detailed log");
        public static string DiagnosticsHint => T(
            "Diagnostika buyrug‘i: AUTOPDFMEDIA — plotter qog‘ozlari va listlar uchun tanlangan qog‘ozni ko‘rsatadi.",
            "Команда диагностики: AUTOPDFMEDIA — показывает форматы плоттера и формат, выбранный для листов.",
            "Diagnostics command: AUTOPDFMEDIA — shows the plotter's papers and the paper chosen for each sheet size.");
        public static string OpenLogFolder => T("Jurnal papkasini ochish", "Открыть папку журнала", "Open log folder");
        public static string InvalidNumbers => T(
            "Son qiymatlari noto‘g‘ri kiritilgan (masalan: 0.5).",
            "Неверно введены числовые значения (например: 0.5).",
            "Invalid numeric values (for example: 0.5).");

        // ---- Summary window ------------------------------------------------------------------
        public static string SummaryTitle => T("Eksport yakunlandi", "Экспорт завершён", "Export finished");
        public static string SummaryAllOk => T("Barcha listlar muvaffaqiyatli yaratildi.", "Все листы успешно созданы.", "All sheets were created successfully.");
        public static string SummarySomeFailed => T(
            "Ba’zi listlar yaratilmadi — quyidagi ro‘yxatni ko‘ring.",
            "Некоторые листы не созданы — см. список ниже.",
            "Some sheets were not created — see the list below.");
        public static string SummaryTotal => T("Jami listlar", "Всего листов", "Total sheets");
        public static string SummarySuccess => T("Muvaffaqiyatli", "Успешно", "Succeeded");
        public static string SummaryWarnings => T("Ogohlantirish bilan", "С предупреждениями", "With warnings");
        public static string SummaryFailed => T("Xatolik", "Ошибки", "Failed");
        public static string SummarySkipped => T("O‘tkazildi", "Пропущено", "Skipped");
        public static string SummaryCancelled => T("Bekor qilindi", "Отменено", "Cancelled");
        public static string SummaryErrors => T("Xatolar va ogohlantirishlar", "Ошибки и предупреждения", "Errors and warnings");
        public static string SummaryFolder => T("Papka: ", "Папка: ", "Folder: ");
        public static string SummaryNoErrors => T("✓ Xato yo‘q.", "✓ Ошибок нет.", "✓ No errors.");
        public static string SummaryLineCancelled => T("bekor qilindi", "отменён", "cancelled");
        public static string SummaryLineSkipped => T("o‘tkazildi", "пропущен", "skipped");
        public static string OpenFolder => T("Papkani ochish", "Открыть папку", "Open folder");
        public static string OpenLog => T("Jurnalni ochish", "Открыть журнал", "Open log");
    }
}
