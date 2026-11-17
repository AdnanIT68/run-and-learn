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
Ukupno 244 unosa, od toga 0 čeka provjeru.


## World1: Nenina bašta

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `apple` | jabuka | تُفَّاحَة | tuffâha | provjereno |
| riječ | `pear` | kruška | كُمَّثْرَى | kummasrâ | provjereno |
| riječ | `banana` | banana | مَوْزَة | mavza | provjereno |
| riječ | `strawberry` | jagoda | فَرَاوِلَة | farâvila | provjereno |
| riječ | `grapes` | grožđe | عِنَب | 'inab | provjereno |
| riječ | `watermelon` | lubenica | بِطِّيخ | bittîh | provjereno |
| riječ | `lemon` | limun | لَيْمُون | lejmûn | provjereno |
| riječ | `carrot` | mrkva | جَزَرَة | džazara | provjereno |
| riječ | `tomato` | paradajz | طَمَاطِم | tamâtim | provjereno |
| riječ | `cucumber` | krastavac | خِيَار | hijâr | provjereno |
| fraza | `praise_1` | Bravo! | رَائِع! | Râ'i'! | provjereno |
| fraza | `praise_2` | Tačno! | صَحِيح! | Sahîh! | provjereno |
| fraza | `praise_3` | Super! | مُمْتَاز! | Mumtâz! | provjereno |
| fraza | `intro` | Slušaj riječ, pa skoči u pravu sliku! | اِسْتَمِعْ إِلَى الْكَلِمَةِ، ثُمَّ اقْفِزْ إِلَى الصُّورَةِ الصَّحِيحَة! | Istami' ila-l-kalimati, summa-kfiz ila-s-sûrati-s-sahîha! | provjereno |
| fraza | `jump` | Skoči! | اِقْفِزْ! | Ikfiz! | provjereno |
| fraza | `goal` | Bravo! Cilj je ispunjen! | رَائِع! تَمَّ الْهَدَف! | Râ'i'! Tamma-l-hadaf! | provjereno |

## World2: Farma kod dede

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `cow` | krava | بَقَرَة | bakara | provjereno |
| riječ | `sheep` | ovca | خَرُوف | harûf | provjereno |
| riječ | `goat` | koza | مَاعِز | mâ'iz | provjereno |
| riječ | `horse` | konj | حِصَان | hisân | provjereno |
| riječ | `hen` | kokoš | دَجَاجَة | dadžâdža | provjereno |
| riječ | `rooster` | pijetao | دِيك | dîk | provjereno |
| riječ | `duck` | patka | بَطَّة | batta | provjereno |
| riječ | `cat` | mačka | قِطَّة | kitta | provjereno |
| riječ | `rabbit` | zec | أَرْنَب | arnab | provjereno |
| riječ | `bee` | pčela | نَحْلَة | nahla | provjereno |
| brojanje | `hen_1` | jedna kokoš | دَجَاجَة وَاحِدَة | dadžâdža vâhida | provjereno |
| brojanje | `hen_2` | dvije kokoši | دَجَاجَتَان | dadžâdžatân | provjereno |
| brojanje | `hen_3` | tri kokoši | ثَلَاثُ دَجَاجَات | salâsu dadžâdžât | provjereno |
| brojanje | `hen_4` | četiri kokoši | أَرْبَعُ دَجَاجَات | arba'u dadžâdžât | provjereno |
| brojanje | `hen_5` | pet kokoši | خَمْسُ دَجَاجَات | hamsu dadžâdžât | provjereno |
| brojanje | `cow_1` | jedna krava | بَقَرَة وَاحِدَة | bakara vâhida | provjereno |
| brojanje | `cow_2` | dvije krave | بَقَرَتَان | bakaratân | provjereno |
| brojanje | `cow_3` | tri krave | ثَلَاثُ بَقَرَات | salâsu bakarât | provjereno |
| brojanje | `cow_4` | četiri krave | أَرْبَعُ بَقَرَات | arba'u bakarât | provjereno |
| brojanje | `cow_5` | pet krava | خَمْسُ بَقَرَات | hamsu bakarât | provjereno |
| brojanje | `sheep_1` | jedna ovca | خَرُوف وَاحِد | harûf vâhid | provjereno |
| brojanje | `sheep_2` | dvije ovce | خَرُوفَان | harûfân | provjereno |
| brojanje | `sheep_3` | tri ovce | ثَلَاثَةُ خِرَاف | salâsatu hirâf | provjereno |
| brojanje | `sheep_4` | četiri ovce | أَرْبَعَةُ خِرَاف | arba'atu hirâf | provjereno |
| brojanje | `sheep_5` | pet ovaca | خَمْسَةُ خِرَاف | hamsatu hirâf | provjereno |
| brojanje | `duck_1` | jedna patka | بَطَّة وَاحِدَة | batta vâhida | provjereno |
| brojanje | `duck_2` | dvije patke | بَطَّتَان | battatân | provjereno |
| brojanje | `duck_3` | tri patke | ثَلَاثُ بَطَّات | salâsu battât | provjereno |
| brojanje | `duck_4` | četiri patke | أَرْبَعُ بَطَّات | arba'u battât | provjereno |
| brojanje | `duck_5` | pet pataka | خَمْسُ بَطَّات | hamsu battât | provjereno |
| brojanje | `rabbit_1` | jedan zec | أَرْنَب وَاحِد | arnab vâhid | provjereno |
| brojanje | `rabbit_2` | dva zeca | أَرْنَبَان | arnabân | provjereno |
| brojanje | `rabbit_3` | tri zeca | ثَلَاثَةُ أَرَانِب | salâsatu arânib | provjereno |
| brojanje | `rabbit_4` | četiri zeca | أَرْبَعَةُ أَرَانِب | arba'atu arânib | provjereno |
| brojanje | `rabbit_5` | pet zečeva | خَمْسَةُ أَرَانِب | hamsatu arânib | provjereno |

