# Játékmenet dokumentáció (Game Design Doc)

Státusz: **VÁZLAT — a rendszer (loop, gazdaság, progresszió) letisztázva, a konkrét tartalmi számok (torony/ellenség statok, node-lista) még nyitottak.** `TBD` jelölés = még nincs végleges szám, de a *mechanizmus* már eldöntött.

## Fő képernyők és navigáció

```
Betöltés
   ↓
Főmenü (Skill fa nézet)
   ├── [Skill fa]      ← ugyanezen a képernyőn, node-ok megjelenítve/költhetők
   ├── [Pálya választó] → Pálya lista (feloldott/zárolt jelöléssel) → Pálya kiválasztása
   └── [Folytatás]     → közvetlenül elindítja a soron következő (legutóbb el nem ért) pályát
```

Pálya kiválasztása után:

```
Pálya betöltve (build fázis, tornyok elhelyezése)
   ↓ [Indítás gomb]
Hullám 1 lezajlik (automatikus harc, nincs építés harc közben)
   ↓ (build szünet — lehet módosítani a kihelyezést)
Hullám 2 ... N
   ↓
Kör vége (győzelem VAGY vereség — ugyanaz a lezárás mindkét esetben)
   ↓
Statisztika popup
   ├── [Preset mentése]  (felülír egy meglévő preset-et erre a pályára)
   ├── [Újra]            → azonnal újraindítja UGYANEZT a pályát (build fázistól), a korábban megszerzett arany/skill pontok megmaradnak
   └── [Befejezés]        → vissza a Főmenübe (Skill fa nézet)
```

## Core loop (részletes)

1. **Build fázis**: a játékos az útvonalon kívüli, szabad mezőkre tornyokat helyez el. A lerakható tornyok:
   - **típusa** a Skill fában unlockolt torony-típusokra korlátozódik
   - **darabszáma** a Skill fában unlockolt **slot-limitre** korlátozódik (max. egyidejűleg lerakott torony összesen, típustól függetlenül) — a térkép szabad helye ezen felül is korlátozhat
   - A tornyok lerakása **ingyenes** ebben a fázisban (nincs pályán belüli költés — lásd "Gazdaság" lent)
2. Játékos megnyomja az Indítás gombot → build fázis lezárul az adott hullámig
3. Hullám lezajlik: ellenségek automatikusan haladnak az útvonalon, tornyok automatikusan tüzelnek hatótávolságban lévő célra
4. Hullám vége → **build szünet**: a játékos módosíthatja a kihelyezést (a slot-limit és tér korlátain belül), mielőtt elindítja a következő hullámot
5. Ellenség halálakor: a `value` értéke hozzáadódik az adott futás gyűjtött aranyához
6. Ellenség célba érésekor: a `dmg` értéke levonódik a játékos életéből
7. Kör vége akkor következik be, ha:
   - a játékos élete eléri a 0-t (**vereség**), VAGY
   - az utolsó hullám is véget ért és a játékos élete még pozitív (**győzelem**)
   - **Mindkét esetben ugyanaz történik**: a kör lezárul, jön a statisztika popup, a gyűjtött arany megmarad
8. Statisztika popup: gyűjtött arany, tornyonkénti összesített sebzés-kimutatás, preset mentési lehetőség, [Újra] / [Befejezés] gombok
9. [Befejezés] → vissza a Főmenübe; a gyűjtött arany ekkor "realizálódik" a globális (skill fa) egyenlegben

## Gazdaság — két különálló valuta-kör

Ez a legfontosabb architekturális döntés, érdemes tisztán tartani:

| | Pályán belüli (build fázis) | Meta (skill fa) |
|---|---|---|
| Mivel fizetsz | Semmivel — a lerakás ingyenes a slot-limiten belül | Arannyal (meta-currency) |
| Mit vásárolsz | Torony pozíciót választod, nem "veszed" a tornyot | Skill fa node-okat (unlock/upgrade) |
| Honnan jön a pénz | — (nincs pályán belüli kereset-elköltés ciklus) | Ellenségek `value`-ja összegyűjtve egy futás alatt |
| Mikor számít el | — | A kör végén, [Befejezés]-kor realizálódik a globális egyenlegben |

