;Language: Uzbek (1091)
;AutoPDF Pro: corrected Uzbek (Latin) translation of the Modern UI texts,
;based on the NSIS translation by Emil Garipov.

!insertmacro LANGFILE "Uzbek" = "O‘zbekcha" "O'zbekcha"

!ifdef MUI_WELCOMEPAGE
  ${LangFileString} MUI_TEXT_WELCOME_INFO_TITLE "$(^NameDA) o‘rnatish dasturiga xush kelibsiz"
  ${LangFileString} MUI_TEXT_WELCOME_INFO_TEXT "Ushbu dastur kompyuteringizga $(^NameDA) ni o‘rnatadi.$\r$\n$\r$\nO‘rnatishdan oldin boshqa dasturlarni yopish tavsiya etiladi.$\r$\n$\r$\n$_CLICK"
!endif

!ifdef MUI_UNWELCOMEPAGE
  ${LangFileString} MUI_UNTEXT_WELCOME_INFO_TITLE "$(^NameDA) ni o‘chirish dasturiga xush kelibsiz"
  ${LangFileString} MUI_UNTEXT_WELCOME_INFO_TEXT "Ushbu dastur $(^NameDA) ni kompyuteringizdan o‘chiradi.$\r$\n$\r$\nO‘chirishdan oldin $(^NameDA) ishlamayotganiga ishonch hosil qiling.$\r$\n$\r$\n$_CLICK"
!endif

!ifdef MUI_LICENSEPAGE
  ${LangFileString} MUI_TEXT_LICENSE_TITLE "Litsenziya shartnomasi"
  ${LangFileString} MUI_TEXT_LICENSE_SUBTITLE "$(^NameDA) ni o‘rnatishdan oldin litsenziya shartlari bilan tanishib chiqing."
  ${LangFileString} MUI_INNERTEXT_LICENSE_BOTTOM "Shartnoma shartlariga rozi bo‘lsangiz, davom etish uchun «Roziman» tugmasini bosing. $(^NameDA) ni o‘rnatish uchun shartnomani qabul qilishingiz kerak."
  ${LangFileString} MUI_INNERTEXT_LICENSE_BOTTOM_CHECKBOX "Shartnoma shartlariga rozi bo‘lsangiz, quyidagi belgini qo‘ying. $(^NameDA) ni o‘rnatish uchun shartnomani qabul qilishingiz kerak. $_CLICK"
  ${LangFileString} MUI_INNERTEXT_LICENSE_BOTTOM_RADIOBUTTONS "Shartnoma shartlariga rozi bo‘lsangiz, quyidagi birinchi variantni tanlang. $(^NameDA) ni o‘rnatish uchun shartnomani qabul qilishingiz kerak. $_CLICK"
!endif

!ifdef MUI_UNLICENSEPAGE
  ${LangFileString} MUI_UNTEXT_LICENSE_TITLE "Litsenziya shartnomasi"
  ${LangFileString} MUI_UNTEXT_LICENSE_SUBTITLE "$(^NameDA) ni o‘chirishdan oldin litsenziya shartlari bilan tanishib chiqing."
  ${LangFileString} MUI_UNINNERTEXT_LICENSE_BOTTOM "Shartnoma shartlariga rozi bo‘lsangiz, davom etish uchun «Roziman» tugmasini bosing. $(^NameDA) ni o‘chirish uchun shartnomani qabul qilishingiz kerak."
  ${LangFileString} MUI_UNINNERTEXT_LICENSE_BOTTOM_CHECKBOX "Shartnoma shartlariga rozi bo‘lsangiz, quyidagi belgini qo‘ying. $(^NameDA) ni o‘chirish uchun shartnomani qabul qilishingiz kerak. $_CLICK"
  ${LangFileString} MUI_UNINNERTEXT_LICENSE_BOTTOM_RADIOBUTTONS "Shartnoma shartlariga rozi bo‘lsangiz, quyidagi birinchi variantni tanlang. $(^NameDA) ni o‘chirish uchun shartnomani qabul qilishingiz kerak. $_CLICK"
!endif

!ifdef MUI_LICENSEPAGE | MUI_UNLICENSEPAGE
  ${LangFileString} MUI_INNERTEXT_LICENSE_TOP "Shartnomaning qolgan qismini ko‘rish uchun Page Down tugmasini bosing."
