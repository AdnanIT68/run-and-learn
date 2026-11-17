"""Voices what Pepi the parrot says (Tools/pepi_lines.json) with the same Azure voices.

Pepi speaks the language the child already speaks: Bosnian, English, German or
Swedish (Arabic is only ever learned, never the menu language). Writes
Assets/Resources/Words/Pepi/Audio/<lang>/<id>.ogg (existing files are kept) and
Assets/Resources/Words/Pepi/lines.json with the texts, for the story card.
For Swedish menus it also records the tutorial's "listen, then jump" line into every
world (Assets/Resources/Words/<Pack>/Audio/sv/intro.ogg).

Usage:
  Tools/.venv/Scripts/python.exe -I Tools/generate_pepi.py
"""

import asyncio
import json
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "Tools"))

import generate_words as words  # noqa: E402

HOME_LANGUAGES = ("bs", "en", "de", "sv")

# Swedish is a menu language only, so it has no voice in generate_words.VOICES.
SWEDISH_VOICE = "sv-SE-SofieNeural"
SWEDISH_INTRO = "Lyssna på ordet och hoppa in i rätt bild!"


def voice_for(lang: str) -> str:
    return words.VOICES.get(lang, SWEDISH_VOICE)


async def main() -> None:
    lines = json.loads((ROOT / "Tools" / "pepi_lines.json").read_text(encoding="utf-8"))["lines"]
    out = ROOT / "Assets" / "Resources" / "Words" / "Pepi" / "Audio"

    for lang in HOME_LANGUAGES:
        (out / lang).mkdir(parents=True, exist_ok=True)
        for line in lines:
            await words.speak(line[lang], voice_for(lang), out / lang / f"{line['id']}.ogg")

    # The texts too, for the story card that shows what Pepi tells.
    manifest = {"lines": [{"id": l["id"], **{lang: l[lang] for lang in HOME_LANGUAGES}} for l in lines]}
    (out.parent / "lines.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Pepi: {len(lines)} lines x {len(HOME_LANGUAGES)} languages -> {out}")

    # The tutorial's first line in Swedish, recorded once and copied into every world.
    packs = [p for p in sorted((ROOT / "Assets" / "Resources" / "Words").glob("World*")) if p.is_dir()]
    if any(not (p / "Audio" / "sv" / "intro.ogg").exists() for p in packs):
        with tempfile.TemporaryDirectory() as folder:
            intro = Path(folder) / "intro.ogg"
            await words.speak(SWEDISH_INTRO, SWEDISH_VOICE, intro)
            for pack in packs:
                target = pack / "Audio" / "sv" / "intro.ogg"
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(intro.read_bytes())
        print(f"Swedish intro -> {len(packs)} worlds")


if __name__ == "__main__":
    print("voices:", "Azure Speech" if words.AZURE else "edge-tts (prototype only)")
    asyncio.run(main())
