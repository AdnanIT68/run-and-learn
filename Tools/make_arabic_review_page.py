"""Builds Store/arabic-review/ (and Store/arabic-review.zip): a page for a native speaker
to check every Arabic text in the game, with the picture, the text with harakat, the
Latin letters and the game's own voice to listen to.

The reviewer opens index.html in any browser (no internet needed), marks each entry
"Tačno" or "Ispravi" with a note, and sends back the file the page saves
(arapski-provjera-rezultati.json). Tools/apply_arabic_review.py then marks the approved
entries as checked and lists the corrections.

Usage:
  Tools/.venv/Scripts/python.exe -I Tools/make_arabic_review_page.py
"""

import base64
import datetime
import json
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "Tools"))

from PIL import Image  # noqa: E402

import generate_words as words  # noqa: E402
from make_arabic_review import SECTIONS, entry_id  # noqa: E402

OUT = ROOT / "Store" / "arabic-review"
ZIP = ROOT / "Store" / "arabic-review.zip"
WORDS = ROOT / "Assets" / "Resources" / "Words"
TRACING_AUDIO = ROOT / "Assets" / "Resources" / "Tracing" / "Audio" / "ar"
FONT = ROOT / "Assets" / "Resources" / "Fonts" / "NotoSansArabic-Bold.ttf"
LETTERS_STATUS = ROOT / "Tools" / "arabic_letters_status.json"


def ffmpeg() -> str:
    found = shutil.which("ffmpeg")
    if not found:
        sys.exit("ffmpeg is needed to convert the voices to mp3.")
    return found


def to_mp3(source: Path, target: Path, tool: str) -> bool:
    if not source.exists():
        return False
    subprocess.run([tool, "-y", "-loglevel", "error", "-i", str(source), "-ac", "1", "-b:a", "64k", str(target)],
                   check=True)
    return True


def thumbnail(source: Path, target: Path) -> bool:
    if not source.exists():
        return False
    with Image.open(source) as image:
        image = image.convert("RGBA")
        image.thumbnail((160, 160))
        image.save(target, optimize=True)
    return True


def note_of(status: str) -> str:
    prefix = "za provjeru"
    if not status.startswith(prefix):
        return ""
    return status[len(prefix):].lstrip(": ").strip()


def collect() -> list:
    """Every Arabic text, grouped by world, with its voice and picture files."""
    groups = []
    seen_phrases = set()
    letter_status = json.loads(LETTERS_STATUS.read_text(encoding="utf-8")) if LETTERS_STATUS.exists() else {}

    pack_files = sorted((ROOT / "Tools").glob("words_world*.json"),
                        key=lambda f: int(f.stem.replace("words_world", "")))
    for pack_file in pack_files:
        pack = json.loads(pack_file.read_text(encoding="utf-8"))
        name = pack["pack"]
        pictures = WORDS / name / "Pictures"
        items = []

        for section, label in SECTIONS:
            for entry in pack.get(section, []):
                key = entry_id(section, entry)
                if section == "phrases":
                    if (key, entry["ar"]) in seen_phrases:
                        continue
                    seen_phrases.add((key, entry["ar"]))

                picture = None
                for candidate in (entry.get("picture"), key, entry.get("word"), entry.get("id")):
                    if candidate and (pictures / f"{candidate}.png").exists():
                        picture = pictures / f"{candidate}.png"
                        break

                status = entry.get("ar_status", "za provjeru")
                items.append({
                    "id": f"{name}/{key}",
                    "kind": label,
                    "bs": entry["bs"],
                    "ar": entry["ar"],
                    "latin": entry["ar_latin"],
                    "note": note_of(status),
                    "checked": not status.startswith("za provjeru"),
                    "audio_src": WORDS / name / "Audio" / "ar" / f"{key}.ogg",
                    "picture_src": picture,
                })

        groups.append({"title": f"{pack['title_bs']}", "items": items})

    # Letter names: the school world speaks a few, "Piši slovo" speaks all 28.
    letters = []
    for letter, (letter_name, latin) in words.ARABIC_LETTERS.items():
        letter_id = "ar_" + words.ARABIC_LETTER_IDS[letter]
        status = letter_status.get(f"letters/{letter_id}", "za provjeru")
        letters.append({
            "id": f"letters/{letter_id}",
            "kind": "slovo",
            "bs": f"slovo {latin}",
            "ar": words.arabic_letter_phrase(letter, letter_name),
            "shown": letter,
            "latin": f"harfu-l-{latin}",
            "note": "glas izgovara „slovo …“ s članom (sunčana slova se udvajaju)",
            "checked": not status.startswith("za provjeru"),
            "audio_src": TRACING_AUDIO / f"{letter_id}.ogg",
            "picture_src": None,
        })
    groups.append({"title": "Imena slova (Škola i „Piši slovo“)", "items": letters})
    return groups


