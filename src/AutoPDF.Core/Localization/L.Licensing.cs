using AutoPDF.Core.Licensing;

namespace AutoPDF.Core.Localization
{
    /// <summary>Texts of the registration form, the licence status window and the approval notifications.</summary>
    public static partial class L
    {
        // ---- Registration form (first launch) ----------------------------------------------------------
        public static string RegTitle => T("Ro‘yxatdan o‘tish", "Регистрация", "Registration");

        public static string RegSubtitle => T(
            "Birinchi ishga tushirish: administrator ruxsatini so‘rash",
            "Первый запуск: запрос разрешения у администратора",
            "First launch: request access from the administrator");

        public static string RegIntro => T(
            "AutoPDF Pro’dan foydalanish uchun administrator ruxsati kerak. Ismingiz va e-pochtangizni kiriting — so‘rov administratorga yuboriladi. Ruxsat berilgach, AUTOPDF ishlaydi va sizga xat keladi.",
            "Для работы AutoPDF Pro нужно разрешение администратора. Введите имя и e-mail — запрос будет отправлен администратору. После одобрения AUTOPDF заработает, а вам придёт письмо.",
            "AutoPDF Pro needs the administrator’s approval. Enter your name and e-mail — the request is sent to the administrator. Once approved, AUTOPDF works and you receive an e-mail.");

        public static string RegName => T("Ism-familiya", "Имя и фамилия", "Full name");
        public static string RegEmail => T("E-pochta", "Почта", "E-mail");
        public static string LicInstallId => T("O‘rnatish ID", "ID установки", "Installation ID");
        public static string LicRequestId => T("So‘rov ID", "ID запроса", "Request ID");
        public static string LicCopy => T("Nusxa olish", "Копировать", "Copy");

        public static string RegConsent => T(
            "Ma’lumotlarni qayta ishlash uchun yuborishga roziman.",
            "Согласен(на) отправить данные для обработки.",
            "I agree to send the data for processing.");

        public static string RegSubmit => T("So‘rov yuborish", "Отправить запрос", "Send request");
        public static string RegSending => T("Yuborilmoqda…", "Отправка…", "Sending…");

        public static string RegNeedName => T("Ismingizni kiriting.", "Введите имя.", "Please enter your name.");
        public static string RegNeedEmail => T("E-pochtangizni kiriting.", "Введите e-mail.", "Please enter your e-mail.");
        public static string RegBadEmail => T("E-pochta manzili noto‘g‘ri.", "Неверный адрес e-mail.", "The e-mail address is not valid.");
        public static string RegNeedConsent => T(
            "Davom etish uchun rozilikni belgilang.",
            "Отметьте согласие, чтобы продолжить.",
            "Please tick the consent box to continue.");

        public static string RegNoInternet => T(
            "Serverga ulanib bo‘lmadi. Ro‘yxatdan o‘tish uchun internet kerak — aloqani tekshirib, qayta urining.",
            "Не удалось связаться с сервером. Для регистрации нужен интернет — проверьте подключение и повторите.",
            "Could not reach the server. Registration needs the internet — check the connection and try again.");

        public static string RegAbortedLog => T(
            "AUTOPDF: ro‘yxatdan o‘tish bekor qilindi.",
            "AUTOPDF: регистрация отменена.",
            "AUTOPDF: registration cancelled.");

        /// <summary>A server error code (see server/licensing/Code.gs) as a sentence for the user.</summary>
        public static string LicServerError(string code)
        {
            switch (code ?? string.Empty)
            {
                case "invalid_field:email": return RegBadEmail;
                case "invalid_field:name": return RegNeedName;
                case "invalid_field:consent": return RegNeedConsent;
                case "rate_limited":
                    return T("Juda ko‘p urinish. Birozdan so‘ng qayta urining.", "Слишком много попыток. Повторите немного позже.", "Too many attempts. Please try again a little later.");
                case "client_mismatch":
                case "machine_mismatch":
                    return T("Bu o‘rnatish boshqa kompyuterga tegishli edi — qaytadan ro‘yxatdan o‘ting.",
                             "Эта установка относилась к другому компьютеру — зарегистрируйтесь заново.",
                             "This installation belonged to another computer — please register again.");
                case "not_configured":
                    return T("Litsenziya serveri sozlanmagan. Administratorga murojaat qiling.",
                             "Сервер лицензий не настроен. Обратитесь к администратору.",
                             "The licence server is not configured. Please contact the administrator.");
                default:
                    return T("Server xatosi: ", "Ошибка сервера: ", "Server error: ") + code;
            }
        }

        // ---- Licence status window ---------------------------------------------------------------------
        public static string LicTitle => T("Litsenziya holati", "Статус лицензии", "Licence status");
        public static string LicStatus => T("Holat", "Статус", "Status");

        public static string LicStateName(LicenseState state)
        {
            switch (state)
            {
                case LicenseState.Pending: return T("Tasdiq kutilmoqda", "Ожидает одобрения", "Pending approval");
                case LicenseState.Approved: return T("Ruxsat berilgan", "Разрешено", "Approved");
                case LicenseState.Rejected: return T("Rad etilgan", "Отклонено", "Rejected");
                case LicenseState.Expired: return T("Tasdiqlash kerak", "Требуется проверка", "Needs verification");
                case LicenseState.Invalid: return T("Tasdiqlanmagan", "Не подтверждено", "Not verified");
                default: return T("Ro‘yxatdan o‘tilmagan", "Не зарегистрировано", "Not registered");
            }
        }

