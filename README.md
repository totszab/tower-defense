# TowerDefense

2D tower defense játék Godot 4.x + C# alatt. Ideiglenes munkanév — végleges név/branding később.

## Cél

Elsődleges cél: egy befejezett, kb. 10 pályás tower defense demo, ami önmagában portfólió-darabként megállja a helyét, és ha az idő/lelkesedés engedi, Steamen is kiadható.

## Dokumentáció

- [docs/TECHNICAL.md](docs/TECHNICAL.md) — architektúra, kódszervezés, kommunikáció, mentésrendszer
- [docs/GAMEPLAY.md](docs/GAMEPLAY.md) — játékmenet: tornyok, ellenségek, hullámok, számítások
- [docs/ROADMAP.md](docs/ROADMAP.md) — fázisok, prioritások

## Helyi fejlesztői környezet

Szükséges eszközök (mind ingyenes):

1. **Godot 4.x — .NET/Mono build** ⚠️ nem a sima (GDScript-only) build kell, a letöltési oldalon külön van jelölve a ".NET" verzió — https://godotengine.org/download
2. **.NET SDK 8.0** — https://dotnet.microsoft.com/download (Godot ezt hívja a C# fordításhoz)
3. **Visual Studio Community 2022** — C# szerkesztéshez és debughoz, "Game development with Godot" vagy legalább a ".NET desktop development" workload legyen bepipálva telepítéskor
4. **Git** — verziókövetéshez

## Projektstruktúra

```
TowerDefense/
├── docs/                  ← tervek, dokumentáció
├── game/                  ← Godot projekt gyökere (ezt nyitja meg a Godot és a Visual Studio .sln-je is itt lesz)
└── README.md
```

## Státusz

🚧 Tervezési fázis — még nincs kód. Lásd [ROADMAP.md](docs/ROADMAP.md).
