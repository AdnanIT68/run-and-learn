"""Builds the word-pack content Unity loads from Resources/Words.

For every word in a pack JSON this writes:
  Assets/Resources/Words/<Pack>/Pictures/<id>.png   (256 px, from Twemoji SVG)
  Assets/Resources/Words/<Pack>/Audio/<lang>/<id>.ogg (neural TTS placeholder)
  Assets/Resources/Words/<Pack>/pack.json            (manifest read at runtime)

The pictures and voices are placeholders for the prototype. Family recordings
and art in Rukajja's style replace them later without code changes, as long
as the file names stay the same.

Usage:
  Tools/.venv/Scripts/python.exe Tools/generate_words.py Tools/words_world1.json
"""

import asyncio
import json
import subprocess
import sys
import tempfile
import urllib.request
from pathlib import Path

import edge_tts
import resvg_py

ROOT = Path(__file__).resolve().parent.parent
TWEMOJI_SVG = "https://cdn.jsdelivr.net/gh/jdecked/twemoji@15.1.0/assets/svg/{code}.svg"
PICTURE_SIZE = 256
# Shared UI icons, written to Resources/Words/Icons.
ICONS = {
    "speaker": "1f50a",
    "check": "2705",
    "album": "1f4d6",
    "star": "2b50",
    "lock": "1f512",
    "parrot": "1f99c",
}
VOICES = {
    "bs": "bs-BA-VesnaNeural",
    "en": "en-US-AnaNeural",
}
# Slower than default so small children can hear every sound.
SPEECH_RATE = "-15%"


def render_picture(code: str, target: Path) -> None:
    if target.exists():
        return

    with urllib.request.urlopen(TWEMOJI_SVG.format(code=code), timeout=30) as response:
        svg = response.read().decode("utf-8")

    png = resvg_py.svg_to_bytes(svg_string=svg, width=PICTURE_SIZE, height=PICTURE_SIZE)
    target.write_bytes(bytes(png))


async def speak(text: str, voice: str, target: Path) -> None:
    if target.exists():
        return

    with tempfile.TemporaryDirectory() as tmp:
        raw = Path(tmp) / "raw.mp3"

        # The speech service drops connections now and then; retry before giving up.
        for attempt in range(4):
            try:
                await edge_tts.Communicate(text, voice, rate=SPEECH_RATE).save(str(raw))
                break
            except Exception:
                if attempt == 3:
                    raise
                await asyncio.sleep(3 * (attempt + 1))

        # Trim leading/trailing silence so the word lands the moment it is triggered.
        subprocess.run(
            [
                "ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", str(raw),
                "-af",
                "silenceremove=start_periods=1:start_threshold=-45dB,"
                "areverse,silenceremove=start_periods=1:start_threshold=-45dB,areverse",
                "-ac", "1", "-ar", "44100", "-c:a", "libvorbis", "-q:a", "5", str(target),
            ],
            check=True,
        )


async def main(pack_file: Path) -> None:
    pack = json.loads(pack_file.read_text(encoding="utf-8"))
    out = ROOT / "Assets" / "Resources" / "Words" / pack["pack"]
    pictures = out / "Pictures"
    pictures.mkdir(parents=True, exist_ok=True)

    for lang in VOICES:
        (out / "Audio" / lang).mkdir(parents=True, exist_ok=True)

    for word in pack["words"]:
        render_picture(word["emoji"], pictures / f"{word['id']}.png")

    icons = ROOT / "Assets" / "Resources" / "Words" / "Icons"
    icons.mkdir(parents=True, exist_ok=True)
    for name, code in ICONS.items():
        render_picture(code, icons / f"{name}.png")

    # Counting phrases ("tri kokoši") reuse the word's picture, drawn several times.
    counts = [
        {"id": f"{c['word']}_{c['count']}", "picture": c["word"], "count": c["count"], "bs": c["bs"], "en": c["en"]}
        for c in pack.get("counts", [])
    ]

    # Spoken commands ("Skoči!") are answered by acting, so their bubbles all show a star.
    commands = [
        {"id": c["id"], "action": c["action"], "picture": "star", "bs": c["bs"], "en": c["en"]}
        for c in pack.get("commands", [])
    ]

    # Size phrases ("velika riba") show the word's picture drawn big or small.
    sizes = [
        {"id": f"{z['word']}_{z['size']}", "picture": z["word"], "size": z["size"], "bs": z["bs"], "en": z["en"]}
        for z in pack.get("sizes", [])
    ]

    for entry in pack["words"] + pack.get("phrases", []) + counts + commands + sizes:
        for lang, voice in VOICES.items():
            await speak(entry[lang], voice, out / "Audio" / lang / f"{entry['id']}.ogg")

    manifest = {
        "pack": pack["pack"],
        "title_bs": pack["title_bs"],
        "title_en": pack["title_en"],
        "words": [{"id": w["id"], "bs": w["bs"], "en": w["en"]} for w in pack["words"]],
        "phrases": [{"id": p["id"], "bs": p["bs"], "en": p["en"]} for p in pack.get("phrases", [])],
        "counts": counts,
        "commands": commands,
        "sizes": sizes,
    }
    (out / "pack.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"{pack['pack']}: {len(pack['words'])} words, {len(manifest['phrases'])} phrases, "
          f"{len(counts)} counts, {len(commands)} commands -> {out}")


if __name__ == "__main__":
    asyncio.run(main(Path(sys.argv[1])))