def build() -> None:
    tool = ffmpeg()
    if OUT.exists():
        shutil.rmtree(OUT)
    (OUT / "audio").mkdir(parents=True)
    (OUT / "pics").mkdir()

    groups = collect()
    total = 0
    missing_audio = []

    for group in groups:
        for item in group["items"]:
            total += 1
            safe = item["id"].replace("/", "_")
            audio = OUT / "audio" / f"{safe}.mp3"
            item["audio"] = f"audio/{safe}.mp3" if to_mp3(item.pop("audio_src"), audio, tool) else ""
            if not item["audio"]:
                missing_audio.append(item["id"])
            source = item.pop("picture_src")
            picture = OUT / "pics" / f"{source.stem}_{safe}.png" if source else None
            item["picture"] = f"pics/{picture.name}" if picture and thumbnail(source, picture) else ""

    data = {
        "made": datetime.date.today().isoformat(),
        "total": total,
        "groups": groups,
    }
    font = base64.b64encode(FONT.read_bytes()).decode("ascii")
    page = (TEMPLATE
            .replace("__FONT__", font)
            .replace("__TOTAL__", str(total))
            .replace("__DATA__", json.dumps(data, ensure_ascii=False).replace("</", "<\\/")))
    (OUT / "index.html").write_text(page, encoding="utf-8")
    (OUT / "PROCITAJ.txt").write_text(README, encoding="utf-8")

    if ZIP.exists():
        ZIP.unlink()
    with zipfile.ZipFile(ZIP, "w", zipfile.ZIP_DEFLATED) as archive:
        for path in sorted(OUT.rglob("*")):
            if path.is_file():
                archive.write(path, Path("arabic-review") / path.relative_to(OUT))

    size = ZIP.stat().st_size / 1024 / 1024
    print(f"{OUT / 'index.html'}: {total} entries")
    print(f"{ZIP}: {size:.1f} MB")
    if missing_audio:
        print("No voice for:", ", ".join(missing_audio))


README = """Run and Learn: provjera arapskog

1. Raspakujte folder i otvorite index.html (dvoklik; radi u Chromeu, Edgeu, Firefoxu,
   i na telefonu). Internet nije potreban.
2. Za svaku riječ poslušajte glas (dugme ▶), pogledajte arapsko pismo s harakatima i
   latinicu, pa izaberite „Tačno“ ili „Ispravi“ i napišite šta treba promijeniti.
3. Odgovori se pamte u pregledniku, pa možete stati i nastaviti kasnije (isti preglednik).
4. Na kraju dugme „Sačuvaj rezultate“ snimi fajl arapski-provjera-rezultati.json.
   Pošaljite taj fajl nazad. Ako snimanje ne radi, „Prikaži kao tekst“ pa kopirajte tekst.

Hvala!
"""

