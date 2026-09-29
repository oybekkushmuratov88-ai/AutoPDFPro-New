#!/usr/bin/env python3
"""Generates the installer graphics (icon, welcome and header bitmaps) for AutoPDF Pro.

    python3 installer/art/make_art.py          (needs Pillow; output next to this script)

AutoPDFPro.ico  16..256 px   installer / uninstaller icon (and "Programs and Features")
welcome.bmp     164 x 314    left image of the Welcome / Finish pages (Modern UI 2)
header.bmp      150 x 57     header image of the other pages

The generated files are committed, so building the installer does not need Python.
"""
import io
import os
import struct
import sys

from PIL import Image, ImageDraw, ImageFont, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, "..", ".."))
PHOTO = os.path.join(REPO, "src", "AutoPDF.UI", "Assets", "author.jpg")
VERSION = sys.argv[1] if len(sys.argv) > 1 else "2.2.0"

# Same colours as the plug-in (src/AutoPDF.UI/Theme/Ui.cs)
HEADER_START = (0x1E, 0x3A, 0x5F)
HEADER_END = (0x25, 0x63, 0xEB)
PDF_RED = (0xDC, 0x26, 0x26)
LIGHT_BLUE = (0xBF, 0xDB, 0xFE)


def font(bold, size):
    candidates = [
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
        "C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
    ]
    for path in candidates:
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def lerp(a, b, t):
    return tuple(int(round(x + (y - x) * t)) for x, y in zip(a, b))


def gradient(w, h, start, end, diagonal=False):
    img = Image.new("RGB", (w, h))
    px = img.load()
    for y in range(h):
        for x in range(w):
            t = ((x / max(w - 1, 1)) + (y / max(h - 1, 1))) / 2 if diagonal else y / max(h - 1, 1)
            px[x, y] = lerp(start, end, t)
    return img


def draw_logo(size):
    """The product icon: blue tile, white drawing sheet with frame + title block, red PDF band."""
    ss = 8
    s = size * ss
    tile = gradient(s, s, HEADER_END, HEADER_START, diagonal=True).convert("RGBA")
    mask = Image.new("L", (s, s), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, s - 1, s - 1), radius=int(s * 0.22), fill=255)
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    img.paste(tile, (0, 0), mask)
    d = ImageDraw.Draw(img)
    simple = size <= 24

    # drawing sheet (landscape, like an A3 sheet)
    sx0, sy0, sx1, sy1 = s * 0.14, s * 0.20, s * 0.86, s * 0.72
    d.rounded_rectangle((sx0, sy0, sx1, sy1), radius=max(1, int(s * 0.025)), fill=(255, 255, 255, 255))
    if not simple:
        # sheet frame and title block
        fw = max(ss, int(s * 0.018))
        fx0, fy0, fx1, fy1 = sx0 + s * 0.07, sy0 + s * 0.045, sx1 - s * 0.035, sy1 - s * 0.045
        d.rectangle((fx0, fy0, fx1, fy1), outline=HEADER_START + (255,), width=fw)
        tx0, ty0 = fx1 - s * 0.26, fy1 - s * 0.12
        d.rectangle((tx0, ty0, fx1, fy1), outline=HEADER_START + (255,), width=fw)
        d.line((tx0, (ty0 + fy1) / 2, fx1, (ty0 + fy1) / 2), fill=HEADER_START + (255,), width=fw)
        # a few "drawing" strokes
        lw = max(ss, int(s * 0.022))
        d.line((fx0 + s * 0.06, fy1 - s * 0.07, fx0 + s * 0.20, fy0 + s * 0.08, fx0 + s * 0.33, fy1 - s * 0.10),
               fill=HEADER_END + (255,), width=lw, joint="curve")
    # PDF band
    bx0, by0, bx1, by1 = s * 0.10, s * 0.62, s * 0.90, s * 0.86
    d.rounded_rectangle((bx0, by0, bx1, by1), radius=int(s * 0.06), fill=PDF_RED + (255,))
    if size >= 32:
        f = font(True, int(s * 0.19))
        text = "PDF"
        tb = d.textbbox((0, 0), text, font=f)
        tw, th = tb[2] - tb[0], tb[3] - tb[1]
        d.text(((bx0 + bx1 - tw) / 2 - tb[0], (by0 + by1 - th) / 2 - tb[1]), text, font=f, fill=(255, 255, 255, 255))
    return img.resize((size, size), Image.LANCZOS)


