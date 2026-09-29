namespace AutoPDF.Core.Localization
{
    /// <summary>Texts of the registration window, the admin user list and the related log lines.</summary>
    public static partial class L
    {
        // ---- Registration window ------------------------------------------------------------------
        public static string RegTitle => T("Ro‘yxatdan o‘tish", "Регистрация", "Registration");

        public static string RegSubtitle => T(
            "Dasturdan foydalanish uchun bir marta ro‘yxatdan o‘ting",
            "Зарегистрируйтесь один раз, чтобы пользоваться программой",
            "Register once to use the program");

        public static string RegIntro => T(
            "AutoPDF Pro’dan foydalanish uchun ismingiz va e-pochtangizni kiriting. Ma’lumot dastur muallifiga bir marta yuboriladi.",
            "Чтобы пользоваться AutoPDF Pro, введите имя и e-mail. Данные один раз отправляются автору программы.",
            "To use AutoPDF Pro, enter your name and e-mail. The details are sent once to the program's author.");

        public static string RegName => T("Ism-familiya", "Имя и фамилия", "Full name");
        public static string RegEmail => T("E-pochta (Gmail)", "Почта (Gmail)", "E-mail (Gmail)");
        public static string RegPhone => T("Telefon (ixtiyoriy)", "Телефон (необязательно)", "Phone (optional)");

        public static string RegWhatIsSent => T(
            "Quyidagi ma’lumot muallifga yuboriladi:",
            "Автору будет отправлено следующее:",
            "The following is sent to the author:");

        public static string RegFieldComputer => T("Kompyuter", "Компьютер", "Computer");
        public static string RegFieldUser => T("Windows foydalanuvchisi", "Пользователь Windows", "Windows user");
        public static string RegFieldVersion => T("Versiya", "Версия", "Version");

        public static string RegConsent => T(
            "Roziman: yuqoridagi ma’lumotlar dastur muallifi (Kushmuratov Oybek)ga yuborilsin.",
            "Согласен(на): отправить указанные данные автору программы (Kushmuratov Oybek).",
            "I agree to send the above details to the program's author (Kushmuratov Oybek).");

        public static string RegSubmit => T("Ro‘yxatdan o‘tish", "Зарегистрироваться", "Register");
        public static string RegSending => T("Yuborilmoqda…", "Отправка…", "Sending…");

        // Validation
        public static string RegNeedName => T("Ismingizni kiriting.", "Введите имя.", "Please enter your name.");
        public static string RegNeedEmail => T("E-pochtangizni kiriting.", "Введите e-mail.", "Please enter your e-mail.");
        public static string RegBadEmail => T("E-pochta manzili noto‘g‘ri.", "Неверный адрес e-mail.", "The e-mail address is not valid.");
        public static string RegNeedConsent => T(
            "Davom etish uchun rozilikni belgilang.",
            "Отметьте согласие, чтобы продолжить.",
            "Please tick the consent box to continue.");

        // Network result
        public static string RegNetworkError => T(
            "Serverga ulanib bo‘lmadi. Internet aloqasini tekshiring.",
            "Не удалось связаться с сервером. Проверьте подключение к интернету.",
            "Could not reach the server. Please check your internet connection.");

        public static string RegServerError(string detail) => T(
            "Server xatosi: ", "Ошибка сервера: ", "Server error: ") + detail;

        public static string RegRetry => T("Qayta urinish", "Повторить", "Retry");

        public static string RegOfflineContinue => T(
            "Keyinroq yuborib, davom etish",
            "Отправить позже и продолжить",
            "Send later and continue");

        public static string RegOfflineNote => T(
            "Ma’lumot bu kompyuterda saqlanadi va internet paydo bo‘lganda avtomatik yuboriladi.",
            "Данные сохранятся на этом компьютере и будут отправлены автоматически при появлении интернета.",
            "The details are saved on this computer and sent automatically once the internet is available.");

        public static string RegMandatoryAbort => T(
            "Ro‘yxatdan o‘tmasdan AutoPDF’dan foydalanib bo‘lmaydi.",
            "Без регистрации пользоваться AutoPDF нельзя.",
            "AutoPDF cannot be used without registering.");

        public static string RegThanks => T("Rahmat! Ro‘yxatdan o‘tdingiz.", "Спасибо! Вы зарегистрированы.", "Thank you! You are registered.");

        // Log lines
        public static string RegSavedLog(string email, bool delivered) => F(T(
            "Ro‘yxatdan o‘tildi: {0} ({1})",
            "Регистрация: {0} ({1})",
            "Registered: {0} ({1})"), email, delivered
                ? T("yuborildi", "отправлено", "sent")
                : T("keyinroq", "позже", "pending"));

        public static string RegResentLog(string email) =>
            T("Ro‘yxatdan o‘tish qayta yuborildi: ", "Регистрация отправлена повторно: ", "Registration re-sent: ") + email;

        public static string RegResendFailedLog(string error) =>
            T("Qayta yuborishda xato: ", "Ошибка повторной отправки: ", "Registration re-send failed: ") + error;

        public static string RegAbortedLog =>
            T("AUTOPDF: ro‘yxatdan o‘tish talab qilinadi — foydalanuvchi bekor qildi.",
              "AUTOPDF: требуется регистрация — отменено пользователем.",
              "AUTOPDF: registration required — cancelled by user.");

        // ---- Admin: user list ---------------------------------------------------------------------
        public static string AdminUsersTitle => T("Foydalanuvchilar", "Пользователи", "Users");
        public static string AdminUsersSubtitle => T(
            "Ro‘yxatdan o‘tgan foydalanuvchilar (faqat admin)",
            "Зарегистрированные пользователи (только для админа)",
            "Registered users (admin only)");
        public static string AdminKey => T("Admin kaliti", "Ключ администратора", "Admin key");
        public static string AdminShow => T("Ko‘rsatish", "Показать", "Show");
        public static string AdminRemember => T("Bu kompyuterda eslab qolish", "Запомнить на этом компьютере", "Remember on this computer");
        public static string AdminLoading => T("Yuklanmoqda…", "Загрузка…", "Loading…");
        public static string AdminForbidden => T("Kalit noto‘g‘ri.", "Неверный ключ.", "Wrong key.");
        public static string AdminNoUsers => T("Hozircha foydalanuvchilar yo‘q.", "Пока нет пользователей.", "No users yet.");
        public static string AdminCount(int n) => F(T("{0} ta foydalanuvchi", "Пользователей: {0}", "{0} users"), n);
        public static string AdminNotConfigured => T(
            "Ro‘yxatdan o‘tish tizimi hali sozlanmagan.",
            "Система регистрации ещё не настроена.",
            "The registration system is not configured yet.");

        public static string ColNo => T("№", "№", "#");
        public static string ColDate => T("Sana", "Дата", "Date");
        public static string ColName => T("Ism", "Имя", "Name");
        public static string ColEmail => T("E-pochta", "Почта", "E-mail");
        public static string ColPhone => T("Telefon", "Телефон", "Phone");
        public static string ColComputer => T("Kompyuter", "Компьютер", "Computer");
        public static string ColWindows => T("Windows", "Windows", "Windows");
        public static string ColVersion => T("Versiya", "Версия", "Version");
    }
}
