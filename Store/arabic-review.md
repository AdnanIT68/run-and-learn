# Arapski: lista za provjeru

Sve arapske riječi i rečenice u igri, za provjeru od strane nekoga ko zna arapski,
prije objave. Provjeriti: prijevod, harakate (znakove za samoglasnike), latinicu i
izgovor u glasu (`Assets/Resources/Words/<Svijet>/Audio/ar/<id>.ogg`).

Pravila koja su korištena: standardni arapski (fusha), riječi bez člana „al-“, u
pauzalnom obliku (bez padežnog nastavka na kraju). Latinica je prilagođena bosanskom
čitanju: dž = ج, š = ش, h = ح/خ/ه, ' = ع ili hamza, kapica (â, î, û) = dug samoglasnik.
Fraze iz reda „intro“ igra pušta na bosanskom; arapska verzija postoji, ali se ne čuje.

Kad je unos provjeren, u `Tools/words_world*.json` promijeniti `ar_status` u „provjereno“
i ponovo pokrenuti `Tools/.venv/Scripts/python.exe Tools/make_arabic_review.py`.
Ukupno 156 unosa, od toga 156 čeka provjeru.


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

## Slova u svijetu 7 (imena slova, glas izgovara ime)

| id | slovo | latinica | status |
|---|---|---|---|
| `letter_ar_jim` | ج | džîm | za provjeru |
| `letter_ar_hha` | ح | hâ' | za provjeru |
| `letter_ar_dal` | د | dâl | za provjeru |
| `letter_ar_qaf` | ق | qâf | za provjeru |
| `letter_ar_kaf` | ك | kâf | za provjeru |
| `letter_ar_mim` | م | mîm | za provjeru |

