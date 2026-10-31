"""Builds the word-pack content Unity loads from Resources/Words.

For every word in a pack JSON this writes:
  Assets/Resources/Words/<Pack>/Pictures/<id>.png   (256 px, from Twemoji SVG)
  Assets/Resources/Words/<Pack>/Audio/<lang>/<id>.ogg (neural TTS)
  Assets/Resources/Words/<Pack>/pack.json            (manifest read at runtime)

Voices come from Azure Speech (licensed for use in the app) when the key file
~/RunAndLearnSigning/azure-speech.txt holds key= and region=;
without it they fall back to edge-tts, which is only fit for prototypes.
Family recordings and art in Rukajja's style can replace the files later
without code changes, as long as the names stay the same.

Usage:
  Tools/.venv/Scripts/python.exe Tools/generate_words.py Tools/words_world1.json
  Tools/.venv/Scripts/python.exe Tools/generate_words.py --revoice Tools/words_world*.json
    (--revoice records every voice again, e.g. after switching to Azure)
"""

import asyncio
import json
import os
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
from pathlib import Path
from xml.sax.saxutils import escape

import edge_tts
import resvg_py
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
DISPLAY_FONT = ROOT / "Assets" / "Resources" / "Fonts" / "TitanOne-Regular.ttf"
DIGIT_COLOURS = [(235, 80, 70), (245, 160, 40), (60, 170, 90), (60, 130, 220), (150, 90, 200)]
# File-safe names for Bosnian letters (Resources paths stay ASCII).
SAFE_LETTERS = {"Š": "SH", "Č": "CH", "Ć": "CJ", "Ž": "ZH", "Đ": "DJ", "Dž": "DZH", "Lj": "LJ", "Nj": "NJ",
                "Ä": "AE", "Ö": "OE", "Ü": "UE"}
# German nouns are learned with their article; the first letter is the noun's.
GERMAN_ARTICLES = ("der ", "die ", "das ")
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
    "gift": "1f381",
    "target": "1f3af",
    "tap": "1f446",
}
VOICES = {
    "bs": "bs-BA-VesnaNeural",
    "en": "en-US-AnaNeural",
    "de": "de-DE-KatjaNeural",
}
LETTER_WORD = {"bs": "Slovo", "en": "Letter", "de": "Buchstabe"}
# Slower than default so small children can hear every sound.
SPEECH_RATE = "-15%"
AZURE_KEY_FILE = Path(os.environ.get("USERPROFILE", str(Path.home()))) / "RunAndLearnSigning" / "azure-speech.txt"
# The free Azure tier allows 20 requests a minute.
AZURE_MIN_GAP = 3.2
REVOICE = False
_last_azure_call = 0.0


def azure_settings():
    """key and region from the key file kept outside the project, or None."""
    if not AZURE_KEY_FILE.exists():
        return None

    values = {}
    for line in AZURE_KEY_FILE.read_text(encoding="utf-8").splitlines():
        if "=" in line and not line.lstrip().startswith("#"):
            name, value = line.split("=", 1)
            values[name.strip()] = value.strip()

    return values if values.get("key") and values.get("region") else None


AZURE = azure_settings()


def azure_speak(text: str, voice: str, target: Path) -> None:
    """One phrase from Azure Speech as MP3, waiting out the free tier's rate limit."""
    global _last_azure_call
    locale = voice[:5]
    ssml = (f"<speak version='1.0' xml:lang='{locale}'><voice name='{voice}'>"
            f"<prosody rate='{SPEECH_RATE}'>{escape(text)}</prosody></voice></speak>")
    request = urllib.request.Request(
        f"https://{AZURE['region']}.tts.speech.microsoft.com/cognitiveservices/v1",
        data=ssml.encode("utf-8"),
        headers={
            "Ocp-Apim-Subscription-Key": AZURE["key"],
            "Content-Type": "application/ssml+xml",
            "X-Microsoft-OutputFormat": "audio-24khz-96kbitrate-mono-mp3",
            "User-Agent": "RunAndLearnContent",
        },
    )

    for attempt in range(6):
        wait = AZURE_MIN_GAP - (time.monotonic() - _last_azure_call)
        if wait > 0:
            time.sleep(wait)
        _last_azure_call = time.monotonic()
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                target.write_bytes(response.read())
            return
        except urllib.error.HTTPError as error:
            # 429: over the free tier's rate; anything else is worth a short retry too.
            if error.code in (401, 403) or attempt == 5:
                raise
            time.sleep(10 * (attempt + 1))
        except urllib.error.URLError:
            if attempt == 5:
                raise
            time.sleep(5 * (attempt + 1))


def render_picture(code: str, target: Path) -> None:
    if target.exists():
        return

    with urllib.request.urlopen(TWEMOJI_SVG.format(code=code), timeout=30) as response:
        svg = response.read().decode("utf-8")

    png = resvg_py.svg_to_bytes(svg_string=svg, width=PICTURE_SIZE, height=PICTURE_SIZE)
    target.write_bytes(bytes(png))


