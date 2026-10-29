"""Builds the app icon set and the Google Play feature graphic.

Inputs:  Store/art/rukajja_wave_cutout.png (cut out of the Rukajja sheet)
         Assets/Resources/Words/World1/Pictures/apple.png
Outputs: Assets/Art/AppIcon/icon_512.png          legacy and round launcher icon
         Assets/Art/AppIcon/adaptive_background.png  Android 8+ adaptive layers (432 px)
         Assets/Art/AppIcon/adaptive_foreground.png
         Store/play/icon_512.png                  Play Console hi-res icon
         Store/play/feature_graphic_1024x500.png  Play Console feature graphic

Usage: Tools/.venv/Scripts/python.exe -I Tools/make_store_art.py
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
CUTOUT = ROOT / "Store" / "art" / "rukajja_wave_cutout.png"
APPLE = ROOT / "Assets" / "Resources" / "Words" / "World1" / "Pictures" / "apple.png"
COW = ROOT / "Assets" / "Resources" / "Words" / "World2" / "Pictures" / "cow.png"
BANANA = ROOT / "Assets" / "Resources" / "Words" / "World1" / "Pictures" / "banana.png"
PARROT = ROOT / "Assets" / "Resources" / "Words" / "Icons" / "parrot.png"
TITLE_FONT = ROOT / "Assets" / "Resources" / "Fonts" / "TitanOne-Regular.ttf"
ICON_DIR = ROOT / "Assets" / "Art" / "AppIcon"
PLAY_DIR = ROOT / "Store" / "play"

SKY = (126, 211, 247)
SKY_LIGHT = (196, 236, 252)
HILL = (104, 196, 92)
HILL_DARK = (78, 165, 70)
FONT_BOLD = "C:/Windows/Fonts/segoeuib.ttf"


def clean_cutout(image: Image.Image) -> Image.Image:
    """Shrinks the alpha edge by a pixel and softens it, removing the sheet's coloured halo."""
    alpha = image.getchannel("A").filter(ImageFilter.MinFilter(5)).filter(ImageFilter.GaussianBlur(0.8))
    cleaned = image.copy()
    cleaned.putalpha(alpha)
    return cleaned


def background(size: int) -> Image.Image:
    """Sky with a soft sun and a green hill, like Grandma's Garden."""
    canvas = Image.new("RGBA", (size, size), SKY)
    draw = ImageDraw.Draw(canvas)
    draw.ellipse((-0.25 * size, -0.3 * size, 0.55 * size, 0.5 * size), fill=SKY_LIGHT)
    draw.ellipse((-0.35 * size, 0.72 * size, 1.35 * size, 1.9 * size), fill=HILL)
    draw.ellipse((0.35 * size, 0.8 * size, 1.6 * size, 2.0 * size), fill=HILL_DARK)
    return canvas