## World3: Naša kuća

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `mom` | mama | أُمّ | umm | provjereno |
| riječ | `dad` | tata | أَب | ab | provjereno |
| riječ | `grandma` | nena | جَدَّة | džadda | provjereno |
| riječ | `grandpa` | dedo | جَدّ | džadd | provjereno |
| riječ | `sister` | sestra | أُخْت | uht | provjereno |
| riječ | `brother` | brat | أَخ | ah | provjereno |
| riječ | `baby` | beba | رَضِيع | radî' | provjereno |
| riječ | `house` | kuća | بَيْت | bejt | provjereno |
| riječ | `bread` | hljeb | خُبْز | hubz | provjereno |
| riječ | `milk` | mlijeko | حَلِيب | halîb | provjereno |

## World4: Čaršija

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `mosque` | džamija | مَسْجِد | masdžid | provjereno |
| riječ | `pigeon` | golub | حَمَامَة | hamâma | provjereno |
| riječ | `tram` | tramvaj | تِرَام | tirâm | provjereno |
| riječ | `bus` | autobus | حَافِلَة | hâfila | provjereno |
| riječ | `bicycle` | bicikl | دَرَّاجَة | darrâdža | provjereno |
| riječ | `ball` | lopta | كُرَة | kura | provjereno |
| riječ | `book` | knjiga | كِتَاب | kitâb | provjereno |
| riječ | `umbrella` | kišobran | مِظَلَّة | mizalla | provjereno |
| riječ | `clock` | sat | سَاعَة | sâ'a | provjereno |
| riječ | `fountain` | česma | نَافُورَة | nâfûra | provjereno |
| komanda | `cmd_jump` | Skoči! | اِقْفِزْ! | Ikfiz! | provjereno |
| komanda | `cmd_stay` | Ostani dolje! | اِبْقَ فِي الْأَسْفَل! | Ibka fi-l-asfal! | provjereno |
| komanda | `cmd_double` | Skoči dva puta! | اِقْفِزْ مَرَّتَيْن! | Ikfiz marratejn! | provjereno |