!endif

!ifdef MUI_COMPONENTSPAGE
  ${LangFileString} MUI_TEXT_COMPONENTS_TITLE "Komponentlarni tanlash"
  ${LangFileString} MUI_TEXT_COMPONENTS_SUBTITLE "$(^NameDA) ning qaysi qismlarini o‘rnatishni tanlang."
!endif

!ifdef MUI_UNCOMPONENTSPAGE
  ${LangFileString} MUI_UNTEXT_COMPONENTS_TITLE "Komponentlarni tanlash"
  ${LangFileString} MUI_UNTEXT_COMPONENTS_SUBTITLE "$(^NameDA) ning qaysi qismlarini o‘chirishni tanlang."
!endif

!ifdef MUI_COMPONENTSPAGE | MUI_UNCOMPONENTSPAGE
  ${LangFileString} MUI_INNERTEXT_COMPONENTS_DESCRIPTION_TITLE "Tavsif"
  !ifndef NSIS_CONFIG_COMPONENTPAGE_ALTERNATIVE
    ${LangFileString} MUI_INNERTEXT_COMPONENTS_DESCRIPTION_INFO "Tavsifini ko‘rish uchun sichqonchani komponent ustiga olib boring."
  !else
    ${LangFileString} MUI_INNERTEXT_COMPONENTS_DESCRIPTION_INFO "Tavsifini ko‘rish uchun komponentni tanlang."
  !endif
!endif

!ifdef MUI_DIRECTORYPAGE
  ${LangFileString} MUI_TEXT_DIRECTORY_TITLE "O‘rnatish joyini tanlash"
  ${LangFileString} MUI_TEXT_DIRECTORY_SUBTITLE "$(^NameDA) o‘rnatiladigan papkani tanlang."
!endif

!ifdef MUI_UNDIRECTORYPAGE
  ${LangFileString} MUI_UNTEXT_DIRECTORY_TITLE "O‘chirish joyini tanlash"
  ${LangFileString} MUI_UNTEXT_DIRECTORY_SUBTITLE "$(^NameDA) o‘chiriladigan papkani tanlang."
!endif

!ifdef MUI_INSTFILESPAGE
  ${LangFileString} MUI_TEXT_INSTALLING_TITLE "O‘rnatilmoqda"
  ${LangFileString} MUI_TEXT_INSTALLING_SUBTITLE "Iltimos, kuting: $(^NameDA) o‘rnatilmoqda."
  ${LangFileString} MUI_TEXT_FINISH_TITLE "O‘rnatish tugadi"
  ${LangFileString} MUI_TEXT_FINISH_SUBTITLE "O‘rnatish muvaffaqiyatli yakunlandi."
  ${LangFileString} MUI_TEXT_ABORT_TITLE "O‘rnatish to‘xtatildi"
  ${LangFileString} MUI_TEXT_ABORT_SUBTITLE "O‘rnatish oxiriga yetmadi."
!endif

!ifdef MUI_UNINSTFILESPAGE
  ${LangFileString} MUI_UNTEXT_UNINSTALLING_TITLE "O‘chirilmoqda"
  ${LangFileString} MUI_UNTEXT_UNINSTALLING_SUBTITLE "Iltimos, kuting: $(^NameDA) o‘chirilmoqda."
  ${LangFileString} MUI_UNTEXT_FINISH_TITLE "O‘chirish tugadi"
  ${LangFileString} MUI_UNTEXT_FINISH_SUBTITLE "O‘chirish muvaffaqiyatli yakunlandi."
  ${LangFileString} MUI_UNTEXT_ABORT_TITLE "O‘chirish to‘xtatildi"
  ${LangFileString} MUI_UNTEXT_ABORT_SUBTITLE "O‘chirish oxiriga yetmadi."
!endif

!ifdef MUI_FINISHPAGE
  ${LangFileString} MUI_TEXT_FINISH_INFO_TITLE "$(^NameDA) o‘rnatilishi yakunlandi"
  ${LangFileString} MUI_TEXT_FINISH_INFO_TEXT "$(^NameDA) kompyuteringizga o‘rnatildi.$\r$\n$\r$\nO‘rnatish dasturini yopish uchun «Tayyor» tugmasini bosing."
  ${LangFileString} MUI_TEXT_FINISH_INFO_REBOOT "$(^NameDA) o‘rnatilishini yakunlash uchun kompyuterni qayta ishga tushirish kerak. Hozir qayta ishga tushirilsinmi?"
