"""Voices what Pepi the parrot says (Tools/pepi_lines.json) with the same Azure voices.

Pepi speaks the language the child already speaks: Bosnian, English or German
(Arabic is only ever learned, never the menu language). Writes
Assets/Resources/Words/Pepi/Audio/<lang>/<id>.ogg (existing files are kept) and
Assets/Resources/Words/Pepi/lines.json with the texts, for the story card.

Usage:
  Tools/.venv/Scripts/python.exe Tools/generate_pepi.py
"""

import asyncio
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "Tools"))

import generate_words as words  # noqa: E402
HOME_LANGUAGES = ("bs", "en", "de")


async def main() -> None:
    lines = json.loads((ROOT / "Tools" / "pepi_lines.json").read_text(encoding="utf-8"))["lines"]
    out = ROOT / "Assets" / "Resources" / "Words" / "Pepi" / "Audio"

    for lang in HOME_LANGUAGES:
        (out / lang).mkdir(parents=True, exist_ok=True)
        for line in lines:
            await words.speak(line[lang], words.VOICES[lang], out / lang / f"{line['id']}.ogg")

    # The texts too, for the story card that shows what Pepi tells.
    manifest = {"lines": [{"id": l["id"], **{lang: l[lang] for lang in HOME_LANGUAGES}} for l in lines]}
    (out.parent / "lines.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Pepi: {len(lines)} lines x {len(HOME_LANGUAGES)} languages -> {out}")


if __name__ == "__main__":
    print("voices:", "Azure Speech" if words.AZURE else "edge-tts (prototype only)")
    asyncio.run(main())
