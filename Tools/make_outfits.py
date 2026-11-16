"""Outfits for the players: pictures and where each kid's head is in every frame.

Writes:
  Assets/Resources/Outfits/<item>.png      (Twemoji pictures: cap, crown, glasses, sunglasses, scarf)
  Assets/Resources/Outfits/anchors.json    (per kid and frame: head centre x, head top y, head width,
                                            in texture pixels with y from the bottom)

The head is found from the frame itself: the top part of the drawing, ignoring thin
tips (hair, raised hands) by taking the first row at least a third as wide as the
widest row there. The game places a hat on top, glasses across the eyes and a scarf
under the head from these numbers.

Usage:
  Tools/.venv/Scripts/python.exe Tools/make_outfits.py [--preview out.png]
"""

import json
import sys
import urllib.request
from pathlib import Path

import resvg_py
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Resources" / "Outfits"
CHARACTERS = ROOT / "Assets" / "Resources" / "Characters"
RUKAJJA = ROOT / "Assets" / "Art" / "Rukajja" / "Frames"
FRAMES = ["Idle", "Run_01", "Run_02", "Run_03", "Run_04", "Run_05", "Jump_Up", "Fall_Air", "Hit"]
TWEMOJI = "https://cdn.jsdelivr.net/gh/jdecked/twemoji@15.1.0/assets/svg/{code}.svg"
ITEMS = {"cap": "1f9e2", "crown": "1f451", "glasses": "1f453", "sunglasses": "1f576", "scarf": "1f9e3"}


def frame_path(kid: str, frame: str) -> Path:
    return RUKAJJA / f"Rukajja_{frame}.png" if kid == "rukajja" else CHARACTERS / kid / f"{frame}.png"


def head(image: Image.Image) -> tuple:
    alpha = image.split()[3]
    w, h = image.size
    px = alpha.load()
    rows = []
    for y in range(h):
        xs = [x for x in range(w) if px[x, y] > 100]
        rows.append((min(xs), max(xs)) if xs else None)
    filled = [y for y in range(h) if rows[y]]
    top, bottom = filled[0], filled[-1]
    region = range(top, top + int((bottom - top) * 0.3))
    widest = max(rows[y][1] - rows[y][0] for y in region if rows[y])
    head_top = next(y for y in region if rows[y] and rows[y][1] - rows[y][0] >= widest / 3)
    band = [y for y in region if rows[y] and rows[y][1] - rows[y][0] >= widest * 0.6]
    centre = sum((rows[y][0] + rows[y][1]) / 2 for y in band) / len(band)
    # The typical width of the head rows, so pigtails or a raised hand do not make it look huge.
    widths = sorted(rows[y][1] - rows[y][0] for y in region if y >= head_top and rows[y])
    return centre, h - head_top, widths[len(widths) // 2]


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for name, code in ITEMS.items():
        target = OUT / f"{name}.png"
        if not target.exists():
            with urllib.request.urlopen(TWEMOJI.format(code=code), timeout=30) as response:
                svg = response.read().decode("utf-8")
            target.write_bytes(bytes(resvg_py.svg_to_bytes(svg_string=svg, width=256, height=256)))

    kids = ["rukajja"] + sorted(p.name for p in CHARACTERS.iterdir() if p.is_dir() and p.name != "rukajja")
    anchors = []
    for kid in kids:
        for frame in FRAMES:
            path = frame_path(kid, frame)
            if not path.exists():
                continue
            x, top, width = head(Image.open(path).convert("RGBA"))
            if kid == "rukajja":
                # Her pigtails hang beside the face in most frames and widen every head row.
                width *= 0.72
            anchors.append({"key": f"{kid}/{frame}", "x": round(x, 1), "top": round(top, 1), "width": round(width, 1)})

    (OUT / "anchors.json").write_text(json.dumps({"anchors": anchors}, indent=1), encoding="utf-8")
    print(f"{len(ITEMS)} items, {len(anchors)} frames -> {OUT}")

    if "--preview" in sys.argv:
        preview(Path(sys.argv[sys.argv.index("--preview") + 1]), anchors)


def place(item: str, x: float, top: float, width: float, height: int) -> tuple:
    """(centre x, centre y from the top, size) of an item; mirrors OutfitRenderer in the game."""
    y_top = height - top
    if item in ("cap", "crown"):
        size = width * 1.0
        return x, y_top + size * 0.02, size
    if item in ("glasses", "sunglasses"):
        size = width * 0.78
        return x + width * 0.06, y_top + width * 0.55, size
    size = width * 0.6
    return x, y_top + width * 1.08 + size * 0.2, size


def preview(target: Path, anchors: list) -> None:
    keys = ["rukajja/Idle", "rukajja/Run_02", "rukajja/Jump_Up", "emir/Run_03", "hind/Jump_Up", "amina/Idle"]
    sheet = Image.new("RGBA", (300 * len(keys), 440), (250, 250, 240, 255))
    for i, key in enumerate(keys):
        a = next(n for n in anchors if n["key"] == key)
        kid, frame = key.split("/")
        img = Image.open(frame_path(kid, frame)).convert("RGBA")
        scale = 400 / img.height
        img = img.resize((int(img.width * scale), 400))
        canvas = Image.new("RGBA", (300, 440), (0, 0, 0, 0))
        canvas.alpha_composite(img, ((300 - img.width) // 2, 20))
        for item in ("cap", "sunglasses", "scarf"):
            cx, cy, size = place(item, a["x"] * scale, a["top"] * scale, a["width"] * scale, 400)
            pic = Image.open(OUT / f"{item}.png").convert("RGBA").resize((int(size), int(size)))
            canvas.alpha_composite(pic, (int((300 - img.width) // 2 + cx - size / 2), int(20 + cy - size / 2)))
        sheet.alpha_composite(canvas, (300 * i, 0))
    sheet.save(target)


if __name__ == "__main__":
    main()
