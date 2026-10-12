using System.Collections.Generic;
using System.Globalization;
using AutoPDF.Core.Models;

namespace AutoPDF.Core.Localization
{
    public enum UiLanguage
    {
        Uzbek,
        Russian,
        English
    }

    /// <summary>Product and author information shown in the UI, the bundle and the assembly metadata.</summary>
    public static class AppInfo
    {
        public const string Product = "AutoPDF Pro";
        public const string Author = "Kushmuratov Oybek";
        public const int CopyrightYear = 2026;

        /// <summary>The author's phone for requests and suggestions (About window, installer, "Apps" entry).</summary>
        public const string Phone = "+998 93 109 12 28";
    }

    /// <summary>
    /// Every user-visible text in three languages (O‘zbekcha / Русский / English). Each text is defined once
    /// with its three translations side by side, so a missing translation is impossible and the compiler
    /// checks every use. <see cref="Current"/> is set from the settings when a command starts.
    /// This file: common words, statuses, units, paper/PDF/settings messages and the About window.
    /// </summary>
    public static partial class L
    {
        public static UiLanguage Current { get; set; } = UiLanguage.Uzbek;

        public static IReadOnlyList<UiLanguage> All { get; } = new[] { UiLanguage.Uzbek, UiLanguage.Russian, UiLanguage.English };

        public static string T(string uz, string ru, string en)
        {
            switch (Current)
            {
                case UiLanguage.Russian: return ru;
                case UiLanguage.English: return en;
                default: return uz;
            }
        }

        /// <summary>Formats with the invariant culture (numbers such as 0.5 or 1:100 look the same everywhere).</summary>
        public static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

        /// <summary>The language's own name, e.g. "Русский" — shown the same in every UI language.</summary>
        public static string NativeName(UiLanguage language)
        {
            switch (language)
            {
                case UiLanguage.Russian: return "Русский";
                case UiLanguage.English: return "English";
                default: return "O‘zbekcha";
            }
        }

        // ---- Product --------------------------------------------------------------------------
        public const string AppTitle = AppInfo.Product;
        /// <summary>The running CAD application, e.g. "ZWCAD 2025" — set by the plug-in at start-up.</summary>
        public static string HostName { get; set; } = "AutoCAD / ZWCAD";

        public static string AppSubtitle => HostName + T(" · listlarni PDF ga eksport qilish", " · экспорт листов в PDF", " · export sheets to PDF");

        // ---- Common words -----------------------------------------------------------------------
        public static string Yes => T("Ha", "Да", "Yes");
        public static string No => T("Yo‘q", "Нет", "No");
        public const string Ok = "OK";
        public static string Cancel => T("Bekor qilish", "Отмена", "Cancel");
        public static string Close => T("Yopish", "Закрыть", "Close");
        public static string Save => T("Saqlash", "Сохранить", "Save");
        public static string Open => T("Ochish", "Открыть", "Open");
        public static string Choose => T("Tanlash…", "Выбрать…", "Browse…");
        public static string Sheet => T("List", "Лист", "Sheet");
        public static string Unknown => T("noma’lum", "неизвестно", "unknown");
        public static string NoneWord => T("yo‘q", "нет", "none");
        public static string Log => T("Jurnal", "Журнал", "Log");

        // ---- Sheet statuses ---------------------------------------------------------------------
        public static string StatusText(SheetStatus status)
        {
            switch (status)
            {
                case SheetStatus.Ready: return T("Tayyor", "Готов", "Ready");
                case SheetStatus.ReadyWithCrop: return T("Tayyor (kesish)", "Готов (обрезка)", "Ready (crop)");
                case SheetStatus.Warning: return T("Ogohlantirish", "Внимание", "Warning");
                case SheetStatus.Error: return T("Xato", "Ошибка", "Error");
                case SheetStatus.Waiting: return T("Navbatda", "В очереди", "Queued");
                case SheetStatus.Plotting: return T("Yaratilmoqda…", "Создаётся…", "Plotting…");
                case SheetStatus.Done: return T("Bajarildi", "Готово", "Done");
                case SheetStatus.DoneWithWarning: return T("Bajarildi (ogohl.)", "Готово (с замеч.)", "Done (warning)");
                case SheetStatus.Failed: return T("Xatolik", "Сбой", "Failed");
                case SheetStatus.Skipped: return T("O‘tkazildi", "Пропущен", "Skipped");
                case SheetStatus.Cancelled: return T("Bekor qilindi", "Отменён", "Cancelled");
                default: return status.ToString();
            }
        }

        // ---- Drawing units (INSUNITS) -------------------------------------------------------------
        public static string UnitsUnitless => T("birliksiz (mm deb olinadi)", "без единиц (считается мм)", "unitless (treated as mm)");
        public static string UnitsInches => T("dyuym", "дюймы", "inches");
        public static string UnitsFeet => T("fut", "футы", "feet");
        public static string UnitsMm => T("mm", "мм", "mm");
        public static string UnitsCm => T("sm", "см", "cm");
        public static string UnitsM => T("m", "м", "m");
        public static string UnitsDm => T("dm", "дм", "dm");

