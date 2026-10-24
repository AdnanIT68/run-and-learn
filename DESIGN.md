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

## Likovi (igrači)

- 10 likova, mala fudbalska ekipa (ideja vlasnika): Rukajja, Emir, Amina otvoreni odmah; Harun 40, Lejla 60, Tarik (golman) 90, Selma 120, Kenan 160, Hana 200, Adam 250 novčića. Fiksna cijena, bez nasumičnih nagrada.
- Spisak i cijene: `Assets/Scripts/Player/CharacterRoster.cs`; izbor u meniju: `Assets/Scripts/UI/CharacterChooserUi.cs` (dugme IGRAČI ili dodir na velikog lika).
- Crtanje: `Tools/.venv/Scripts/python.exe Tools/make_characters.py` → `Assets/Resources/Characters/<id>/` (9 sličica kao kod Rukajje + portrait). Pravila: šorc do koljena i štucne preko koljena, djevojčice duge rukave, neke sportski hidžab.
- U igri `CharacterSkin` mijenja Rukajjine sličice istoimenim sličicama izabranog lika; kretanje i sudari ostaju isti. Prave ilustracije se mogu ubaciti kasnije pod istim imenima fajlova.

## Dnevni poklon i zadaci

- `Assets/Scripts/Learning/DailyRewards.cs`: poklon jednom dnevno po stazi od 7 dana (10, 15, 20, 25, 30, 40, 60 novčića);
  propušten dan ne vraća na početak. Tri zadatka dnevno (uvijek „Uhvati 8 riječi“, plus nivo/3 zvjezdice i
  novčići/nove riječi po danu), nagrade fiksne i vidljive unaprijed, bez nasumičnih nagrada.
- `DailyMissionTracker` (na GameManageru) broji uhvaćene i nove riječi, novčiće i završene nivoe; `MissionToastUi`
  pokaže poruku u igri; `Assets/Scripts/UI/DailyUi.cs` je ekran DANAS (ikona poklona u meniju, sam se otvori
  prvi put u danu dok poklon čeka).

## Objava na Google Play

- Vodič s odgovorima za Play Console: `Store/play-console-vodic.md`; politika privatnosti: `Store/privacy-policy.html`.
- Build za upload: `CommandLineBuild.BuildAndroidBundle` → `Builds/RunAndLearn-<verzija>-<code>.aab`, potpisan upload ključem
  iz `%USERPROFILE%\RunAndLearnSigning` (van projekta, nikad u git). Verzija: `AndroidSetup.VersionName` / `VersionCode`.
- Igra nema dozvolu za internet (`Assets/Editor/RemoveInternetPermission.cs`); jedina dozvola je vibracija.
- Prije produkcije: glasovi su napravljeni preko edge-tts (bez komercijalne licence) i treba ih zamijeniti
  Azure Speech glasovima ili snimcima porodice (isti nazivi fajlova).

## Build za telefon

```
"C:/Program Files/Unity/Hub/Editor/6000.3.1f1/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod FootballRunner.EditorTools.CommandLineBuild.BuildAndroidApk -logFile Logs/build.log
```

APK se pojavi u `Builds/`. Unity Editor mora biti zatvoren dok build radi.
