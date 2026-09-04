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

## Skill fa adatmodell

- `SkillNodeData : Resource` — mezők: `Id`, `Cost` (meta-arany), `Prerequisites (SkillNodeData[])`, `EffectType` (enum: `GlobalStat`, `TowerUnlock`, `TowerUpgrade`, `Ability`), `EffectValue`, `TargetTowerId` (ha torony-specifikus)
- A fa maga adatból épül fel (a `Prerequisites` referenciák alkotják a gráfot), nincs kódba égetett fa-struktúra
- `SkillTreeManager` (autoload): felelős a node-ok unlock állapotáért, az elkölthető egyenlegért, és azért, hogy a build fázisban mely torony-típusok/hány slot érhető el — ez olvassa a `PlayerProgress`-t (lásd Mentésrendszer)

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
- `PlayerProgress` (a mentett `SaveData` gyökér-objektuma) tartalma:
  - `MetaCurrency` (int) — elkölthető skill-fa arany egyenleg
  - `UnlockedSkillNodeIds` (string lista) — mely skill-node-ok vannak unlockolva
  - `HighestUnlockedLevelIndex` (int) — meddig jutott a játékos lineárisan (ez határozza meg mit indít a [Folytatás] gomb, és mely pályák érhetők el a [Pálya választó]-ban)
  - `LevelPresets` (Dictionary<levelId, PresetData>) — pályánként **egy** mentett elrendezés (`PresetData`: torony-típus + pozíció lista), felülírásos mentéssel
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
