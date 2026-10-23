"""Art for the main menu, written to Assets/Resources/Menu.

background.png  1920x1080 sky gradient, sun and two layers of rolling hills
cloud.png       soft white cloud that drifts across the sky
rukajja_wave.png  the waving Rukajja cut-out with a cleaned edge
play.png        white rounded play triangle for the big Play button
icons           Twemoji: settings, help, language, exit, trophy, coin

Usage: Tools/.venv/Scripts/python.exe -I Tools/make_menu_art.py
"""

import urllib.request
from pathlib import Path

import resvg_py
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Resources" / "Menu"
CUTOUT = ROOT / "Store" / "art" / "rukajja_wave_cutout.png"
TWEMOJI_SVG = "https://cdn.jsdelivr.net/gh/jdecked/twemoji@15.1.0/assets/svg/{code}.svg"

ICONS = {
    "icon_settings": "2699",
    "icon_help": "2753",
    "icon_language": "1f310",
    "icon_exit": "1f6aa",
    "icon_trophy": "1f3c6",
    "icon_coin": "1fa99",
}

W, H = 1920, 1080


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def background() -> Image.Image:
    # Drawn at twice the size and scaled down, so hill edges are smooth without blur halos.
    s = 2
    top, bottom = (120, 200, 255), (206, 240, 255)
    image = Image.new("RGB", (W * s, H * s))
    draw = ImageDraw.Draw(image)
    for y in range(H * s):
        draw.line([(0, y), (W * s, y)], fill=lerp(top, bottom, y / (H * s)))

    # The glow layer starts as transparent sun colour, not transparent black,
    # otherwise blurring it leaves a dark ring.
    glow = Image.new("RGBA", (W * s, H * s), (255, 244, 190, 0))
    ImageDraw.Draw(glow).ellipse((1440 * s, -140 * s, 1880 * s, 300 * s), fill=(255, 244, 190, 170))
    glow = glow.filter(ImageFilter.GaussianBlur(70 * s))
    image = Image.alpha_composite(image.convert("RGBA"), glow)

    draw = ImageDraw.Draw(image)
    draw.ellipse((1580 * s, 20 * s, 1760 * s, 200 * s), fill=(255, 221, 87, 255))

    for box, colour in (
        ((-400, 640, 1100, 1500), (150, 214, 120)),
        ((700, 600, 2400, 1500), (140, 206, 110)),
        ((-200, 790, 1300, 1600), (96, 184, 84)),
        ((900, 820, 2300, 1700), (84, 170, 74)),
    ):
        draw.ellipse(tuple(v * s for v in box), fill=colour + (255,))

    return image.resize((W, H), Image.LANCZOS).convert("RGB")


def cloud() -> Image.Image:
    # Transparent white, not black, so the blurred edge stays white.
    image = Image.new("RGBA", (420, 200), (255, 255, 255, 0))
    draw = ImageDraw.Draw(image)
    for box in ((20, 80, 180, 190), (110, 30, 290, 190), (230, 70, 400, 190), (60, 110, 360, 195)):
        draw.ellipse(box, fill=(255, 255, 255, 235))
    return image.filter(ImageFilter.GaussianBlur(2))


def play_triangle() -> Image.Image:
    size = 256
    image = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    draw = ImageDraw.Draw(image)
    points = [(70, 40), (70, 216), (220, 128)]
    draw.polygon(points, fill=(255, 255, 255, 255))
    return image.filter(ImageFilter.GaussianBlur(0.6))


def twemoji(code: str, size: int = 256) -> Image.Image:
    with urllib.request.urlopen(TWEMOJI_SVG.format(code=code), timeout=30) as response:
        svg = response.read().decode("utf-8")
    png = resvg_py.svg_to_bytes(svg_string=svg, width=size, height=size)
    from io import BytesIO
    return Image.open(BytesIO(bytes(png))).convert("RGBA")


def rukajja() -> Image.Image:
    """The cut-out keeps a faint pink glow and specks from the sheet's background.
    Dropping weak alpha removes the glow; an opening (erode, then dilate) removes
    specks without thinning the outline; a slight blur re-smooths the edge."""
    figure = Image.open(CUTOUT).convert("RGBA")
    alpha = figure.getchannel("A").point(lambda a: 0 if a < 110 else min(255, (a - 110) * 255 // 90))
    alpha = alpha.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.MaxFilter(3))
    figure.putalpha(alpha.filter(ImageFilter.GaussianBlur(0.7)))
    return figure


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    background().save(OUT / "background.png")
    cloud().save(OUT / "cloud.png")
    play_triangle().save(OUT / "play.png")
    rukajja().save(OUT / "rukajja_wave.png")

    for name, code in ICONS.items():
        target = OUT / f"{name}.png"
        if not target.exists():
            twemoji(code).save(target)

    print(f"menu art written to {OUT}")


if __name__ == "__main__":
    main()
