# Arapski: lista za provjeru

Sve arapske riječi i rečenice u igri, za provjeru od strane nekoga ko zna arapski,
prije objave. Provjeriti: prijevod, harakate (znakove za samoglasnike), latinicu i
izgovor u glasu (`Assets/Resources/Words/<Svijet>/Audio/ar/<id>.ogg`).

Pravila koja su korištena: standardni arapski (fusha), riječi bez člana „al-“, u
pauzalnom obliku (bez padežnog nastavka na kraju). Latinica je prilagođena bosanskom
čitanju: dž = ج, š = ش, h = ح/خ/ه, ' = ع ili hamza, kapica (â, î, û) = dug samoglasnik.
Fraze iz reda „intro“ igra pušta na bosanskom; arapska verzija postoji, ali se ne čuje.

Za recenzenta: `Tools/make_arabic_review_page.py` pravi `Store/arabic-review.zip` (slika, tekst, glas);
njegov fajl s rezultatima upisuje `Tools/apply_arabic_review.py`, koji ponovo piše i ovu listu.
Ukupno 230 unosa, od toga 230 čeka provjeru.


## World1: Nenina bašta

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `apple` | jabuka | تُفَّاحَة | tuffâha | za provjeru |
| riječ | `pear` | kruška | كُمَّثْرَى | kummasrâ | za provjeru |
| riječ | `banana` | banana | مَوْزَة | mavza | za provjeru |
| riječ | `strawberry` | jagoda | فَرَاوِلَة | farâvila | za provjeru |
| riječ | `grapes` | grožđe | عِنَب | 'inab | za provjeru |
| riječ | `watermelon` | lubenica | بِطِّيخ | bittîh | za provjeru |
| riječ | `lemon` | limun | لَيْمُون | lejmûn | za provjeru |
| riječ | `carrot` | mrkva | جَزَرَة | džazara | za provjeru |
| riječ | `tomato` | paradajz | طَمَاطِم | tamâtim | za provjeru |
| riječ | `cucumber` | krastavac | خِيَار | hijâr | za provjeru |
| fraza | `praise_1` | Bravo! | رَائِع! | Râ'i'! | za provjeru |
| fraza | `praise_2` | Tačno! | صَحِيح! | Sahîh! | za provjeru |
| fraza | `praise_3` | Super! | مُمْتَاز! | Mumtâz! | za provjeru |
| fraza | `intro` | Slušaj riječ, pa skoči u pravu sliku! | اِسْتَمِعْ إِلَى الْكَلِمَةِ، ثُمَّ اقْفِزْ إِلَى الصُّورَةِ الصَّحِيحَة! | Istami' ila-l-kalimati, summa-kfiz ila-s-sûrati-s-sahîha! | za provjeru: muški rod imperativa; igra ovu rečenicu pušta na bosanskom |
| fraza | `jump` | Skoči! | اِقْفِزْ! | Ikfiz! | za provjeru: muški rod imperativa (ženski: اِقْفِزِي) |
| fraza | `goal` | Bravo! Cilj je ispunjen! | رَائِع! تَمَّ الْهَدَف! | Râ'i'! Tamma-l-hadaf! | za provjeru |