def word_bubble(size: int, picture: Path = APPLE) -> Image.Image:
    """White bubble with the apple: the game's core idea in one picture."""
    bubble = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(bubble)
    rim = max(2, size // 28)
    draw.ellipse((0, 0, size - 1, size - 1), fill=(255, 255, 255, 245), outline=(214, 226, 236, 255), width=rim)
    apple = Image.open(picture).convert("RGBA").resize((int(size * 0.62),) * 2, Image.LANCZOS)
    bubble.alpha_composite(apple, ((size - apple.width) // 2, (size - apple.height) // 2))
    return bubble


def rukajja(height: int) -> Image.Image:
    figure = clean_cutout(Image.open(CUTOUT).convert("RGBA"))
    width = int(figure.width * height / figure.height)
    return figure.resize((width, height), Image.LANCZOS)


def icon(size: int) -> Image.Image:
    canvas = background(size)
    figure = rukajja(int(size * 1.18))
    # Head and waving hand fill the icon; legs fall below the bottom edge.
    canvas.alpha_composite(figure, (int(size * 0.02), int(size * 0.1)))
    bubble = word_bubble(int(size * 0.38))
    canvas.alpha_composite(bubble, (int(size * 0.58), int(size * 0.07)))
    return canvas


def adaptive_layers(size: int = 432) -> tuple[Image.Image, Image.Image]:
    """Android masks adaptive icons to the centre 66%, so the foreground is drawn smaller."""
    back = background(size)
    front = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    figure = rukajja(int(size * 0.9))
    front.alpha_composite(figure, (int(size * 0.14), int(size * 0.2)))
    bubble = word_bubble(int(size * 0.27))
    front.alpha_composite(bubble, (int(size * 0.56), int(size * 0.18)))
    return back, front


def sticker_figure(height: int) -> Image.Image:
    """Rukajja as a die-cut sticker: a hard alpha cut drops the sheet's glow, and the
    thick white rim with a soft shadow hides what is left of the rough edge."""
    figure = Image.open(CUTOUT).convert("RGBA")
    alpha = figure.getchannel("A").point(lambda a: 255 if a >= 110 else 0)
    alpha = alpha.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.MaxFilter(5))
    # Keep only the shape connected to her body; loose specks would get their own white rim.
    ImageDraw.floodfill(alpha, (alpha.width // 2, alpha.height // 2), 128)
    alpha = alpha.point(lambda a: 255 if a == 128 else 0)
    figure.putalpha(alpha.filter(ImageFilter.GaussianBlur(0.6)))
    width = int(figure.width * height / figure.height)
    figure = figure.resize((width, height), Image.LANCZOS)

    pad = 30
    sticker = Image.new("RGBA", (width + 2 * pad, height + 2 * pad), (0, 0, 0, 0))
    body = Image.new("L", sticker.size, 0)
    body.paste(figure.getchannel("A"), (pad, pad))
    rim = body.filter(ImageFilter.MaxFilter(15)).filter(ImageFilter.GaussianBlur(1.2))
    shade = Image.new("RGBA", sticker.size, (10, 30, 70, 0))
    shade.putalpha(rim.point(lambda a: a * 90 // 255))
    sticker.alpha_composite(shade.filter(ImageFilter.GaussianBlur(10)), (0, 10))
    white = Image.new("RGBA", sticker.size, (255, 255, 255, 0))
    white.putalpha(rim)
    sticker.alpha_composite(white)
    sticker.alpha_composite(figure, (pad, pad))
    return sticker


def cloud(width: int) -> Image.Image:
    height, pad = width // 2, 8
    layer = Image.new("RGBA", (width + 2 * pad, height + 2 * pad), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for x0, y0, x1, y1 in ((0, 0.5, 0.42, 1), (0.58, 0.5, 1, 1), (0.2, 0.55, 0.8, 1),
                           (0.14, 0.22, 0.54, 0.92), (0.4, 0, 0.84, 0.88)):
        draw.ellipse((pad + x0 * width, pad + y0 * height, pad + x1 * width, pad + y1 * height),
                     fill=(255, 255, 255, 235))
    return layer.filter(ImageFilter.GaussianBlur(1.5))


def shadowed_text(canvas: Image.Image, xy: tuple, text: str, font: ImageFont.FreeTypeFont, **kwargs) -> None:
    shade = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    ImageDraw.Draw(shade).text((xy[0], xy[1] + 6), text, font=font, fill=(10, 30, 70, 120), anchor="lm",
                               stroke_width=kwargs.get("stroke_width", 0), stroke_fill=(10, 30, 70, 120))
    canvas.alpha_composite(shade.filter(ImageFilter.GaussianBlur(4)))
    ImageDraw.Draw(canvas).text(xy, text, font=font, anchor="lm", **kwargs)


def feature_graphic() -> Image.Image:
    width, height = 1024, 500
    mask = Image.linear_gradient("L").resize((width, height))
    canvas = Image.composite(Image.new("RGBA", (width, height), (64, 156, 238)),
                             Image.new("RGBA", (width, height), (150, 220, 252)), mask)

    glow = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    ImageDraw.Draw(glow).ellipse((380, -220, 1100, 200), fill=(255, 255, 255, 70))
    canvas.alpha_composite(glow.filter(ImageFilter.GaussianBlur(50)))
    canvas.alpha_composite(cloud(170), (480, 22))

    draw = ImageDraw.Draw(canvas)
    draw.ellipse((-300, 380, 760, 900), fill=HILL)
    draw.ellipse((420, 405, 1500, 1000), fill=HILL_DARK)

    figure = sticker_figure(440)
    canvas.alpha_composite(figure, (8, 40))
    parrot = Image.open(PARROT).convert("RGBA").resize((118, 118), Image.LANCZOS)
    canvas.alpha_composite(parrot, (300, 42))

    title = "Run and Learn"
    size = 96
    while ImageFont.truetype(str(TITLE_FONT), size).getlength(title) > 560:
        size -= 2
    title_font = ImageFont.truetype(str(TITLE_FONT), size)
    shadowed_text(canvas, (420, 168), title, title_font, fill=(255, 255, 255), stroke_width=7,
                  stroke_fill=(28, 84, 160))

    tagline_font = ImageFont.truetype(str(TITLE_FONT), 30)
    shadowed_text(canvas, (424, 252), "Run, jump and learn new words!", tagline_font, fill=(255, 255, 255))

    pill_font = ImageFont.truetype(str(TITLE_FONT), 26)
    x = 424
    for label in ("Bosanski", "English", "Deutsch"):
        text_width = pill_font.getlength(label)
        box = (x, 300, x + text_width + 40, 348)
        shade = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        ImageDraw.Draw(shade).rounded_rectangle((box[0], box[1] + 5, box[2], box[3] + 5), 24, fill=(10, 30, 70, 90))
        canvas.alpha_composite(shade.filter(ImageFilter.GaussianBlur(4)))
        ImageDraw.Draw(canvas).rounded_rectangle(box, 24, fill=(255, 255, 255))
        ImageDraw.Draw(canvas).text((x + 20, 324), label, font=pill_font, fill=(28, 84, 160), anchor="lm")
        x = box[2] + 14

    canvas.alpha_composite(word_bubble(112), (880, 28))
    canvas.alpha_composite(word_bubble(124, COW), (818, 352))
    canvas.alpha_composite(word_bubble(96, BANANA), (700, 392))
    return canvas


def main() -> None:
    ICON_DIR.mkdir(parents=True, exist_ok=True)
    PLAY_DIR.mkdir(parents=True, exist_ok=True)

    big = icon(512)
    big.save(ICON_DIR / "icon_512.png")
    big.convert("RGB").save(PLAY_DIR / "icon_512.png")

    back, front = adaptive_layers()
    back.save(ICON_DIR / "adaptive_background.png")
    front.save(ICON_DIR / "adaptive_foreground.png")

    feature_graphic().convert("RGB").save(PLAY_DIR / "feature_graphic_1024x500.png")
    print("icon, adaptive layers and feature graphic written")


if __name__ == "__main__":
    main()
