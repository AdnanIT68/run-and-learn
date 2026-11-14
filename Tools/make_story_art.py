"""Draws Brbljavka the magpie for the story (flat, Twemoji-like style).

Writes Assets/Resources/Story/magpie.png (wings up) and magpie_down.png (wings
down) at 512 px, plus magpie_letters.png and magpie_down_letters.png: the magpie
carrying stolen letters, for the chase at the end of a world.

Usage:
  Tools/.venv/Scripts/python.exe Tools/make_story_art.py
"""

from pathlib import Path

import resvg_py

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / "Assets" / "Resources" / "Story"
SIZE = 512

BLACK = "#22262e"
SHEEN = "#2f5d8a"
SHEEN_LIGHT = "#4f86b8"
WHITE = "#f7f8fa"
BEAK = "#3b3f47"
EYE = "#ffffff"
FOOT = "#e0a030"


def magpie(wing_up: bool, letters: bool) -> str:
    wing = (
        # Raised wing: a fan above the back.
        f'<path d="M58 62 C60 34 78 18 98 16 C92 30 90 44 80 58 Z" fill="{SHEEN}"/>'
        f'<path d="M64 60 C68 40 80 28 94 24 C88 38 84 48 76 58 Z" fill="{SHEEN_LIGHT}"/>'
        f'<path d="M60 62 C62 52 66 46 72 44 C70 52 68 58 66 64 Z" fill="{WHITE}"/>'
        if wing_up else
        # Folded wing along the side.
        f'<path d="M50 66 C62 58 86 60 96 74 C84 82 64 84 50 78 Z" fill="{SHEEN}"/>'
        f'<path d="M58 68 C68 64 84 66 90 74 C80 78 66 79 58 76 Z" fill="{SHEEN_LIGHT}"/>'
        f'<path d="M52 66 C56 62 62 61 66 62 C62 66 58 70 54 72 Z" fill="{WHITE}"/>'
    )
    carried = (
        # Three stolen letter cards hanging from the beak on a string.
        '<path d="M104 58 C106 70 104 84 98 94" stroke="#8a6a40" stroke-width="1.6" fill="none"/>'
        '<g transform="rotate(-8 92 100)"><rect x="82" y="92" width="18" height="20" rx="3" fill="#ffd54f"/>'
        '<text x="91" y="108" font-family="Arial" font-weight="bold" font-size="14" text-anchor="middle" fill="#7a4b00">A</text></g>'
        '<g transform="rotate(10 106 110)"><rect x="98" y="100" width="18" height="20" rx="3" fill="#81d4fa"/>'
        '<text x="107" y="116" font-family="Arial" font-weight="bold" font-size="14" text-anchor="middle" fill="#0b4f72">B</text></g>'
        if letters else ""
    )
    return f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128">
  <!-- long tail -->
  <path d="M46 74 L6 92 C4 94 6 98 9 97 L50 84 Z" fill="{SHEEN}"/>
  <path d="M46 76 L14 90 L48 82 Z" fill="{SHEEN_LIGHT}"/>
  <!-- legs -->
  <path d="M74 92 L72 108 M84 92 L86 108" stroke="{FOOT}" stroke-width="3" stroke-linecap="round"/>
  <path d="M66 109 L78 109 M80 109 L92 109" stroke="{FOOT}" stroke-width="3" stroke-linecap="round"/>
  <!-- body: black back, white belly -->
  <ellipse cx="74" cy="74" rx="28" ry="20" fill="{BLACK}"/>
  <path d="M60 80 C66 94 90 96 100 80 C96 72 88 70 80 72 C72 74 64 76 60 80 Z" fill="{WHITE}"/>
  {wing}
  <!-- head -->
  <circle cx="98" cy="52" r="16" fill="{BLACK}"/>
  <path d="M112 50 L126 54 L112 58 Z" fill="{BEAK}"/>
  <circle cx="103" cy="48" r="5.5" fill="{EYE}"/>
  <circle cx="104.5" cy="48" r="3" fill="{BLACK}"/>
  <circle cx="105.5" cy="47" r="1" fill="{EYE}"/>
  <!-- cheeky eyebrow -->
  <path d="M97 41 C100 39 105 39 108 41" stroke="{WHITE}" stroke-width="1.5" fill="none" stroke-linecap="round"/>
  {carried}
</svg>"""


def render(svg: str, name: str) -> None:
    png = resvg_py.svg_to_bytes(svg_string=svg, width=SIZE, height=SIZE)
    (OUT / name).write_bytes(bytes(png))


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    render(magpie(wing_up=True, letters=False), "magpie.png")
    render(magpie(wing_up=False, letters=False), "magpie_down.png")
    render(magpie(wing_up=True, letters=True), "magpie_letters.png")
    render(magpie(wing_up=False, letters=True), "magpie_down_letters.png")
    print("Brbljavka ->", OUT)


if __name__ == "__main__":
    main()