## World2: Farma kod dede

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `cow` | krava | بَقَرَة | bakara | za provjeru |
| riječ | `sheep` | ovca | خَرُوف | harûf | za provjeru |
| riječ | `goat` | koza | مَاعِز | mâ'iz | za provjeru |
| riječ | `horse` | konj | حِصَان | hisân | za provjeru |
| riječ | `hen` | kokoš | دَجَاجَة | dadžâdža | za provjeru |
| riječ | `rooster` | pijetao | دِيك | dîk | za provjeru |
| riječ | `duck` | patka | بَطَّة | batta | za provjeru |
| riječ | `cat` | mačka | قِطَّة | kitta | za provjeru |
| riječ | `rabbit` | zec | أَرْنَب | arnab | za provjeru |
| riječ | `bee` | pčela | نَحْلَة | nahla | za provjeru |
| brojanje | `hen_1` | jedna kokoš | دَجَاجَة وَاحِدَة | dadžâdža vâhida | za provjeru |
| brojanje | `hen_2` | dvije kokoši | دَجَاجَتَان | dadžâdžatân | za provjeru |
| brojanje | `hen_3` | tri kokoši | ثَلَاثُ دَجَاجَات | salâsu dadžâdžât | za provjeru |
| brojanje | `hen_4` | četiri kokoši | أَرْبَعُ دَجَاجَات | arba'u dadžâdžât | za provjeru |
| brojanje | `hen_5` | pet kokoši | خَمْسُ دَجَاجَات | hamsu dadžâdžât | za provjeru |
| brojanje | `cow_1` | jedna krava | بَقَرَة وَاحِدَة | bakara vâhida | za provjeru |
| brojanje | `cow_2` | dvije krave | بَقَرَتَان | bakaratân | za provjeru |
| brojanje | `cow_3` | tri krave | ثَلَاثُ بَقَرَات | salâsu bakarât | za provjeru |
| brojanje | `cow_4` | četiri krave | أَرْبَعُ بَقَرَات | arba'u bakarât | za provjeru |
| brojanje | `cow_5` | pet krava | خَمْسُ بَقَرَات | hamsu bakarât | za provjeru |
| brojanje | `sheep_1` | jedna ovca | خَرُوف وَاحِد | harûf vâhid | za provjeru |
| brojanje | `sheep_2` | dvije ovce | خَرُوفَان | harûfân | za provjeru |
| brojanje | `sheep_3` | tri ovce | ثَلَاثَةُ خِرَاف | salâsatu hirâf | za provjeru |
| brojanje | `sheep_4` | četiri ovce | أَرْبَعَةُ خِرَاف | arba'atu hirâf | za provjeru |
| brojanje | `sheep_5` | pet ovaca | خَمْسَةُ خِرَاف | hamsatu hirâf | za provjeru |
| brojanje | `duck_1` | jedna patka | بَطَّة وَاحِدَة | batta vâhida | za provjeru |
| brojanje | `duck_2` | dvije patke | بَطَّتَان | battatân | za provjeru |
| brojanje | `duck_3` | tri patke | ثَلَاثُ بَطَّات | salâsu battât | za provjeru |
| brojanje | `duck_4` | četiri patke | أَرْبَعُ بَطَّات | arba'u battât | za provjeru |
| brojanje | `duck_5` | pet pataka | خَمْسُ بَطَّات | hamsu battât | za provjeru |
| brojanje | `rabbit_1` | jedan zec | أَرْنَب وَاحِد | arnab vâhid | za provjeru: rod riječi أَرْنَب (klasično ženski) |
| brojanje | `rabbit_2` | dva zeca | أَرْنَبَان | arnabân | za provjeru |
| brojanje | `rabbit_3` | tri zeca | ثَلَاثَةُ أَرَانِب | salâsatu arânib | za provjeru: slaganje broja s rodom |
| brojanje | `rabbit_4` | četiri zeca | أَرْبَعَةُ أَرَانِب | arba'atu arânib | za provjeru: slaganje broja s rodom |
| brojanje | `rabbit_5` | pet zečeva | خَمْسَةُ أَرَانِب | hamsatu arânib | za provjeru: slaganje broja s rodom |

## World3: Naša kuća

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `mom` | mama | أُمّ | umm | za provjeru |
| riječ | `dad` | tata | أَب | ab | za provjeru |
| riječ | `grandma` | nena | جَدَّة | džadda | za provjeru |
| riječ | `grandpa` | dedo | جَدّ | džadd | za provjeru |
| riječ | `sister` | sestra | أُخْت | uht | za provjeru |
| riječ | `brother` | brat | أَخ | ah | za provjeru |
| riječ | `baby` | beba | رَضِيع | radî' | za provjeru |
| riječ | `house` | kuća | بَيْت | bejt | za provjeru |
| riječ | `bread` | hljeb | خُبْز | hubz | za provjeru |
| riječ | `milk` | mlijeko | حَلِيب | halîb | za provjeru |

