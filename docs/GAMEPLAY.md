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

**Vegyes fa**: egy központi hub node + 4 irányba induló ág, mindegyik vonallal összekötve a hub-bal (vizuálisan is). Minden node 0-5 szintig fejleszthető, minden szintlépésnek ára van (meta-arany). **A node feliratok angolul jelennek meg a UI-ban** (Tower Number, Health, Attack Speed, Gold, Damage), a dokumentáció itt magyarul hivatkozik rájuk.

| Node (UI felirat) | id | Irány | Hatás / szint | Ár (1→2, 2→3, 3→4, 4→5) |
|---|---|---|---|---|
| Tower Number | `towers` | Hub (közép) | Overall torony-slot szám; **1. szinten indul alapból** (baseline) | 100 / 250 / 500 / 750 |
| Health | `hp` | Fel | +2 kezdő élet | 5 / 10 / 20 / 35 / 50 |
| Attack Speed | `fireRate` | Le | Az (egyelőre egyetlen) toronytípus tűzgyorsasága +5%/szint. Ez az ág a torony-specifikus upgrade-ek kezdete — később minden toronytípusnak lehet saját ilyen ága. | 5 / 10 / 20 / 35 / 50 (**TBD, placeholder** — nincs végleges ár megadva) |
| Gold | `currency` | Jobb | +10% szerzett arany minden megölt ellenségért (szorzó, nem fix bónusz) | 50 / 150 / 300 / 500 / 1000 |
| Damage | `dmg` | Bal | +1 sebzés MINDEN toronynak, globálisan | 5 / 10 / 20 / 35 / 50 |

**Baseline megoldva**: a `towers` node alapból (friss mentésnél is) legalább 1. szinten van, tehát mindig lerakható az első torony — a korábbi verzióban felmerült "friss játékos beszorul" probléma ezzel elhárult. Az árlista emiatt csak 4 lépést tartalmaz (1→5), nincs "0→1" ár.

**Node megjelenítés**: hover nélkül a node a JELENLEGI kumulált hatást mutatja + szintet (pl. `+3 damage, 3/5`); hover-re (natív Godot tooltip) az egy szintnyi (marginális) hatás jelenik meg + ár vagy "MAX LEVEL" (pl. `+1 damage / Cost: 50 gold`).

**Eszköz/képesség ág**: egyelőre nincs a fenti 5 node között. A `fireRate` node jelzi az irányt (torony-specifikus ág), de a teljes "eszköz/képesség" kategória (GAMEPLAY.md korábbi tervei) még nyitott.

**Fontos, rögzített elv**: a globális stat ág (élet, sebzés, torony slot stb.) kiemelt prioritás a skill fa tervezésénél — ez az az ág, ami minden futásra érezhető hatással van, nem csak egy-egy toronyra.

**Induló állapot (Fázis 2 bootstrap, "üres" skill fa)**: a játékos 0 meta-arannyal indul, semmi nincs megvásárolva a fán. A skill fától **függetlenül**, alapból (baseline, nem unlock-kötött) rendelkezésre áll:
- 1 torony típus (lásd lent)
- 1 torony slot (egyszerre 1 lerakott torony engedélyezett)

A skill fa node-jai *ezen a baseline-on felül* adnak további torony típusokat és slot-okat — tehát a fa nem "0-ról épít fel mindent", hanem a minimális játszható állapotot bővíti.

## Tornyok

**Fázis 2 bootstrap — az első torony** (ezzel lesz először tesztelhető a teljes kör):

| Mező | Érték |
|---|---|
| Damage | 1 |
| FireRate (sebesség — **hányszor lő másodpercenként**) | 1 |
| Range | 3 (tile) — lásd TECHNICAL.md "Pálya rács" a tile-alapú egységről |
| Sprite | Placeholder (egyszerű geometrikus forma, pl. négyzet) |

**Célkép (Fázis 3-ra)**: **3-4 torony típus**, mindegyik egyértelműen más szerepkörrel — a fenti az "Alap lövő" szerepkör első, minimál változata.

| Torony | Szerepkör | TBD részletek |
|---|---|---|
| Alap lövő | Kiegyensúlyozott damage/range/rate (lásd bootstrap fent) | Fázis 3: véglegesítendő számok |
| ? | Terület sebzés (AoE) | típus, számok |
| ? | Lassítás/kontroll | típus, számok |
| ? | Nagy sebzés, lassú tűzgyorsaság (sniper jellegű) | típus, számok |

Minden toronyhoz (lásd TECHNICAL.md "Adatvezérelt dizájn"): `Damage`, `Range`, `FireRate`. Nincs `Cost` mező, mert a lerakás ingyenes — helyette a skill fában van ár az *unlockhoz* és az *upgrade-ekhez*.

## Ellenségek

**Nehézségi alapszabály (miért ezek a HP-számok)**: egy torony `_enemiesInRange`-ből mindig csak a legrégebb óta bent lévőt (a legelöl járót) lövi — a többi csak vár. Emiatt egy torony csak akkor tud **veszteség nélkül** végigvinni egy folyamatos ellenség-áradatot, ha `EnemyHP / TowerDPS ≤ SpawnInterval` — különben a hátrébb sorban állók csak részleges sebzést kapnak, mielőtt kilépnek a lőtávolból, és a maradék HP-jüktől függetlenül **teljes `Dmg`-et** visznek el becsapódáskor. `SpawnInterval` egységesen 2 mp, az alap torony DPS-e 1 (`Damage 1 × FireRate 1`) — innen jön a HP-skálázás:

