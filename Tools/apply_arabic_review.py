"""Reads the reviewer's answers from the Arabic review page and records them.

- "Tačno": the entry's ar_status in Tools/words_world*.json becomes "provjereno"
  (letter names and picture book pages go to Tools/arabic_extra_status.json).
- "Ispravi": nothing is changed; the entry and the reviewer's note are listed in
  Store/arabic-corrections.md, to be fixed by hand (then generate the voices again).

Afterwards Store/arabic-review.md is written again.

Usage:
  Tools/.venv/Scripts/python.exe -I Tools/apply_arabic_review.py path/to/arapski-provjera-rezultati.json
"""

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "Tools"))

import make_arabic_review  # noqa: E402
from make_arabic_review import SECTIONS, entry_id  # noqa: E402

LETTERS_STATUS = ROOT / "Tools" / "arabic_extra_status.json"
CORRECTIONS = ROOT / "Store" / "arabic-corrections.md"


def dump_pack(pack: dict) -> str:
    """The words files' own layout: one entry per line, so a change shows as a one-line diff."""
    lines = ["{"]
    keys = list(pack.keys())
    for i, key in enumerate(keys):
        comma = "," if i < len(keys) - 1 else ""
        value = pack[key]
        if isinstance(value, list):
            lines.append(f"  {json.dumps(key)}: [")
            for j, item in enumerate(value):
                inner = ", ".join(f"{json.dumps(k, ensure_ascii=False)}: {json.dumps(v, ensure_ascii=False)}"
                                  for k, v in item.items())
                lines.append(f"    {{ {inner} }}" + ("," if j < len(value) - 1 else ""))
            lines.append("  ]" + comma)
        else:
            lines.append(f"  {json.dumps(key)}: {json.dumps(value, ensure_ascii=False)}{comma}")
    lines.append("}")
    return "\n".join(lines) + "\n"


def main() -> None:
    if len(sys.argv) != 2:
        sys.exit(__doc__)

    answers = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    by_id = {a["id"]: a for a in answers.get("entries", [])}
    approved = 0
    fixes = []
    changed_text = []

    for pack_file in sorted((ROOT / "Tools").glob("words_world*.json")):
        pack = json.loads(pack_file.read_text(encoding="utf-8"))
        touched = False
        for section, _ in SECTIONS:
            for entry in pack.get(section, []):
                answer = by_id.get(f"{pack['pack']}/{entry_id(section, entry)}")
                # The shared phrases (praise, goal, jump) are listed once but live in every world.
                if answer is None and section == "phrases":
                    answer = next((a for i, a in by_id.items()
                                   if i.endswith("/" + entry_id(section, entry)) and a["ar"] == entry["ar"]), None)
                if answer is None:
                    continue
                if answer["ar"] != entry["ar"]:
                    # The text changed after the page was made: this answer is about the old text.
                    changed_text.append(answer["id"])
                    continue
                if answer["result"] == "ok":
                    if entry.get("ar_status") != "provjereno":
                        entry["ar_status"] = "provjereno"
                        touched = True
                        approved += 1
        if touched:
            pack_file.write_text(dump_pack(pack), encoding="utf-8", newline="\n")

    # The Arabic of letter names and picture book pages as it is now, to catch answers about older text.
    import generate_words as words
    current = {}
    for letter, (name, _) in words.ARABIC_LETTERS.items():
        current["letters/ar_" + words.ARABIC_LETTER_IDS[letter]] = words.arabic_letter_phrase(letter, name)
    books_file = ROOT / "Tools" / "books.json"
    if books_file.exists():
        for book in json.loads(books_file.read_text(encoding="utf-8"))["books"]:
            for number, page in enumerate(book["pages"], start=1):
                current[f"books/{book['pack']}/{number}"] = page["ar"]

    letters = json.loads(LETTERS_STATUS.read_text(encoding="utf-8")) if LETTERS_STATUS.exists() else {}
    for answer_id, answer in by_id.items():
        if answer_id.startswith(("letters/", "books/")) and answer["result"] == "ok":
            if current.get(answer_id) != answer["ar"]:
                changed_text.append(answer_id)
                continue
            letters[answer_id] = "provjereno"
            approved += 1
    if letters:
        LETTERS_STATUS.write_text(json.dumps(letters, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    for answer in by_id.values():
        if answer["result"] == "fix":
            fixes.append(answer)

    lines = ["# Arapski: ispravke od recenzenta", "",
             f"Iz fajla `{Path(sys.argv[1]).name}` (stranica napravljena {answers.get('made', '?')}).", "",
             "| id | bosanski | arapski sada | latinica sada | napomena |", "|---|---|---|---|---|"]
    for answer in fixes:
        note = answer.get("note", "").replace("\n", " ").replace("|", "/")
        lines.append(f"| `{answer['id']}` | {answer['bs']} | {answer['ar']} | {answer['latin']} | {note} |")
    if changed_text:
        lines += ["", "Odgovori za tekst koji je u međuvremenu promijenjen (nisu primijenjeni): "
                  + ", ".join(f"`{i}`" for i in changed_text)]
    CORRECTIONS.write_text("\n".join(lines) + "\n", encoding="utf-8")

    make_arabic_review.main()
    print(f"Approved: {approved}. To fix: {len(fixes)} (see {CORRECTIONS}). Skipped (text changed): {len(changed_text)}.")


if __name__ == "__main__":
    main()
