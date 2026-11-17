# Run and Learn: prijava za produkciju (Play Console)

Kad se na nadzornoj ploči otvori dugme „Prijavite se za produkciju“ (14 dana s 12 testera, očekivano
18. 11. 2026), Play postavlja pitanja o zatvorenom testiranju. Odgovori ispod su na engleskom (Google ih
tako najlakše čita) i drže se činjenica: testeri su s Adnanove liste kontakata, jedina konkretna prijava
problema je bila Samirova (uređaj nije podržan), ostalo smo našli sami. Ništa nije izmišljeno.

Nakon slanja Google odgovara obično za nekoliko dana (do 7). Tek kad odobri, build 6 (1.3.0) ide u
produkciju.

---

## 1. O zatvorenom testiranju

**How did you recruit users for your closed test?**
I invited people from my own network by e-mail: family, friends and colleagues, many of them parents of
young children in Bosnia and Herzegovina and in the diaspora. They joined through the closed testing
opt-in link and installed the game from Google Play. When the count was below 12, I sent a reminder.

**How easy was it to recruit testers for your app?**
Odabrati: *Neither easy nor difficult* (ni lako ni teško). Trebao je podsjetnik da se dođe do 12.

**Describe the engagement you received from testers during your closed test.**
Testers installed the game on their own phones and tablets and played it with their children over the
two weeks. At least 12 testers stayed enrolled for the whole 14 days. Engagement was quiet but steady:
most feedback came by personal messages rather than in the app.

**Provide a summary of the feedback you received from testers. Include how you collected the feedback.**
Feedback was collected by e-mail and personal messages, and through Play Console (crashes, ANRs and
device compatibility). One tester reported that Google Play showed "Your device isn't compatible with this
version": the first builds were 64-bit only. I added 32-bit (ARMv7) support in version 1.2.1 and the
device count grew by about 2,800 phones and 1,500 tablets. No crashes or ANRs were reported. Other
testers had no problems to report, so most later improvements came from my own testing with children.

## 2. O igri

**Who is the intended audience of your app?**
Children aged 4 to 10 and their parents: children of the Bosnian diaspora who learn Bosnian (menus in
English or German), children in Bosnia and Herzegovina who learn English, German or Arabic words, and
parents who want a gentle, ad-free learning game.

**Describe how your app provides value to users.**
Run and Learn teaches words through play: a voice says a word and the child jumps into the right picture.
It has 11 worlds with 55 levels, read-aloud picture books, mini-games, letter tracing, simple sums, a
sticker album and diplomas to print. For parents there is a time limit, an easy mode for ages 3 to 4, a
weekly report and worksheets to print. It has no ads, no purchases, works offline and collects no data.

**How many installs do you expect your app to have in your first year?**
Odabrati: *0 – 10K* (realno za malu porodičnu igru).

## 3. Spremnost za produkciju

**What changes did you make to your app based on what you learned during your closed test?**
- Added 32-bit (ARMv7) devices after a tester could not install the game (1.2.1).
- Made saving safer (a backup copy, so progress is never lost) and made the game smaller and faster.
- Added an easy mode for ages 3 to 4, a daily time limit and a weekly report for parents.
- Added up to 4 child profiles on one phone, picture books, mini-games, letter tracing, sums,
  diplomas and worksheets, Arabic as a learning language and a better layout on tablets (1.3.0).

**How did you decide that your app is ready for production?**
The game ran for more than two weeks in closed testing with at least 12 testers without crashes, ANRs or
open problems, and the one compatibility problem was fixed. Version 1.3.0 also went through internal
testing on my own devices, more than 250 automated tests and checks on phones and tablets of different
sizes. The store listing, content rating, target audience (children) and privacy policy are complete.

---

## Prije slanja (Adnan)
- [ ] Test 1.3.0 na telefonu preko internog testiranja (lista `Builds\Test-na-telefonu-1.3.md`).
- [ ] Ako nešto nije tačno u odgovorima (npr. ko su testeri), reci mi i ispravim prije slanja.
