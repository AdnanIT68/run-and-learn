"""Turns raw game captures into captioned Google Play phone screenshots.

Input:  Store/raw/*.png (from the StoreScreenshots play mode test)
Output: Store/play/screenshots/<lang>/<nn>_<name>.png at 1920x1080

Usage: Tools/.venv/Scripts/python.exe -I Tools/make_store_screenshots.py
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
RAW = ROOT / "Store" / "raw"
OUT = ROOT / "Store" / "play" / "screenshots"
FONT_BOLD = "C:/Windows/Fonts/segoeuib.ttf"

WIDTH, HEIGHT = 1920, 1080
BAND = (46, 111, 176)
TEXT = (255, 255, 255)

# Listing order and captions. "hr" carries the Bosnian text (Play has no Bosnian slot).
SHOTS = [
    ("02_catch_the_word", {
        "en": "Hear the word, jump into the right picture!",
        "hr": "Čuj riječ i skoči u pravu sliku!",
        "de": "Wort hören, ins richtige Bild springen!"}),
    ("03_counting_farm", {
        "en": "Count farm animals from one to five",
        "hr": "Broji životinje od jedan do pet",
        "de": "Tiere zählen von eins bis fünf"}),
    ("08_command_bazaar", {
        "en": "Listen and do it: Jump! Stay down!",
        "hr": "Slušaj i uradi: Skoči! Ostani dolje!",
        "de": "Hören und tun: Spring! Bleib unten!"}),
    ("09_big_small_river", {
        "en": "Big or small? Listen closely!",
        "hr": "Veliko ili malo? Slušaj pažljivo!",
        "de": "Groß oder klein? Gut zuhören!"}),
    ("04_bravo", {
        "en": "Five words, one big BRAVO!",
        "hr": "Pet riječi, jedno veliko BRAVO!",
        "de": "Fünf Wörter, ein großes BRAVO!"}),
    ("05_album", {
        "en": "Every word saved in a sticker album",
        "hr": "Svaka riječ ostaje u albumu sličica",
        "de": "Jedes Wort bleibt im Stickeralbum"}),
    ("06_learning_english", {
        "en": "Children in Bosnia learn English too",
        "hr": "Djeca u BiH uče i engleski",
        "de": "Kinder in Bosnien lernen Englisch"}),
    ("07_worlds", {
        "en": "New worlds to unlock",
        "hr": "Novi svjetovi za otključavanje",
        "de": "Neue Welten freischalten"}),
    ("01_main_menu", {
        "en": "Learn Bosnian or English",
        "hr": "Uči bosanski ili engleski",
        "de": "Bosnisch oder Englisch lernen"}),
]


def rounded(image: Image.Image, radius: int) -> Image.Image:
    mask = Image.new("L", image.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, image.width - 1, image.height - 1), radius, fill=255)
    result = image.convert("RGBA")
    result.putalpha(mask)
    return result


def compose(raw: Image.Image, caption: str) -> Image.Image:
    canvas = Image.new("RGBA", (WIDTH, HEIGHT), BAND)
    draw = ImageDraw.Draw(canvas)

    font_size = 76
    font = ImageFont.truetype(FONT_BOLD, font_size)
    while draw.textlength(caption, font=font) > WIDTH - 160 and font_size > 40:
        font_size -= 4
        font = ImageFont.truetype(FONT_BOLD, font_size)

    draw.text((WIDTH // 2, 92), caption, font=font, fill=TEXT, anchor="mm")

    shot_height = HEIGHT - 200
    shot_width = int(raw.width * shot_height / raw.height)
    shot = rounded(raw.resize((shot_width, shot_height), Image.LANCZOS), 28)
    canvas.alpha_composite(shot, ((WIDTH - shot_width) // 2, 176))
    return canvas.convert("RGB")


def main() -> None:
    for lang in ("en", "hr", "de"):
        folder = OUT / lang
        folder.mkdir(parents=True, exist_ok=True)

        for index, (name, captions) in enumerate(SHOTS, start=1):
            raw = Image.open(RAW / f"{name}.png").convert("RGB")
            compose(raw, captions[lang]).save(folder / f"{index:02d}_{name}.png")

    print(f"{len(SHOTS)} screenshots x 3 languages written to {OUT}")


if __name__ == "__main__":
    main()