TEMPLATE = r"""<!doctype html>
<html lang="bs">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Provjera arapskog</title>
<style>
@font-face { font-family: "GameArabic"; src: url(data:font/ttf;base64,__FONT__) format("truetype"); font-weight: 700; }
:root {
  --bg: #f6f3ec; --card: #ffffff; --ink: #1f2733; --muted: #5d6673; --line: #e3ddd0;
  --accent: #2f6fb5; --ok: #2f8f46; --ok-bg: #e5f4e8; --fix: #c2410c; --fix-bg: #fdecdf; --bar: #ffffffee;
}
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    --bg: #14181f; --card: #1d232c; --ink: #e8ecf1; --muted: #9aa4b1; --line: #2c3440;
    --accent: #6ea8ea; --ok: #5cc277; --ok-bg: #1d3325; --fix: #f08a4b; --fix-bg: #3a2519; --bar: #1d232cee;
  }
}
* { box-sizing: border-box; }
body { margin: 0; background: var(--bg); color: var(--ink); font: 16px/1.45 system-ui, -apple-system, "Segoe UI", Roboto, sans-serif; }
main { max-width: 860px; margin: 0 auto; padding: 20px 16px 120px; }
h1 { font-size: 1.6rem; margin: 0 0 4px; }
h2 { font-size: 1.15rem; margin: 32px 0 10px; }
.sub { color: var(--muted); margin: 0 0 14px; }
details { background: var(--card); border: 1px solid var(--line); border-radius: 12px; padding: 10px 14px; }
details summary { cursor: pointer; font-weight: 600; }
details p, details li { color: var(--muted); }
.tools { display: flex; flex-wrap: wrap; gap: 10px; align-items: center; margin: 16px 0 0; }
.tools label { display: flex; gap: 6px; align-items: center; color: var(--muted); }
.card { display: grid; grid-template-columns: 76px 1fr; gap: 12px; background: var(--card); border: 1px solid var(--line);
  border-radius: 14px; padding: 12px; margin: 10px 0; }
.card.ok { border-color: var(--ok); }
.card.fix { border-color: var(--fix); }
.pic { width: 76px; height: 76px; border-radius: 10px; background: var(--bg); display: grid; place-items: center; overflow: hidden; }
.pic img { max-width: 70px; max-height: 70px; }
.pic .glyph { font-family: "GameArabic", "Noto Naskh Arabic", "Traditional Arabic", serif; font-size: 2.6rem; }
.top { display: flex; justify-content: space-between; gap: 8px; align-items: baseline; flex-wrap: wrap; }
.bs { font-weight: 600; }
.kind { color: var(--muted); font-size: .85rem; }
.ar { font-family: "GameArabic", "Noto Naskh Arabic", "Traditional Arabic", serif; font-size: 2rem; line-height: 1.9;
  direction: rtl; text-align: right; overflow-wrap: anywhere; }
.latin { color: var(--muted); }
.note { font-size: .85rem; color: var(--fix); margin-top: 2px; }
.actions { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 8px; }
button { font: inherit; border-radius: 999px; border: 1px solid var(--line); background: var(--card); color: var(--ink);
  padding: 7px 14px; cursor: pointer; min-height: 40px; }
button.play { border-color: var(--accent); color: var(--accent); }
button.okb[aria-pressed="true"] { background: var(--ok-bg); border-color: var(--ok); color: var(--ok); font-weight: 600; }
button.fixb[aria-pressed="true"] { background: var(--fix-bg); border-color: var(--fix); color: var(--fix); font-weight: 600; }
button.primary { background: var(--accent); border-color: var(--accent); color: #fff; font-weight: 600; }
textarea { width: 100%; min-height: 70px; margin-top: 8px; font: inherit; color: var(--ink); background: var(--bg);
  border: 1px solid var(--line); border-radius: 10px; padding: 8px; }
.bar { position: fixed; left: 0; right: 0; bottom: 0; background: var(--bar); border-top: 1px solid var(--line);
  backdrop-filter: blur(6px); }
.bar div { max-width: 860px; margin: 0 auto; padding: 10px 16px; display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
.progress { flex: 1 1 160px; color: var(--muted); }
.meter { height: 6px; background: var(--line); border-radius: 3px; overflow: hidden; margin-top: 4px; }
.meter i { display: block; height: 100%; background: var(--ok); width: 0; }
#export { width: 100%; min-height: 160px; font: 13px/1.4 ui-monospace, Consolas, monospace; display: none; }
.hidden { display: none; }
</style>
</head>
<body>
<main>
  <h1>Provjera arapskog za Run and Learn</h1>
  <p class="sub">Igra za djecu od 4 do 10 godina koja govore bosanski. Ukupno __TOTAL__ unosa.</p>
  <details open>
    <summary>Šta provjeriti</summary>
    <ul>
      <li>Je li prijevod tačan i prirodan za malo dijete.</li>
      <li>Harakati (znakovi za samoglasnike) i pisanje.</li>
      <li>Latinica ispod riječi, pisana za bosansko čitanje: dž = ج, š = ش, h = ح/خ/ه, ' = ع ili hamza, â î û = dug samoglasnik.</li>
      <li>Izgovor u glasu (dugme ▶). Glas je sintetički (Azure), pa je važno čuti greške u izgovoru.</li>
    </ul>
    <p>Korištena pravila: standardni arapski (fusha), riječi bez člana „al-“, u pauzalnom obliku. Imperativ je u muškom rodu
      (npr. „Skoči!“); ako mislite da treba drugačije, napišite.</p>
    <p>Odgovori se pamte u ovom pregledniku. Na kraju pritisnite „Sačuvaj rezultate“ i pošaljite fajl.</p>
  </details>
  <div class="tools">
    <label><input type="checkbox" id="onlyOpen"> Prikaži samo neodgovorene</label>
  </div>
  <div id="list"></div>
  <textarea id="export" readonly aria-label="Rezultati kao tekst"></textarea>
</main>
<div class="bar"><div>
  <div class="progress"><span id="count"></span><div class="meter"><i id="meter"></i></div></div>
  <button id="asText">Prikaži kao tekst</button>
  <button class="primary" id="save">Sačuvaj rezultate</button>
</div></div>
<script id="data" type="application/json">__DATA__</script>
<script>
(function () {
  var DATA = JSON.parse(document.getElementById("data").textContent);
  var KEY = "rl-arabic-review-v1";
  var state = {};
  try { state = JSON.parse(localStorage.getItem(KEY)) || {}; } catch (e) { state = {}; }
  function store() { try { localStorage.setItem(KEY, JSON.stringify(state)); } catch (e) {} }

  var player = new Audio();
  var list = document.getElementById("list");
  var onlyOpen = document.getElementById("onlyOpen");
  var cards = [];

  function el(tag, cls, text) {
    var node = document.createElement(tag);
    if (cls) node.className = cls;
    if (text != null) node.textContent = text;
    return node;
  }

  DATA.groups.forEach(function (group) {
    list.appendChild(el("h2", null, group.title));
    group.items.forEach(function (item) {
      var card = el("section", "card");
      var pic = el("div", "pic");
      if (item.picture) { var img = el("img"); img.src = item.picture; img.alt = ""; pic.appendChild(img); }
      else if (item.shown) { var g = el("span", "glyph", item.shown); g.lang = "ar"; pic.appendChild(g); }
      card.appendChild(pic);

      var body = el("div");
      var top = el("div", "top");
      top.appendChild(el("span", "bs", item.bs));
      top.appendChild(el("span", "kind", item.kind + " · " + item.id));
      body.appendChild(top);
      var ar = el("div", "ar", item.ar); ar.lang = "ar"; ar.dir = "rtl";
      body.appendChild(ar);
      body.appendChild(el("div", "latin", item.latin));
      if (item.note) body.appendChild(el("div", "note", "Napomena: " + item.note));
      if (item.checked) body.appendChild(el("div", "kind", "Već provjereno ranije."));

      var actions = el("div", "actions");
      var play = el("button", "play", "▶ Poslušaj");
      if (!item.audio) { play.disabled = true; play.textContent = "Nema glasa"; }
      play.addEventListener("click", function () { player.src = item.audio; player.play().catch(function () {}); });
      var ok = el("button", "okb", "✓ Tačno");
      var fix = el("button", "fixb", "✎ Ispravi");
      actions.appendChild(play); actions.appendChild(ok); actions.appendChild(fix);
      body.appendChild(actions);

      var note = el("textarea");
      note.placeholder = "Šta treba promijeniti: arapski tekst, harakati, latinica ili izgovor";
      body.appendChild(note);
      card.appendChild(body);
      list.appendChild(card);

      function show() {
        var answer = state[item.id] || {};
        ok.setAttribute("aria-pressed", answer.result === "ok");
        fix.setAttribute("aria-pressed", answer.result === "fix");
        card.classList.toggle("ok", answer.result === "ok");
        card.classList.toggle("fix", answer.result === "fix");
        note.classList.toggle("hidden", answer.result !== "fix");
        note.value = answer.note || "";
        card.classList.toggle("hidden", onlyOpen.checked && !!answer.result);
      }
      function set(result) {
        var answer = state[item.id] || {};
        answer.result = answer.result === result ? undefined : result;
        if (!answer.result) delete state[item.id]; else state[item.id] = answer;
        store(); show(); progress();
        if (answer.result === "fix") note.focus();
      }
      ok.addEventListener("click", function () { set("ok"); });
      fix.addEventListener("click", function () { set("fix"); });
      note.addEventListener("input", function () {
        var answer = state[item.id] || { result: "fix" };
        answer.note = note.value; state[item.id] = answer; store();
      });
      cards.push({ item: item, show: show });
    });
  });

  function progress() {
    var done = 0, fixes = 0;
    cards.forEach(function (c) {
      var a = state[c.item.id];
      if (a && a.result) { done++; if (a.result === "fix") fixes++; }
    });
    document.getElementById("count").textContent =
      "Odgovoreno " + done + " od " + DATA.total + (fixes ? " · za ispravku: " + fixes : "");
    document.getElementById("meter").style.width = (100 * done / DATA.total) + "%";
  }

  function results() {
    var entries = [];
    cards.forEach(function (c) {
      var a = state[c.item.id];
      if (a && a.result) {
        entries.push({ id: c.item.id, bs: c.item.bs, ar: c.item.ar, latin: c.item.latin,
          result: a.result, note: a.note || "" });
      }
    });
    return JSON.stringify({ made: DATA.made, answered: entries.length, total: DATA.total, entries: entries }, null, 2);
  }

  document.getElementById("save").addEventListener("click", function () {
    var blob = new Blob([results()], { type: "application/json" });
    var link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = "arapski-provjera-rezultati.json";
    document.body.appendChild(link); link.click(); link.remove();
  });
  document.getElementById("asText").addEventListener("click", function () {
    var box = document.getElementById("export");
    box.value = results(); box.style.display = "block"; box.focus(); box.select();
  });
  onlyOpen.addEventListener("change", function () { cards.forEach(function (c) { c.show(); }); });

  cards.forEach(function (c) { c.show(); });
  progress();
})();
</script>
</body>
</html>
"""

if __name__ == "__main__":
    build()