## World5: Rijeka i Stari most

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `sun` | sunce | شَمْس | šams | provjereno |
| riječ | `rain` | kiša | مَطَر | matar | provjereno |
| riječ | `rainbow` | duga | قَوْسُ قُزَح | kavsu kuzah | provjereno |
| riječ | `bridge` | most | جِسْر | džisr | provjereno |
| riječ | `fish` | riba | سَمَكَة | samaka | provjereno |
| riječ | `frog` | žaba | ضِفْدَع | difda' | provjereno |
| riječ | `butterfly` | leptir | فَرَاشَة | farâša | provjereno |
| riječ | `flower` | cvijet | زَهْرَة | zahra | provjereno |
| riječ | `tree` | drvo | شَجَرَة | šadžara | provjereno |
| riječ | `boat` | čamac | قَارِب | kârib | provjereno |
| veliko/malo | `fish_big` | velika riba | سَمَكَة كَبِيرَة | samaka kabîra | provjereno |
| veliko/malo | `fish_small` | mala riba | سَمَكَة صَغِيرَة | samaka sagîra | provjereno |
| veliko/malo | `frog_big` | velika žaba | ضِفْدَع كَبِير | difda' kabîr | provjereno |
| veliko/malo | `frog_small` | mala žaba | ضِفْدَع صَغِير | difda' sagîr | provjereno |
| veliko/malo | `boat_big` | veliki čamac | قَارِب كَبِير | kârib kabîr | provjereno |
| veliko/malo | `boat_small` | mali čamac | قَارِب صَغِير | kârib sagîr | provjereno |
| veliko/malo | `flower_big` | veliki cvijet | زَهْرَة كَبِيرَة | zahra kabîra | provjereno |
| veliko/malo | `flower_small` | mali cvijet | زَهْرَة صَغِيرَة | zahra sagîra | provjereno |
| veliko/malo | `tree_big` | veliko drvo | شَجَرَة كَبِيرَة | šadžara kabîra | provjereno |
| veliko/malo | `tree_small` | malo drvo | شَجَرَة صَغِيرَة | šadžara sagîra | provjereno |

## World6: Planina zimi

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `snow` | snijeg | ثَلْج | saldž | provjereno |
| riječ | `snowman` | snjegović | رَجُلُ الثَّلْج | radžulu-s-saldž | provjereno |
| riječ | `sled` | sanke | مِزْلَجَة | mizladža | provjereno |
| riječ | `skis` | skije | زَلَّاجَات | zallâdžât | provjereno |
| riječ | `gloves` | rukavice | قُفَّازَات | kuffâzât | provjereno |
| riječ | `scarf` | šal | وِشَاح | višâh | provjereno |
| riječ | `boots` | čizme | جَزْمَة | džazma | provjereno |
| riječ | `coat` | jakna | مِعْطَف | mi'taf | provjereno |
| riječ | `mountain` | planina | جَبَل | džabal | provjereno |
| riječ | `ice` | led | جَلِيد | džalîd | provjereno |
| rečenica | `s_snowing` | Pada snijeg. | الثَّلْجُ يَتَسَاقَط. | As-saldžu jatasâkat. | provjereno |
| rečenica | `s_sunny` | Sija sunce. | الشَّمْسُ مُشْرِقَة. | Aš-šamsu mušrika. | provjereno |
| rečenica | `s_raining` | Pada kiša. | السَّمَاءُ تُمْطِر. | As-samâ'u tumtir. | provjereno |
| rečenica | `s_sledding` | Rukajja se sanka. | رُقَيَّةُ تَتَزَلَّجُ بِالْمِزْلَجَة. | Rukajjatu tatazalladžu bi-l-mizladža. | provjereno |
| rečenica | `s_skiing` | Rukajja skija. | رُقَيَّةُ تَتَزَلَّجُ بِالزَّلَّاجَات. | Rukajjatu tatazalladžu bi-z-zallâdžât. | provjereno |
| rečenica | `s_snowman` | Rukajja pravi snjegovića. | رُقَيَّةُ تَصْنَعُ رَجُلَ الثَّلْج. | Rukajjatu tasna'u radžula-s-saldž. | provjereno |
| rečenica | `s_gloves` | Nosim rukavice. | أَلْبَسُ قُفَّازَات. | Albasu kuffâzât. | provjereno |
| rečenica | `s_scarf` | Nosim šal. | أَلْبَسُ وِشَاحًا. | Albasu višâhâ. | provjereno |
| rečenica | `s_boots` | Nosim čizme. | أَلْبَسُ جَزْمَة. | Albasu džazma. | provjereno |

