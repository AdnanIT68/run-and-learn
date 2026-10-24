"""Turns raw game captures into captioned Google Play phone screenshots.

Input:  Store/raw/<lang>/*.png (from the StoreScreenshots play mode test)
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

# Listing order and captions (Play takes at most eight phone screenshots).
# "hr" carries the Bosnian text (Play has no Bosnian slot). Each language has its
# own raw captures, with the game's menus in the way that audience plays.
SHOTS = [
    ("01_catch_the_word", {
        "en": "Hear the word, jump into the right picture!",
        "hr": "Čuj riječ i skoči u pravu sliku!",
        "de": "Wort hören, ins richtige Bild springen!"}),
    ("02_players", {
        "en": "Pick your player: a team of ten kids",
        "hr": "Izaberi igrača: ekipa od deset djece",
        "de": "Wähle deinen Spieler: ein Team aus zehn Kindern"}),
    ("03_worlds", {
        "en": "8 worlds, 40 levels",
        "hr": "8 svjetova, 40 nivoa",
        "de": "8 Welten, 40 Level"}),
    ("04_counting_farm", {
        "en": "Count farm animals from one to five",
        "hr": "Broji životinje od jedan do pet",
        "de": "Tiere zählen von eins bis fünf"}),
    ("05_command_bazaar", {
        "en": "Listen and do it: Jump! Stay down!",
        "hr": "Slušaj i uradi: Skoči! Ostani dolje!",
        "de": "Hören und tun: Spring! Bleib unten!"}),
    ("06_level_complete", {
        "en": "Finish the level, collect your stars!",
        "hr": "Završi nivo i skupi zvjezdice!",
        "de": "Level schaffen, Sterne sammeln!"}),
    ("07_today", {
        "en": "A daily gift and three small tasks",
        "hr": "Dnevni poklon i tri mala zadatka",
        "de": "Tägliches Geschenk und drei kleine Aufgaben"}),
    ("08_main_menu", {
        "en": "Learn Bosnian, English or German",
        "hr": "Uči bosanski, engleski ili njemački",
        "de": "Bosnisch, Englisch oder Deutsch lernen"}),
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
        for old in folder.glob("*.png"):
            old.unlink()

        for name, captions in SHOTS:
            raw = Image.open(RAW / lang / f"{name}.png").convert("RGB")
            compose(raw, captions[lang]).save(folder / f"{name}.png")

    print(f"{len(SHOTS)} screenshots x 3 languages written to {OUT}")


if __name__ == "__main__":
    main()
