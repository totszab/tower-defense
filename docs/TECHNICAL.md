# Technikai dokumentáció

Státusz: **élő dokumentum** — a kódbázis fejlődésével frissül. Ez az architekturális "miért", nem a teljes API-referencia.

## Tech stack

| Elem | Választás | Indoklás |
|---|---|---|
| Motor | Godot 4.x (.NET build) | Ingyenes, nyílt forráskódú, jó 2D támogatás, kész Steam integráció (GodotSteam) ha odáig jutunk |
| Nyelv | C# | Meglévő Java (2,5 év) + C# tapasztalat jól átvihető, statikus típusrendszer |
| Verziókövetés | Git + GitHub | — |
| Mentés (kezdetben) | Lokális JSON, `user://` mappa | Lásd "Mentésrendszer" lent |

## Projektstruktúra (Godot projekt: `game/`)

```
game/
├── project.godot
├── TowerDefense.sln / .csproj
├── Scenes/
│   ├── Main/              ← fő menü, pálya-választó
│   ├── Levels/             ← az egyes pályák scene-jei
│   ├── Towers/             ← torony scene-ek (vizuál + hitbox + logika node)
│   ├── Enemies/             ← ellenség scene-ek
│   └── UI/                 ← HUD, menük
├── Scripts/
│   ├── Core/                ← game loop, wave manager, economy
│   ├── Data/                 ← Resource definíciók (lásd lent)
│   ├── Save/                  ← ISaveProvider és implementációk
│   └── UI/
├── Data/
│   ├── Towers/                ← .tres Resource fájlok, egy torony = egy fájl
│   ├── Enemies/
│   ├── Waves/
│   └── SkillTree/              ← skill fa node definíciók (lásd "Skill fa adatmodell")
└── Assets/
    ├── Sprites/
    ├── Audio/
    └── Fonts/
```

**Elv**: kód (`Scripts/`) és tartalom (`Data/`, `Assets/`) élesen elválik. Egy új torony hozzáadása = új `.tres` fájl + sprite, nem új kód.

## Adatvezérelt dizájn (art/tartalom cserélhetőség)

A cél: kezdetben ingyenes assetekkel dolgozunk, később lecserélhető legyen kód-módosítás nélkül.

- Minden torony/ellenség/hullám/skill-node egy Godot **`Resource`** (`.tres` fájl), nem hardcode-olt kód
- Példa: `TowerData : Resource` mezői — `Damage`, `Range`, `FireRate`, `Sprite (Texture2D)`, `ProjectileScene (PackedScene)` — **nincs `Cost` mező**, mert a pályán belüli lerakás ingyenes (lásd GAMEPLAY.md "Gazdaság"); az ár a hozzá tartozó `SkillNodeData`-n van, ami az unlockot fedezi
- A sprite/hang mindig **referencia** a Resource-ban, a kód sosem hivatkozik fájlnévre stringként
- Sprite/hang csere = a `.tres` fájlban átkattintod az új asset referenciát, kód nem változik

## Pálya rács / koordináta-rendszer

- **Rácsos (tile-alapú)** elrendezés — a pálya egy N×M rács, minden mező vagy útvonal, vagy építhető (vagy egyik sem, pl. dekoráció/akadály)
- **Tile méret: 80×80 px logikai render-méret** — a natív asset méret (Kenney.nl, 64×64) 1.25×-ös sprite-skálázással jelenik meg nagyobb, teljes képernyőt jobban kitöltő tile-okként. A `GridConstants.TileSize` (a `Range`/`Speed` egysége) ezt a 80px-es értéket tükrözi, nem a natív asset méretet.
- A `Range` (torony hatótávolság) és a `Speed` (ellenség sebesség) **tile-egységben** értendő, nem raw pixelben — pl. `Range = 3` azt jelenti, 3 tile sugarú körben lát célt a torony. Ez balance-oláskor is átláthatóbb, mint a pixelszám.
- Pálya-méret (N×M) pályánként eltérhet, nincs egységes rögzített méret — ez a level layout kérdése (GAMEPLAY.md "Pályák")

## Skill fa adatmodell

**Jelenlegi (bootstrap) implementáció** — egyszerűbb, mint az eredetileg tervezett `SkillNodeData` Resource-gráf, mert egyelőre csak 5, fixen 0-5 szintig fejleszthető node létezik:

- `PlayerProgress.SkillLevels : Dictionary<string, int>` — node id (`"dmg"`, `"hp"`, `"towers"`, `"currency"`, `"enemy"`) → jelenlegi szint. Ez perzisztálódik a `LocalFileSaveProvider`-en keresztül.
- A node-ok id-ja, ára (szintenkénti tömb), hatása és pozíciója **kódba égetve** a `MainMenu.cs`-ben (`CostsFor`, `NodePositions`), nem külön Resource-fájlokban.
- A hatásokat a fogyasztó kód (`Tower.cs`, `LevelBuild.cs`) közvetlenül olvassa: mindegyik saját `_Ready()`-jében betölti a `PlayerProgress`-t és kiszámolja a rá vonatkozó bónuszt (`GetSkillLevel("dmg")` stb.) — nincs központi `SkillTreeManager` autoload még.

