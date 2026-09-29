namespace AutoPDF.Core.Localization
{
    /// <summary>Texts of the AutoCAD side: command-line prompts, detection, plotting, export and log messages.</summary>
    public static partial class L
    {
        // ---- Command line ---------------------------------------------------------------------
        public static string PromptSelectFrames => T(
            "\nList ramkalarini tanlang (to‘rtburchak polilinya yoki blok): ",
            "\nВыберите рамки листов (прямоугольная полилиния или блок): ",
            "\nSelect the sheet frames (rectangular polyline or block): ");
        public static string PromptSelectSample => T(
            "\nNamuna ramkani tanlang (to‘rtburchak polilinya yoki blok): ",
            "\nУкажите рамку-образец (прямоугольная полилиния или блок): ",
            "\nPick a sample frame (rectangular polyline or block): ");
        public static string PromptSampleReject => T(
            "\nFaqat yopiq to‘rtburchak polilinya yoki blok tanlang.",
            "\nВыберите только замкнутую прямоугольную полилинию или блок.",
            "\nSelect a closed rectangular polyline or a block only.");
        public static string PromptSelectRange => T(
            "\nQidiriladigan hududni tanlang (ramka bilan o‘rab oling): ",
            "\nВыберите область поиска (рамкой выбора): ",
            "\nSelect the area to search (selection window): ");
        public static string PromptReview => T(
            "\nListlar raqamlandi. Chizmani ko‘zdan kechiring (zoom/pan ishlaydi), so‘ng ENTER bosing: ",
            "\nЛисты пронумерованы. Проверьте чертёж (зумирование и панорамирование работают), затем нажмите ENTER: ",
            "\nThe sheets are numbered. Review the drawing (zoom/pan work), then press ENTER: ");

        public static string LoadedMessage(string version) => F(T(
            "\nAutoPDF Pro v{0} yuklandi (© {1}). Buyruq: AUTOPDF  (diagnostika: AUTOPDFMEDIA, jurnal: AUTOPDFLOG)\n",
            "\nAutoPDF Pro v{0} загружен (© {1}). Команда: AUTOPDF  (диагностика: AUTOPDFMEDIA, журнал: AUTOPDFLOG)\n",
            "\nAutoPDF Pro v{0} loaded (© {1}). Command: AUTOPDF  (diagnostics: AUTOPDFMEDIA, log: AUTOPDFLOG)\n"), version, AppInfo.Author);
        public static string LoadedLog(string version, string folder) => F(T("AutoPDF Pro {0} yuklandi: {1}", "AutoPDF Pro {0} загружен: {1}", "AutoPDF Pro {0} loaded: {1}"), version, folder);
        public static string EchoError => T("AutoPDF XATO: ", "AutoPDF ОШИБКА: ", "AutoPDF ERROR: ");
        public static string EchoWarning => T("AutoPDF ogohlantirish: ", "AutoPDF предупреждение: ", "AutoPDF warning: ");
        public static string UnexpectedError => T("Kutilmagan xato", "Непредвиденная ошибка", "Unexpected error");
        public static string LogFileIs(string path) => T("AutoPDF jurnali: ", "Журнал AutoPDF: ", "AutoPDF log: ") + path;
        public static string SysVarSetFailed(string name, string error) => F(T("SysVar {0} o‘rnatilmadi: {1}", "Не удалось установить {0}: {1}", "Could not set {0}: {1}"), name, error);
        public static string SysVarRestoreFailed(string name, string error) => F(T("SysVar {0} tiklanmadi: {1}", "Не удалось восстановить {0}: {1}", "Could not restore {0}: {1}"), name, error);

        // ---- AUTOPDFMEDIA -------------------------------------------------------------------
        public static string MediaDiagTitle => T("AutoPDF Pro — plotter diagnostikasi", "AutoPDF Pro — диагностика плоттера", "AutoPDF Pro — plotter diagnostics");
        public static string Plotters => T("Plotterlar", "Плоттеры", "Plotters");
        public static string PlotterNotFound(string name) => F(T("“{0}” plotteri topilmadi.", "Плоттер «{0}» не найден.", "Plotter “{0}” not found."), name);
        public static string CustomPlotterPresent(string name) => T("Maxsus o‘lchamlar plotteri mavjud: ", "Плоттер нестандартных форматов найден: ", "Custom-sizes plotter found: ") + name;
        public static string CustomPlotterMissing(string name) => F(T(
            "Maxsus o‘lchamlar plotteri (“{0}”) o‘rnatilmagan — nostandart listlar kesish usulida chiqariladi.",
            "Плоттер нестандартных форматов («{0}») не установлен — нестандартные листы печатаются с обрезкой.",
            "The custom-sizes plotter (“{0}”) is not installed — non-standard sheets are plotted and cropped."), name);
        public static string Details(string path) => T("Batafsil: ", "Подробно: ", "Details: ") + path;
        public static string MediaCommandError => T("AUTOPDFMEDIA xato: ", "Ошибка AUTOPDFMEDIA: ", "AUTOPDFMEDIA error: ");
        public static string DiagFullBleed => T("chetsiz", "без полей", "full bleed");
        public static string DiagWithMargins(int count) => F(T("(+ {0} ta chetli qog‘oz — ishlatilmaydi)", "(+ форматов с полями: {0} — не используются)", "(+ {0} papers with margins — not used)"), count);
        public static string DiagChoices => T("Listlar uchun tanlov:", "Выбор для листов:", "Paper chosen for sheets:");
        public static string DiagExact => T("(aniq)", "(точно)", "(exact)");
        public static string DiagNone(string problem) => T("YO‘Q: ", "НЕТ: ", "NONE: ") + problem;

        // ---- Paper / plotter catalogue --------------------------------------------------------
        public static string DeviceListFailed(string error) => T("Plotterlar ro‘yxatini olib bo‘lmadi: ", "Не удалось получить список плоттеров: ", "Could not get the plotter list: ") + error;
        public static string DeviceOpenFailed(string device, string error) => F(T("“{0}” plotterini ochib bo‘lmadi: {1}", "Не удалось открыть плоттер «{0}»: {1}", "Could not open plotter “{0}”: {1}"), device, error);
        public static string MediaCatalog(string device, int count, int fullBleed, int failed) => F(T(
            "Plotter “{0}”: {1} ta qog‘oz ({2} tasi chetsiz/full-bleed){3}.",
            "Плоттер «{0}»: форматов {1} (без полей/full bleed: {2}){3}.",
            "Plotter “{0}”: {1} papers ({2} full bleed){3}."),
            device, count, fullBleed,
            failed > 0 ? F(T(", {0} tasi o‘qilmadi", ", не прочитано: {0}", ", {0} unreadable"), failed) : string.Empty);
        public static string PlotterNotFoundSettings(string name) => F(T(
            "“{0}” plotteri topilmadi (Sozlamalar → Plotter).",
            "Плоттер «{0}» не найден (Настройки → Плоттер).",
            "Plotter “{0}” not found (Settings → Plotter)."), name);
        public static string CustomPlotterReadFailed(string error) => T(
            "Maxsus o‘lchamlar plotteri o‘qilmadi: ",
            "Не удалось прочитать плоттер нестандартных форматов: ",
            "Could not read the custom-sizes plotter: ") + error;
        public static string NoMediaForCrop(string device, object paper) => F(T(
            "“{0}” da {1} ni sig‘diradigan chetsiz (full bleed) qog‘oz yo‘q.",
            "У «{0}» нет формата без полей (full bleed), вмещающего {1}.",
            "“{0}” has no full-bleed paper that fits {1}."), device, paper);
        public static string NoMediaExact(string device, object paper) => F(T(
            "“{0}” da {1} o‘lchamli qog‘oz yo‘q (kesish o‘chirilgan — Sozlamalarda yoqing yoki README bo‘yicha maxsus o‘lcham qo‘shing).",
            "У «{0}» нет формата {1} (обрезка отключена — включите её в Настройках или добавьте формат по README).",
            "“{0}” has no {1} paper (cropping is off — enable it in Settings or add the size as described in the README)."), device, paper);

        // ---- Plot settings / environment ---------------------------------------------------------
        public static string StyleAdapted(bool colorDependent, string wanted, string adapted) => F(T(
            "Chizma {0} plot uslubida — “{1}” o‘rniga “{2}”.",
            "Чертёж использует {0} стили печати — вместо «{1}» используется «{2}».",
            "The drawing uses {0} plot styles — “{2}” is used instead of “{1}”."),
            colorDependent
                ? T("rangga bog‘liq (CTB)", "цветозависимые (CTB)", "colour-dependent (CTB)")
                : T("nomli (STB)", "именованные (STB)", "named (STB)"),
            wanted, adapted);
        public static string StyleListFailed(string error) => T("Plot uslublari ro‘yxatini olib bo‘lmadi: ", "Не удалось получить список стилей печати: ", "Could not get the plot style list: ") + error;
        public static string StyleNotFound(string name) => F(T(
            "“{0}” plot uslubi jadvali topilmadi — uslubsiz chiqariladi.",
            "Таблица стилей «{0}» не найдена — печать без таблицы стилей.",
            "Plot style table “{0}” not found — plotting without a style table."), name);
        public static string SwitchedToModel => T(
            "Model fazosiga o‘tildi (listlar model fazosida qidiriladi).",
            "Выполнен переход в пространство модели (листы ищутся в модели).",
            "Switched to model space (sheets are searched in model space).");
        public static string LayoutRestoreFailed(string error) => T("Oldingi layout tiklanmadi: ", "Не удалось вернуть предыдущий лист (layout): ", "Could not restore the previous layout: ") + error;
        public static string EnvPlotterMissing(string name) => "✖ " + PlotterNotFound(name);
        public static string EnvPlotterOk(string name, int count) => F(T("✓ Plotter: {0} ({1} ta qog‘oz)", "✓ Плоттер: {0} (форматов: {1})", "✓ Plotter: {0} ({1} papers)"), name, count);
        public static string EnvPlotterMediaFailed(string error) => T("✖ Plotter qog‘ozlari o‘qilmadi: ", "✖ Не удалось прочитать форматы плоттера: ", "✖ Could not read the plotter's papers: ") + error;
        public static string EnvCustomPlotter(string name) => T("✓ Maxsus o‘lchamlar plotteri: ", "✓ Плоттер нестандартных форматов: ", "✓ Custom-sizes plotter: ") + name;
        public static string EnvStyle(string name) => T("✓ Plot uslubi: ", "✓ Стиль печати: ", "✓ Plot style: ") + name;
        public static string EnvUnits(string units) => T("ℹ Chizma birliklari: ", "ℹ Единицы чертежа: ", "ℹ Drawing units: ") + units;

        // ---- Plot engine / -PLOT ------------------------------------------------------------
        public static string PlotBusy => T(
            "AutoCAD boshqa chop etish jarayonini bajaryapti (fon rejimidagi plot tugashini kuting).",
            "AutoCAD уже выполняет печать (дождитесь окончания фоновой печати).",
            "AutoCAD is already plotting (wait for the background plot to finish).");
        public static string ValidatedFormat => T(
            "Tasdiqlangan: qurilma={0}, qog‘oz={1} ({2:0.##} x {3:0.##} mm), burish={4}, masshtab={5}:{6}, oyna(DCS)={7}, uslub={8}",
            "Проверено: устройство={0}, формат={1} ({2:0.##} x {3:0.##} мм), поворот={4}, масштаб={5}:{6}, окно(DCS)={7}, стиль={8}",
            "Validated: device={0}, paper={1} ({2:0.##} x {3:0.##} mm), rotation={4}, scale={5}:{6}, window(DCS)={7}, style={8}");
        public static string ProgressCancelJob => T("Bekor qilish", "Отмена", "Cancel");
        public static string ProgressCancelSheet => T("Listni bekor qilish", "Отменить лист", "Cancel sheet");
        public static string ProgressJob => T("Jarayon", "Процесс", "Progress");
        public static string PlotCancelledByUser => T("Chop etish oynasida bekor qilindi.", "Печать отменена в окне хода печати.", "Cancelled in the plot progress dialog.");
        public static string CmdAnswers(string tokens) => T("-PLOT javoblari: ", "Ответы -PLOT: ", "-PLOT answers: ") + tokens;
        public static string CmdFailed(string error) => F(T(
            "-PLOT buyrug‘i xato bilan tugadi: {0}. Buyruq qatori tarixini (F2) tekshiring; yuborilgan javoblar jurnalda.",
            "Команда -PLOT завершилась с ошибкой: {0}. Проверьте историю командной строки (F2); отправленные ответы есть в журнале.",
            "The -PLOT command failed: {0}. Check the command-line history (F2); the answers sent are in the log."), error);
        public static string CmdNoFile => T(
            "-PLOT buyrug‘i PDF yaratmadi. Buyruq qatori tarixida (F2) qaysi so‘rov boshqacha ekanini ko‘ring; yuborilgan javoblar jurnalda.",
            "Команда -PLOT не создала PDF. Посмотрите в истории командной строки (F2), какой запрос отличался; отправленные ответы есть в журнале.",
            "The -PLOT command did not create a PDF. Check in the command-line history (F2) which prompt differed; the answers sent are in the log.");
        public static string CmdCapabilityUnknown(string error) => F(T(
            "PlotToFileCapability aniqlanmadi ({0}) — PDF qurilma deb olinadi.",
            "Не удалось определить PlotToFileCapability ({0}) — считается PDF-устройством.",
            "PlotToFileCapability unknown ({0}) — treated as a PDF device."), error);

        // ---- Export of one sheet ------------------------------------------------------------
        public static string PlanChosen(string plan, bool exact) => T("Qog‘oz: ", "Формат: ", "Paper: ") + plan + (exact
            ? T(" (aniq o‘lcham)", " (точный размер)", " (exact size)")
            : T(" (keyin kesiladi)", " (будет обрезан)", " (cropped afterwards)"));
        public static string FileLocked(string path, string problem) => F(T(
            "Fayl band — boshqa dasturda (PDF ko‘ruvchi) ochiq bo‘lishi mumkin: {0} ({1})",
            "Файл занят — возможно, открыт в другой программе (просмотрщик PDF): {0} ({1})",
            "The file is in use — probably open in another program (PDF viewer): {0} ({1})"), path, problem);
        public static string PageUnreadable(string error) => F(T(
            "PDF sahifa o‘lchamini o‘qib bo‘lmadi ({0}) — o‘lcham tekshiruvi bajarilmadi.",
            "Не удалось прочитать размер страницы PDF ({0}) — проверка размера не выполнена.",
            "Could not read the PDF page size ({0}) — size check skipped."), error);
        public static string PageUnreadableCrop(string error, object page) => F(T(
            "PDF sahifa o‘lchamini o‘qib bo‘lmadi ({0}) — o‘lcham tekshiruvi va kesish bajarilmadi (sahifa {1} bo‘lib qoldi).",
            "Не удалось прочитать размер страницы PDF ({0}) — проверка и обрезка не выполнены (страница осталась {1}).",
            "Could not read the PDF page size ({0}) — size check and cropping skipped (page left at {1})."), error, page);
        public static string PageCount(int pages) => F(T("PDF da {0} ta sahifa bor (1 ta kutilgan).", "В PDF страниц: {0} (ожидалась 1).", "The PDF has {0} pages (1 expected)."), pages);
        public static string PageDebugFormat => T(
            "PDF sahifa: {0} (MediaBox {1:0.#}x{2:0.#} pt, /Rotate {3}), kutilgan {4} → {5}",
            "Страница PDF: {0} (MediaBox {1:0.#}x{2:0.#} pt, /Rotate {3}), ожидалось {4} → {5}",
            "PDF page: {0} (MediaBox {1:0.#}x{2:0.#} pt, /Rotate {3}), expected {4} → {5}");
        public static string OrientationSwapped(object actual, object expected) => F(T(
            "Sahifa yo‘nalishi teskari chiqdi ({0}, kutilgan {1}) — boshqa burish bilan qayta chiqarilmoqda.",
            "Ориентация страницы получилась обратной ({0}, ожидалось {1}) — повторная печать с другим поворотом.",
            "The page orientation came out swapped ({0}, expected {1}) — plotting again with the other rotation."), actual, expected);
        public static string DeleteSwappedFailed(string error) => T("Teskari chiqqan PDF ni o‘chirib bo‘lmadi: ", "Не удалось удалить PDF с неверной ориентацией: ", "Could not delete the wrongly oriented PDF: ") + error;
        public static string PageMismatch(object actual, object expected) => F(T("PDF sahifasi {0}, kutilgan {1}.", "Страница PDF {0}, ожидалось {1}.", "The PDF page is {0}, expected {1}."), actual, expected);
        public static string WrongPaperOrOrientation => T(" Qog‘oz yoki yo‘nalish noto‘g‘ri.", " Неверный формат или ориентация.", " Wrong paper or orientation.");
        public static string CropFailed(object target, string error) => F(T("PDF ni {0} o‘lchamga qirqib bo‘lmadi: {1}", "Не удалось обрезать PDF до {0}: {1}", "Could not crop the PDF to {0}: {1}"), target, error);
        public static string CroppedUnreadable(string error) => T("Qirqilgan PDF ni o‘qib bo‘lmadi: ", "Не удалось прочитать обрезанный PDF: ", "Could not read the cropped PDF: ") + error;
        public static string CroppedPage(object size) => T("Qirqilgan sahifa: ", "Обрезанная страница: ", "Cropped page: ") + size;
        public static string FinalSizeWarning(object actual, object sheet) => F(T("Yakuniy PDF o‘lchami {0}, list {1}.", "Итоговый размер PDF {0}, лист {1}.", "Final PDF size {0}, sheet {1}."), actual, sheet);
        public static string EmptyPage => T("Ramka ichida chizma topilmadi (bo‘sh sahifa).", "Внутри рамки нет графики (пустая страница).", "Nothing was plotted inside the frame (empty page).");
        public static string ApiFailedFallback(string error) => F(T(
            "Plot API xatosi: {0} → -PLOT buyrug‘i bilan urinilmoqda.",
            "Ошибка plot API: {0} → попытка через команду -PLOT.",
            "Plot API error: {0} → trying the -PLOT command."), error);
        public static string ApiFailedWarning(string step, string error) => F(T(
            "Plot API xatosi [{0}] {1} — -PLOT bilan yaratildi.",
            "Ошибка plot API [{0}] {1} — создано через -PLOT.",
            "Plot API error [{0}] {1} — created with -PLOT."), step, error);

        // ---- Frame reading ----------------------------------------------------------------
        public static string NotAFrame(string type) => F(T("{0} ramka emas", "{0} — не рамка", "{0} is not a frame"), type);
        public static string NotInXyPlane => T("polilinya XY tekisligida emas", "полилиния не в плоскости XY", "the polyline is not in the XY plane");
        public static string NotRectangle(string reason) => F(T("to‘rtburchak emas ({0})", "не прямоугольник ({0})", "not a rectangle ({0})"), reason);
        public static string CurvedPolyline => T("egri (fit/spline) polilinya", "сглаженная (fit/spline) полилиния", "curve-fit/spline polyline");
        public static string BlockRotated => T(
            "blok burchak ostida qo‘yilgan — o‘lcham tashqi to‘rtburchak bo‘yicha olindi",
            "блок вставлен под углом — размер взят по габаритному прямоугольнику",
            "the block is inserted at an angle — the size is taken from its bounding box");
        public static string BlockNoRectangle => T(
            "blok ichida to‘rtburchak ramka topilmadi — blok chegarasi olindi",
            "в блоке нет прямоугольной рамки — взяты габариты блока",
            "no rectangular frame inside the block — the block extents are used");
        public static string BlockExtentsFailed(string error) => T("blok chegarasini o‘qib bo‘lmadi: ", "не удалось получить габариты блока: ", "could not read the block extents: ") + error;
        public static string BlockEmpty => T("blok bo‘sh", "блок пустой", "the block is empty");

        // ---- Sheet detection -----------------------------------------------------------------
        public static string DetectStart(object mode, string units, string scale) => F(T(
            "Aniqlash boshlandi: usul={0}, INSUNITS={1}, masshtab={2}",
            "Поиск листов: способ={0}, INSUNITS={1}, масштаб={2}",
            "Detection started: mode={0}, INSUNITS={1}, scale={2}"), mode, units, scale);
        public static string ScaleAutoShort => T("avto", "авто", "auto");
        public static string SelectionCancelled => T("Tanlash bekor qilindi.", "Выбор отменён.", "Selection cancelled.");
        public static string ManualSelectionLog(int objects, int frames, int rejected) => F(T(
            "Qo‘lda tanlash: {0} obyekt, {1} ramka, {2} ta ramka emas.",
            "Ручной выбор: объектов {0}, рамок {1}, не рамок {2}.",
            "Manual selection: {0} objects, {1} frames, {2} not frames."), objects, frames, rejected);
        public static string EnterBlockNames => T(
            "Blok nomlarini kiriting (masalan: Ramka_A3, Format*).",
            "Введите имена блоков (например: Рамка_A3, Формат*).",
            "Enter block names (for example: Frame_A3, Format*).");
        public static string BlockSkipped(string handle, string name, string why) => F(T("Blok {0} '{1}' o‘tkazildi: {2}", "Блок {0} '{1}' пропущен: {2}", "Block {0} '{1}' skipped: {2}"), handle, name, why);
        public static string ModelSpaceBlocks(string list) => T("Model fazosidagi bloklar: ", "Блоки в пространстве модели: ", "Blocks in model space: ") + list;
        public static string BlockNameMatches(string names, int count) => F(T(
            "Blok nomi bo‘yicha ({0}): {1} ta blok mos keldi.",
            "По имени блока ({0}): найдено блоков — {1}.",
            "By block name ({0}): {1} blocks matched."), names, count);
        public static string BlockNotFound(string names, string existing) => F(T(
            "“{0}” nomli blok topilmadi. Chizmadagi bloklar: {1}",
            "Блоки с именем «{0}» не найдены. Блоки в чертеже: {1}",
            "No block named “{0}” found. Blocks in the drawing: {1}"), names, existing);
        public static string SampleNotPicked => T("Namuna tanlanmadi.", "Образец не выбран.", "No sample picked.");
        public static string SampleNotFrame(string why) => F(T("Namuna ramka emas: {0}.", "Образец не является рамкой: {0}.", "The sample is not a frame: {0}."), why);
        public static string SampleBlock(string name) => F(T("blok “{0}”", "блок «{0}»", "block “{0}”"), name);
        public static string SampleRectangle(string layer) => F(T("to‘rtburchak, qatlam “{0}”", "прямоугольник, слой «{0}»", "rectangle, layer “{0}”"), layer);
        public static string SampleLog(string sample, string size, bool sizeOnly) => T("Namuna: ", "Образец: ", "Sample: ") + sample + ", " + size +
            (sizeOnly ? T(" (faqat shu o‘lcham)", " (только этот размер)", " (this size only)") : string.Empty);
        public static string RangeNotSelected => T("Hudud tanlanmadi.", "Область не выбрана.", "No area selected.");
        public static string SimilarFrames(int count) => F(T("Namunaga o‘xshash ramkalar: {0}.", "Рамок, похожих на образец: {0}.", "Frames similar to the sample: {0}."), count);
        public static string ObjectSkipped(string handle, string type, string why) => F(T("Obyekt {0} ({1}) o‘tkazildi: {2}", "Объект {0} ({1}) пропущен: {2}", "Object {0} ({1}) skipped: {2}"), handle, type, why);
        public static string ObjectUnreadable(string id, string error) => F(T("Obyekt {0} o‘qilmadi: {1}", "Не удалось прочитать объект {0}: {1}", "Could not read object {0}: {1}"), id, error);
        public static string FrameProblem(string handle, string size, string problem) => F(T("Ramka {0} {1}: {2}", "Рамка {0} {1}: {2}", "Frame {0} {1}: {2}"), handle, size, problem);
        public static string FoundCount(int count) => F(T("{0} ta list topildi", "Найдено листов: {0}", "{0} sheets found"), count);
        public static string RemovedNested(int count) => F(T("{0} ta ichki ramka chiqarildi", "исключено внутренних рамок: {0}", "{0} inner frames removed"), count);
        public static string RemovedDuplicates(int count) => F(T("{0} ta takror ramka chiqarildi", "исключено дубликатов: {0}", "{0} duplicate frames removed"), count);
        public static string RemovedContainers(int count) => F(T("{0} ta umumiy chegara chiqarildi", "исключено общих контуров: {0}", "{0} enclosing outlines removed"), count);
        public static string InvalidMarked(int count) => F(T(
            "{0} ta juda kichik/katta to‘rtburchak xato deb belgilandi",
            "слишком малых/больших прямоугольников отмечено как ошибка: {0}",
            "{0} too small/large rectangles marked as errors"), count);
        public static string InvalidIgnored(int count) => F(T(
            "{0} ta juda kichik/katta to‘rtburchak e’tiborsiz qoldirildi",
            "проигнорировано слишком малых/больших прямоугольников: {0}",
            "{0} too small/large rectangles ignored"), count);
        public static string NotFramesSuffix(int count) => F(T(" ({0} ta obyekt ramka emas)", " (не рамки: {0})", " ({0} objects are not frames)"), count);
        public static string FrameDebug(string handle, string name, object bounds, string paper, string format) => F(T(
            "Ramka {0} {1} {2} → {3} [{4}]", "Рамка {0} {1} {2} → {3} [{4}]", "Frame {0} {1} {2} → {3} [{4}]"), handle, name, bounds, paper, format);

        // ---- AUTOPDF workflow ----------------------------------------------------------------
        public static string RunFinished => T("===== AUTOPDF tugadi =====", "===== AUTOPDF завершён =====", "===== AUTOPDF finished =====");
        public static string UnitsStatus(string units) => T("Chizma birliklari: ", "Единицы чертежа: ", "Drawing units: ") + units + ". ";
        public static string SettingsSaved => T("Sozlamalar saqlandi.", "Настройки сохранены.", "Settings saved.");
        public static string DetectionFailed => T("Aniqlashda xato", "Ошибка при поиске листов", "Detection failed");
        public static string Cancelled => T("Bekor qilindi.", "Отменено.", "Cancelled.");
        public static string CheckLog(string note) => T("Tekshiruv: ", "Проверка: ", "Check: ") + note;
        public static string HighlightFailed(string error) => T("Listlarni chizmada ko‘rsatib bo‘lmadi: ", "Не удалось показать листы на чертеже: ", "Could not show the sheets on the drawing: ") + error;
        public static string CheckSummary(int ready, int crop, int warnings, int errors, string notes) => F(T(
            "Tekshiruv: {0} tayyor, {1} kesish bilan, {2} ogohlantirish, {3} xato. {4}",
            "Проверка: готово {0}, с обрезкой {1}, предупреждений {2}, ошибок {3}. {4}",
            "Check: {0} ready, {1} with crop, {2} warnings, {3} errors. {4}"), ready, crop, warnings, errors, notes);
        public static string ExportCancelled => T("Eksport bekor qilindi.", "Экспорт отменён.", "Export cancelled.");
        public static string ExistingPolicy(int count, object policy) => F(T("Mavjud fayllar: {0} — siyosat: {1}.", "Существующие файлы: {0} — действие: {1}.", "Existing files: {0} — policy: {1}."), count, policy);
        public static string ExportStart(int total, string folder, string plotter, string style, object method) => F(T(
            "Eksport: {0} ta list → {1} (plotter {2}, uslub {3}, usul {4}).",
            "Экспорт: листов {0} → {1} (плоттер {2}, стиль {3}, способ {4}).",
            "Export: {0} sheets → {1} (plotter {2}, style {3}, method {4})."), total, folder, plotter, style, method);
        public static string FileExists => T("fayl mavjud", "файл существует", "file exists");
        public static string EscPressed => T("Foydalanuvchi ESC bosdi — eksport to‘xtatildi.", "Нажата клавиша ESC — экспорт остановлен.", "ESC pressed — export stopped.");
        public static string CancelledInProgress => T(
            "Chop etish oynasida bekor qilindi — eksport to‘xtatildi.",
            "Отменено в окне печати — экспорт остановлен.",
            "Cancelled in the plot progress dialog — export stopped.");
        public static string StopPolicyApplied => T(
            "Xatolik siyosati: to‘xtatish — qolgan listlar chiqarilmadi.",
            "Режим «остановить при ошибке» — остальные листы не созданы.",
            "Error policy is “stop” — the remaining sheets were not created.");
        public static string PreviewFailed(string error) => F(T(
            "Ko‘rib chiqish oynasini ochib bo‘lmadi ({0}) — qolgan listlar avtomatik yaratiladi.",
            "Не удалось открыть окно просмотра ({0}) — остальные листы создаются автоматически.",
            "Could not open the preview window ({0}) — the remaining sheets are created automatically."), error);
        public static string PreviewCancelledAfter(string file) => F(T("Ko‘rib chiqishda bekor qilindi ({0} dan keyin).", "Отменено при просмотре (после {0}).", "Cancelled in the preview (after {0})."), file);
        public static string ConfirmedAll => T(
            "Tasdiqlandi — qolgan listlar avtomatik yaratiladi.",
            "Подтверждено — остальные листы создаются автоматически.",
            "Confirmed — the remaining sheets are created automatically.");
        public static string ExportStopped => T("Eksport to‘xtadi", "Экспорт прерван", "Export stopped");
        public static string ExportTotals(int total, int ok, int failed, int skipped, int cancelled, double seconds) => F(T(
            "Eksport yakuni: jami {0}, muvaffaqiyatli {1}, xato {2}, o‘tkazildi {3}, bekor {4} ({5:0.0} s).",
            "Итог экспорта: всего {0}, успешно {1}, ошибок {2}, пропущено {3}, отменено {4} ({5:0.0} с).",
            "Export result: total {0}, succeeded {1}, failed {2}, skipped {3}, cancelled {4} ({5:0.0} s)."), total, ok, failed, skipped, cancelled, seconds);
        public static string ExportStatus(int ok, int failed, string folder) => F(T(
            "Eksport: {0} ta PDF yaratildi, {1} ta xato. Papka: {2}",
            "Экспорт: создано PDF — {0}, ошибок — {1}. Папка: {2}",
            "Export: {0} PDFs created, {1} errors. Folder: {2}"), ok, failed, folder);
    }
}