def ico_bytes(images):
    """ICO with 32-bit BMP frames up to 64 px and a PNG frame for 256 px (standard Vista+ layout)."""
    entries, blobs = [], []
    for im in images:
        w, h = im.size
        if w >= 256:
            buf = io.BytesIO()
            im.save(buf, format="PNG")
            data = buf.getvalue()
        else:
            bgra = im.convert("RGBA").transpose(Image.FLIP_TOP_BOTTOM).tobytes("raw", "BGRA")
            mask_row = ((w + 31) // 32) * 4
            header = struct.pack("<IiiHHIIiiII", 40, w, h * 2, 1, 32, 0, len(bgra) + mask_row * h, 0, 0, 0, 0)
            data = header + bgra + b"\x00" * (mask_row * h)
        entries.append((w, h, data))
        blobs.append(data)
    out = io.BytesIO()
    out.write(struct.pack("<HHH", 0, 1, len(entries)))
    offset = 6 + 16 * len(entries)
    for w, h, data in entries:
        out.write(struct.pack("<BBBBHHII", w % 256, h % 256, 0, 0, 1, 32, len(data), offset))
        offset += len(data)
    for data in blobs:
        out.write(data)
    return out.getvalue()


def round_photo(diameter, ring, ss=4):
    photo = Image.open(PHOTO).convert("RGB")
    photo = ImageOps.fit(photo, (diameter * ss, diameter * ss), Image.LANCZOS, centering=(0.5, 0.4))
    total = (diameter + 2 * ring) * ss
    out = Image.new("RGBA", (total, total), (0, 0, 0, 0))
    d = ImageDraw.Draw(out)
    d.ellipse((0, 0, total - 1, total - 1), fill=(255, 255, 255, 255))
    mask = Image.new("L", photo.size, 0)
    ImageDraw.Draw(mask).ellipse((0, 0, photo.size[0] - 1, photo.size[1] - 1), fill=255)
    out.paste(photo, (ring * ss, ring * ss), mask)
    size = diameter + 2 * ring
    return out.resize((size, size), Image.LANCZOS)


def centered_text(d, y, text, f, fill, width):
    tb = d.textbbox((0, 0), text, font=f)
    d.text(((width - (tb[2] - tb[0])) / 2 - tb[0], y), text, font=f, fill=fill)


def welcome_bitmap():
    w, h = 164, 314
    img = gradient(w, h, HEADER_START, HEADER_END)
    logo = draw_logo(64)
    img.paste(logo, ((w - 64) // 2, 26), logo)
    d = ImageDraw.Draw(img)
    centered_text(d, 100, "AutoPDF Pro", font(True, 17), (255, 255, 255), w)
    centered_text(d, 124, "v" + VERSION + " · AutoCAD 2024", font(False, 10), LIGHT_BLUE, w)
    d.line((42, 152, w - 42, 152), fill=lerp(HEADER_START, (255, 255, 255), 0.45), width=1)
    avatar = round_photo(74, 3)
    img.paste(avatar, ((w - avatar.size[0]) // 2, 168), avatar)
    centered_text(d, 256, "Kushmuratov Oybek", font(True, 11), (255, 255, 255), w)
    centered_text(d, 274, "Muallif · Автор · Author", font(False, 9), LIGHT_BLUE, w)
    return img


def header_bitmap():
    w, h = 150, 57
    img = Image.new("RGB", (w, h), (255, 255, 255))
    logo = draw_logo(40)
    img.paste(logo, (w - 40 - 8, (h - 40) // 2), logo)
    d = ImageDraw.Draw(img)
    f1, f2 = font(True, 12), font(False, 9)
    for text, f, y, fill in (("AutoPDF Pro", f1, 15, HEADER_START), ("v" + VERSION, f2, 32, (0x4B, 0x55, 0x63))):
        tb = d.textbbox((0, 0), text, font=f)
        d.text((w - 40 - 16 - (tb[2] - tb[0]) - tb[0], y), text, font=f, fill=fill)
    return img


def main():
    sizes = [16, 20, 24, 32, 40, 48, 64, 256]
    with open(os.path.join(HERE, "AutoPDFPro.ico"), "wb") as fh:
        fh.write(ico_bytes([draw_logo(s) for s in sizes]))
    welcome_bitmap().save(os.path.join(HERE, "welcome.bmp"))
    header_bitmap().save(os.path.join(HERE, "header.bmp"))
    draw_logo(256).save(os.path.join(HERE, "logo-256.png"))
    print("written to", HERE)


if __name__ == "__main__":
    main()