**Nincs klasszikus "menetközbeni építés-gazdálkodás"** (nem kell a harc közben mérlegelni mit engedhetsz meg magadnak) — a stratégiai döntés a *build fázisban a rendelkezésre álló slot-okkal/típusokkal való elrendezés*, a *hosszabb távú* stratégiai döntés pedig a *skill fában mire költesz arany*.

**Farmolás**: mivel [Újra] minden lejátszásnál újra megadja az adott futás során szerzett aranyat, egy már teljesített, könnyű pálya tudatosan farmolható extra skill pontokért. Ez szándékos, nem bug — ha a nehézség-görbe később úgy alakul, hogy ez túl domináns stratégia lenne, itt kell visszanyúlni.

## Skill fa

**Vegyes fa**: egy központi hub node + 4 kardinális irányba induló upgrade-ág (0-5 szintig fejleszthető, minden szintlépésnek ára van), plusz 2 diagonális **egyszeri unlock** node (0 vagy 1 szint — torony TÍPUST nyitnak, nem statot). Mindegyik vonallal összekötve a hub-bal (vizuálisan is). **A node feliratok angolul jelennek meg a UI-ban**, a dokumentáció itt magyarul hivatkozik rájuk.

| Node (UI felirat) | id | Irány | Hatás / szint | Ár |
|---|---|---|---|---|
| Tower Number | `towers` | Hub (közép) | Overall torony-slot szám; **1. szinten indul alapból** (baseline) | 100 / 250 / 500 / 750 (1→5) |
| Health | `hp` | Fel | +2 kezdő élet | 5 / 10 / 20 / 35 / 50 |
| Attack Speed | `fireRate` | Le | MINDEN torony tűzgyorsasága +5%/szint (globális, nem torony-specifikus — lásd lent) | 5 / 10 / 20 / 35 / 50 (**TBD, placeholder** — nincs végleges ár megadva) |
| Gold | `currency` | Jobb | +10% szerzett arany minden megölt ellenségért (szorzó, nem fix bónusz) | 50 / 150 / 300 / 500 / 1000 |
| Damage | `dmg` | Bal | +1 sebzés MINDEN toronynak, globálisan | 5 / 10 / 20 / 35 / 50 |
| Splash Tower | `unlockSplash` | Jobb-fent (diagonál) | Egyszeri: feloldja a Splash Tower típust (lásd "Tornyok") — 0/1 szint, nincs upgrade | 150 |
| Sniper Tower | `unlockSniper` | Jobb-lent (diagonál) | Egyszeri: feloldja a Sniper Tower típust | 400 |

**Baseline megoldva**: a `towers` node alapból (friss mentésnél is) legalább 1. szinten van, tehát mindig lerakható az első torony — a korábbi verzióban felmerült "friss játékos beszorul" probléma ezzel elhárult. Az árlista emiatt csak 4 lépést tartalmaz (1→5), nincs "0→1" ár.

**Node megjelenítés**: hover nélkül a node a JELENLEGI kumulált hatást mutatja + szintet (pl. `+3 damage, 3/5`); hover-re (natív Godot tooltip) az egy szintnyi (marginális) hatás jelenik meg + ár vagy "MAX LEVEL"/"Unlocked" (egyszeri node-oknál) (pl. `+1 damage / Cost: 50 gold`). Az egyszeri unlock node-ok `MainMenu.MaxLevelFor()` szerint 1-nél maxolnak ki, nem 5-nél — a `towers`/`hp`/`currency`/`dmg`/`fireRate` node-októl eltérően.

**Eszköz/képesség ág**: egyelőre nincs a fenti node-ok között — a globális `fireRate` node NEM torony-specifikus, minden toronyra egyformán hat (3 torony típus is ugyanazt a globális multiplikátort kapja). Egy valódi torony-specifikus upgrade-rendszer még TBD.

**Fontos, rögzített elv**: a globális stat ág (élet, sebzés, torony slot stb.) kiemelt prioritás a skill fa tervezésénél — ez az az ág, ami minden futásra érezhető hatással van, nem csak egy-egy toronyra.