        // ---- Paper size detection -----------------------------------------------------------------
        public static string PaperInvalidSize => T(
            "Ramka o‘lchami noto‘g‘ri (nol yoki manfiy).",
            "Неверный размер рамки (ноль или отрицательный).",
            "Invalid frame size (zero or negative).");

        public static string PaperTooSmall(object paper, double minMm) => F(T(
            "List juda kichik: {0} (minimal qisqa tomon {1:0} mm).",
            "Лист слишком мал: {0} (минимальная короткая сторона {1:0} мм).",
            "Sheet too small: {0} (minimum short side {1:0} mm)."), paper, minMm);

        public static string PaperTooLarge(object paper, double maxMm) => F(T(
            "List juda katta: {0} (maksimal uzun tomon {1:0} mm).",
            "Лист слишком велик: {0} (максимальная длинная сторона {1:0} мм).",
            "Sheet too large: {0} (maximum long side {1:0} mm)."), paper, maxMm);

        public static string PaperUnknown => T("Qog‘oz o‘lchami aniqlanmagan.", "Формат бумаги не определён.", "Paper size not determined.");
        public static string PlanCrop => T("kesish", "обрезка", "crop");

        // ---- PDF file checks ------------------------------------------------------------------
        public static string PdfNotCreated(string path) => T("PDF fayl yaratilmadi: ", "PDF-файл не создан: ", "The PDF file was not created: ") + path;
        public static string PdfEmpty => T("PDF fayl bo‘sh (0 bayt).", "PDF-файл пустой (0 байт).", "The PDF file is empty (0 bytes).");
        public static string PdfUnreadable(string message) => T("PDF faylni o‘qib bo‘lmadi: ", "Не удалось прочитать PDF-файл: ", "Could not read the PDF file: ") + message;
        public static string PdfNoHeader => T("Fayl PDF emas (%PDF- sarlavhasi yo‘q).", "Файл не является PDF (нет заголовка %PDF-).", "Not a PDF file (no %PDF- header).");
        public static string PdfIncomplete => T("PDF fayl to‘liq yozilmagan (%%EOF yo‘q).", "PDF-файл записан не полностью (нет %%EOF).", "The PDF file is incomplete (no %%EOF).");
        public static string PdfNoPages => T("PDF faylda sahifa yo‘q.", "В PDF-файле нет страниц.", "The PDF file has no pages.");

        // ---- Settings file ----------------------------------------------------------------------
        public static string SettingsMissing(string path) => T(
            "Sozlamalar fayli yo‘q — standart sozlamalar ishlatiladi: ",
            "Файл настроек не найден — используются настройки по умолчанию: ",
            "No settings file — using the defaults: ") + path;

        public static string SettingsBroken(string error, string backup) => F(T(
            "Sozlamalar fayli o‘qilmadi ({0}). Standart sozlamalar olindi, eski fayl: {1}",
            "Не удалось прочитать файл настроек ({0}). Загружены настройки по умолчанию, старый файл: {1}",
            "Could not read the settings file ({0}). Defaults loaded, old file: {1}"), error, backup);

        public static string SettingsSaveFailed(string error) => T("Sozlamalarni saqlab bo‘lmadi: ", "Не удалось сохранить настройки: ", "Could not save the settings: ") + error;

        public static string NoFreeFileName(string path) => T("Bo‘sh fayl nomi topilmadi: ", "Не найдено свободное имя файла: ", "No free file name found: ") + path;

        // ---- About / author ---------------------------------------------------------------------
        public static string About => T("Dastur haqida", "О программе", "About");
        public static string AboutTooltip => T("Dastur va muallif haqida", "О программе и авторе", "About the program and its author");
        public static string AuthorRole => T("Dastur muallifi va ishlab chiquvchisi", "Автор и разработчик программы", "Author and developer");

        public static string AboutDescription => T(
            "AutoCAD / ZWCAD chizmasidagi listlarni (ramkalarni) avtomatik topib, har birini to‘g‘ri qog‘oz o‘lchami, masshtab va yo‘nalish bilan alohida PDF faylga chiqaradi.",
            "Автоматически находит листы (рамки) на чертеже AutoCAD / ZWCAD и сохраняет каждый в отдельный PDF с правильным форматом, масштабом и ориентацией.",
            "Automatically finds the sheets (frames) in an AutoCAD / ZWCAD drawing and exports each one to its own PDF with the correct paper size, scale and orientation.");

        public static string VersionText(string version) => T("Versiya ", "Версия ", "Version ") + version;
        public static string Compatibility => T(CompatibilityHosts + " · Windows 10/11 (64-bit)", CompatibilityHosts + " · Windows 10/11 (64 бит)", CompatibilityHosts + " · Windows 10/11 (64-bit)");
        public const string CompatibilityHosts = "AutoCAD 2021–2026 · ZWCAD 2024–2026";

        public static string Copyright => F(T(
            "© {0} {1}. Barcha huquqlar himoyalangan.",
            "© {0} {1}. Все права защищены.",
            "© {0} {1}. All rights reserved."), AppInfo.CopyrightYear, AppInfo.Author);

        public static string ContactLine => T(
            "Taklif va murojaatlar uchun: ",
            "Предложения и пожелания: ",
            "Requests and suggestions: ") + AppInfo.Phone;
    }
}
