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
    # One ground hazard per world that belongs there, jumped like a crate.
    "prop_World1": "1faa3",  # bucket in the garden
    "prop_World2": "1f411",  # sheep on the farm
    "prop_World3": "1f9f8",  # teddy bear left on the floor
    "prop_World4": "1f3fa",  # clay jug at a bazaar stall
    "prop_World5": "1fab5",  # log by the river
    "prop_World6": "26c4",   # snowman on the mountain
    "prop_World7": "1f4da",  # pile of books at school
    "prop_World8": "1faa8",  # space rock (tinted in the game)
}

# Backdrop decoration: clouds or stars in the sky, plants in front of the track.
DECOR = {
    "cloud": "2601",
    "star": "2b50",
    "front_World1_a": "1f337", "front_World1_b": "1f33c",
    "front_World2_a": "1f33e", "front_World2_b": "1f33b",
    "front_World3_a": "1fab4", "front_World3_b": "1f33f",
    "front_World4_a": "1fab4", "front_World4_b": "1f9fa",
    "front_World5_a": "1f33e", "front_World5_b": "1faa8",
    "front_World6_a": "1f332", "front_World6_b": "1faa8",
    "front_World7_a": "1f33c", "front_World7_b": "1f33f",
    "front_World8_a": "1f344", "front_World8_b": "1faa8",
}
DECOR_OUT = ROOT / "Assets" / "Resources" / "Worlds" / "Decor"


def write_meta(png: Path) -> None:
    meta = png.with_suffix(".png.meta")
    if meta.exists():
        return
    template = META_TEMPLATE.read_text(encoding="utf-8")
    meta.write_text(re.sub(r"guid: [0-9a-f]{32}", "guid: " + uuid.uuid4().hex, template, count=1), encoding="utf-8")


def write_folder(folder: Path) -> None:
    folder.mkdir(parents=True, exist_ok=True)
    folder_meta = folder.parent / (folder.name + ".meta")
    if not folder_meta.exists():
        folder_meta.write_text(
            "fileFormatVersion: 2\nguid: " + uuid.uuid4().hex +
            "\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n"
            "  assetBundleVariant: \n", encoding="utf-8")


def render(folder: Path, pictures: dict) -> None:
    write_folder(folder)
    for name, code in pictures.items():
        target = folder / (name + ".png")
        if not target.exists():
            with urllib.request.urlopen(TWEMOJI_SVG.format(code=code), timeout=30) as response:
                svg = response.read().decode("utf-8")
            target.write_bytes(bytes(resvg_py.svg_to_bytes(svg_string=svg, width=SIZE, height=SIZE)))
        write_meta(target)
        print("ok", name)


def main() -> None:
    render(OUT, PICTURES)
    render(DECOR_OUT, DECOR)


if __name__ == "__main__":
    main()