**Induló állapot (Fázis 2 bootstrap, "üres" skill fa)**: a játékos 0 meta-arannyal indul, semmi nincs megvásárolva a fán. A skill fától **függetlenül**, alapból (baseline, nem unlock-kötött) rendelkezésre áll:
- 1 torony típus (Rocket Tower, lásd lent) — a másik 2 skill fa vásárláshoz kötött (lásd "Torony-feloldás")
- 1 torony slot (egyszerre 1 lerakott torony engedélyezett)

A skill fa node-jai *ezen a baseline-on felül* adnak további torony **slot**-okat, damage/fire rate szintet, ÉS torony **típusokat** (lásd "Torony-feloldás") — tehát a fa nem "0-ról épít fel mindent", hanem a minimális játszható állapotot bővíti.

## Tornyok

Minden toronyhoz (lásd TECHNICAL.md "Adatvezérelt dizájn"): `Damage`, `Range`, `FireRate`, és opcionálisan `SplashRadius` (0 = nincs terület-sebzés, a lövedék csak a célpontot sebzi). Nincs `Cost` mező, mert a lerakás ingyenes — helyette a skill fában van ár a torony **slot**-okhoz és a globális damage/fire rate szintekhez, amik MINDEN torony típusra egyformán hatnak.

| Torony | Szerepkör | Damage | FireRate | Range | SplashRadius | Feloldás |
|---|---|---|---|---|---|---|
| Rocket Tower | Kiegyensúlyozott alap lövő, egyetlen célpontra | 1 | 1/mp | 3 tile | — | Alapból elérhető |
| Splash Tower | Terület sebzés — a becsapódási pont körül MINDEN ellenséget sebez, jó áradatok ellen | 1 | 0,7/mp | 2,5 tile | 1,5 tile | Level 2 elérésekor |
| Sniper Tower | Nagy sebzés, lassú tűzgyorsaság, hosszú lőtáv — jó bossok ellen | 4 | 0,4/mp | 5 tile | — | Level 3 elérésekor |

**Torony-feloldás**: a torony TÍPUSOK a **skill fából nyílnak** (`unlockSplash`/`unlockSniper` node, lásd "Skill fa"), nem a pálya-progressztől függenek — egy Splash Tower akár Level 1-en is megvehető/használható, ha van rá arany, nem kell előbb Level 2-t elérni. Egyszer megvéve **globálisan, minden pályán** elérhető marad. `LevelBuild.TowerUnlocks` (kód, `(ScenePath, RequiredSkillNodeId)` párok) tükrözi ezt.

**Vizuál**: mindhárom toronynak saját sprite-ja van (Kenney Tower Defense Top-Down pack, `Assets/Sprites/{tower_basic,splash_tower,sniper_tower}.png` — más-más tile ugyanabból a csomagból, nem csak színezett verzió), a Sniper Tower emellett kék-szürke tinttel (`Sprite2D.Modulate`, közvetlenül a `.tscn`-ben) is el van tolva a Rocket Tower vöröses árnyalatától, hogy a torony-paletta gombjain és a pályán is egyértelműen megkülönböztethetők legyenek.

## Ellenségek

**Nehézségi alapszabály (miért ezek a HP-számok)**: egy torony `_enemiesInRange`-ből mindig csak a legrégebb óta bent lévőt (a legelöl járót) lövi — a többi csak vár. Emiatt egy torony csak akkor tud **veszteség nélkül** végigvinni egy folyamatos ellenség-áradatot, ha `EnemyHP / TowerDPS ≤ SpawnInterval` — különben a hátrébb sorban állók csak részleges sebzést kapnak, mielőtt kilépnek a lőtávolból, és a maradék HP-jüktől függetlenül **teljes `Dmg`-et** visznek el becsapódáskor. `SpawnInterval` egységesen 2 mp, az alap torony DPS-e 1 (`Damage 1 × FireRate 1`) — innen jön a HP-skálázás:

| Ellenség | HP | Dmg | Value | Speed | Megjegyzés |
|---|---|---|---|---|---|
| Green Slime | 2 | 1 | 1 | 1 tile/mp | Alapból (dmg skill nélkül) veszteség nélkül ölhető — 1-2. kör |
| Blue Slime | 4 | 1 | 2 | 1 tile/mp | 1 `dmg` skill-szint (5 arany, DPS 2) kell a tiszta öléshez — 3-6. kör |
| Purple Slime | 6 | 2 | 3 | 1 tile/mp | 2 `dmg` skill-szint (15 arany, DPS 3) kell hozzá — 6-10. kör |
| Blue Slime (Mini Boss) | 30 | 5 | 5 | 1 tile/mp | Önálló egység (nem áradat), a lőtávban töltött ~5,7 mp alatt kell megölni — 5. kör záró ellenfele |
| Purple Slime (Final Boss) | 150 | 10 | 20 | 1 tile/mp | Kb. 27 összesített DPS kell hozzá (több torony egymást átfedő lőtávval, felturbózott dmg/tűzgyorsasággal) — 10. kör záró ellenfele |
| Green/Blue/Purple Triangle | 2 / 4 / 6 | 1 / 1 / 2 | 1 / 2 / 3 | **2** tile/mp | Ugyanaz a HP/Dmg/Value mint a hasonló színű Slime-nál, csak dupla sebesség — Level 1-en tartalék variáns, nincs körbe téve |

**Pálya-közti skálázás (miért ugranak ekkorát a Level 2/3 statok)**: mivel a skill fa **globális** (a meta-progresszió minden pályán megmarad), mire a játékos legyőzi Level 1 final bossát, már jelentős dmg/fireRate/torony-szint befektetéssel rendelkezik — egy vadonatúj Level 1-hez tervezett HP-szint triviális lenne neki. A Level 1-es görbe a `dmg` skill-szintekre épített "kaput" (1-2-3 szint, ld. fent); a Level 2/3-as görbe helyette a **torony-számra** épít (mivel a dmg 5. szinten befagy, max ~6 DPS/torony 1,25-szörös tűzgyorsasággal), tehát a magasabb HP-hoz több, egymást átfedő lőtávú toronyt kell csoportosítani ugyanarra a pálya-szakaszra — ugyanaz a taktika, mint a bossoknál.

| Ellenség | HP | Dmg | Value | Speed | Pálya |
|---|---|---|---|---|---|
| Red Slime | 10 | 2 | 5 | 1 tile/mp | Level 2, 1-2. kör |
| Orange Slime | 18 | 3 | 9 | 1 tile/mp | Level 2, 4-10. kör |
| Red/Orange Triangle | 10 / 18 | 2 / 3 | 5 / 9 | 2 tile/mp | Level 2, 6. (Red) és 7. (Orange) kör |
| Red Slime (Mini Boss) | 250 | 15 | 35 | 1 tile/mp | Level 2, 5. kör záró ellenfele |
| Orange Slime (Final Boss) | 500 | 20 | 60 | 1 tile/mp | Level 2, 10. kör záró ellenfele |
| Cyan Slime | 26 | 4 | 13 | 1 tile/mp | Level 3, 1-2. kör |
| Magenta Slime | 36 | 5 | 18 | 1 tile/mp | Level 3, 4-10. kör |
| Cyan/Magenta Triangle | 26 / 36 | 4 / 5 | 13 / 18 | 2 tile/mp | Level 3, 6. (Cyan) és 7. (Magenta) kör |
| Cyan Slime (Mini Boss) | 900 | 30 | 100 | 1 tile/mp | Level 3, 5. kör záró ellenfele |
| Magenta Slime (Final Boss) | 1600 | 40 | 150 | 1 tile/mp | Level 3, 10. kör záró ellenfele — a játék jelenlegi végső bossa |

Minden ellenséghez: `HP`, `Dmg`, `Value`, `Speed`, `DisplayName`.

**Vizuális variánsok placeholder-tier módon**: nincs egyedi art minden típushoz — a kör alakú (Slime) típusoknál ugyanazt a sprite-ot **színezzük** (`Tint`, Godot `Modulate`) és **skálázzuk** (`SpriteScale`, a hitbox-szal együtt `HitRadius`); a háromszög típusoknál (`EnemyData.Shape = Triangle`) nincs is sprite, kód-rajzolt alakzat helyettesíti (`Enemy._Draw()` a pályán, `TriangleIcon` a menükben). A mini/final boss ugyanígy csak egy nagyobbra skálázott, erősebb statú variáns — amikor lesz saját art, csak a `Sprite`/`Tint` mezőt kell cserélni, a rendszer nem változik.