def render_digit(n: int, target: Path) -> None:
    """A big, friendly numeral in the game's display font, for number bubbles."""
    if target.exists():
        return

    image = Image.new("RGBA", (PICTURE_SIZE, PICTURE_SIZE), (255, 255, 255, 0))
    draw = ImageDraw.Draw(image)
    font = ImageFont.truetype(str(DISPLAY_FONT), 190 if n < 10 else 150)
    colour = DIGIT_COLOURS[(n - 1) % len(DIGIT_COLOURS)]
    draw.text((PICTURE_SIZE / 2, PICTURE_SIZE / 2 + 6), str(n), font=font, fill=colour + (255,), anchor="mm",
              stroke_width=10, stroke_fill=(255, 255, 255, 255))
    image.save(target)


def initial(word: str) -> str:
    """First letter as children learn it: Bosnian digraphs count as one letter."""
    lower = word.lower()
    for article in GERMAN_ARTICLES:
        if lower.startswith(article):
            lower = lower[len(article):]
            break
    for digraph in ("dž", "lj", "nj"):
        if lower.startswith(digraph):
            return digraph.capitalize()
    return lower[0].upper()


async def speak(text: str, voice: str, target: Path) -> None:
    if target.exists() and not REVOICE:
        return

    with tempfile.TemporaryDirectory() as tmp:
        raw = Path(tmp) / "raw.mp3"

        if AZURE:
            azure_speak(text, voice, raw)
        else:
            # The free edge-tts service drops connections now and then; retry before giving up.
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


def texts(entry: dict) -> dict:
    """The entry's text in every voiced language, e.g. {"bs": ..., "en": ..., "de": ...}."""
    return {lang: entry[lang] for lang in VOICES}


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
        {"id": f"{c['word']}_{c['count']}", "picture": c["word"], "count": c["count"], **texts(c)}
        for c in pack.get("counts", [])
    ]

    # Spoken commands ("Skoči!") are answered by acting, so their bubbles all show a star.
    commands = [
        {"id": c["id"], "action": c["action"], "picture": "star", **texts(c)}
        for c in pack.get("commands", [])
    ]

    # Size phrases ("velika riba") show the word's picture drawn big or small.
    sizes = [
        {"id": f"{z['word']}_{z['size']}", "picture": z["word"], "size": z["size"], **texts(z)}
        for z in pack.get("sizes", [])
    ]

    # Sentences ("Pada snijeg.") point at a picture; the wrong option comes from
    # the same group. A sentence may bring its own picture via "emoji".
    sentences = []
    for z in pack.get("sentences", []):
        if "emoji" in z:
            render_picture(z["emoji"], pictures / f"{z['picture']}.png")
        sentences.append({"id": z["id"], "group": z["group"], "picture": z["picture"], **texts(z)})

    # Numbers ("sedam") are caught as big numerals drawn in the display font.
    numbers = []
    for z in pack.get("numbers", []):
        render_digit(z["n"], pictures / f"digit_{z['n']}.png")
        numbers.append({"id": f"num_{z['n']}", "picture": f"digit_{z['n']}", **texts(z)})

    # Letters: one per first letter of the words, per language, voiced only in that language.
    letters = []
    if pack.get("letters"):
        for lang in VOICES:
            for letter in sorted({initial(w[lang]) for w in pack["words"]}):
                safe = SAFE_LETTERS.get(letter, letter)
                entry_id = f"letter_{lang}_{safe}"
                spoken = f"{LETTER_WORD[lang]} {letter}."
                await speak(spoken, VOICES[lang], out / "Audio" / lang / f"{entry_id}.ogg")
                shown = f"{letter} {letter.lower()}"
                letters.append({"id": entry_id, "letter": letter, "lang": lang, **{l: shown for l in VOICES}})

    for entry in pack["words"] + pack.get("phrases", []) + counts + commands + sizes + sentences + numbers:
        for lang, voice in VOICES.items():
            await speak(entry[lang], voice, out / "Audio" / lang / f"{entry['id']}.ogg")

    manifest = {
        "pack": pack["pack"],
        "title_bs": pack["title_bs"],
        "title_en": pack["title_en"],
        "title_de": pack.get("title_de", pack["title_en"]),
        "words": [{"id": w["id"], **texts(w)} for w in pack["words"]],
        "phrases": [{"id": p["id"], **texts(p)} for p in pack.get("phrases", [])],
        "counts": counts,
        "commands": commands,
        "sizes": sizes,
        "sentences": sentences,
        "numbers": numbers,
        "letters": letters,
        "reading": bool(pack.get("reading", False)),
        "review": bool(pack.get("review", False)),
        "goal": int(pack.get("goal", 0)),
    }
    (out / "pack.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"{pack['pack']}: {len(pack['words'])} words, {len(manifest['phrases'])} phrases, "
          f"{len(counts)} counts, {len(commands)} commands -> {out}")


if __name__ == "__main__":
    arguments = sys.argv[1:]
    if "--revoice" in arguments:
        REVOICE = True
        arguments.remove("--revoice")
    print("voices:", "Azure Speech (" + AZURE["region"] + ")" if AZURE else "edge-tts (prototype only)")
    for pack_file in arguments:
        asyncio.run(main(Path(pack_file)))
