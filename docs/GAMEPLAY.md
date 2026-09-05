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

**Fázis 2 bootstrap — az első ellenség**:

| Mező | Érték |
|---|---|
| HP | 5 |
| Dmg (mennyi életet vesz el, ha célba ér) | 1 |
| Value (mennyi aranyat ad, ha megölik) | 1 |
| Speed (sebesség — **mekkora távolságot tesz meg időegység alatt**, tile/mp) | 1 |
| Sprite | Placeholder (egyszerű geometrikus forma, pl. fekete kör vagy háromszög) |

**Célkép (Fázis 3-ra)**: **3-4 ellenség típus**, a fenti az "Alap" szerepkör első, minimál változata.

| Ellenség | Jellemző | TBD részletek |
|---|---|---|
| Alap | Kiegyensúlyozott HP/sebesség (lásd bootstrap fent) | Fázis 3: véglegesítendő számok |
| ? | Gyors, kevés HP | számok |
| ? | Lassú, sok HP ("tank") | számok |
| ? | Speciális (pl. páncél/resist bizonyos torony ellen, vagy repülő) | típus, számok |

Minden ellenséghez: `HP`, `Dmg`, `Value`, `Speed`.

**Terminológiai megjegyzés**: a "sebesség" szó két különböző mezőt takar attól függően, hogy toronyról vagy ellenségről van szó — toronynál `FireRate` (lövés/másodperc), ellenségnél `Speed` (tile/másodperc). A kódban és az adatmezőkben emiatt tudatosan más néven szerepelnek, hogy ne keveredjenek. Mindkét torony- és ellenség-mérték (`Range`, `Speed`) a rácsos pálya tile-egységére épül — lásd TECHNICAL.md "Pálya rács / koordináta-rendszer".

## Pályák

- **~10 pálya** az MVP-ben, lineáris feloldási sorrend (a [Folytatás] gomb mindig a legutóbb el nem ért pályát indítja)
- A [Pálya választó] bármelyik **már feloldott** pályát engedi újraindítani (nem csak a legutóbbit)
- Minden pálya: saját útvonal-elrendezés (layout) és saját elhelyezhető mezők, de a torony/ellenség *típusok* megegyeznek — a nehézség elsősorban **szám-skálázással** nő
- TBD: lineáris vagy elágazó útvonalak? Egy vagy több útvonal pályánként?
- Pályánként **egy preset** menthető (build elrendezés), felülírásos — a statisztika popup "Preset mentése" gombjával

## Nehézség-skálázás (számítási logika)

Elv (lásd TECHNICAL.md is): egy központi görbe szorozza az alap ellenség-statokat a pálya/hullám sorszáma alapján.

Vázlat-formula (TBD, finomítandó):
```
effectiveHP    = baseHP    × (1 + levelIndex × hpGrowthRate)
effectiveSpeed = baseSpeed × (1 + levelIndex × speedGrowthRate)   ← vagy fix marad, csak HP nő
reward         = baseValue × (1 + levelIndex × rewardGrowthRate)
```
Nyitott kérdés: a skálázás pályaszintű (minden pálya egy fix szorzó) vagy hullámszintű is (pályán belül is nő)? Valószínűleg mindkettő, de az arányokat játszva kell belőni.

## Hullámok (waves)

- Pályánként N diszkrét hullám, hullámonként meghatározott ellenség-mix (típus + darabszám)
- Build szünet minden hullám között, ameddig a játékos akarja (nincs időnyomás — a következő hullám a játékos indítására indul, nem automatikusan)
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
- Boss-ellenségek (kivéve ha egyszerű bevezetni később)
- Multiplayer / bármilyen hálózati funkció
- Menetközbeni (harc alatti) építés/módosítás

## Következő lépés

A rendszer (loop, gazdaság, skill fa szerkezet) le van fektetve. Következő kör: konkrét torony-/ellenség-lista és statok, skill fa node-lista, difficulty curve pontos formulája — mielőtt a tartalom-fázisba (ROADMAP fázis 3) lépünk.