!endif

!ifdef MUI_UNFINISHPAGE
  ${LangFileString} MUI_UNTEXT_FINISH_INFO_TITLE "$(^NameDA) o‘chirilishi yakunlandi"
  ${LangFileString} MUI_UNTEXT_FINISH_INFO_TEXT "$(^NameDA) kompyuteringizdan o‘chirildi.$\r$\n$\r$\nDasturni yopish uchun «Tayyor» tugmasini bosing."
  ${LangFileString} MUI_UNTEXT_FINISH_INFO_REBOOT "$(^NameDA) o‘chirilishini yakunlash uchun kompyuterni qayta ishga tushirish kerak. Hozir qayta ishga tushirilsinmi?"
!endif

!ifdef MUI_FINISHPAGE | MUI_UNFINISHPAGE
  ${LangFileString} MUI_TEXT_FINISH_REBOOTNOW "Hozir qayta ishga tushirish"
  ${LangFileString} MUI_TEXT_FINISH_REBOOTLATER "Keyinroq o‘zim qayta ishga tushiraman"
  ${LangFileString} MUI_TEXT_FINISH_RUN "$(^NameDA) ni &ishga tushirish"
  ${LangFileString} MUI_TEXT_FINISH_SHOWREADME "&Readme faylini ko‘rsatish"
  ${LangFileString} MUI_BUTTONTEXT_FINISH "&Tayyor"
!endif

!ifdef MUI_STARTMENUPAGE
  ${LangFileString} MUI_TEXT_STARTMENU_TITLE "«Start» menyusidagi papka"
  ${LangFileString} MUI_TEXT_STARTMENU_SUBTITLE "$(^NameDA) yorliqlari uchun «Start» menyusidagi papkani tanlang."
  ${LangFileString} MUI_INNERTEXT_STARTMENU_TOP "Dastur yorliqlari yaratiladigan «Start» menyusi papkasini tanlang. Yangi papka yaratish uchun uning nomini yozishingiz ham mumkin."
  ${LangFileString} MUI_INNERTEXT_STARTMENU_CHECKBOX "Yorliqlar yaratilmasin"
!endif

!ifdef MUI_UNCONFIRMPAGE
  ${LangFileString} MUI_UNTEXT_CONFIRM_TITLE "$(^NameDA) ni o‘chirish"
  ${LangFileString} MUI_UNTEXT_CONFIRM_SUBTITLE "$(^NameDA) ni kompyuterdan o‘chirish."
!endif

!ifdef MUI_ABORTWARNING
  ${LangFileString} MUI_TEXT_ABORTWARNING "$(^Name) ni o‘rnatishni bekor qilmoqchimisiz?"
!endif

!ifdef MUI_UNABORTWARNING
  ${LangFileString} MUI_UNTEXT_ABORTWARNING "$(^Name) ni o‘chirishni bekor qilmoqchimisiz?"
!endif

!ifdef MULTIUSER_INSTALLMODEPAGE
  ${LangFileString} MULTIUSER_TEXT_INSTALLMODE_TITLE "Foydalanuvchilarni tanlash"
  ${LangFileString} MULTIUSER_TEXT_INSTALLMODE_SUBTITLE "$(^NameDA) qaysi foydalanuvchilar uchun o‘rnatilishini tanlang."
  ${LangFileString} MULTIUSER_INNERTEXT_INSTALLMODE_TOP "$(^NameDA) ni faqat o‘zingiz uchun yoki ushbu kompyuterning barcha foydalanuvchilari uchun o‘rnatishni tanlang. $(^ClickNext)"
  ${LangFileString} MULTIUSER_INNERTEXT_INSTALLMODE_ALLUSERS "Kompyuterning barcha foydalanuvchilari uchun o‘rnatish"
  ${LangFileString} MULTIUSER_INNERTEXT_INSTALLMODE_CURRENTUSER "Faqat men uchun o‘rnatish"
!endif