## World4: Čaršija

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `mosque` | džamija | مَسْجِد | masdžid | za provjeru |
| riječ | `pigeon` | golub | حَمَامَة | hamâma | za provjeru |
| riječ | `tram` | tramvaj | تِرَام | tirâm | za provjeru: posuđenica; provjeriti uobičajen oblik |
| riječ | `bus` | autobus | حَافِلَة | hâfila | za provjeru |
| riječ | `bicycle` | bicikl | دَرَّاجَة | darrâdža | za provjeru |
| riječ | `ball` | lopta | كُرَة | kura | za provjeru |
| riječ | `book` | knjiga | كِتَاب | kitâb | za provjeru |
| riječ | `umbrella` | kišobran | مِظَلَّة | mizalla | za provjeru |
| riječ | `clock` | sat | سَاعَة | sâ'a | za provjeru |
| riječ | `fountain` | česma | نَافُورَة | nâfûra | za provjeru |
| komanda | `cmd_jump` | Skoči! | اِقْفِزْ! | Ikfiz! | za provjeru: muški rod imperativa |
| komanda | `cmd_stay` | Ostani dolje! | اِبْقَ فِي الْأَسْفَل! | Ibka fi-l-asfal! | za provjeru: muški rod imperativa |
| komanda | `cmd_double` | Skoči dva puta! | اِقْفِزْ مَرَّتَيْن! | Ikfiz marratejn! | za provjeru: muški rod imperativa |

## World5: Rijeka i Stari most

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `sun` | sunce | شَمْس | šams | za provjeru |
| riječ | `rain` | kiša | مَطَر | matar | za provjeru |
| riječ | `rainbow` | duga | قَوْسُ قُزَح | kavsu kuzah | za provjeru |
| riječ | `bridge` | most | جِسْر | džisr | za provjeru |
| riječ | `fish` | riba | سَمَكَة | samaka | za provjeru |
| riječ | `frog` | žaba | ضِفْدَع | difda' | za provjeru |
| riječ | `butterfly` | leptir | فَرَاشَة | farâša | za provjeru |
| riječ | `flower` | cvijet | زَهْرَة | zahra | za provjeru |
| riječ | `tree` | drvo | شَجَرَة | šadžara | za provjeru |
| riječ | `boat` | čamac | قَارِب | kârib | za provjeru |
| veliko/malo | `fish_big` | velika riba | سَمَكَة كَبِيرَة | samaka kabîra | za provjeru |
| veliko/malo | `fish_small` | mala riba | سَمَكَة صَغِيرَة | samaka sagîra | za provjeru |
| veliko/malo | `frog_big` | velika žaba | ضِفْدَع كَبِير | difda' kabîr | za provjeru |
| veliko/malo | `frog_small` | mala žaba | ضِفْدَع صَغِير | difda' sagîr | za provjeru |
| veliko/malo | `boat_big` | veliki čamac | قَارِب كَبِير | kârib kabîr | za provjeru |
| veliko/malo | `boat_small` | mali čamac | قَارِب صَغِير | kârib sagîr | za provjeru |
| veliko/malo | `flower_big` | veliki cvijet | زَهْرَة كَبِيرَة | zahra kabîra | za provjeru |
| veliko/malo | `flower_small` | mali cvijet | زَهْرَة صَغِيرَة | zahra sagîra | za provjeru |
| veliko/malo | `tree_big` | veliko drvo | شَجَرَة كَبِيرَة | šadžara kabîra | za provjeru |
| veliko/malo | `tree_small` | malo drvo | شَجَرَة صَغِيرَة | šadžara sagîra | za provjeru |

## World6: Planina zimi

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `snow` | snijeg | ثَلْج | saldž | za provjeru |
| riječ | `snowman` | snjegović | رَجُلُ الثَّلْج | radžulu-s-saldž | za provjeru |
| riječ | `sled` | sanke | مِزْلَجَة | mizladža | za provjeru |
| riječ | `skis` | skije | زَلَّاجَات | zallâdžât | za provjeru: provjeriti riječ za skije |
| riječ | `gloves` | rukavice | قُفَّازَات | kuffâzât | za provjeru |
| riječ | `scarf` | šal | وِشَاح | višâh | za provjeru |
| riječ | `boots` | čizme | جَزْمَة | džazma | za provjeru: provjeriti: čizme (možda حِذَاءٌ طَوِيل) |
| riječ | `coat` | jakna | مِعْطَف | mi'taf | za provjeru |
| riječ | `mountain` | planina | جَبَل | džabal | za provjeru |
| riječ | `ice` | led | جَلِيد | džalîd | za provjeru |
| rečenica | `s_snowing` | Pada snijeg. | الثَّلْجُ يَتَسَاقَط. | As-saldžu jatasâkat. | za provjeru |
| rečenica | `s_sunny` | Sija sunce. | الشَّمْسُ مُشْرِقَة. | Aš-šamsu mušrika. | za provjeru |
| rečenica | `s_raining` | Pada kiša. | السَّمَاءُ تُمْطِر. | As-samâ'u tumtir. | za provjeru |
| rečenica | `s_sledding` | Rukajja se sanka. | رُقَيَّةُ تَتَزَلَّجُ بِالْمِزْلَجَة. | Rukajjatu tatazalladžu bi-l-mizladža. | za provjeru: ime Rukajja = رُقَيَّة |
| rečenica | `s_skiing` | Rukajja skija. | رُقَيَّةُ تَتَزَلَّجُ بِالزَّلَّاجَات. | Rukajjatu tatazalladžu bi-z-zallâdžât. | za provjeru |
| rečenica | `s_snowman` | Rukajja pravi snjegovića. | رُقَيَّةُ تَصْنَعُ رَجُلَ الثَّلْج. | Rukajjatu tasna'u radžula-s-saldž. | za provjeru |
| rečenica | `s_gloves` | Nosim rukavice. | أَلْبَسُ قُفَّازَات. | Albasu kuffâzât. | za provjeru |
| rečenica | `s_scarf` | Nosim šal. | أَلْبَسُ وِشَاحًا. | Albasu višâhâ. | za provjeru |
| rečenica | `s_boots` | Nosim čizme. | أَلْبَسُ جَزْمَة. | Albasu džazma. | za provjeru |

