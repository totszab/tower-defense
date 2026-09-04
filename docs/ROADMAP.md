# Roadmap

Státusz: élő dokumentum, a fázisok/prioritások finomodhatnak útközben.

## Elsődleges cél

Egy befejezett, ~10 pályás tower defense demo → portfólió-darabnak mindenképp jó, és ha idáig eljutunk energiával/idővel, Steamre kiadható.

## Fázis 0 — Előkészítés (jelenleg itt tartunk)

- [x] Rendszerterv, tech stack döntések
- [x] Mappastruktúra, dokumentáció váza
- [ ] Helyi eszközök telepítve (Godot .NET, .NET SDK, Visual Studio Community)
- [ ] GitHub repó létrehozva, első commit
- [ ] Játékmenet dokumentum konkrét számokkal feltöltve (torony/ellenség lista, formulák)

## Fázis 1 — Godot/C# alapok

Cél: működő Godot + C# projekt, alap node/scene mozgás, semmi végleges tartalom.

- [ ] Godot projekt inicializálva, C# build működik (üres jelenet, egy mozgó node)
- [ ] Projektstruktúra létrehozva (Scenes/Scripts/Data/Assets mappák)
- [ ] Első ingyenes asset pack becsatlakoztatva teszt gyanánt

## Fázis 2 — Core loop (1 pálya, minimál tartalom, teljes vázlat körbe-körbe)

Cél: a **teljes** navigációs kör végigjátszható placeholder tartalommal — build fázis → hullám → statisztika popup → skill fa → vissza. Ez korábban jön be, mint a tartalombővítés, mert ez a legkockázatosabb architekturális rész (több rendszer találkozása).

- [ ] Útvonal mentén mozgó ellenség
- [ ] Torony: célzás, tüzelés, sebzés (slot-limit betartva a build fázisban)
- [ ] Wave manager: diszkrét hullámok, build szünet köztük
- [ ] `RunState`: gyűjtött arany, élet, tornyonkénti sebzés-számláló; győzelem/vereség szimmetrikus lezárás
- [ ] `TowerData`/`EnemyData`/`SkillNodeData` Resource-ok bevezetve (adatvezérelt dizájn már itt, nem utólag)
- [ ] Minimál Skill fa (1-2 placeholder node: pl. 1 torony unlock, 1 slot-limit növelés) — hogy a teljes kör tesztelhető legyen, nem a végleges tartalom
- [ ] Statisztika popup: preset mentés/betöltés, [Újra]/[Befejezés] működik

## Fázis 3 — Tartalombővítés

- [ ] 3-4 torony típus a GAMEPLAY.md szerint véglegesítve
- [ ] 3-4 ellenség típus
- [ ] Nehézség-skálázó görbe implementálva
- [ ] 10 pálya layout elkészítve
- [ ] Teljes skill fa node-lista (globális + torony-specifikus ágak)

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
