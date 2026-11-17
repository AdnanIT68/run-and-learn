# Run and Learn 1.3.0 (versionCode 6): objava na Google Play

Fajl za upload: `Builds\RunAndLearn-1.3.0-6.aab` (potpisan ključem za upload).
Bilješke o izdanju: `Store/release-notes-1.3.0.md` (en, hr = bosanski tekst, de; svaka ispod 500 znakova).
Opisi: `Store/listing.md`. Snimke: `Store/play/screenshots/<en|hr|de>/` (telefon) i
`Store/play/tablet/<en|hr|de>/` (tablet 7" i 10").

## 1. Prvo interno testiranje (preporuka, ~10 minuta)

Interni test se instalira s Playa preko postojeće igre: ne treba deinstalirati, napredak ostaje,
i 12 testera zatvorenog testiranja ništa ne primijeti.

1. Play Console → Run and Learn → Testiranje → Interno testiranje → Napravi novo izdanje.
2. Prevuci `RunAndLearn-1.3.0-6.aab`.
3. Bilješke: zalijepi tri bloka iz `Store/release-notes-1.3.0.md`.
4. Sačuvaj → Pregled izdanja → Pokreni uvođenje u interno testiranje.
5. Na telefonu otvori link za interno testiranje i ažuriraj igru s Playa.
6. Prođi listu `Builds\Test-na-telefonu-1.3.md` (posebno: stari napredak ostao, dijeljenje diplome i radnog lista, slikovnica, novi svijet).

Stanje 17. 11. 2026 u 10:51: build 6 (1.3.0) je OBJAVLJEN u internom testiranju (link
https://play.google.com/apps/internaltest/4701732897561276308, lista "ProfitPilot Testeri").

## 2. Kad interni test prođe

Prijava za produkciju: odgovori su pripremljeni u `Store/prijava-za-produkciju.md`.

- Ako je odobren pristup produkciji (14 dana s 12 testera, oko 18. 11.): Produkcija → Napravi novo
  izdanje → "Dodaj iz biblioteke" (isti build 6) → bilješke → slanje na pregled.
- Ako još nije: isti build u Zatvoreno testiranje (Alpha), da testeri dobiju 1.3.

## 3. Stranica u trgovini (može odmah, neovisno o buildu)

- Opisi iz `Store/listing.md` (en-US, hr, de-DE): kratki i puni opis.
- Snimke za telefon zamijeniti novima; dodati snimke za tablet 7" i 10" (isti set za oba).
- Napomena: arapski ide u 1.3 bez provjere izvornog govornika (Adnanova odluka 17. 11. 2026).
  Paket za provjeru: `Store/arabic-review.zip`; ispravke idu u 1.3.1.