## World7: Škola

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `pencil` | olovka | قَلَمُ رَصَاص | kalamu rasâs | za provjeru |
| riječ | `notebook` | sveska | دَفْتَر | daftar | za provjeru |
| riječ | `backpack` | ranac | حَقِيبَة | hakîba | za provjeru |
| riječ | `scissors` | makaze | مِقَصّ | mikass | za provjeru |
| riječ | `ruler` | lenjir | مِسْطَرَة | mistara | za provjeru |
| riječ | `crayon` | bojica | قَلَمُ تَلْوِين | kalamu talvîn | za provjeru |
| riječ | `globe` | globus | كُرَة أَرْضِيَّة | kura ardijja | za provjeru |
| riječ | `abacus` | računaljka | مِعْدَاد | mi'dâd | za provjeru |
| riječ | `bell` | zvono | جَرَس | džaras | za provjeru |
| riječ | `school` | škola | مَدْرَسَة | madrasa | za provjeru |
| broj | `num_1` | jedan | وَاحِد | vâhid | za provjeru |
| broj | `num_2` | dva | اِثْنَان | isnân | za provjeru |
| broj | `num_3` | tri | ثَلَاثَة | salâsa | za provjeru |
| broj | `num_4` | četiri | أَرْبَعَة | arba'a | za provjeru |
| broj | `num_5` | pet | خَمْسَة | hamsa | za provjeru |
| broj | `num_6` | šest | سِتَّة | sitta | za provjeru |
| broj | `num_7` | sedam | سَبْعَة | sab'a | za provjeru |
| broj | `num_8` | osam | ثَمَانِيَة | samânija | za provjeru |
| broj | `num_9` | devet | تِسْعَة | tis'a | za provjeru |
| broj | `num_10` | deset | عَشَرَة | 'ašara | za provjeru |
| fraza | `op_plus` | plus | زَائِد | zâ'id | za provjeru: riječ za sabiranje (2 + 3) |
| fraza | `op_minus` | minus | نَاقِص | nâqis | za provjeru: riječ za oduzimanje (5 − 2) |

## World8: Noćno nebo

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `star_word` | zvijezda | نَجْمَة | nadžma | za provjeru |
| riječ | `moon` | mjesec | قَمَر | kamar | za provjeru |
| riječ | `rocket` | raketa | صَارُوخ | sârûh | za provjeru |
| riječ | `planet` | planeta | كَوْكَب | kavkab | za provjeru |
| riječ | `owl` | sova | بُومَة | bûma | za provjeru |
| riječ | `comet` | kometa | مُذَنَّب | muzannab | za provjeru |
| riječ | `telescope` | teleskop | تِلِسْكُوب | tiliskûb | za provjeru: posuđenica; standardno i مِقْرَاب |
| riječ | `bed` | krevet | سَرِير | sarîr | za provjeru |
| riječ | `satellite` | satelit | قَمَر صِنَاعِيّ | kamar sinâ'î | za provjeru |
| riječ | `night` | noć | لَيْل | lejl | za provjeru |
| fraza | `finale` | Bravo! Svi svjetovi su završeni! | رَائِع! اِنْتَهَتْ كُلُّ الْعَوَالِم! | Râ'i'! Intahat kullu-l-'avâlim! | za provjeru |

