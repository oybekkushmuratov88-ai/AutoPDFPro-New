using AutoPDF.Core.Models;

namespace AutoPDF.Core.Localization
{
    /// <summary>All user-visible texts (Uzbek, Latin script). One place to review or translate.</summary>
    public static class Uz
    {
        public const string AppTitle = "AutoPDF Pro";
        public const string AppSubtitle = "AutoCAD 2024 · listlarni PDF ga eksport qilish";

        // ---- Main window -------------------------------------------------------------------
        public const string DetectionMethod = "Aniqlash usuli";
        public const string DetectManual = "Listlarni qo‘lda tanlash";
        public const string DetectBlockName = "Blok nomi bo‘yicha topish";
        public const string DetectSample = "Namuna ramka bo‘yicha (butun chizma)";
        public const string DetectSampleRange = "Namuna ramka + diapazon (qisman tanlash)";
        public const string BlockNamesLabel = "Blok nomlari (vergul bilan, * va ? mumkin):";
        public const string SortDirection = "Saralash yo‘nalishi";
        public const string SortLeftToRight = "Chapdan o‘ngga";
        public const string SortTopToBottom = "Yuqoridan pastga";
        public const string SortLeftToRightHint = "Qator-qator: yuqori qatordan boshlab, har qator chapdan o‘ngga";
        public const string SortTopToBottomHint = "Ustun-ustun: chap ustundan boshlab, har ustun yuqoridan pastga";
        public const string StartNumber = "Boshlang‘ich raqam";
        public const string OutputFolder = "Saqlash papkasi";
        public const string Choose = "Tanlash…";
        public const string Open = "Ochish";
        public const string FoundSheets = "Topilgan listlar";
        public const string Scan = "Skanerlash";
        public const string Check = "Tekshirish";
        public const string CreatePdf = "PDF yaratish";
        public const string Settings = "Sozlamalar";
        public const string ScanHint = "Tanlangan usul bo‘yicha chizmadan listlarni topish";
        public const string CheckHint = "Listlarni chizmada raqamlab ko‘rsatish va qog‘oz/plotterni tekshirish";
        public const string CreatePdfHint = "Har bir listni alohida PDF faylga chiqarish (01.pdf, 02.pdf …)";

        public const string ColNumber = "#";
        public const string ColBlock = "Blok";
        public const string ColWidth = "Eni";
        public const string ColHeight = "Bo‘yi";
        public const string ColPaper = "Qog‘oz";
        public const string ColStatus = "Holat";

        public const string NoSheetsYet = "Listlar hali aniqlanmagan. Usulni tanlab «Skanerlash» tugmasini bosing.";
        public const string NoSheetsFound = "Hech qanday list topilmadi.";
        public const string ChooseFolderFirst = "Avval saqlash papkasini tanlang.";
        public const string FolderNotWritable = "Papkaga yozib bo‘lmadi:";
        public const string ScanFirst = "Avval listlarni aniqlang: usulni tanlab «Skanerlash» tugmasini bosing.";

        // ---- Statuses ----------------------------------------------------------------------
        public static string StatusText(SheetStatus status)
        {
            switch (status)
            {
                case SheetStatus.Ready: return "Tayyor";
                case SheetStatus.ReadyWithCrop: return "Tayyor (kesish)";
                case SheetStatus.Warning: return "Ogohlantirish";
                case SheetStatus.Error: return "Xato";
                case SheetStatus.Waiting: return "Navbatda";
                case SheetStatus.Plotting: return "Yaratilmoqda…";
                case SheetStatus.Done: return "Bajarildi";
                case SheetStatus.DoneWithWarning: return "Bajarildi (ogohl.)";
                case SheetStatus.Failed: return "Xatolik";
                case SheetStatus.Skipped: return "O‘tkazildi";
                case SheetStatus.Cancelled: return "Bekor qilindi";
                default: return status.ToString();
            }
        }

        // ---- Preview -----------------------------------------------------------------------
        public const string PreviewTitle = "PDF ko‘rib chiqish";
        public const string PreviewCancel = "Bekor qilish";
        public const string PreviewConfirm = "Tasdiqlash";
        public const string PreviewConfirmAll = "Tasdiqlash va qolganlarini avtomatik yaratish";
        public const string PreviewOpenExternal = "Tashqi dasturda ochish";
        public const string PreviewUnavailable = "Ko‘rish imkoni yo‘q — faylni tashqi dasturda oching.";
        public const string PreviewCancelHint = "Eksportni to‘xtatish (yaratilgan fayllar saqlanib qoladi)";
        public const string PreviewConfirmHint = "Keyingi listni yaratib, uni ham ko‘rsatish";
        public const string PreviewConfirmAllHint = "Qolgan barcha listlarni ko‘rsatmasdan ketma-ket yaratish";

