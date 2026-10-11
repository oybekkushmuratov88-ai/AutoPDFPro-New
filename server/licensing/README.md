# Litsenziya serverini sozlash (Google Sheets + Apps Script)

`Code.gs` — AutoPDF Pro’ning **ruxsat (litsenziya) serveri**. U **sizning Google akkauntingiz ichida**
ishlaydi: so‘rovlarni Google Jadvalga yozadi, sizga xat yuboradi, ruxsat/rad etishni saqlaydi va
tasdiqlangan kompyuterlarga **imzolangan litsenziya** beradi. Imzo kaliti va maxfiy qiymatlar faqat skript
sozlamalarida (Script Properties) saqlanadi — plaginda hech qanday maxfiy narsa yo‘q.

Bir marta, ~10 daqiqada sozlanadi. (English summary at the end.)

## 1. Jadval va skript

1. 2.2 versiyada ishlatgan **o‘sha Google Jadvalni** oching (yangisi kerak bo‘lsa — [sheets.new](https://sheets.new)).
2. **Extensions ▸ Apps Script** (Kengaytmalar ▸ Apps Script).
3. `Code.gs` dagi **hamma** kodni o‘chiring va shu papkadagi `Code.gs` ning **butun** mazmunini joylashtiring.
   *Maslahat:* faylni Notepad’da oching → **Ctrl+A**, **Ctrl+C** → Apps Script’da **Ctrl+A**, **Ctrl+V**.
   Fayl faqat lotin harflari va belgilardan iborat (ASCII), shuning uchun nusxalashda buzilmaydi.
4. **Saqlash** (Ctrl+S).

## 2. `setup` ni bir marta ishga tushiring

1. Yuqoridagi ro‘yxatdan **`setup`** funksiyasini tanlang → **Run** (Ishga tushirish).
2. Google ruxsat so‘raydi: **Review permissions** → akkauntingiz → “Google hasn’t verified this app” chiqsa
   **Advanced ▸ Go to … (unsafe)** → **Allow**. (Bu o‘zingizning skriptingiz.)
3. *Execution log* da sozlamalar hisoboti chiqadi. `setup` quyidagilarni qiladi: **Requests** va **Log**
   varaqlarini yaratadi, havolalar uchun maxfiy kalitni (LINK_SECRET) yaratadi, Status ustuni uchun
   triggerni o‘rnatadi. Qayta ishga tushirish xavfsiz.

## 3. Imzo kalitini yarating

1. Jadval sahifasini yangilang (F5) — yuqorida **AutoPDF** menyusi paydo bo‘ladi.
2. **AutoPDF ▸ Generate signing keys** → **Generate and save**.
   Kalit **brauzeringizda** yaratiladi va to‘g‘ridan-to‘g‘ri skript sozlamalariga saqlanadi (hech qayerga
   nusxalash kerak emas). Oynada *Key ID* va **ochiq kalit** (public key) ko‘rinadi — ochiq kalitni ulashish
   xavfsiz.

## 4. Joylash (Deploy) — havola o‘zgarmaydi

1. **Deploy ▸ Manage deployments** → mavjud **Web app** → ✏️ **Edit** → **Version: New version** → **Deploy**.
   Havola (`…/exec`) **o‘zgarmaydi**, plagin uni allaqachon biladi.
   *(Agar “New deployment” qilsangiz, havola o‘zgaradi va plaginni yangi havola bilan qayta yig‘ish kerak
   bo‘ladi.)*
2. Sozlamalar: **Execute as: Me**, **Who has access: Anyone**.
3. `…/exec` havolasini brauzerda bir marta oching — **“AutoPDF Pro licence server is running.”** chiqishi kerak
   (skript o‘z havolasini xatlardagi tugmalar uchun eslab qoladi).
4. **AutoPDF ▸ Check configuration** — barcha qatorlar `OK` bo‘lishi kerak.

## 5. Administrator e-pochtasi

Yangi so‘rovlar haqidagi xatlar **Oybekkushmuratov88@gmail.com** ga boradi (`Code.gs` boshidagi
`DEFAULT_ADMIN_EMAIL`). O‘zgartirish: **AutoPDF ▸ Set administrator e-mail** — darhol kuchga kiradi, qayta
joylash shart emas.

## Kundalik ish

* Foydalanuvchi birinchi marta `AUTOPDF` yozganda ism, e-pochta va **o‘rnatish ID** yuboradi →
  **Requests** varag‘ida `pending` qator paydo bo‘ladi → sizga xat keladi.
* **Ruxsat berish / rad etish** — uch usul:
  1. xatdagi **Ruxsat berish / Approve** yoki **Rad etish / Reject** tugmasi → tasdiqlash sahifasi → tugma
     (havolalar 7 kun amal qiladi; havolani ochishning o‘zi hech narsani o‘zgartirmaydi);
  2. jadvalda qator(lar)ni belgilab **AutoPDF ▸ Approve selected rows / Reject selected rows**;
  3. **Status** ustunida qiymatni tanlash: `pending` / `approved` / `rejected`.
* Foydalanuvchiga natija haqida xat boradi (o‘z tilida), plagin ham xabar ko‘rsatadi.
* **Ruxsatni bekor qilish:** statusni `rejected` ga o‘zgartiring — keyingi tekshiruvda plagin to‘xtaydi.
* Qatorni o‘chirsangiz, foydalanuvchi qaytadan ro‘yxatdan o‘tishi kerak bo‘ladi.
* **Log** varag‘ida barcha amallar yoziladi.

## Sozlamalar (`Code.gs` boshida, `CFG`)

| Qiymat | Ma’nosi | Standart |
|---|---|---|
| `licenseTtlDays` | tasdiqlangan kompyuter internetsiz necha kun ishlaydi | 14 |
| `linkTtlDays` | xatdagi havolalar muddati | 7 |
| `maxRegisterPerInstall`, `maxRegisterPerEmail` | soatiga ro‘yxatdan o‘tish urinishlari | 5 |
| `maxNewPerHour` | soatiga yangi so‘rovlar (barchasi) | 60 |
| `maxStatusPerInstall` | soatiga holat tekshiruvlari (bitta o‘rnatish) | 120 |

Kodni o‘zgartirgandan keyin: **Deploy ▸ Manage deployments ▸ Edit ▸ New version ▸ Deploy**.

## Xavfsizlik

* **Script Properties** (Project Settings ▸ Script Properties) dagi `PRIVATE_KEY` va `LINK_SECRET` ni hech
  kimga bermang. Ular faqat sizning akkauntingizda.
* Jadvalni faqat ishonchli odamlar bilan ulashing — jadvalni tahrirlay oladigan kishi ruxsat bera oladi.
* Kalitni almashtirish: **AutoPDF ▸ Generate signing keys** ni qayta bajaring — kompyuterlar keyingi onlayn
  tekshiruvda yangi kalitga o‘tadi.
* Qat’iy rejim (ixtiyoriy): **AutoPDF ▸ Show public key** dagi ochiq kalitni plaginga “o‘rnatish” mumkin
  (`LicenseConfig.PinnedPublicKey`), keyin plagin faqat shu kalitni qabul qiladi.
* Gmail kuniga ~100 ta xat yuborishga ruxsat beradi; limit tugasa xat yuborilmaydi (Log’da yoziladi), lekin
  so‘rovlar baribir jadvalga tushadi.

## Muammolar

| Belgi | Sabab / yechim |
|---|---|
| Plaginda “Unexpected reply from the licence server” | Web app sifatida joylanmagan yoki *Who has access* ≠ *Anyone*. 4-qadamni takrorlang. |
| “HTTP 401/403” | *Who has access: Anyone* bo‘lishi kerak. |
| Ruxsat berildi, lekin plagin “tasdiqlanmadi” deydi | Imzo kaliti yo‘q: **AutoPDF ▸ Generate signing keys**, so‘ng plaginda **Hozir tekshirish**. |
| Xat kelmayapti | Spam papkasi; **Check configuration** dagi “E-mails left today”; administrator e-pochtasi to‘g‘riligi. |
| Xatdagi tugma “not valid (expired)” | Havola muddati o‘tgan — jadvaldan yoki menyudan foydalaning. |
| Xatlardagi havolalar ochilmaydi | `/exec` havolasini brauzerda bir marta oching (3-qadam), **Check configuration** da *Web app URL* `OK` bo‘lsin. |

## Testlar (dasturchilar uchun)

`node --test server/licensing/test/*.test.js` — `Code.gs` ni Google xizmatlarining soxta (mock)
nusxalari bilan tekshiradi (validatsiya, cheklovlar, imzolar, havolalar, egasi-tekshiruvi, jadval
xavfsizligi). `serve.js` esa C# mijozining uchidan-uchiga testida (`LicensingEndToEndTests`) ishlatiladi.

---

## English summary

1. Open the Google Sheet → *Extensions ▸ Apps Script* → replace `Code.gs` with this folder's `Code.gs` → save.
2. Run `setup` once and authorise (creates the *Requests* and *Log* sheets, the link secret and the Status
   trigger).
3. Reload the sheet → *AutoPDF ▸ Generate signing keys* (RSA-2048 generated in your browser with WebCrypto and
   stored in Script Properties; the public key is shown).
4. *Deploy ▸ Manage deployments ▸ Edit ▸ New version ▸ Deploy* (keeps the `/exec` URL; *Execute as: Me*,
   *Who has access: Anyone*). Open the `/exec` URL once; *AutoPDF ▸ Check configuration* must show `OK`.
5. Administrator e-mail: *AutoPDF ▸ Set administrator e-mail* (default `DEFAULT_ADMIN_EMAIL`).

Decide with the signed e-mail links (confirmation page), the *AutoPDF* menu or the Status column; set
`rejected` to revoke. Design and security: [docs/LICENSING.md](../../docs/LICENSING.md).
