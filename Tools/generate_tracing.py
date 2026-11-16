"""Letters to trace with a finger, for the mini-game "Piši slovo".

For every letter of the Bosnian, English, German and Arabic alphabets this writes:
  Assets/Resources/Tracing/Glyphs/<id>.png   (256 px guide: the letter, pale, to trace over)
  Assets/Resources/Tracing/masks.json        (a 32 x 32 grid per letter: which cells the letter covers)
  Assets/Resources/Tracing/Audio/<lang>/<id>.ogg  ("Slovo A.", "Letter A.", "Buchstabe A.", "حَرْفُ الْأَلِف")

The guide and the mask come from the same drawing, so they line up exactly. Latin
letters use Titan One (capitals), Arabic the Noto Sans Arabic Bold of the game
(isolated forms). Existing voices are kept.

Usage:
  Tools/.venv/Scripts/python.exe Tools/generate_tracing.py
"""

import asyncio
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

import generate_words as words

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Resources" / "Tracing"
LATIN_FONT = ROOT / "Assets" / "Resources" / "Fonts" / "TitanOne-Regular.ttf"
ARABIC_FONT = ROOT / "Assets" / "Resources" / "Fonts" / "NotoSansArabic-Bold.ttf"
SIZE = 256
GRID = 32
GUIDE = (196, 214, 238, 255)
OUTLINE = (150, 175, 215, 255)

ALPHABETS = {
    "bs": ["A", "B", "C", "Č", "Ć", "D", "Dž", "Đ", "E", "F", "G", "H", "I", "J", "K", "L", "Lj", "M", "N", "Nj",
           "O", "P", "R", "S", "Š", "T", "U", "V", "Z", "Ž"],
    "en": [chr(c) for c in range(ord("A"), ord("Z") + 1)],
    "de": [chr(c) for c in range(ord("A"), ord("Z") + 1)] + ["Ä", "Ö", "Ü"],
    "ar": list(words.ARABIC_LETTERS.keys()),
}


def letter_id(letter: str) -> str:
    if words.is_arabic(letter):
        return "ar_" + words.ARABIC_LETTER_IDS[letter]
    return "latin_" + words.SAFE_LETTERS.get(letter, letter)


def draw(letter: str) -> Image.Image:
    """The letter as large as fits in the picture, centred on its own outline (Arabic
    letters reach far below the line, so centring on the font's line would cut them)."""
    arabic = words.is_arabic(letter)
    shown = letter.upper() if not arabic else letter
    path = str(ARABIC_FONT if arabic else LATIN_FONT)
    size = 230
    while True:
        font = ImageFont.truetype(path, size)
        left, top, right, bottom = font.getbbox(shown, stroke_width=4)
        if (right - left <= 216 and bottom - top <= 216) or size <= 60:
            break
        size -= 6

    image = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 0))
    d = ImageDraw.Draw(image)
    x = (SIZE - (right - left)) / 2 - left
    y = (SIZE - (bottom - top)) / 2 - top
    d.text((x, y), shown, font=font, fill=GUIDE, stroke_width=4, stroke_fill=OUTLINE)
    return image


def mask(image: Image.Image) -> str:
    """The letter's cells on a GRID x GRID raster, row by row from the top, as '0'/'1'."""
    small = image.split()[3].resize((GRID, GRID), Image.BOX)
    return "".join("1" if small.getpixel((x, y)) > 110 else "0" for y in range(GRID) for x in range(GRID))


def spoken(letter: str, lang: str) -> str:
    if lang == "ar":
        name, _ = words.ARABIC_LETTERS[letter]
        return words.arabic_letter_phrase(letter, name)
    return f"{words.LETTER_WORD[lang]} {letter.upper() if len(letter) == 1 else letter}."


async def main() -> None:
    glyphs = OUT / "Glyphs"
    glyphs.mkdir(parents=True, exist_ok=True)
    masks = {}
    letters = {}

    for lang, alphabet in ALPHABETS.items():
        (OUT / "Audio" / lang).mkdir(parents=True, exist_ok=True)
        letters[lang] = []
        for letter in alphabet:
            lid = letter_id(letter)
            if lid not in masks:
                image = draw(letter)
                image.save(glyphs / f"{lid}.png")
                masks[lid] = mask(image)
            letters[lang].append({"id": lid, "letter": letter})
            await words.speak(spoken(letter, lang), words.VOICES[lang], OUT / "Audio" / lang / f"{lid}.ogg")

    manifest = {"grid": GRID, "masks": [{"id": k, "cells": v} for k, v in masks.items()],
                "alphabets": [{"lang": k, "letters": v} for k, v in letters.items()]}
    (OUT / "masks.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"{len(masks)} letters, {sum(len(v) for v in letters.values())} voices -> {OUT}")


if __name__ == "__main__":
    print("voices:", "Azure Speech" if words.AZURE else "edge-tts (prototype only)")
    asyncio.run(main())