        public static string LicHeadline(LicenseState state)
        {
            switch (state)
            {
                case LicenseState.Pending: return T("Administrator tasdig‘i kutilmoqda", "Ожидается одобрение администратора", "Waiting for the administrator’s approval");
                case LicenseState.Approved: return T("Ruxsat berildi ✓", "Доступ разрешён ✓", "Access approved ✓");
                case LicenseState.Rejected: return T("So‘rov rad etildi", "Запрос отклонён", "Request not approved");
                case LicenseState.Expired: return T("Litsenziyani serverda tasdiqlash kerak", "Нужно подтвердить лицензию на сервере", "The licence must be confirmed with the server");
                case LicenseState.Invalid: return T("Litsenziya tasdiqlanmadi", "Лицензия не подтверждена", "The licence could not be verified");
                default: return T("Ro‘yxatdan o‘tilmagan", "Регистрация не выполнена", "Not registered");
            }
        }

        public static string LicExplanation(LicenseState state)
        {
            switch (state)
            {
                case LicenseState.Pending:
                    return T("So‘rovingiz administratorga yuborildi. Ruxsat berilmaguncha PDF yaratish o‘chirilgan. Bu oyna holatni o‘zi tekshirib turadi; natija e-pochtangizga ham keladi.",
                             "Ваш запрос отправлен администратору. До одобрения создание PDF отключено. Окно само проверяет статус; результат также придёт на e-mail.",
                             "Your request has been sent to the administrator. Creating PDFs is disabled until it is approved. This window keeps checking; you will also get an e-mail.");
                case LicenseState.Approved:
                    return T("AutoPDF Pro’dan foydalanishingiz mumkin.", "Можно пользоваться AutoPDF Pro.", "You can use AutoPDF Pro.");
                case LicenseState.Rejected:
                    return T("Administrator so‘rovni tasdiqlamadi, PDF yaratish o‘chirilgan. Savollar bo‘lsa, administratorga murojaat qiling.",
                             "Администратор не одобрил запрос, создание PDF отключено. По вопросам обратитесь к администратору.",
                             "The administrator did not approve the request; creating PDFs is disabled. Please contact the administrator if you have questions.");
                case LicenseState.Expired:
                    return T("Kompyuter uzoq vaqt server bilan bog‘lanmadi (yoki soat orqaga surilgan). Internetga ulanib, «Hozir tekshirish» tugmasini bosing.",
                             "Компьютер долго не связывался с сервером (или часы переведены назад). Подключитесь к интернету и нажмите «Проверить сейчас».",
                             "This computer has not reached the server for too long (or the clock was turned back). Connect to the internet and press “Check now”.");
                case LicenseState.Invalid:
                    return T("Saqlangan ruxsatning imzosi bu kompyuter uchun to‘g‘ri emas. Internetga ulanib, «Hozir tekshirish» tugmasini bosing.",
                             "Подпись сохранённого разрешения не подходит для этого компьютера. Подключитесь к интернету и нажмите «Проверить сейчас».",
                             "The stored approval is not validly signed for this computer. Connect to the internet and press “Check now”.");
                default:
                    return RegIntro;
            }
        }

        public static string LicCheckNow => T("Hozir tekshirish", "Проверить сейчас", "Check now");
        public static string LicChecking => T("Tekshirilmoqda…", "Проверка…", "Checking…");
        public static string LicContinue => T("Davom etish", "Продолжить", "Continue");
        public static string LicSubmitted => T("Yuborilgan", "Отправлено", "Submitted");
        public static string LicLastOnline => T("Server bilan oxirgi aloqa", "Последняя связь с сервером", "Last contact with the server");
        public static string LicValidOffline => T("Internetsiz ishlaydi … gacha", "Работает без интернета до", "Works offline until");
        public static string LicNever => T("hali yo‘q", "ещё не было", "not yet");

        public static string LicOffline(string detail) => T(
            "Server bilan aloqa yo‘q: ", "Нет связи с сервером: ", "No connection to the server: ") + detail;

        // ---- Notifications and the command line ----------------------------------------------------
        public static string LicToastApproved => T(
            "AutoPDF Pro: ruxsat berildi! Endi AUTOPDF buyrug‘idan foydalanishingiz mumkin.",
            "AutoPDF Pro: доступ разрешён! Теперь можно пользоваться командой AUTOPDF.",
            "AutoPDF Pro: access approved! You can now use the AUTOPDF command.");

        public static string LicToastRejected => T(
            "AutoPDF Pro: so‘rovingiz administrator tomonidan rad etildi.",
            "AutoPDF Pro: ваш запрос отклонён администратором.",
            "AutoPDF Pro: your request was not approved by the administrator.");

        public static string LicBlockedEcho(LicenseState state) => F(T(
            "\nAutoPDF Pro: PDF yaratish administrator ruxsatisiz o‘chirilgan (holat: {0}). Batafsil: AUTOPDFLICENSE\n",
            "\nAutoPDF Pro: создание PDF отключено без разрешения администратора (статус: {0}). Подробно: AUTOPDFLICENSE\n",
            "\nAutoPDF Pro: creating PDFs is disabled until the administrator approves (status: {0}). Details: AUTOPDFLICENSE\n"), LicStateName(state));
    }
}