## World7: Škola

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `pencil` | olovka | قَلَمُ رَصَاص | kalamu rasâs | provjereno |
| riječ | `notebook` | sveska | دَفْتَر | daftar | provjereno |
| riječ | `backpack` | ranac | حَقِيبَة | hakîba | provjereno |
| riječ | `scissors` | makaze | مِقَصّ | mikass | provjereno |
| riječ | `ruler` | lenjir | مِسْطَرَة | mistara | provjereno |
| riječ | `crayon` | bojica | قَلَمُ تَلْوِين | kalamu talvîn | provjereno |
| riječ | `globe` | globus | كُرَة أَرْضِيَّة | kura ardijja | provjereno |
| riječ | `abacus` | računaljka | مِعْدَاد | mi'dâd | provjereno |
| riječ | `bell` | zvono | جَرَس | džaras | provjereno |
| riječ | `school` | škola | مَدْرَسَة | madrasa | provjereno |
| broj | `num_1` | jedan | وَاحِد | vâhid | provjereno |
| broj | `num_2` | dva | اِثْنَان | isnân | provjereno |
| broj | `num_3` | tri | ثَلَاثَة | salâsa | provjereno |
| broj | `num_4` | četiri | أَرْبَعَة | arba'a | provjereno |
| broj | `num_5` | pet | خَمْسَة | hamsa | provjereno |
| broj | `num_6` | šest | سِتَّة | sitta | provjereno |
| broj | `num_7` | sedam | سَبْعَة | sab'a | provjereno |
| broj | `num_8` | osam | ثَمَانِيَة | samânija | provjereno |
| broj | `num_9` | devet | تِسْعَة | tis'a | provjereno |
| broj | `num_10` | deset | عَشَرَة | 'ašara | provjereno |
| fraza | `op_plus` | plus | زَائِد | zâ'id | provjereno |
| fraza | `op_minus` | minus | نَاقِص | nâqis | provjereno |

## World8: Noćno nebo

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `star_word` | zvijezda | نَجْمَة | nadžma | provjereno |
| riječ | `moon` | mjesec | قَمَر | kamar | provjereno |
| riječ | `rocket` | raketa | صَارُوخ | sârûh | provjereno |
| riječ | `planet` | planeta | كَوْكَب | kavkab | provjereno |
| riječ | `owl` | sova | بُومَة | bûma | provjereno |
| riječ | `comet` | kometa | مُذَنَّب | muzannab | provjereno |
| riječ | `telescope` | teleskop | تِلِسْكُوب | tiliskûb | provjereno |
| riječ | `bed` | krevet | سَرِير | sarîr | provjereno |
| riječ | `satellite` | satelit | قَمَر صِنَاعِيّ | kamar sinâ'î | provjereno |
| riječ | `night` | noć | لَيْل | lejl | provjereno |
| fraza | `finale` | Bravo! Svi svjetovi su završeni! | رَائِع! اِنْتَهَتْ كُلُّ الْعَوَالِم! | Râ'i'! Intahat kullu-l-'avâlim! | provjereno |

## World9: Lijepe riječi

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `selam` | selam | السَّلَامُ عَلَيْكُم | es-selâmu alejkum | provjereno |
| riječ | `thanks` | hvala | شُكْرًا | šukran | provjereno |
| riječ | `here_you_are` | izvoli | تَفَضَّل | tafaddal | provjereno |
| riječ | `sorry` | oprosti | آسِف | âsif | provjereno |
| riječ | `bismillah` | bismillah | بِسْمِ اللّٰه | bismillâh | provjereno |
| riječ | `mashallah` | mašallah | مَا شَاءَ اللّٰه | mâ šâ' allâh | provjereno |

## World10: Boje i oblici

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `red` | crvena | أَحْمَر | ahmar | provjereno |
| riječ | `blue` | plava | أَزْرَق | azrak | provjereno |
| riječ | `yellow` | žuta | أَصْفَر | asfar | provjereno |
| riječ | `green` | zelena | أَخْضَر | ahdar | provjereno |
| riječ | `orange` | narandžasta | بُرْتُقَالِيّ | burtukâlî | provjereno |
| riječ | `purple` | ljubičasta | بَنَفْسَجِيّ | banafsadžî | provjereno |
| riječ | `circle` | krug | دَائِرَة | dâ'ira | provjereno |
| riječ | `square` | kvadrat | مُرَبَّع | murabba' | provjereno |
| riječ | `triangle` | trougao | مُثَلَّث | musallas | provjereno |
| riječ | `heart` | srce | قَلْب | kalb | provjereno |

## World11: Tijelo i odjeća