**Eredeti terv (később, ha a fa bővül túl ezen az 5 node-on)**: `SkillNodeData : Resource` mezőkkel (`Id`, `Cost`, `Prerequisites`, `EffectType` enum, `EffectValue`, `TargetTowerId`), adatvezérelt gráf-struktúra, és egy `SkillTreeManager` autoload, ami egy helyen number olvassa a `PlayerProgress`-t ahelyett, hogy minden fogyasztó külön töltené be a mentést. Erre akkor érdemes átállni, amikor a node-szám és a torony-specifikus upgrade-ek (GAMEPLAY.md "Skill fa") megjelennek — a jelenlegi kódba-égetett megoldás ennyi node-ra még átlátható, de nem skálázódik jól.

- **Baseline (skill fától független) hiány**: az eredeti terv szerint kellene egy `BaseTowerSlots`/`BaseUnlockedTowerIds` minimum a skill fától függetlenül (hogy egy friss játékos ne szoruljon be) — ez a bootstrap implementációban **még nincs bekötve** (lásd GAMEPLAY.md "Skill fa" ismert probléma).

## Kommunikáció / komponensek közötti kapcsolat

Godot-ban a natív mintát követjük, nem építünk saját event bus-t rá feleslegesen:

- **Signal-ok** (Godot beépített observer-mintája) a laza csatolású kommunikációhoz — pl. `Tower` kilövi a `EnemyKilled` signalt, a `EconomyManager` és a `WaveManager` külön-külön feliratkozik rá, nem ismerik egymást
- **Autoload / Singleton** csak a valóban globális állapotra: `RunState` (aktuális futás állapota: gyűjtött arany, élet, hullám száma, tornyonkénti sebzés-számláló), `SkillTreeManager` (unlock állapotok, elkölthető meta-arany), `SaveManager`
- `RunState` a build fázistól a statisztika popup-ig él, onnantól "realizálódik" — a benne lévő gyűjtött arany a `SkillTreeManager` egyenlegébe megy, a `RunState` pedig resetelődik a következő pályaindításkor
- Szülő-gyerek node kommunikáció közvetlen referenciával (Godot konvenció), csak amikor tényleg szülő-gyerek viszony van (pl. torony → saját lövedék)
- **Nem** használunk C# eventeket Godot signalok helyett a motor-szintű dolgokra — a Godot editor csak a signalokat látja/debug-olja jól

## Mentésrendszer

- Interfész mögé rejtve: `ISaveProvider` (`Save(SaveData)`, `Load() : SaveData`, `HasSave() : bool`)
- **v1 implementáció**: `LocalFileSaveProvider` — JSON szerializáció a `user://save.json`-ba (Godot userdata mappa, OS-független útvonal)
- **Later (opcionális)**: `SteamCloudSaveProvider` — ugyanaz az interfész, csak Steamworks API hívás mögötte. A hívó kód (`GameState`, menük) nem tud/nem érdekli melyik implementáció fut.
- `PlayerProgress` (a mentett `SaveData` gyökér-objektuma) — **implementált mezők**:
  - `MetaCurrency` (int) — elkölthető skill-fa arany egyenleg
  - `SkillLevels` (Dictionary<string, int>) — node id → szint (0-5), lásd "Skill fa adatmodell"
  - **Még nem implementált** (eredeti terv, ROADMAP Fázis 4-hez kötve):
    - `HighestUnlockedLevelIndex` (int) — meddig jutott a játékos lineárisan ([Folytatás] gomb, [Pálya választó])
    - `LevelPresets` (Dictionary<levelId, PresetData>) — pályánként egy mentett torony-elrendezés
    - Globális beállítások (hangerő stb.)
- Amit **szándékosan NEM** mentünk state-ként: legjobb eredmény/statisztika pályánként — a statisztika popup egy adott futás lezárása, nem perzisztens ranglista (nincs ilyen elvárás egyelőre)

## Nehézség-skálázás

- Nincs külön "nehéz mód" tartalom pályánként — egy központi szorzó-görbe (`DifficultyCurve`) a hullám/pálya sorszámából számol HP/sebesség/jutalom szorzót
- Ez azt jelenti: az `EnemyData` alap-statokat tárol, a ténylegesen pályán megjelenő ellenség stat = alap-stat × görbe(pálya, hullám)
- Pontos formula: **TBD**, finomítjuk amikor a játékmenet dokumentumban leszünk a konkrét számoknál

## Kódolási konvenciók

- C# standard konvenciók (PascalCase publikus tag, camelCase privát mező `_prefix`-szel, ahogy Godot C# style guide ajánlja)
- Egy scene = egy felelősség (pl. `Tower.tscn` + `Tower.cs` csak a torony saját viselkedését kezeli, nem a wave logikát)
- Namespace-ek a mappastruktúrát tükrözik (`TowerDefense.Core`, `TowerDefense.Data`, `TowerDefense.Save`, ...)

## Nyitott kérdések / TBD

- Pontos Resource mezők tornyonként/ellenségenként (kötve a GAMEPLAY.md finomításához)
- Difficulty curve pontos formulája
- Skill fa pontos node-lista, árak, fa-alak
- "Eszköz/képesség" `EffectType` pontos viselkedése (harc közbeni aktiválás mechanikája) — GAMEPLAY.md-ben is TBD
- Build/CI (Steam feltöltéskor releváns lesz, most nem blocker)
