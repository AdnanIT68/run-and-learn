"""Turns raw game captures into captioned Google Play phone screenshots.

Each capture sits in a landscape phone frame on a coloured gradient, under a
two-tone caption in the game's own font, with one word-picture sticker.

Input:  Store/raw/<lang>/*.png (from the StoreScreenshots play mode test)
Output: Store/play/screenshots/<lang>/<nn>_<name>.png at 1920x1080

Usage: Tools/.venv/Scripts/python.exe -I Tools/make_store_screenshots.py
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
RAW = ROOT / "Store" / "raw"
OUT = ROOT / "Store" / "play" / "screenshots"
WORDS = ROOT / "Assets" / "Resources" / "Words"
FONT = ROOT / "Assets" / "Resources" / "Fonts" / "TitanOne-Regular.ttf"

WIDTH, HEIGHT = 1920, 1080
SCREEN_W, SCREEN_H = 1440, 810
BEZEL = 24
SCREEN_X, SCREEN_Y = (WIDTH - SCREEN_W) // 2, 222
FRAME = (20, 26, 38)
FRAME_RIM = (52, 62, 82)
TEXT = (255, 255, 255)
HIGHLIGHT = (255, 225, 77)

# Gradient (top, bottom) per screenshot, picked to stay apart from the game's own colours.
THEMES = {
    "sky": ((66, 170, 245), (28, 96, 204)),
    "green": ((60, 190, 120), (24, 122, 82)),
    "violet": ((132, 98, 250), (78, 52, 196)),
    "coral": ((255, 128, 112), (214, 64, 84)),
    "teal": ((40, 196, 196), (18, 120, 150)),
    "magenta": ((236, 92, 180), (156, 48, 150)),
    "navy": ((70, 120, 230), (40, 52, 150)),
}

# Listing order and captions (Play takes at most eight phone screenshots).
# "hr" carries the Bosnian text (Play has no Bosnian slot). Each language has its
# own raw captures, with the game's menus in the way that audience plays.
# Text inside {braces} is drawn in the highlight colour.
SHOTS = [
    ("01_catch_the_word", "sky", "World1/Pictures/apple.png", {
        "en": "Hear the word, {jump to the picture!}",
        "hr": "Čuj riječ i {skoči na pravu sliku!}",
        "de": "Wort hören, {ins richtige Bild springen!}"}),
    ("02_players", "green", "World4/Pictures/ball.png", {
        "en": "Pick your player: {a team of ten kids}",
        "hr": "Izaberi igrača: {ekipa od desetero djece}",
        "de": "Wähle deinen Spieler: {zehn Kinder}"}),
    ("03_worlds", "violet", "World8/Pictures/rocket.png", {
        "en": "{8 worlds,} 40 levels",
        "hr": "{8 svjetova,} 40 nivoa",
        "de": "{8 Welten,} 40 Level"}),
    ("04_counting_farm", "teal", "World2/Pictures/cow.png", {
        "en": "Count the animals {from one to five}",
        "hr": "Broji životinje {od jedan do pet}",
        "de": "Tiere zählen {von eins bis fünf}"}),
    ("05_command_bazaar", "coral", "World4/Pictures/mosque.png", {
        "en": "Listen and do it: {Jump! Stay down!}",
        "hr": "Slušaj i uradi: {Skoči! Ostani dolje!}",
        "de": "Hören und tun: {Spring! Bleib unten!}"}),
    ("06_level_complete", "magenta", "World8/Pictures/star_word.png", {
        "en": "Finish the level, {collect your stars!}",
        "hr": "Završi nivo i {skupi zvjezdice!}",
        "de": "Level schaffen, {Sterne sammeln!}"}),
    ("07_today", "navy", "World5/Pictures/rainbow.png", {
        "en": "{A daily gift} and three small tasks",
        "hr": "{Dnevni poklon} i tri mala zadatka",
        "de": "{Tägliches Geschenk} und drei Aufgaben"}),
    ("08_main_menu", "sky", "World3/Pictures/grandma.png", {
        "en": "Learn {Bosnian, English or German}",
        "hr": "Uči {bosanski, engleski ili njemački}",
        "de": "Lerne {Bosnisch, Englisch oder Deutsch}"}),
]


def gradient(top: tuple, bottom: tuple) -> Image.Image:
    mask = Image.linear_gradient("L").resize((WIDTH, HEIGHT))
    return Image.composite(Image.new("RGBA", (WIDTH, HEIGHT), bottom),
                           Image.new("RGBA", (WIDTH, HEIGHT), top), mask)


def bokeh() -> Image.Image:
    """A few large, soft light circles so the background is not flat."""
    layer = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for x, y, r, a in ((180, 120, 260, 34), (1760, 980, 320, 30), (1650, 90, 150, 26), (90, 900, 140, 22)):
        draw.ellipse((x - r, y - r, x + r, y + r), fill=(255, 255, 255, a))
    return layer.filter(ImageFilter.GaussianBlur(30))


def rounded_mask(size: tuple, radius: int) -> Image.Image:
    mask = Image.new("L", size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, size[0] - 1, size[1] - 1), radius, fill=255)
    return mask


def shadow(box: tuple, radius: int, offset: int, blur: int, alpha: int) -> Image.Image:
    layer = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    x0, y0, x1, y1 = box
    ImageDraw.Draw(layer).rounded_rectangle((x0, y0 + offset, x1, y1 + offset), radius, fill=(10, 20, 40, alpha))
    return layer.filter(ImageFilter.GaussianBlur(blur))


def phone(canvas: Image.Image, raw: Image.Image) -> None:
    frame_box = (SCREEN_X - BEZEL, SCREEN_Y - BEZEL, SCREEN_X + SCREEN_W + BEZEL, SCREEN_Y + SCREEN_H + BEZEL)
    canvas.alpha_composite(shadow(frame_box, 64, 22, 30, 120))
    draw = ImageDraw.Draw(canvas)
    draw.rounded_rectangle(frame_box, 64, fill=FRAME, outline=FRAME_RIM, width=3)
    camera_y = SCREEN_Y + SCREEN_H // 2
    draw.ellipse((SCREEN_X - 18, camera_y - 6, SCREEN_X - 6, camera_y + 6), fill=FRAME_RIM)

    screen = raw.convert("RGBA").resize((SCREEN_W, SCREEN_H), Image.LANCZOS)
    screen.putalpha(rounded_mask(screen.size, 40))
    canvas.alpha_composite(screen, (SCREEN_X, SCREEN_Y))


def sticker(picture: Path, size: int) -> Image.Image:
    """White word bubble with a soft shadow, like the bubbles in the game."""
    pad = 30
    layer = Image.new("RGBA", (size + 2 * pad, size + 2 * pad), (0, 0, 0, 0))
    shade = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    ImageDraw.Draw(shade).ellipse((pad, pad + 12, pad + size, pad + size + 12), fill=(10, 20, 40, 110))
    layer.alpha_composite(shade.filter(ImageFilter.GaussianBlur(12)))
    ImageDraw.Draw(layer).ellipse((pad, pad, pad + size, pad + size), fill=(255, 255, 255, 255),
                                  outline=(220, 232, 244, 255), width=6)
    art = Image.open(picture).convert("RGBA").resize((int(size * 0.62),) * 2, Image.LANCZOS)
    layer.alpha_composite(art, (pad + (size - art.width) // 2, pad + (size - art.height) // 2))
    return layer


def parse(caption: str) -> list[tuple[str, tuple]]:
    """Splits "plain {highlighted} plain" into coloured runs."""
    runs, colour, text = [], TEXT, ""
    for char in caption:
        if char in "{}":
            if text:
                runs.append((text, colour))
            text, colour = "", HIGHLIGHT if char == "{" else TEXT
        else:
            text += char
    if text:
        runs.append((text, colour))
    return runs


def caption(canvas: Image.Image, text: str) -> None:
    runs = parse(text)
    draw = ImageDraw.Draw(canvas)
    size = 86
    while True:
        font = ImageFont.truetype(str(FONT), size)
        total = sum(draw.textlength(run, font=font) for run, _ in runs)
        if total <= WIDTH - 260 or size <= 48:
            break
        size -= 2

    y = 112
    x = (WIDTH - total) / 2
    shade = Image.new("RGBA", (WIDTH, HEIGHT), (0, 0, 0, 0))
    shade_draw = ImageDraw.Draw(shade)
    sx = x
    for run, _ in runs:
        shade_draw.text((sx, y + 6), run, font=font, fill=(10, 20, 50, 110), anchor="lm")
        sx += draw.textlength(run, font=font)
    canvas.alpha_composite(shade.filter(ImageFilter.GaussianBlur(5)))
    for run, colour in runs:
        draw.text((x, y), run, font=font, fill=colour, anchor="lm")
        x += draw.textlength(run, font=font)


def compose(raw: Image.Image, text: str, theme: str, picture: Path, index: int) -> Image.Image:
    canvas = gradient(*THEMES[theme])
    canvas.alpha_composite(bokeh())
    caption(canvas, text)
    phone(canvas, raw)

    badge = sticker(picture, 176)
    # Alternate the sticker between the lower-left and upper-right corners of the phone.
    if index % 2 == 0:
        position = (SCREEN_X - BEZEL - badge.width // 2 - 6, HEIGHT - badge.height + 4)
    else:
        position = (SCREEN_X + SCREEN_W + BEZEL - badge.width // 2 + 6, SCREEN_Y - badge.height // 2 + 30)
    canvas.alpha_composite(badge, position)
    return canvas.convert("RGB")


def main() -> None:
    for lang in ("en", "hr", "de"):
        folder = OUT / lang
        folder.mkdir(parents=True, exist_ok=True)
        for old in folder.glob("*.png"):
            old.unlink()

        for index, (name, theme, picture, captions) in enumerate(SHOTS):
            raw = Image.open(RAW / lang / f"{name}.png").convert("RGB")
            compose(raw, captions[lang], theme, WORDS / picture, index).save(folder / f"{name}.png")

    print(f"{len(SHOTS)} screenshots x 3 languages written to {OUT}")


if __name__ == "__main__":
    main()