## World9: Lijepe riječi

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `selam` | selam | السَّلَامُ عَلَيْكُم | es-selâmu alejkum | za provjeru: pozdrav, pauzalni oblik |
| riječ | `thanks` | hvala | شُكْرًا | šukran | za provjeru |
| riječ | `here_you_are` | izvoli | تَفَضَّل | tafaddal | za provjeru: muški rod (ženski: تَفَضَّلِي) |
| riječ | `sorry` | oprosti | آسِف | âsif | za provjeru: muški rod (ženski: آسِفَة) |
| riječ | `bismillah` | bismillah | بِسْمِ اللّٰه | bismillâh | za provjeru |
| riječ | `mashallah` | mašallah | مَا شَاءَ اللّٰه | mâ šâ' allâh | za provjeru |

## World10: Boje i oblici

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `red` | crvena | أَحْمَر | ahmar | za provjeru |
| riječ | `blue` | plava | أَزْرَق | azrak | za provjeru |
| riječ | `yellow` | žuta | أَصْفَر | asfar | za provjeru |
| riječ | `green` | zelena | أَخْضَر | ahdar | za provjeru |
| riječ | `orange` | narandžasta | بُرْتُقَالِيّ | burtukâlî | za provjeru |
| riječ | `purple` | ljubičasta | بَنَفْسَجِيّ | banafsadžî | za provjeru |
| riječ | `circle` | krug | دَائِرَة | dâ'ira | za provjeru |
| riječ | `square` | kvadrat | مُرَبَّع | murabba' | za provjeru |
| riječ | `triangle` | trougao | مُثَلَّث | musallas | za provjeru |
| riječ | `heart` | srce | قَلْب | kalb | za provjeru |

## Imena slova (Škola i „Piši slovo“; glas kaže „slovo …“)

| id | slovo | izgovoreno | latinica | status |
|---|---|---|---|---|
| `ar_alif` | ا | حَرْفُ الْأَلِف | alif | za provjeru |
| `ar_ba` | ب | حَرْفُ الْبَاء | bâ' | za provjeru |
| `ar_ta` | ت | حَرْفُ التَّاء | tâ' | za provjeru |
| `ar_tha` | ث | حَرْفُ الثَّاء | sâ' | za provjeru |
| `ar_jim` | ج | حَرْفُ الْجِيم | džîm | za provjeru |
| `ar_hha` | ح | حَرْفُ الْحَاء | hâ' | za provjeru |
| `ar_kha` | خ | حَرْفُ الْخَاء | hâ' | za provjeru |
| `ar_dal` | د | حَرْفُ الدَّال | dâl | za provjeru |
| `ar_dhal` | ذ | حَرْفُ الذَّال | zâl | za provjeru |
| `ar_ra` | ر | حَرْفُ الرَّاء | râ' | za provjeru |
| `ar_zay` | ز | حَرْفُ الزَّاي | zâj | za provjeru |
| `ar_sin` | س | حَرْفُ السِّين | sîn | za provjeru |
| `ar_shin` | ش | حَرْفُ الشِّين | šîn | za provjeru |
| `ar_sad` | ص | حَرْفُ الصَّاد | sâd | za provjeru |
| `ar_dad` | ض | حَرْفُ الضَّاد | dâd | za provjeru |
| `ar_tta` | ط | حَرْفُ الطَّاء | tâ' | za provjeru |
| `ar_dha` | ظ | حَرْفُ الظَّاء | zâ' | za provjeru |
| `ar_ayn` | ع | حَرْفُ الْعَيْن | 'ajn | za provjeru |
| `ar_ghayn` | غ | حَرْفُ الْغَيْن | gajn | za provjeru |
| `ar_fa` | ف | حَرْفُ الْفَاء | fâ' | za provjeru |
| `ar_qaf` | ق | حَرْفُ الْقَاف | qâf | za provjeru |
| `ar_kaf` | ك | حَرْفُ الْكَاف | kâf | za provjeru |
| `ar_lam` | ل | حَرْفُ اللَّام | lâm | za provjeru |
| `ar_mim` | م | حَرْفُ الْمِيم | mîm | za provjeru |
| `ar_nun` | ن | حَرْفُ النُّون | nûn | za provjeru |
| `ar_ha` | ه | حَرْفُ الْهَاء | hâ' | za provjeru |
| `ar_waw` | و | حَرْفُ الْوَاو | vâv | za provjeru |
| `ar_ya` | ي | حَرْفُ الْيَاء | jâ' | za provjeru |