**Célkép (Fázis 3+)**: mind a 3 pálya (30 kör) kész (lásd "Pályák és körök" lent); hosszabb távon minden ellenségnek lehet saját sprite-ja a jelenlegi tint/scale/kód-rajzolt trükk helyett.

**Terminológiai megjegyzés**: a "sebesség" szó két különböző mezőt takar attól függően, hogy toronyról vagy ellenségről van szó — toronynál `FireRate` (lövés/másodperc), ellenségnél `Speed` (tile/másodperc). A kódban és az adatmezőkben emiatt tudatosan más néven szerepelnek, hogy ne keveredjenek. Mindkét torony- és ellenség-mérték (`Range`, `Speed`) a rácsos pálya tile-egységére épül — lásd TECHNICAL.md "Pálya rács / koordináta-rendszer".

## Pályák és körök

**Egy pálya = 10 kör.** Jelenleg 3 pálya létezik (Level 1, 2, 3), mindegyik ugyanazt a `Level01Test.tscn` scene-t tölti be újra — a scene és a `LevelBuild.cs` kód pálya-agnosztikus, csak a `LevelBuild.RequestedLevelId` (`"Level1"`/`"Level2"`/`"Level3"`) alapján tölti be a megfelelő `Data/Waves/<LevelId>/RoundN.tres` fájlt.

**Vizuális téma pályánként**: a grass/path csempeszín és a háttér-shader színe pályánként eltér (`LevelBuild.ThemeFor`) — Level 1 erdő (zöld fű, barna földút), Level 2 sivatag/láva (homok, vörösesbarna út), Level 3 idegen/kozmikus (lila talaj, sötét cián út) — hogy a pályák ránézésre is megkülönböztethetők legyenek, nem csak az ellenség-színek alapján. **A pálya ALAKJA (egyenes sáv, nincsenek kanyarok) és az építhető csempék száma egyelőre MINDEN pályán azonos** — ezek nagyobb, az ellenség-mozgatás architektúráját (jelenleg `Position += Vector2.Right * Speed`, nincs waypoint-rendszer) és a már kiszámolt nehézségi görbét is érintő változtatások lennének, ezért külön tervezési kör kell hozzájuk, mielőtt belevágunk (lásd "Következő lépés").

- Minden pályának **10 köre** van, lineáris feloldási sorrenddel — egy kör sikeres teljesítése (győzelem, nem vereség/visszavonulás) feloldja a pálya következő körét
- **Az 5. és a 10. kör végén boss van**: az 5. kör végén egy **mini boss**, a 10. kör végén egy **final boss** zárja a hullámot
- **A pályák is lineárisan oldódnak fel**: az N. pálya 10. körének (final boss) teljesítése nyitja meg az N+1. pályát (`LevelBuild.LevelOrder` / `MainMenu.LevelIds`) — Level 1 alapból nyitva van
- A `PlayerProgress.HighestUnlockedRoundByLevel` (pálya-kulcsos map) tárolja pályánként meddig jutott a játékos; `PresetByLevel` ugyanígy pályánként EGY mentett torony-elrendezést. Egy hiányzó kulcs = az a pálya még nincs feloldva (`PlayerProgress.IsLevelUnlocked`)
- A Főmenü Play gombja mögötti popupban **pálya-fülek** (Level 1/2/3, fel nem oldott pálya letiltva) választják ki, melyik pálya kör-rácsát látjuk; alapból a legutóbb játszott pálya van kiválasztva (`PlayerProgress.LastPlayedLevelId`)

**Régi mentés migrálása**: a pálya-kulcsos map bevezetése előtt a mentés egyetlen lapos `HighestUnlockedRound`/`Level1Preset` mezőt használt. `LocalFileSaveProvider.Load()` felismeri ezeket a régi kulcsokat és átmásolja `HighestUnlockedRoundByLevel["Level1"]`/`PresetByLevel["Level1"]`-be (plusz ha Level 1 már teljesen kész volt, Level 2-t is feloldja) — enélkül egy meglévő mentés elveszítené a Level 1-es haladást.

