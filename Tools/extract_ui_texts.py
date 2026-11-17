"""Lists the English menu texts in the code, for the Swedish table (Assets/Resources/Locale/sv.json).

Every LearningProgress.Ui(bs, en, de) call is read; the English argument becomes a key.
Interpolated parts ($"Child {number}") become numbered holes ("Child {0}"). Calls whose
English argument is not a literal (e.g. item.NameEn) are listed separately, so their
values can be added by hand.

Usage:
  Tools/.venv/Scripts/python.exe -I Tools/extract_ui_texts.py            (print what is missing)
  Tools/.venv/Scripts/python.exe -I Tools/extract_ui_texts.py --all      (print every key)
"""

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TABLE = ROOT / "Assets" / "Resources" / "Locale" / "sv.json"


def read_string(code: str, i: int):
    """Reads a C# string literal at i ("..." or $"..."); returns (template, end) or (None, i)."""
    interpolated = code.startswith('$"', i)
    if interpolated:
        i += 1
    if i >= len(code) or code[i] != '"':
        return None, i
    i += 1
    out, holes, depth = [], 0, 0
    while i < len(code):
        c = code[i]
        if c == "\\":
            nxt = code[i + 1]
            out.append({"n": "\n", "t": "\t", '"': '"', "\\": "\\"}.get(nxt, nxt))
            i += 2
            continue
        if interpolated and c == "{":
            if code.startswith("{{", i):
                out.append("{")
                i += 2
                continue
            depth = 1
            i += 1
            while depth and i < len(code):
                depth += {"{": 1, "}": -1}.get(code[i], 0)
                i += 1
            out.append("{" + str(holes) + "}")
            holes += 1
            continue
        if interpolated and code.startswith("}}", i):
            out.append("}")
            i += 2
            continue
        if c == '"':
            return "".join(out), i + 1
        out.append(c)
        i += 1
    return None, i


def arguments(code: str, start: int):
    """Splits the call's arguments at top-level commas; returns the raw argument texts."""
    args, depth, i, begin = [], 0, start, start
    while i < len(code):
        c = code[i]
        if c in '"$' and (code[i] == '"' or code.startswith('$"', i)):
            _, i = read_string(code, i)
            continue
        if c in "([{":
            depth += 1
        elif c in ")]}":
            if depth == 0:
                args.append(code[begin:i].strip())
                return args
            depth -= 1
        elif c == "," and depth == 0:
            args.append(code[begin:i].strip())
            begin = i + 1
        i += 1
    return args


def literal(arg: str):
    """The template of an argument made of string literals joined with +; None otherwise."""
    parts, i = [], 0
    while i < len(arg):
        if arg[i].isspace() or arg[i] == "+":
            i += 1
            continue
        text, end = read_string(arg, i)
        if text is None:
            return None
        parts.append(text)
        i = end
    return "".join(parts) if parts else None


def collect():
    keys, dynamic = set(), []
    for path in sorted((ROOT / "Assets" / "Scripts").rglob("*.cs")):
        code = path.read_text(encoding="utf-8")
        for match in re.finditer(r"\bUi\(", code):
            args = arguments(code, match.end())
            if len(args) != 3:
                continue
            english = literal(args[1])
            if english is None:
                dynamic.append(f"{path.relative_to(ROOT)}: {args[1]}")
            else:
                keys.add(english)
    return keys, dynamic


def main() -> None:
    keys, dynamic = collect()
    table = json.loads(TABLE.read_text(encoding="utf-8")) if TABLE.exists() else {}
    texts = {entry["en"] for entry in table.get("entries", [])}
    shown = sorted(keys) if "--all" in sys.argv else sorted(k for k in keys if k not in texts)
    print(json.dumps(shown, ensure_ascii=False, indent=1))
    print(f"{len(keys)} keys, {len(shown)} shown; not literal:", file=sys.stderr)
    for line in dynamic:
        print("  " + line, file=sys.stderr)


if __name__ == "__main__":
    main()
