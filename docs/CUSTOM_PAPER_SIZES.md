# Custom paper sizes (630×297, 891×420, 1260×594 …)

`DWG To PDF.pc3` contains the ISO sizes (A0–A4, both orientations, "full bleed" = no margins) but not the
ГОСТ multiples or arbitrary sizes. AutoPDF Pro handles them in two ways.

## 1. Automatic (default, nothing to install): plot on a larger sheet and crop

For a 630×297 sheet the plug-in plots on *ISO full bleed A1 (841×594)*, centred, at the exact scale,
then sets the PDF page box to the centred 630×297 mm area. The result is a PDF whose page is exactly
630×297 mm with the drawing at the correct scale. The table shows these sheets as **Tayyor (kesish)**.

Setting: *Sozlamalar → Nostandart o‘lcham: kattaroq qog‘ozga chiqarib, PDF ni aniq o‘lchamga qirqish*
(on by default).

## 2. Optional: a plotter with the exact sizes

If you prefer real media of the exact size (e.g. for printing services that read the paper name), create a
**copy** of DWG To PDF with user-defined sizes. The original `DWG To PDF.pc3` is not touched.

In AutoCAD 2024 (Russian UI names in brackets):

1. `PLOTTERMANAGER` (Диспетчер плоттеров) opens the *Plotters* folder.
2. Copy `DWG To PDF.pc3` and rename the copy to **`AutoPDFPro PDF.pc3`**.
3. Double-click the copy → *Device and Document Settings* (Устройства и документы) →
   *Custom Paper Sizes* (Пользовательские размеры листа) → *Add…* (Добавить).
4. *Start from scratch* → width **630**, height **297**, units **Millimeters** → printable area margins
   **0 / 0 / 0 / 0** → name e.g. `A4x3 630x297` → Finish.
5. Repeat for every size you need (891×420, 1260×594, …). One orientation is enough.
6. *OK* → *Save*.
7. Run `AUTOPDFMEDIA`: the new sizes must appear in the list (the plug-in reads the list from AutoCAD at
   every `AUTOPDF` run).

The plug-in looks for the plotter named in *Sozlamalar → Maxsus o‘lchamlar plotteri* (default
`AutoPDFPro PDF.pc3`). For each sheet it uses, in this order: exact paper on the main plotter → exact paper
on this plotter → crop.

> Margins must be 0. A paper with unprintable margins would clip the frame edges and is ignored.
