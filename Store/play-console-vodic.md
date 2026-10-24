# Run and Learn – objava na Google Play (vodič korak po korak)

Sve što se upisuje u Play Console je ovdje. Odgovori važe za verziju 1.0.0 (versionCode 1).

## 0. Šta je već spremno

| Stvar | Gdje |
|---|---|
| Build za upload (.aab, potpisan upload ključem) | `Builds/RunAndLearn-1.0.0-1.aab` |
| Upload ključ + lozinke (NE dijeliti, napraviti kopiju) | `C:\Users\Korisnik\RunAndLearnSigning\` |
| Ikona 512×512 | `Store/play/icon_512.png` |
| Feature graphic 1024×500 | `Store/play/feature_graphic_1024x500.png` |
| Snimke ekrana: 8 po jeziku, svaka s menijem na svom jeziku | `Store/play/screenshots/en`, `hr`, `de` |
| Tekst opisa (en-US, hr = bosanski, de-DE) | `Store/listing.md` |
| Politika privatnosti | `Store/privacy-policy.html` (treba je staviti na web, vidi korak 2) |

## 1. Napravi aplikaciju

Play Console → **Create app**
- App name: **Run and Learn**
- Default language: **English (United States) – en-US**
- App or game: **Game**
- Free or paid: **Free**
- Označi obje izjave (Developer Program Policies, US export laws) → **Create app**

## 2. Politika privatnosti (URL)

Play traži javnu web adresu. `Store/privacy-policy.html` je gotova (engleski, bosanski, njemački).
Prije postavljanja zamijeni `CONTACT_EMAIL` (3 mjesta) adresom za kontakt.
Mogućnosti: Google Sites (besplatno, oko 5 minuta) ili GitHub Pages. Adresu upiši u
**Policy → App content → Privacy policy**.

## 3. App content (Policy → App content)

| Odjeljak | Odgovor |
|---|---|
| Privacy policy | URL iz koraka 2 |
| App access | **All functionality is available without special access** |
| Ads | **No, my app does not contain ads** |
| Content rating | vidi 3a |
| Target audience | vidi 3b |
| Data safety | vidi 3c |
| Government apps | No |
| Financial features | My app doesn't provide any financial features |
| Health | My app does not have any health features |
| News app | No |

### 3a. Content rating (IARC upitnik)

- E-mail za ocjenu: tvoj e-mail
- Category: **Game** (All Other Game Types)
- Violence: **No** na sva pitanja (lik se samo spotakne o prepreku, bez nasilja)
- Fear / Sexuality / Language / Controlled substances / Crude humour: **No**
- Gambling (pravi ili simulirani): **No**. Novčići se zarađuju igranjem i ne mogu se kupiti.
- Users interact or share content: **No**
- Shares user's location: **No**
- Allows purchase of digital goods: **No**
- Unrestricted internet: **No**

Očekivana ocjena: PEGI 3 / Everyone.

### 3b. Target audience and content

- Target age groups: **5 and under**, **6–8**, **9–12**
- Appeals to children: **Yes** (igra je za djecu)
- Time igra spada u **Families** program. Moramo poštovati Families pravila, i ispunjavamo ih:
  nema reklama, nema prikupljanja podataka, nema interneta, nema kupovine.
- Teacher Approved: opcionalno. Kasnije se može prijaviti za oznaku „Teacher Approved“.

### 3c. Data safety

- Does your app collect or share any of the required user data types? **No**
- Is all of the user data collected by your app encrypted in transit? (ne pojavljuje se kad je odgovor No)
- Account creation: **My app does not allow users to create an account**
- Rezultat na Playu: „No data collected · No data shared“.

## 4. Store listing (Grow → Store presence → Main store listing)

- App name: Run and Learn
- Short description i Full description: iz `Store/listing.md` (en-US)
- App icon, Feature graphic: iz `Store/play/`; Phone screenshots: svih 8 iz `Store/play/screenshots/en/`
  (za prijevode: `hr/` uz bosanski tekst, `de/` uz njemački)
- Category: **Game → Educational**
- Tags: Educational, Language learning, Kids, Casual
- Contact e-mail: tvoj e-mail (obavezno)
- Prijevodi: **Add translations** → Croatian (hr) = bosanski tekst, German (de-DE) = njemački tekst, oboje iz `Store/listing.md`

## 5. Interna proba (prvo na tvoj telefon)

Test and release → Testing → **Internal testing** → Create new release
1. Play App Signing: prihvati **Google-managed key** (preporučeno).
2. Upload: `Builds/RunAndLearn-1.0.0-1.aab`
3. Release name: `1.0.0`; release notes (en-US): „First release: eight worlds, ten players.“
4. Testers: napravi listu i dodaj svoj Gmail (i porodicu).
5. Save → Review → **Start rollout to Internal testing**
6. Otvori **Copy link** na telefonu (prijavljen istim Gmailom) → Accept invite → instaliraj s Playa.

## 6. Zatvoreno testiranje i produkcija

Novi lični (personal) developer nalozi moraju prije produkcije imati **zatvoreno testiranje sa
najmanje 12 testera kroz 14 dana**. Play Console to pokaže na **Dashboardu** ako važi za tvoj nalog.
Ako važi: Closed testing → nova lista testera (porodica, prijatelji, 12+ Gmail adresa) → isti .aab →
pričekati 14 dana → **Apply for production**.

## 7. Svako sljedeće ažuriranje

U `Assets/Editor/AndroidSetup.cs` povećati `VersionName` (npr. 1.0.1) i `VersionCode` (2, 3, …),
pa napraviti novi build:

```
"C:/Program Files/Unity/Hub/Editor/6000.3.1f1/Editor/Unity.exe" -batchmode -quit -projectPath . -buildTarget Android -executeMethod FootballRunner.EditorTools.CommandLineBuild.BuildAndroidBundle -logFile Logs/build-aab.log
```