## Slikovnice (rečenice koje glas čita)

| id | bosanski | arapski | latinica | status |
|---|---|---|---|---|
| `books/World1/1` | Rukajja je u neninoj bašti. Gle, crvena jabuka! | رُقَيَّةُ فِي حَدِيقَةِ الْجَدَّة. اُنْظُرْ، تُفَّاحَةٌ حَمْرَاء! | Rukajjatu fî hadîkati-l-džadda. Unzur, tuffâhatun hamrâ'! | za provjeru |
| `books/World1/2` | Nena bere jagode. Pepi jede bananu. | الْجَدَّةُ تَقْطِفُ الْفَرَاوِلَة. بِيبِي يَأْكُلُ مَوْزَة. | Al-džaddatu taktifu-l-farâvila. Pîpî ja'kulu mavza. | za provjeru |
| `books/World1/3` | U zemlji raste mrkva. Rukajja je čupa. Hop! | الْجَزَرَةُ فِي الْأَرْض. رُقَيَّةُ تَسْحَبُهَا. هُوب! | Al-džazaratu fi-l-ard. Rukajjatu tashabuhâ. Hûb! | za provjeru |
| `books/World1/4` | Na kraju jedu veliku lubenicu. Mmm, slatka je! | فِي النِّهَايَةِ يَأْكُلُونَ بِطِّيخًا كَبِيرًا. مَا أَحْلَاه! | Fi-n-nihâjati ja'kulûna bittîhan kabîran. Mâ ahlâh! | za provjeru |
| `books/World2/1` | Kukuriku! Pijetao budi farmu kod dede. | كُوكُو كُوكُو! الدِّيكُ يُوقِظُ مَزْرَعَةَ الْجَدّ. | Kûkû kûkû! Ad-dîku jûkizu mazra'ata-l-džadd. | za provjeru |
| `books/World2/2` | Dedo muze kravu. Evo svježeg mlijeka! | الْجَدُّ يَحْلُبُ الْبَقَرَة. هَذَا حَلِيبٌ طَازَج! | Al-džaddu jahlubu-l-bakara. Hâzâ halîbun tâzadž! | za provjeru |
| `books/World2/3` | Kokoš traži zrnca. Patka pliva u bari. | الدَّجَاجَةُ تَبْحَثُ عَنِ الْحَبّ. الْبَطَّةُ تَسْبَحُ فِي الْبِرْكَة. | Ad-dadžâdžatu tabhasu 'ani-l-habb. Al-battatu tasbahu fi-l-birka. | za provjeru |
| `books/World2/4` | Zec skače: hop, hop! Rukajja skače s njim. | الْأَرْنَبُ يَقْفِز: هُوب، هُوب! رُقَيَّةُ تَقْفِزُ مَعَه. | Al-arnabu jakfiz: hûb, hûb! Rukajjatu takfizu ma'ah. | za provjeru |
| `books/World3/1` | Ovo je naša kuća. Ovdje živi Rukajja. | هَذَا بَيْتُنَا. هُنَا تَسْكُنُ رُقَيَّة. | Hâzâ bajtunâ. Hunâ taskunu Rukajja. | za provjeru |
| `books/World3/2` | Mama peče hljeb. Cijela kuća lijepo miriše. | الْأُمُّ تَخْبِزُ الْخُبْز. رَائِحَةُ الْبَيْتِ جَمِيلَة. | Al-ummu tahbizu-l-hubz. Râ'ihatu-l-bajti džamîla. | za provjeru |
| `books/World3/3` | Dedo čita, a nena plete šal. | الْجَدُّ يَقْرَأُ، وَالْجَدَّةُ تَحِيكُ وِشَاحًا. | Al-džaddu jakra'u, va-l-džaddatu tahîku višâhan. | za provjeru |
| `books/World3/4` | Beba pije mlijeko i zaspi. Laku noć! | الرَّضِيعُ يَشْرَبُ الْحَلِيبَ وَيَنَام. تُصْبِحُ عَلَى خَيْر! | Ar-radî'u jašrabu-l-halîba va janâm. Tusbihu 'alâ hajr! | za provjeru |
| `books/World4/1` | Na čaršiji golubovi piju vodu sa česme. | فِي السُّوقِ الْقَدِيمِ يَشْرَبُ الْحَمَامُ مِنَ النَّافُورَة. | Fi-s-sûki-l-kadîmi jašrabu-l-hamâmu mina-n-nâfûra. | za provjeru |
| `books/World4/2` | Tramvaj zvoni: cin-cin! Pazi, Rukajja! | التِّرَامُ يَرِنّ: تِن تِن! اِنْتَبِهِي يَا رُقَيَّة! | At-tirâmu jarinn: tin tin! Intabihî jâ Rukajja! | za provjeru |
| `books/World4/3` | Veliki sat pokazuje podne. | السَّاعَةُ الْكَبِيرَةُ تُشِيرُ إِلَى الظُّهْر. | As-sâ'atu-l-kabîratu tušîru ila-z-zuhr. | za provjeru |
| `books/World4/4` | Djeca igraju loptu. Rukajja daje gol! | الْأَطْفَالُ يَلْعَبُونَ بِالْكُرَة. رُقَيَّةُ تُسَجِّلُ هَدَفًا! | Al-atfâlu jal'abûna bi-l-kura. Rukajjatu tusaddžilu hadafan! | za provjeru |
| `books/World5/1` | Sunce sija iznad Starog mosta. | الشَّمْسُ تُشْرِقُ فَوْقَ الْجِسْرِ الْقَدِيم. | Aš-šamsu tušriku favka-l-džisri-l-kadîm. | za provjeru |
| `books/World5/2` | U rijeci pliva mala riba. | سَمَكَةٌ صَغِيرَةٌ تَسْبَحُ فِي النَّهْر. | Samakatun sagîratun tasbahu fi-n-nahr. | za provjeru |
| `books/World5/3` | Pada kiša. Žaba je sretna: kre-kre! | الْمَطَرُ يَنْزِل. الضِّفْدَعُ سَعِيد: نَقْ نَقْ! | Al-mataru janzil. Ad-difda'u sa'îd: nak nak! | za provjeru |
| `books/World5/4` | Poslije kiše dođe duga. Leptir leti do nje. | بَعْدَ الْمَطَرِ يَظْهَرُ قَوْسُ قُزَح. الْفَرَاشَةُ تَطِيرُ إِلَيْه. | Ba'da-l-matari jazharu kavsu kuzah. Al-farâšatu tatîru ilajh. | za provjeru |
| `books/World6/1` | Na planini pada snijeg. Sve je bijelo! | الثَّلْجُ يَتَسَاقَطُ عَلَى الْجَبَل. كُلُّ شَيْءٍ أَبْيَض! | As-saldžu jatasâkatu 'ala-l-džabal. Kullu šaj'in abjad! | za provjeru |
| `books/World6/2` | Rukajja oblači jaknu i rukavice. | رُقَيَّةُ تَلْبَسُ الْمِعْطَفَ وَالْقُفَّازَات. | Rukajjatu talbasu-l-mi'tafa va-l-kuffâzât. | za provjeru |
| `books/World6/3` | Sanke jure niz brdo. Juhuu! | الْمِزْلَجَةُ تَنْزَلِقُ مِنَ التَّلّ. يَاه! | Al-mizladžatu tanzaliku mina-t-tall. Jâh! | za provjeru |
| `books/World6/4` | Na kraju prave snjegovića sa šalom. | فِي النِّهَايَةِ يَصْنَعُونَ رَجُلَ ثَلْجٍ بِوِشَاح. | Fi-n-nihâjati jasna'ûna radžula saldžin bi-višâh. | za provjeru |
| `books/World7/1` | Zvono zvoni. Počinje škola! | الْجَرَسُ يَرِنّ. تَبْدَأُ الْمَدْرَسَة! | Al-džarasu jarinn. Tabda'u-l-madrasa! | za provjeru |
| `books/World7/2` | Rukajja vadi svesku iz ranca. | رُقَيَّةُ تُخْرِجُ الدَّفْتَرَ مِنَ الْحَقِيبَة. | Rukajjatu tuhridžu-d-daftara mina-l-hakîba. | za provjeru |
| `books/World7/3` | Olovkom piše slova: A, B, C. | تَكْتُبُ الْحُرُوفَ بِقَلَمِ الرَّصَاص: أَلِف، بَاء، تَاء. | Taktubu-l-hurûfa bi-kalami-r-rasâs: alif, bâ', tâ'. | za provjeru |
| `books/World7/4` | Bojicama crta sunce. Bravo, Rukajja! | تَرْسُمُ شَمْسًا بِأَقْلَامِ التَّلْوِين. أَحْسَنْتِ يَا رُقَيَّة! | Tarsumu šamsan bi-aklâmi-t-talvîn. Ahsanti jâ Rukajja! | za provjeru |
| `books/World8/1` | Pala je noć. Na nebu sija mjesec. | جَاءَ اللَّيْل. الْقَمَرُ يُضِيءُ فِي السَّمَاء. | Džâ'a-l-lajl. Al-kamaru judî'u fi-s-samâ'. | za provjeru |
| `books/World8/2` | Rukajja gleda kroz teleskop. Koliko zvijezda! | رُقَيَّةُ تَنْظُرُ بِالتِّلِسْكُوب. مَا أَكْثَرَ النُّجُوم! | Rukajjatu tanzuru bi-t-tiliskûb. Mâ aksara-n-nudžûm! | za provjeru |
| `books/World8/3` | Sova kaže: hu-hu! Ona noću ne spava. | الْبُومَةُ تَقُول: هُو هُو! هِيَ لَا تَنَامُ فِي اللَّيْل. | Al-bûmatu takûl: hû hû! Hija lâ tanâmu fi-l-lajl. | za provjeru |
| `books/World8/4` | Rukajja ide u krevet i sanja rakete. Laku noć! | رُقَيَّةُ تَذْهَبُ إِلَى السَّرِيرِ وَتَحْلُمُ بِالصَّوَارِيخ. تُصْبِحِينَ عَلَى خَيْر! | Rukajjatu tazhabu ila-s-sarîri va tahlumu bi-s-savârîh. Tusbihîna 'alâ hajr! | za provjeru |
| `books/World9/1` | Rukajja sretne komšiju i kaže: Selam! | رُقَيَّةُ تَلْتَقِي بِالْجَارِ وَتَقُول: السَّلَامُ عَلَيْكُم! | Rukajjatu taltakî bi-l-džâri va takûl: As-salâmu 'alajkum! | za provjeru |
| `books/World9/2` | Prije jela kaže: Bismillah. | قَبْلَ الْأَكْلِ تَقُول: بِسْمِ اللّٰه. | Kabla-l-akli takûl: Bismillâh. | za provjeru |
| `books/World9/3` | Daje jabuku bratu: Izvoli! On kaže: Hvala! | تُعْطِي أَخَاهَا تُفَّاحَة: تَفَضَّل! يَقُول: شُكْرًا! | Tu'tî ahâhâ tuffâha: Tafaddal! Jakûl: Šukran! | za provjeru |
| `books/World9/4` | Kad slučajno gurne drugaricu, kaže: Oprosti! | عِنْدَمَا تَدْفَعُ صَدِيقَتَهَا بِالْخَطَأِ تَقُول: آسِفَة! | 'Indamâ tadfa'u sadîkatahâ bi-l-hata'i takûl: Âsifa! | za provjeru |
| `books/World10/1` | Pepi nosi crveni krug. | بِيبِي يَحْمِلُ دَائِرَةً حَمْرَاء. | Pîpî jahmilu dâ'iratan hamrâ'. | za provjeru |
| `books/World10/2` | Rukajja slaže plavi kvadrat. | رُقَيَّةُ تَضَعُ مُرَبَّعًا أَزْرَق. | Rukajjatu tada'u murabba'an azrak. | za provjeru |
| `books/World10/3` | Žuti trougao ide na vrh. To je krov! | الْمُثَلَّثُ الْأَصْفَرُ فَوْق. هَذَا سَقْف! | Al-musallasu-l-asfaru favk. Hâzâ sakf! | za provjeru |
| `books/World10/4` | Zeleno drvo i crveno srce. Gotova je slika! | شَجَرَةٌ خَضْرَاءُ وَقَلْبٌ أَحْمَر. اِنْتَهَتِ الصُّورَة! | Šadžaratun hadrâ'u va kalbun ahmar. Intahati-s-sûra! | za provjeru |

