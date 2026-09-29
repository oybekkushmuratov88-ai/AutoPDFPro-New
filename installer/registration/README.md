# Ro‘yxatdan o‘tish tizimini sozlash (Google Apps Script)

Bu papkadagi `Code.gs` — foydalanuvchilar ro‘yxatdan o‘tganda **sizning Gmailingizga xabar yuboradigan**
va ularni **Google Jadvalga yozadigan** kichik dastur. U **sizning Google akkauntingiz ichida** ishlaydi —
shuning uchun plaginda hech qanday parol yoki maxfiy kalit bo‘lmaydi. Plagin faqat quyida hosil qiladigan
umumiy `/exec` havolasini biladi (xuddi forma manzili kabi).

Bir marta, ~5–10 daqiqada sozlanadi.

## 1. Google Jadval yarating

1. Brauzerda [sheets.new](https://sheets.new) ni oching (yoki Google Drive → Yangi → Google Jadval).
2. Nomini bering, masalan **AutoPDF Pro — foydalanuvchilar**.

## 2. Skriptni joylashtiring

1. Jadvalda yuqoridagi menyudan **Extensions ▸ Apps Script** (Kengaytmalar ▸ Apps Script) ni tanlang.
2. Ochilgan oynadagi hamma kodni o‘chirib, `installer/registration/Code.gs` faylining **butun** mazmunini
   joylashtiring.
3. Faylning yuqorisidagi ikki qatorni o‘zgartiring:
   * `NOTIFY_EMAIL` — xabar keladigan pochta (hozir `Oybekkushmuratov88@gmail.com`).
   * `ADMIN_KEY` — **o‘zingiz o‘ylab topgan maxfiy so‘z** (masalan `Oybek-2026-#autopdf`). Bu so‘z
     foydalanuvchilar ro‘yxatini ko‘rish uchun so‘raladi. Hech kimga bermang.
4. **Saqlash** (diskcha belgisi yoki Ctrl+S).

## 3. Veb-ilova sifatida joylang (Deploy)

1. O‘ng yuqorida **Deploy ▸ New deployment** (Joylash ▸ Yangi joylash).
2. **Select type** (turini tanlash) → **Web app**.
3. Sozlamalar:
   * **Execute as:** *Me* (men) — xat sizning nomingizdan yuboriladi.
   * **Who has access:** *Anyone* (Hamma) — plagin (kirmasdan) yuborishi uchun.
4. **Deploy** → Google ruxsat so‘raydi: **Authorize access** → akkauntingizni tanlang →
   “Google hasn’t verified this app” chiqsa **Advanced ▸ Go to … (unsafe)** → **Allow**.
   (Bu o‘zingizning skriptingiz, xavfsiz.)
5. Chiqqan **Web app URL** — `…/exec` bilan tugaydi. Uni **nusxa oling**.

## 4. Havolani ulash

`…/exec` havolasini menga yuboring — men uni plaginga qo‘shib, yangi `Setup.exe` tayyorlab beraman.
(Texnik jihatdan u `src/AutoPDF.Core/Registration/RegistrationConfig.cs` dagi `Endpoint` qatoriga
yoziladi.) Havola qo‘shilmaguncha ro‘yxatdan o‘tish **o‘chirilgan** bo‘ladi va plagin avvalgidek ishlayveradi.

## 5. Tekshirish

Havolani brauzerda oching — `OK` chiqishi kerak. Ulangandan keyin:

* Foydalanuvchi AutoCAD’da birinchi marta `AUTOPDF` yozganda **Ro‘yxatdan o‘tish** oynasi chiqadi.
  Ism va e-pochtasini yozib, rozilikni belgilab tugmani bosadi.
* Sizga darhol **“yangi foydalanuvchi … shu kompyuterdan kirdi”** degan xat keladi, va u Google Jadvalga
  yoziladi.

## Foydalanuvchilarni ko‘rish (faqat admin)

Ikki yo‘l:

1. **Google Jadval** — barcha foydalanuvchilar shu yerda (faqat sizda).
2. **AutoCAD’da** — `AUTOPDFADMIN` buyrug‘ini yozing, `ADMIN_KEY` ni kiriting → ro‘yxat oynada chiqadi.
   “Bu kompyuterda eslab qolish” ni belgilasangiz, kalit shu kompyuterda saqlanadi.

## Savol-javob

* **Xat kelmadimi?** Spam papkasini tekshiring; `NOTIFY_EMAIL` to‘g‘riligiga ishonch hosil qiling.
* **Kodni o‘zgartirsangiz** (masalan `ADMIN_KEY`) — qayta **Deploy ▸ Manage deployments ▸ Edit ▸
  New version ▸ Deploy** qiling (havola o‘zgarmaydi).
* **Xavfsizlik:** havola ochiq bo‘lgani uchun begona odam soxta ro‘yxat yuborishi mumkin (kam ehtimol).
  Jadvaldan bunday qatorlarni o‘chirib tashlash mumkin. Foydalanuvchilar ro‘yxati esa `ADMIN_KEY` bilan
  himoyalangan.
* **Internet:** korxona kompyuterida internet cheklangan bo‘lsa, foydalanuvchi ma’lumoti shu kompyuterda
  saqlanib, internet paydo bo‘lganda avtomatik yuboriladi.
