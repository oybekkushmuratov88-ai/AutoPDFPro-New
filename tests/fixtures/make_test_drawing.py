#!/usr/bin/env python3
"""Writes AutoPDF-test-sheets.dxf: six sheet frames for the in-CAD checklist (docs/TESTING.md).

    python3 tests/fixtures/make_test_drawing.py

Plain DXF R12 (opens in AutoCAD 2021-2026 and ZWCAD 2024-2026). Units: millimetres (INSUNITS is not stored
in R12; AutoPDF treats unitless drawings as millimetres). Frames are closed polylines on layer RAMKA, each
with a GOST-style inner border (which AutoPDF must ignore), a title block, diagonals and a label.
"""
import os

SHEETS = [
    # number, label, x, y, width, height (drawing units), scale
    (1, "A4 portrait 1:1", 0, 0, 210, 297, 1),
    (2, "A3 landscape 1:1", 300, 0, 420, 297, 1),
    (3, "A1 landscape 1:1", 800, 0, 841, 594, 1),
    (4, "A4x3 630x297 1:1 (crop)", 1750, 0, 630, 297, 1),
    (5, "A2 portrait 1:1", 2500, 0, 420, 594, 1),
    (6, "A3 landscape 1:100", 3100, 0, 42000, 29700, 100),
]

out = []


def emit(*pairs):
    for code, value in pairs:
        out.append(str(code))
        out.append(str(value))


def polyline(points, layer):
    emit((0, "POLYLINE"), (8, layer), (66, 1), (10, 0.0), (20, 0.0), (30, 0.0), (70, 1))
    for x, y in points:
        emit((0, "VERTEX"), (8, layer), (10, round(x, 4)), (20, round(y, 4)), (30, 0.0))
    emit((0, "SEQEND"), (8, layer))


def rect(x, y, w, h, layer):
    polyline([(x, y), (x + w, y), (x + w, y + h), (x, y + h)], layer)


def line(x1, y1, x2, y2, layer="0"):
    emit((0, "LINE"), (8, layer), (10, x1), (20, y1), (30, 0.0), (11, x2), (21, y2), (31, 0.0))


def text(x, y, h, s, layer="0"):
    emit((0, "TEXT"), (8, layer), (10, x), (20, y), (30, 0.0), (40, h), (1, s))


emit((0, "SECTION"), (2, "HEADER"), (9, "$ACADVER"), (1, "AC1009"), (9, "$MEASUREMENT"), (70, 1), (0, "ENDSEC"))
emit((0, "SECTION"), (2, "ENTITIES"))
for n, label, x, y, w, h, s in SHEETS:
    rect(x, y, w, h, "RAMKA")                                  # the sheet frame
    rect(x + 20 * s, y + 5 * s, w - 25 * s, h - 10 * s, "RAMKA")  # inner border: must not become a sheet
    tbx, tby = x + w - 5 * s - 185 * s, y + 5 * s               # title block 185 x 55 in the corner
    for yy in (0, 15, 30, 55):
        line(tbx, tby + yy * s, tbx + 185 * s, tby + yy * s)
    line(tbx, tby, tbx, tby + 55 * s)
    line(x + 20 * s, y + 5 * s, x + w - 5 * s, y + h - 5 * s)   # diagonals: show orientation and centring
    line(x + 20 * s, y + h - 5 * s, x + w - 5 * s, y + 5 * s)
    text(tbx + 5 * s, tby + 38 * s, 7 * s, "Sheet %d" % n)
    text(tbx + 5 * s, tby + 20 * s, 5 * s, label)
    text(x + 25 * s, y + h - 20 * s, 10 * s, "TOP %d" % n)      # must appear at the top of the PDF page
emit((0, "ENDSEC"), (0, "EOF"))

path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "AutoPDF-test-sheets.dxf")
with open(path, "w", newline="\r\n") as f:
    f.write("\n".join(out) + "\n")
print("written", path)
