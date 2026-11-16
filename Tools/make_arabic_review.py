"""Writes Store/arabic-review.md: every Arabic text in the game for a native speaker to check.

Every entry in Tools/words_world*.json carries "ar" (with harakat), "ar_latin" and
"ar_status". The status starts as "za provjeru"; once a reviewer approves an entry,
set it to "provjereno" and run this again.

Usage:
  Tools/.venv/Scripts/python.exe Tools/make_arabic_review.py
"""

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "Tools"))
SECTIONS = [
    ("words", "riječ"), ("counts", "brojanje"), ("commands", "komanda"), ("sizes", "veliko/malo"),
    ("sentences", "rečenica"), ("numbers", "broj"), ("phrases", "fraza"),
]


def entry_id(section: str, entry: dict) -> str:
    if section == "counts":
        return f"{entry['word']}_{entry['count']}"
    if section == "sizes":
        return f"{entry['word']}_{entry['size']}"
    if section == "numbers":
        return f"num_{entry['n']}"
    return entry["id"]


def main() -> None:
    lines = [
        "# Arapski: lista za provjeru",
        "",
        "Sve arapske riječi i rečenice u igri, za provjeru od strane nekoga ko zna arapski,",
        "prije objave. Provjeriti: prijevod, harakate (znakove za samoglasnike), latinicu i",
        "izgovor u glasu (`Assets/Resources/Words/<Svijet>/Audio/ar/<id>.ogg`).",
        "",
        "Pravila koja su korištena: standardni arapski (fusha), riječi bez člana „al-“, u",
        "pauzalnom obliku (bez padežnog nastavka na kraju). Latinica je prilagođena bosanskom",
        "čitanju: dž = ج, š = ش, h = ح/خ/ه, ' = ع ili hamza, kapica (â, î, û) = dug samoglasnik.",
        "Fraze iz reda „intro“ igra pušta na bosanskom; arapska verzija postoji, ali se ne čuje.",
        "",
        "Za recenzenta: `Tools/make_arabic_review_page.py` pravi `Store/arabic-review.zip` (slika, tekst, glas);",
        "njegov fajl s rezultatima upisuje `Tools/apply_arabic_review.py`, koji ponovo piše i ovu listu.",
        "",
    ]
    pending = 0
    total = 0
    seen_phrases = set()

    for pack_file in sorted((ROOT / "Tools").glob("words_world*.json"), key=lambda f: int(f.stem.replace("words_world", ""))):
        pack = json.loads(pack_file.read_text(encoding="utf-8"))
        lines += [f"## {pack['pack']}: {pack['title_bs']}", "",
                  "| vrsta | id | bosanski | arapski | latinica | status |",
                  "|---|---|---|---|---|---|"]
        for section, label in SECTIONS:
            for entry in pack.get(section, []):
                key = entry_id(section, entry)
                # The same praise and goal lines repeat in every world; list them once.
                if section == "phrases":
                    if (key, entry["ar"]) in seen_phrases:
                        continue
                    seen_phrases.add((key, entry["ar"]))
                status = entry.get("ar_status", "za provjeru")
                total += 1
                pending += status.startswith("za provjeru")
                lines.append(f"| {label} | `{key}` | {entry['bs']} | {entry['ar']} | {entry['ar_latin']} | {status} |")
        lines.append("")

    # Letter names (the school world and "Piši slovo") come from the generator, not from the JSON;
    # their review status is kept in Tools/arabic_letters_status.json (Tools/apply_arabic_review.py).
    import generate_words as words
    status_file = ROOT / "Tools" / "arabic_letters_status.json"
    letter_status = json.loads(status_file.read_text(encoding="utf-8")) if status_file.exists() else {}
    lines += ["## Imena slova (Škola i „Piši slovo“; glas kaže „slovo …“)", "",
              "| id | slovo | izgovoreno | latinica | status |", "|---|---|---|---|---|"]
    for letter, (name, latin) in words.ARABIC_LETTERS.items():
        letter_id = "ar_" + words.ARABIC_LETTER_IDS[letter]
        status = letter_status.get(f"letters/{letter_id}", "za provjeru")
        total += 1
        pending += status.startswith("za provjeru")
        lines.append(f"| `{letter_id}` | {letter} | {words.arabic_letter_phrase(letter, name)} | {latin} | {status} |")
    lines.append("")

    lines.insert(13, f"Ukupno {total} unosa, od toga {pending} čeka provjeru.")
    lines.insert(14, "")
    out = ROOT / "Store" / "arabic-review.md"
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"{out}: {total} entries, {pending} to check")


if __name__ == "__main__":
    main()