| vrsta | id | bosanski | arapski | latinica | status |
|---|---|---|---|---|---|
| riječ | `hand` | ruka | يَد | jad | provjereno |
| riječ | `foot` | stopalo | قَدَم | kadam | provjereno |
| riječ | `eye` | oko | عَيْن | 'ajn | provjereno |
| riječ | `ear` | uho | أُذُن | uzun | provjereno |
| riječ | `nose` | nos | أَنْف | anf | provjereno |
| riječ | `mouth` | usta | فَم | fam | provjereno |
| riječ | `shirt` | majica | قَمِيص | kamîs | provjereno |
| riječ | `dress` | haljina | فُسْتَان | fustân | provjereno |
| riječ | `shoe` | patika | حِذَاء | hizâ' | provjereno |
| riječ | `sock` | čarapa | جَوْرَب | džavrab | provjereno |

## Imena slova (Škola i „Piši slovo“; glas kaže „slovo …“)

| id | slovo | izgovoreno | latinica | status |
|---|---|---|---|---|
| `ar_alif` | ا | حَرْفُ الْأَلِف | alif | provjereno |
| `ar_ba` | ب | حَرْفُ الْبَاء | bâ' | provjereno |
| `ar_ta` | ت | حَرْفُ التَّاء | tâ' | provjereno |
| `ar_tha` | ث | حَرْفُ الثَّاء | sâ' | provjereno |
| `ar_jim` | ج | حَرْفُ الْجِيم | džîm | provjereno |
| `ar_hha` | ح | حَرْفُ الْحَاء | hâ' | provjereno |
| `ar_kha` | خ | حَرْفُ الْخَاء | hâ' | provjereno |
| `ar_dal` | د | حَرْفُ الدَّال | dâl | provjereno |
| `ar_dhal` | ذ | حَرْفُ الذَّال | zâl | provjereno |
| `ar_ra` | ر | حَرْفُ الرَّاء | râ' | provjereno |
| `ar_zay` | ز | حَرْفُ الزَّاي | zâj | provjereno |
| `ar_sin` | س | حَرْفُ السِّين | sîn | provjereno |
| `ar_shin` | ش | حَرْفُ الشِّين | šîn | provjereno |
| `ar_sad` | ص | حَرْفُ الصَّاد | sâd | provjereno |
| `ar_dad` | ض | حَرْفُ الضَّاد | dâd | provjereno |
| `ar_tta` | ط | حَرْفُ الطَّاء | tâ' | provjereno |
| `ar_dha` | ظ | حَرْفُ الظَّاء | zâ' | provjereno |
| `ar_ayn` | ع | حَرْفُ الْعَيْن | 'ajn | provjereno |
| `ar_ghayn` | غ | حَرْفُ الْغَيْن | gajn | provjereno |
| `ar_fa` | ف | حَرْفُ الْفَاء | fâ' | provjereno |
| `ar_qaf` | ق | حَرْفُ الْقَاف | qâf | provjereno |
| `ar_kaf` | ك | حَرْفُ الْكَاف | kâf | provjereno |
| `ar_lam` | ل | حَرْفُ اللَّام | lâm | provjereno |
| `ar_mim` | م | حَرْفُ الْمِيم | mîm | provjereno |
| `ar_nun` | ن | حَرْفُ النُّون | nûn | provjereno |
| `ar_ha` | ه | حَرْفُ الْهَاء | hâ' | provjereno |
| `ar_waw` | و | حَرْفُ الْوَاو | vâv | provjereno |
| `ar_ya` | ي | حَرْفُ الْيَاء | jâ' | provjereno |

## Slikovnice (rečenice koje glas čita)

