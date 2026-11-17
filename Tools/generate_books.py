"""Builds the picture books (Tools/books.json): four narrated pages per world.

Writes, per world:
  Assets/Resources/Books/<Pack>/book.json             (texts the game shows)
  Assets/Resources/Books/<Pack>/Audio/<lang>/page_<n>.ogg (the page read aloud, Azure voices)

Arabic is shaped for TextMeshPro sentence by sentence ("ar_shown", one sentence per
line), because a whole reversed paragraph would wrap in the wrong order.
Existing voice files are kept; delete one to record it again.

Usage:
  Tools/.venv/Scripts/python.exe -I Tools/generate_books.py
"""

import asyncio
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "Tools"))

import generate_words as words  # noqa: E402

SOURCE = ROOT / "Tools" / "books.json"
OUT = ROOT / "Assets" / "Resources" / "Books"


def arabic_lines(text: str) -> list:
    """One sentence per line, keeping its punctuation."""
    return [part.strip() for part in re.split(r"(?<=[.!?؟])\s+", text) if part.strip()]


def shown(text: str) -> str:
    return "\n".join(words.shape_arabic(line) for line in arabic_lines(text))


async def main() -> None:
    books = json.loads(SOURCE.read_text(encoding="utf-8"))["books"]
    voiced = 0

    for book in books:
        folder = OUT / book["pack"]
        pages = []

        for number, page in enumerate(book["pages"], start=1):
            for lang, voice in words.VOICES.items():
                target = folder / "Audio" / lang / f"page_{number}.ogg"
                target.parent.mkdir(parents=True, exist_ok=True)
                existed = target.exists()
                await words.speak(page[lang], voice, target)
                voiced += not existed

            pages.append({
                "words": page["words"],
                "bs": page["bs"],
                "en": page["en"],
                "de": page["de"],
                "ar": page["ar"],
                "ar_shown": shown(page["ar"]),
                "ar_latin": page["ar_latin"],
            })

        (folder / "book.json").write_text(json.dumps({"pack": book["pack"], "pages": pages}, ensure_ascii=False, indent=2),
                                          encoding="utf-8")

    print(f"{len(books)} books, {sum(len(b['pages']) for b in books)} pages, {voiced} new voice files -> {OUT}")


if __name__ == "__main__":
    print("voices:", "Azure Speech" if words.AZURE else "edge-tts (prototype only)")
    asyncio.run(main())