**Level 1 tartalmi állapota** (Green → Blue → Purple Slime):

| Kör | Tartalom | Státusz |
|---|---|---|
| 1 | 10× Green Slime, 2 mp-enként 1 | Kész |
| 2 | 10× (2 Green Slime), 2 mp-enként | Kész |
| 3 | 2 Green Slime + 1 Blue Slime mintázat, 5×, összesen 10 Green + 5 Blue | Kész |
| 4 | 10× (2 Blue Slime), 2 mp-enként | Kész |
| 5 | 10× (3 Blue Slime), majd 1 Mini Boss a végén | Kész |
| 6 | 2 Blue Slime + 1 Purple Slime mintázat, 6×, összesen 12 Blue + 6 Purple | Kész |
| 7 | 10× (2 Purple Slime), 2 mp-enként | Kész |
| 8 | 10× (3 Purple Slime), 2 mp-enként | Kész |
| 9 | 10× (4 Purple Slime), 2 mp-enként | Kész |
| 10 | 10× (4 Purple Slime), majd 1 Final Boss a végén | Kész |

**Level 2 tartalmi állapota** (Red → Orange Slime, ugyanaz a sablon mint Level 1):

| Kör | Tartalom | Státusz |
|---|---|---|
| 1 | 10× Red Slime, 2 mp-enként 1 | Kész |
| 2 | 10× (2 Red Slime), 2 mp-enként | Kész |
| 3 | 2 Red Slime + 1 Orange Slime mintázat, 5×, összesen 10 Red + 5 Orange | Kész |
| 4 | 10× (2 Orange Slime), 2 mp-enként | Kész |
| 5 | 10× (3 Orange Slime), majd 1 Mini Boss (Red Slime Boss) a végén | Kész |
| 6 | 10× (2 Red Triangle), 2 mp-enként — gyors, sebesség-próba kör | Kész |
| 7 | Orange Slime + Orange Triangle mintázat, 6×, összesen 12 + 6 | Kész |
| 8 | 10× (3 Orange Slime), 2 mp-enként | Kész |
| 9 | 10× (4 Orange Slime), 2 mp-enként | Kész |
| 10 | 10× (4 Orange Slime), majd 1 Final Boss (Orange Slime Boss) a végén | Kész |

**Level 3 tartalmi állapota** (Cyan → Magenta Slime, ugyanaz a sablon):

| Kör | Tartalom | Státusz |
|---|---|---|
| 1 | 10× Cyan Slime, 2 mp-enként 1 | Kész |
| 2 | 10× (2 Cyan Slime), 2 mp-enként | Kész |
| 3 | 2 Cyan Slime + 1 Magenta Slime mintázat, 5×, összesen 10 Cyan + 5 Magenta | Kész |
| 4 | 10× (2 Magenta Slime), 2 mp-enként | Kész |
| 5 | 10× (3 Magenta Slime), majd 1 Mini Boss (Cyan Slime Boss) a végén | Kész |
| 6 | 10× (2 Cyan Triangle), 2 mp-enként — gyors, sebesség-próba kör | Kész |
| 7 | Magenta Slime + Magenta Triangle mintázat, 6×, összesen 12 + 6 | Kész |
| 8 | 10× (3 Magenta Slime), 2 mp-enként | Kész |
| 9 | 10× (4 Magenta Slime), 2 mp-enként | Kész |
| 10 | 10× (4 Magenta Slime), majd 1 Final Boss (Magenta Slime Boss) a végén — a játék jelenlegi vége | Kész |

## Hullámok (waves) — adatvezérelt "spawn step" modell

A korábban tervezett "központi görbe szorozza az alap statokat" formula-alapú nehézség-skálázást **felváltotta a kézzel megtervezett, körönkénti tartalom** — legalábbis Level 1 első felében. Minden kör egy `WaveData` resource-ban van leírva:

