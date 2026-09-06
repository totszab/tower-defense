# Roadmap

Státusz: élő dokumentum, a fázisok/prioritások finomodhatnak útközben.

## Elsődleges cél

Egy befejezett, ~10 pályás tower defense demo → portfólió-darabnak mindenképp jó, és ha idáig eljutunk energiával/idővel, Steamre kiadható.

## Fázis 0 — Előkészítés

- [x] Rendszerterv, tech stack döntések
- [x] Mappastruktúra, dokumentáció váza
- [x] Helyi eszközök telepítve (Godot .NET, .NET SDK, Visual Studio Community)
- [x] GitHub repó létrehozva, első commit
- [x] Játékmenet dokumentum konkrét számokkal feltöltve (torony/ellenség lista, formulák)

## Fázis 1 — Godot/C# alapok

- [x] Godot projekt inicializálva, C# build működik
- [x] Projektstruktúra létrehozva (Scenes/Scripts/Data/Assets mappák)
- [x] Első ingyenes asset pack becsatlakoztatva (Kenney Tower Defense Top-Down)

## Fázis 2 — Core loop (teljes vázlat körbe-körbe) — kész

Cél: a **teljes** navigációs kör végigjátszható — build fázis → hullám → statisztika popup → skill fa → vissza. Ez a legkockázatosabb architekturális rész volt (több rendszer találkozása), ezért jött korábban, mint a tartalombővítés.

- [x] Útvonal mentén mozgó ellenség
- [x] Torony: célzás, tüzelés (lövedékkel), sebzés (slot-limit betartva a build fázisban)
- [x] Wave manager: `WaveData`/`SpawnStepData` adatvezérelt hullám-rendszer, build szünet köröket között
- [x] Helyi futás-állapot (`LevelBuild` mezői): gyűjtött arany, élet (piros bar), tornyonkénti sebzés-számláló (`DamageTracker`); győzelem/vereség/visszavonulás szimmetrikus lezárás — **nincs még külön `RunState` autoload**, ez egy fogyasztóban (LevelBuild) él
- [x] `TowerData`/`EnemyData`/`WaveData`/`SpawnStepData` Resource-ok bevezetve — a skill fa node-jai **kódba égetve** vannak (`MainMenu.cs`), nem `SkillNodeData` gráf (lásd TECHNICAL.md "Skill fa adatmodell" indoklás)
- [x] Teljes 5-node skill fa (hub + 4 irány), nem csak placeholder
- [x] Statisztika popup: siker/vereség/visszavonulás cím, gyűjtött arany, tornyonkénti total dmg + dmg/sec, preset mentés (pályánként egy elrendezés, automatikusan alkalmazva a pálya bármelyik körének indításakor)

## Fázis 3 — Tartalombővítés (folyamatban)

- [ ] 3-4 torony típus a GAMEPLAY.md szerint véglegesítve (jelenleg 1 van)
- [x] Level 1, mind a 10 köre kész tartalommal (Green → Blue → Purple Slime, mini boss az 5., final boss a 10. körben), lásd GAMEPLAY.md "Pályák és körök" és "Ellenségek" (a HP-görbe indoklásával)
- [x] Kör-választó UI a Főmenüben (Play gomb → popup, mind a 10 kör, feloldottság szerint)
- [ ] 2. pálya (ha idáig eljutunk energiával — jelenleg csak Level 1 létezik)

## Fázis 4 — UI/UX és mentés

- [ ] Főmenü (skill fa nézet), pálya-választó (lock/unlock állapot), [Folytatás] gomb
- [ ] HUD, torony-paletta (unlockolt típusok + hátralévő slot), statisztika popup polish
- [ ] `ISaveProvider` + `LocalFileSaveProvider` implementálva, `PlayerProgress` (meta-arany, unlockok, pálya-előrehaladás, presetek) perzisztál
- [ ] Alap polish: hangok, egyszerű VFX/feedback találatra és halálra

## Fázis 5 — Demo lezárás (portfólió-kész állapot)

- [ ] Végigjátszható 10 pálya hibák nélkül
- [ ] Windows build tesztelve tiszta gépen/más gépen
- [ ] Rövid README/bemutató (GIF vagy videó) a GitHub repóhoz

## Fázis 6 — Opcionális: Steam kiadás

Csak ha idáig eljutunk energiával.

- [ ] Steamworks fiók + Steam Direct díj ($100) rendezve
- [ ] GodotSteam vagy hasonló integráció bekötve
- [ ] `SteamCloudSaveProvider` implementálva (a meglévő `ISaveProvider` interfész mögé)
- [ ] Store page, achievement-ek (opcionális), IARC content rating
- [ ] SteamPipe build feltöltés, playtest/review folyamat

## Prioritási elv

Ha bármikor időhiány van: **funkció-teljesség helyett pálya-teljesség** — inkább kevesebb torony/ellenség típussal, de mind a 10 pálya végigjátszható és a mentés működik, mint sok félkész mechanika.
