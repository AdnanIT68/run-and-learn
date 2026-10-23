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


def feature_graphic() -> Image.Image:
    width, height = 1024, 500
    canvas = Image.new("RGBA", (width, height), SKY)
    draw = ImageDraw.Draw(canvas)
    draw.ellipse((-200, -260, 520, 320), fill=SKY_LIGHT)
    draw.ellipse((-300, 360, 1400, 1100), fill=HILL)
    draw.ellipse((500, 400, 1600, 1200), fill=HILL_DARK)

    figure = rukajja(470)
    canvas.alpha_composite(figure, (60, 40))

    canvas.alpha_composite(word_bubble(118), (880, 22))
    canvas.alpha_composite(word_bubble(130, COW), (800, 330))

    title_font = ImageFont.truetype(FONT_BOLD, 92)
    sub_font = ImageFont.truetype(FONT_BOLD, 40)
    draw.text((382, 150), "Run and Learn", font=title_font, fill=(255, 255, 255), stroke_width=8,
              stroke_fill=(36, 92, 140))
    draw.text((392, 280), "Bosanski  ·  English", font=sub_font, fill=(36, 92, 140))
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