| id | bosanski | arapski | latinica | status |
|---|---|---|---|---|
| `books/World1/1` | Rukajja je u neninoj bašti. Gle, crvena jabuka! | رُقَيَّةُ فِي حَدِيقَةِ الْجَدَّة. اُنْظُرْ، تُفَّاحَةٌ حَمْرَاء! | Rukajjatu fî hadîkati-l-džadda. Unzur, tuffâhatun hamrâ'! | provjereno |
| `books/World1/2` | Nena bere jagode. Pepi jede bananu. | الْجَدَّةُ تَقْطِفُ الْفَرَاوِلَة. بِيبِي يَأْكُلُ مَوْزَة. | Al-džaddatu taktifu-l-farâvila. Pîpî ja'kulu mavza. | provjereno |
| `books/World1/3` | U zemlji raste mrkva. Rukajja je čupa. Hop! | الْجَزَرَةُ فِي الْأَرْض. رُقَيَّةُ تَسْحَبُهَا. هُوب! | Al-džazaratu fi-l-ard. Rukajjatu tashabuhâ. Hûb! | provjereno |
| `books/World1/4` | Na kraju jedu veliku lubenicu. Mmm, slatka je! | فِي النِّهَايَةِ يَأْكُلُونَ بِطِّيخًا كَبِيرًا. مَا أَحْلَاه! | Fi-n-nihâjati ja'kulûna bittîhan kabîran. Mâ ahlâh! | provjereno |
| `books/World2/1` | Kukuriku! Pijetao budi farmu kod dede. | كُوكُو كُوكُو! الدِّيكُ يُوقِظُ مَزْرَعَةَ الْجَدّ. | Kûkû kûkû! Ad-dîku jûkizu mazra'ata-l-džadd. | provjereno |
| `books/World2/2` | Dedo muze kravu. Evo svježeg mlijeka! | الْجَدُّ يَحْلُبُ الْبَقَرَة. هَذَا حَلِيبٌ طَازَج! | Al-džaddu jahlubu-l-bakara. Hâzâ halîbun tâzadž! | provjereno |
| `books/World2/3` | Kokoš traži zrnca. Patka pliva u bari. | الدَّجَاجَةُ تَبْحَثُ عَنِ الْحَبّ. الْبَطَّةُ تَسْبَحُ فِي الْبِرْكَة. | Ad-dadžâdžatu tabhasu 'ani-l-habb. Al-battatu tasbahu fi-l-birka. | provjereno |
| `books/World2/4` | Zec skače: hop, hop! Rukajja skače s njim. | الْأَرْنَبُ يَقْفِز: هُوب، هُوب! رُقَيَّةُ تَقْفِزُ مَعَه. | Al-arnabu jakfiz: hûb, hûb! Rukajjatu takfizu ma'ah. | provjereno |
| `books/World3/1` | Ovo je naša kuća. Ovdje živi Rukajja. | هَذَا بَيْتُنَا. هُنَا تَسْكُنُ رُقَيَّة. | Hâzâ bajtunâ. Hunâ taskunu Rukajja. | provjereno |
| `books/World3/2` | Mama peče hljeb. Cijela kuća lijepo miriše. | الْأُمُّ تَخْبِزُ الْخُبْز. رَائِحَةُ الْبَيْتِ جَمِيلَة. | Al-ummu tahbizu-l-hubz. Râ'ihatu-l-bajti džamîla. | provjereno |
| `books/World3/3` | Dedo čita, a nena plete šal. | الْجَدُّ يَقْرَأُ، وَالْجَدَّةُ تَحِيكُ وِشَاحًا. | Al-džaddu jakra'u, va-l-džaddatu tahîku višâhan. | provjereno |
| `books/World3/4` | Beba pije mlijeko i zaspi. Laku noć! | الرَّضِيعُ يَشْرَبُ الْحَلِيبَ وَيَنَام. تُصْبِحُ عَلَى خَيْر! | Ar-radî'u jašrabu-l-halîba va janâm. Tusbihu 'alâ hajr! | provjereno |
| `books/World4/1` | Na čaršiji golubovi piju vodu sa česme. | فِي السُّوقِ الْقَدِيمِ يَشْرَبُ الْحَمَامُ مِنَ النَّافُورَة. | Fi-s-sûki-l-kadîmi jašrabu-l-hamâmu mina-n-nâfûra. | provjereno |
| `books/World4/2` | Tramvaj zvoni: cin-cin! Pazi, Rukajja! | التِّرَامُ يَرِنّ: تِن تِن! اِنْتَبِهِي يَا رُقَيَّة! | At-tirâmu jarinn: tin tin! Intabihî jâ Rukajja! | provjereno |
| `books/World4/3` | Veliki sat pokazuje podne. | السَّاعَةُ الْكَبِيرَةُ تُشِيرُ إِلَى الظُّهْر. | As-sâ'atu-l-kabîratu tušîru ila-z-zuhr. | provjereno |
| `books/World4/4` | Djeca igraju loptu. Rukajja daje gol! | الْأَطْفَالُ يَلْعَبُونَ بِالْكُرَة. رُقَيَّةُ تُسَجِّلُ هَدَفًا! | Al-atfâlu jal'abûna bi-l-kura. Rukajjatu tusaddžilu hadafan! | provjereno |
| `books/World5/1` | Sunce sija iznad Starog mosta. | الشَّمْسُ تُشْرِقُ فَوْقَ الْجِسْرِ الْقَدِيم. | Aš-šamsu tušriku favka-l-džisri-l-kadîm. | provjereno |
| `books/World5/2` | U rijeci pliva mala riba. | سَمَكَةٌ صَغِيرَةٌ تَسْبَحُ فِي النَّهْر. | Samakatun sagîratun tasbahu fi-n-nahr. | provjereno |
| `books/World5/3` | Pada kiša. Žaba je sretna: kre-kre! | الْمَطَرُ يَنْزِل. الضِّفْدَعُ سَعِيد: نَقْ نَقْ! | Al-mataru janzil. Ad-difda'u sa'îd: nak nak! | provjereno |
| `books/World5/4` | Poslije kiše dođe duga. Leptir leti do nje. | بَعْدَ الْمَطَرِ يَظْهَرُ قَوْسُ قُزَح. الْفَرَاشَةُ تَطِيرُ إِلَيْه. | Ba'da-l-matari jazharu kavsu kuzah. Al-farâšatu tatîru ilajh. | provjereno |
| `books/World6/1` | Na planini pada snijeg. Sve je bijelo! | الثَّلْجُ يَتَسَاقَطُ عَلَى الْجَبَل. كُلُّ شَيْءٍ أَبْيَض! | As-saldžu jatasâkatu 'ala-l-džabal. Kullu šaj'in abjad! | provjereno |
| `books/World6/2` | Rukajja oblači jaknu i rukavice. | رُقَيَّةُ تَلْبَسُ الْمِعْطَفَ وَالْقُفَّازَات. | Rukajjatu talbasu-l-mi'tafa va-l-kuffâzât. | provjereno |
| `books/World6/3` | Sanke jure niz brdo. Juhuu! | الْمِزْلَجَةُ تَنْزَلِقُ مِنَ التَّلّ. يَاه! | Al-mizladžatu tanzaliku mina-t-tall. Jâh! | provjereno |
| `books/World6/4` | Na kraju prave snjegovića sa šalom. | فِي النِّهَايَةِ يَصْنَعُونَ رَجُلَ ثَلْجٍ بِوِشَاح. | Fi-n-nihâjati jasna'ûna radžula saldžin bi-višâh. | provjereno |
| `books/World7/1` | Zvono zvoni. Počinje škola! | الْجَرَسُ يَرِنّ. تَبْدَأُ الْمَدْرَسَة! | Al-džarasu jarinn. Tabda'u-l-madrasa! | provjereno |
| `books/World7/2` | Rukajja vadi svesku iz ranca. | رُقَيَّةُ تُخْرِجُ الدَّفْتَرَ مِنَ الْحَقِيبَة. | Rukajjatu tuhridžu-d-daftara mina-l-hakîba. | provjereno |
| `books/World7/3` | Olovkom piše slova: A, B, C. | تَكْتُبُ الْحُرُوفَ بِقَلَمِ الرَّصَاص: أَلِف، بَاء، تَاء. | Taktubu-l-hurûfa bi-kalami-r-rasâs: alif, bâ', tâ'. | provjereno |
| `books/World7/4` | Bojicama crta sunce. Bravo, Rukajja! | تَرْسُمُ شَمْسًا بِأَقْلَامِ التَّلْوِين. أَحْسَنْتِ يَا رُقَيَّة! | Tarsumu šamsan bi-aklâmi-t-talvîn. Ahsanti jâ Rukajja! | provjereno |
| `books/World8/1` | Pala je noć. Na nebu sija mjesec. | جَاءَ اللَّيْل. الْقَمَرُ يُضِيءُ فِي السَّمَاء. | Džâ'a-l-lajl. Al-kamaru judî'u fi-s-samâ'. | provjereno |
| `books/World8/2` | Rukajja gleda kroz teleskop. Koliko zvijezda! | رُقَيَّةُ تَنْظُرُ بِالتِّلِسْكُوب. مَا أَكْثَرَ النُّجُوم! | Rukajjatu tanzuru bi-t-tiliskûb. Mâ aksara-n-nudžûm! | provjereno |
| `books/World8/3` | Sova kaže: hu-hu! Ona noću ne spava. | الْبُومَةُ تَقُول: هُو هُو! هِيَ لَا تَنَامُ فِي اللَّيْل. | Al-bûmatu takûl: hû hû! Hija lâ tanâmu fi-l-lajl. | provjereno |
| `books/World8/4` | Rukajja ide u krevet i sanja rakete. Laku noć! | رُقَيَّةُ تَذْهَبُ إِلَى السَّرِيرِ وَتَحْلُمُ بِالصَّوَارِيخ. تُصْبِحِينَ عَلَى خَيْر! | Rukajjatu tazhabu ila-s-sarîri va tahlumu bi-s-savârîh. Tusbihîna 'alâ hajr! | provjereno |
| `books/World9/1` | Rukajja sretne komšiju i kaže: Selam! | رُقَيَّةُ تَلْتَقِي بِالْجَارِ وَتَقُول: السَّلَامُ عَلَيْكُم! | Rukajjatu taltakî bi-l-džâri va takûl: As-salâmu 'alajkum! | provjereno |
| `books/World9/2` | Prije jela kaže: Bismillah. | قَبْلَ الْأَكْلِ تَقُول: بِسْمِ اللّٰه. | Kabla-l-akli takûl: Bismillâh. | provjereno |
| `books/World9/3` | Daje jabuku bratu: Izvoli! On kaže: Hvala! | تُعْطِي أَخَاهَا تُفَّاحَة: تَفَضَّل! يَقُول: شُكْرًا! | Tu'tî ahâhâ tuffâha: Tafaddal! Jakûl: Šukran! | provjereno |
| `books/World9/4` | Kad slučajno gurne drugaricu, kaže: Oprosti! | عِنْدَمَا تَدْفَعُ صَدِيقَتَهَا بِالْخَطَأِ تَقُول: آسِفَة! | 'Indamâ tadfa'u sadîkatahâ bi-l-hata'i takûl: Âsifa! | provjereno |
| `books/World10/1` | Pepi nosi crveni krug. | بِيبِي يَحْمِلُ دَائِرَةً حَمْرَاء. | Pîpî jahmilu dâ'iratan hamrâ'. | provjereno |
| `books/World10/2` | Rukajja slaže plavi kvadrat. | رُقَيَّةُ تَضَعُ مُرَبَّعًا أَزْرَق. | Rukajjatu tada'u murabba'an azrak. | provjereno |
| `books/World10/3` | Žuti trougao ide na vrh. To je krov! | الْمُثَلَّثُ الْأَصْفَرُ فَوْق. هَذَا سَقْف! | Al-musallasu-l-asfaru favk. Hâzâ sakf! | provjereno |
| `books/World10/4` | Zeleno drvo i crveno srce. Gotova je slika! | شَجَرَةٌ خَضْرَاءُ وَقَلْبٌ أَحْمَر. اِنْتَهَتِ الصُّورَة! | Šadžaratun hadrâ'u va kalbun ahmar. Intahati-s-sûra! | provjereno |
| `books/World11/1` | Rukajja maše rukom: Zdravo, Pepi! | رُقَيَّةُ تُلَوِّحُ بِيَدِهَا: مَرْحَبًا يَا بِيبِي! | Rukajjatu tulavvihu bi-jadihâ: Marhaban jâ Pîpî! | provjereno |
| `books/World11/2` | Očima gleda, a ušima sluša. | تَنْظُرُ بِعَيْنَيْهَا وَتَسْمَعُ بِأُذُنَيْهَا. | Tanzuru bi-'ajnajhâ va tasma'u bi-uzunajhâ. | provjereno |
| `books/World11/3` | Oblači majicu i čarape. | تَلْبَسُ قَمِيصًا وَجَوَارِب. | Talbasu kamîsan va džavârib. | provjereno |
| `books/World11/4` | Na kraju obuje patike i trči. Hop, hop! | فِي النِّهَايَةِ تَلْبَسُ الْحِذَاءَ وَتَجْرِي. هُوب، هُوب! | Fi-n-nihâjati talbasu-l-hizâ'a va tadžrî. Hûb, hûb! | provjereno |