| Ellenség | HP | Dmg | Value | Speed | Megjegyzés |
|---|---|---|---|---|---|
| Green Slime | 2 | 1 | 1 | 1 tile/mp | Alapból (dmg skill nélkül) veszteség nélkül ölhető — 1-2. kör |
| Blue Slime | 4 | 1 | 2 | 1 tile/mp | 1 `dmg` skill-szint (5 arany, DPS 2) kell a tiszta öléshez — 3-6. kör |
| Purple Slime | 6 | 2 | 3 | 1 tile/mp | 2 `dmg` skill-szint (15 arany, DPS 3) kell hozzá — 6-10. kör |
| Blue Slime (Mini Boss) | 30 | 5 | 5 | 1 tile/mp | Önálló egység (nem áradat), a lőtávban töltött ~5,7 mp alatt kell megölni — 5. kör záró ellenfele |
| Purple Slime (Final Boss) | 150 | 10 | 20 | 1 tile/mp | Kb. 27 összesített DPS kell hozzá (több torony egymást átfedő lőtávval, felturbózott dmg/tűzgyorsasággal) — 10. kör záró ellenfele |
| Green/Blue/Purple Triangle | 2 / 4 / 6 | 1 / 1 / 2 | 1 / 2 / 3 | **2** tile/mp | Ugyanaz a HP/Dmg/Value mint a hasonló színű Slime-nál, csak dupla sebesség — egyelőre egyik körbe sincs betéve, tartalék variáns jövőbeli körökhöz |

Minden ellenséghez: `HP`, `Dmg`, `Value`, `Speed`, `DisplayName`.

**Vizuális variánsok placeholder-tier módon**: nincs egyedi art minden típushoz — a kör alakú (Slime) típusoknál ugyanazt a sprite-ot **színezzük** (`Tint`, Godot `Modulate`) és **skálázzuk** (`SpriteScale`, a hitbox-szal együtt `HitRadius`); a háromszög típusoknál (`EnemyData.Shape = Triangle`) nincs is sprite, kód-rajzolt alakzat helyettesíti (`Enemy._Draw()` a pályán, `TriangleIcon` a menükben). A mini/final boss ugyanígy csak egy nagyobbra skálázott, erősebb statú variáns — amikor lesz saját art, csak a `Sprite`/`Tint` mezőt kell cserélni, a rendszer nem változik.

**Célkép (Fázis 3+)**: Level 1 mind a 10 köre kész (lásd "Pályák és körök" lent); hosszabb távon minden ellenségnek lehet saját sprite-ja a jelenlegi tint/scale/kód-rajzolt trükk helyett, és a Triangle variánsok is bekerülhetnek konkrét körökbe (gyorsabb, kevesebb HP-s "raider" hullámként).

**Terminológiai megjegyzés**: a "sebesség" szó két különböző mezőt takar attól függően, hogy toronyról vagy ellenségről van szó — toronynál `FireRate` (lövés/másodperc), ellenségnél `Speed` (tile/másodperc). A kódban és az adatmezőkben emiatt tudatosan más néven szerepelnek, hogy ne keveredjenek. Mindkét torony- és ellenség-mérték (`Range`, `Speed`) a rácsos pálya tile-egységére épül — lásd TECHNICAL.md "Pálya rács / koordináta-rendszer".

## Pályák és körök

**Egy pálya = 10 kör.** Ez a korábbi "pálya = egy hullám-sorozat" elképzelés pontosítása: amit eddig "pályaként" teszteltünk (a `Level01Test` scene), az valójában **Level 1, 1. kör**.

- Minden pályának **10 köre** van, lineáris feloldási sorrenddel — egy kör sikeres teljesítése (győzelem, nem vereség/visszavonulás) feloldja a pálya következő körét
- **Az 5. és a 10. kör végén boss van**: az 5. kör végén egy **mini boss**, a 10. kör végén egy **final boss** zárja a hullámot
- A `PlayerProgress.HighestUnlockedRound` tárolja meddig jutott a játékos — ez egyelőre **egy-pályás egyszerűsítés** (nincs "melyik pálya" dimenzió, mert csak Level 1 létezik); ha 2. pálya is készül, ez pálya-kulcsos map-re bővül (lásd TECHNICAL.md)
- A Főmenüben egy kör-választó sáv (`R1`..`R5` gombok, feloldottság szerint engedélyezve) indítja a választott kört

**Level 1 tartalmi állapota**:

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

A rendszer (loop, gazdaság, skill fa szerkezet, hullám-modell) le van fektetve, Level 1 mind a 10 köre kész tartalommal (lásd "Pályák és körök" és "Ellenségek" fent). Következő kör: a nehézségi görbe éles teszttel való finomhangolása, és hosszabb távon a torony-választék bővítése (jelenleg még csak 1 torony típus van, a GAMEPLAY.md "Tornyok" szekció 3-4 típust vázol fel).
