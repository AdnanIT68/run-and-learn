"""
Pictures for the hazards that are not plain ground blocks: the flyers a child
passes under by staying down (one kind per world) and the rolling football.
Twemoji, rendered at 256 px into Assets/Resources/Hazards/<name>.png with a
sprite .meta copied from the word icons.

Run: Tools/.venv/Scripts/python.exe -I Tools/make_hazards.py
"""

import re
import urllib.request
import uuid
from pathlib import Path

import resvg_py

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Resources" / "Hazards"
META_TEMPLATE = ROOT / "Assets" / "Resources" / "Words" / "Icons" / "star.png.meta"
TWEMOJI_SVG = "https://cdn.jsdelivr.net/gh/jdecked/twemoji@15.1.0/assets/svg/{code}.svg"
SIZE = 256

# Flyers per world: something that belongs in the air there and is friendly to bump into.
PICTURES = {
    "flyer_World1": "1f41d",  # bee in Grandma's Garden
    "flyer_World2": "1f426",  # bird over Grandpa's Farm
    "flyer_World3": "1f98b",  # butterfly at home
    "flyer_World4": "1f54a",  # dove over the bazaar
    "flyer_World5": "1f986",  # duck by the river
    "flyer_World6": "2744",   # snowflake on the mountain
    "flyer_World7": "1fa81",  # kite at school
    "flyer_World8": "1f6f8",  # flying saucer in the night sky
    "flyer_default": "1f426",
    "ball": "26bd",
}


def write_meta(png: Path) -> None:
    meta = png.with_suffix(".png.meta")
    if meta.exists():
        return
    template = META_TEMPLATE.read_text(encoding="utf-8")
    meta.write_text(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, template, count=1), encoding="utf-8")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    folder_meta = OUT.parent / "Hazards.meta"
    if not folder_meta.exists():
        folder_meta.write_text(
            "fileFormatVersion: 2\nguid: " + uuid.uuid4().hex +
            "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n"
            "  assetBundleVariant: \n", encoding="utf-8")

    for name, code in PICTURES.items():
        target = OUT / (name + ".png")
        if not target.exists():
            with urllib.request.urlopen(TWEMOJI_SVG.format(code=code), timeout=30) as response:
                svg = response.read().decode("utf-8")
            target.write_bytes(bytes(resvg_py.svg_to_bytes(svg_string=svg, width=SIZE, height=SIZE)))
        write_meta(target)
        print("ok", name)


if __name__ == "__main__":
    main()