- `SpawnInterval`: hány másodpercenként pörög le a következő "tick" (jelenleg egységesen 2 mp)
- `Steps`: egy **sorrendben lejátszott lista**, minden elem (`SpawnStepData`) egy adott `EnemyData`-ból `Count` darabot indít egyszerre

Ez a modell egyetlen struktúrával fedi le mindhárom eddig előforduló mintázatot:
- **Egyenletes hullám**: minden Step ugyanaz (pl. Round 1: 10× "1 Green Slime")
- **Csoportos spawn**: minden Step többet indít egyszerre (pl. Round 4: 10× "2 Blue Slime")
- **Kevert/interleaved sorrend**: a Step-ek váltakoznak (pl. Round 3: Green, Green, Blue, ismétlve)
- **Záró boss**: az utolsó Step egy nagyobb, erősebb `EnemyData`-t indít 1 darabszámmal (Round 5)

Ez a rendszer **felváltja**, nem kiegészíti a korábbi "difficulty curve" tervet — ha később mégis szükség lenne formula-alapú skálázásra (pl. 6-10. kör gyors legenerálásához ahelyett, hogy mindent kézzel megterveznénk), az egy külön `WaveData`-generátor lenne, ami ugyanezt a Step-listát állítaná elő kódból.

- Build szünet minden kör előtt, ameddig a játékos akarja (nincs időnyomás — a kör a játékos indítására indul, nem automatikusan)
- TBD: van-e "gyorsítás" gomb a harc alatt (2x sebesség stb.)

## Statisztika popup — tartalma

- Összegyűjtött arany (ebben a futásban)
- Tornyonkénti összesített sebzés-kimutatás (melyik lerakott torony mennyit sebzett)
- [Preset mentése] gomb
- [Újra] gomb — azonnali újrajátszás, korábbi eredmények/arany megmaradnak
- [Befejezés] gomb — vissza a Főmenübe, arany realizálódik

## UI (MVP)

- Főmenü = Skill fa nézet + [Pálya választó] + [Folytatás] gombok
- Pálya-választó: lista, feloldott/zárolt állapot jelölve
- Pályán belül (build fázis): HUD (élet, hullám száma/összes), torony-paletta (unlockolt típusok, hátralévő slot-szám kijelezve), [Indítás] gomb
- Pályán belül (harc közben): csak megfigyelés, nincs interakció a build-del
- Statisztika popup — lásd fent

## Ami explicit NEM cél az MVP-ben

- Több útvonal / branching path (kivéve ha egyszerű megvalósítani)
- Pontos "eszköz/képesség" rendszer kidolgozása (a skill fában helye van fenntartva, de tartalom TBD)
- Multiplayer / bármilyen hálózati funkció
- Menetközbeni (harc alatti) építés/módosítás

## Következő lépés

A rendszer (loop, gazdaság, skill fa szerkezet, hullám-modell, több pálya, torony-választék) le van fektetve, mindhárom pálya (30 kör) kész tartalommal, 3 torony típussal (lásd "Pályák és körök", "Ellenségek", "Tornyok", "Skill fa" fent). Nyitott, tervezést igénylő tételek:
- A nehézségi görbe éles teszttel való finomhangolása (főleg Level 2/3, ahol a "hány tornyot kell csoportosítani" feltevés még nincs élesben tesztelve)
- **Pálya-forma (kanyarok)**: waypoint-alapú útvonal az ellenség-mozgáshoz a jelenlegi egyenes sáv helyett — ez újraszámolja a torony lőtáv-lefedettséget (jelenleg az 5,657 tile-os "ablak" számítás egyenes útra épül), tehát a meglévő HP/DPS görbét is érintheti
- **Kevesebb építhető csempe** későbbi pályákon — óvatosan kell bevezetni, mert a Level 2/3 nehézségi modell kifejezetten arra épít, hogy a játékos több tornyot tud egymás mellé csoportosítani (lásd "Pálya-közti skálázás")
- **Több belépési pont** (ellenfelek két irányból) — hosszabb távú ötlet, a fentinél is nagyobb átalakítás (több GoalArea, több spawn-pont)
- Egy negyedik, lassítás/kontroll szerepkörű torony típus
