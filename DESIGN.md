# Rukajja: Run & Learn (radni naziv)

Edukativni beskonačni trkač za telefone (Android, položeno). Djeca iz dijaspore uče
bosanski, a djeca iz BiH engleski. Isti sadržaj radi u oba smjera.

## Pravila sadržaja (obavezna)

- **Bez muzike i instrumenata.** Dozvoljeni su zvučni efekti i izgovorene riječi.
  Eventualno samo tihi nasheedi ili kaside bez instrumenata, kao opcija koja se može isključiti.
- **Ništa suprotno islamskim vrijednostima:** bez svinja, alkohola, kockanja i nasumičnih
  plaćenih nagrada (loot box), bez praznika drugih vjera, odjeća likova uvijek pristojna.
- **Imena likova:** uglavnom bosanska muslimanska (Amina, Emir, Hana, Harun, Lejla, Tarik,
  Selma, Kenan), poneko međunarodno (Adam, Sara, Lina, Jusuf).
- **Za djecu:** bez kazne za grešku, bez reklama, kupovina samo iza roditeljske kapije.

## Glavna mehanika: "Uhvati riječ"

Glas izgovori riječ, ispred Rukajje lebde 3 balončića sa slikama, dijete skokom uhvati
pravi. Greška nije kazna: glas ponovi riječ i pravi balončić zasvijetli. Riječi koje dijete
promaši vraćaju se češće.

## Napredovanje

Priča: svraka Brbljavka ukrala je riječi, a papagaj Pepi pomaže Rukajji (govori oba jezika).
Svaki svijet donosi novu temu, novu mehaniku i jedan korak više u jeziku:

1. Nenina bašta — voće, povrće, boje (riječi)
2. Farma kod dede — životinje, brojevi (broj + riječ)
3. Naša kuća — porodica, hrana (položaj)
4. Čaršija — grad, odjeća (glasovne komande: "Skoči!")
5. Rijeke i Stari most — priroda, vrijeme (pridjevi)
6. Planina zimi — godišnja doba, tijelo (rečenice)
7. Škola — slova, brojevi (čitanje)
8. Noćno nebo — finale: 10 noćnih riječi + ponavljanje svega iz svjetova 1–7 (cilj 8 riječi, bez dugmeta DALJE)

Prva verzija za objavu: samo svijet 1.

## Sadržaj riječi

- Liste riječi: `Tools/words_world1.json` (Nenina bašta), `Tools/words_world2.json` (Farma kod dede), `Tools/words_world3.json` (Naša kuća), `Tools/words_world4.json` (Čaršija, s komandama), `Tools/words_world5.json` (Rijeka i Stari most, veliko/malo), `Tools/words_world6.json` (Planina zimi, rečenice), `Tools/words_world7.json` (Škola: slova, brojevi, čitanje), `Tools/words_world8.json` (Noćno nebo, finale: `"review": true` spaja izazove i riječi ranijih svjetova, `"goal": 8`)
- Generisanje slika i glasova: `Tools/.venv/Scripts/python.exe Tools/generate_words.py Tools/words_world2.json`
- Novi svijet: dodati JSON, pokrenuti generator, upisati ime u `WordPack.PackOrder` i sliku u `WordPack.CoverWordId`.
- Svijet se otključava kad dijete nauči 6 riječi u prethodnom svijetu (`LearningProgress.WordsToUnlockNext`).
- Slike i glasovi su privremeni (Twemoji, sintetički glas). Zamjena: isti nazivi fajlova
  u `Assets/Resources/Words/<Pack>/`. Izvori: `Assets/Resources/Words/SOURCES.md`.

## Build za telefon

```
"C:/Program Files/Unity/Hub/Editor/6000.3.1f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod FootballRunner.EditorTools.CommandLineBuild.BuildAndroidApk -logFile Logs/build.log
```

APK se pojavi u `Builds/`. Unity Editor mora biti zatvoren dok build radi.