        // ---- Settings ----------------------------------------------------------------------
        public const string SettingsTitle = "Sozlamalar";
        public const string GroupPlot = "Chop etish (plot)";
        public const string GroupDetection = "Aniqlash";
        public const string GroupExport = "Eksport";
        public const string GroupDiagnostics = "Diagnostika";
        public const string Plotter = "Plotter (PC3):";
        public const string CustomSizesPlotter = "Maxsus o‘lchamlar plotteri (ixtiyoriy):";
        public const string StyleTable = "Plot uslubi jadvali:";
        public const string UseMonochrome = "Plot uslubi jadvalini qo‘llash (monochrome.ctb — oq-qora)";
        public const string PlotWithStyles = "Plot uslublari bilan chop etish";
        public const string PlotLineweights = "Chiziq qalinliklarini chop etish";
        public const string ScaleLineweights = "Chiziq qalinligini masshtablash";
        public const string CenterPlot = "Chizmani markazlashtirish";
        public const string Scale = "Masshtab:";
        public const string ScaleAuto = "Avtomatik";
        public const string AllowCrop = "Nostandart o‘lcham: kattaroq qog‘ozga chiqarib, PDF ni aniq o‘lchamga qirqish";
        public const string PlotMethod = "Plot usuli:";
        public const string PlotMethodAuto = "Avtomatik (API, kerak bo‘lsa -PLOT)";
        public const string PlotMethodApi = "Faqat API (PlotEngine)";
        public const string PlotMethodCmd = "Faqat -PLOT buyrug‘i";
        public const string SnapTolerance = "O‘lcham tolerantligi (mm):";
        public const string MinSheetSize = "Minimal list tomoni (mm):";
        public const string CustomFormats = "Qo‘shimcha formatlar (mm):";
        public const string CustomFormatsHint = "Masalan: 630x297; 891x420; 1260x594";
        public const string SampleMatchLayer = "Namuna: faqat shu qatlamdagi (sloy) ramkalar";
        public const string SampleMatchSize = "Namuna: faqat bir xil o‘lchamdagi ramkalar";
        public const string FileNamePattern = "Fayl nomi shabloni:";
        public const string FileNamePatternHint = "{N} – raqam (01, 02 …), {DWG} – chizma nomi, {FORMAT} – format (A4, A3x3 …)";
        public const string Overwrite = "Fayl mavjud bo‘lsa:";
        public const string OverwriteAsk = "So‘rash";
        public const string OverwriteReplace = "Ustiga yozish";
        public const string OverwriteRename = "Yangi nom bilan (01_1.pdf)";
        public const string OverwriteSkip = "O‘tkazib yuborish";
        public const string OnError = "Xatolik bo‘lganda:";
        public const string OnErrorContinue = "Davom etish (keyingi listga o‘tish)";
        public const string OnErrorStop = "To‘xtatish";
        public const string PreviewEachSheet = "Har bir PDF dan keyin ko‘rib chiqish (tasdiqlash oynasi)";
        public const string OpenFolderWhenDone = "Tugagach papkani ochish";
        public const string PageTolerance = "PDF sahifa tolerantligi (mm):";
        public const string VerboseLog = "Batafsil jurnal (log)";
        public const string OpenLogFolder = "Jurnal papkasini ochish";
        public const string Save = "Saqlash";
        public const string Cancel = "Bekor qilish";
        public const string Close = "Yopish";

        // ---- Summary -----------------------------------------------------------------------
        public const string SummaryTitle = "Eksport yakunlandi";
        public const string SummaryTotal = "Jami listlar";
        public const string SummarySuccess = "Muvaffaqiyatli";
        public const string SummaryWarnings = "Ogohlantirish bilan";
        public const string SummaryFailed = "Xatolik";
        public const string SummarySkipped = "O‘tkazildi";
        public const string SummaryCancelled = "Bekor qilindi";
        public const string SummaryErrors = "Xatolar va ogohlantirishlar";
        public const string OpenFolder = "Papkani ochish";
        public const string OpenLog = "Jurnalni ochish";

        // ---- Dialog buttons ----------------------------------------------------------------
        public const string Yes = "Ha";
        public const string No = "Yo‘q";
        public const string Ok = "OK";

        public const string ExistingFilesQuestion =
            "Saqlash papkasida {0} ta PDF fayl allaqachon mavjud.\n\n" +
            "Ha — ustiga yozish\nYo‘q — yangi nom bilan saqlash (01_1.pdf)\nBekor qilish — eksportni to‘xtatish";

        // ---- Command line ------------------------------------------------------------------
        public const string PromptSelectFrames = "\nList ramkalarini tanlang (to‘rtburchak polilinya yoki blok): ";
        public const string PromptSelectSample = "\nNamuna ramkani tanlang (to‘rtburchak polilinya yoki blok): ";
        public const string PromptSampleReject = "\nFaqat yopiq to‘rtburchak polilinya yoki blok tanlang.";
        public const string PromptSelectRange = "\nQidiriladigan hududni tanlang (ramka bilan o‘rab oling): ";
        public const string PromptReview = "\nListlar raqamlandi. Chizmani ko‘zdan kechiring (zoom/pan ishlaydi), so‘ng ENTER bosing: ";
    }
}
