# 🚗 Endless Runner — Новая Архитектура
## Для Яндекс.Игры (WebGL)

---

## 📁 Структура скриптов

```
Scripts/
├── Core/
│   ├── GameEventBus.cs          ← Центральная шина событий
│   ├── DifficultyManager.cs     ← Скорость + уровень сложности
│   ├── DistanceTracker.cs       ← UI дистанции и скорости
│   └── YandexLeaderboard.cs     ← Рекорды Яндекс Games
│
├── Road/
│   ├── Road.cs                  ← Компонент сегмента дороги
│   ├── RoadPool.cs              ← Пул объектов (O(1) Queue)
│   ├── RoadManager.cs           ← Главный спавнер
│   └── PlatformMovement.cs      ← Движение платформ
│
├── Biome/
│   ├── BiomeZone.cs             ← Данные зоны (структура)
│   ├── BiomeController.cs       ← Логика выбора биомов
│   ├── BiomeVisualTransition.cs ← Переход неба/тумана
│   ├── RoadBiomeData.cs         ← ScriptableObject биома
│   └── RoadBiomeSettings.cs     ← Глобальные настройки
│
└── Obstacles/
    ├── ObstacleSetData.cs       ← ScriptableObject набора препятствий
    └── ObstacleSpawner.cs       ← Процедурный спавн препятствий
```

---

## 🧩 Схема событий (GameEventBus)

```
RoadManager  ──► OnRoadSpawned    ──► ObstacleSpawner
             ──► OnRoadPassed     ──► DistanceTracker
             ──► OnDistanceChanged──► DifficultyManager
             ──► OnBiomeChanged   ──► BiomeVisualTransition

DifficultyManager ──► OnSpeedChanged ──► DistanceTracker (speedLabel)

GameManager  ──► OnGameOver  ──► YandexLeaderboard, PlatformMovement
             ──► OnGameRestart──► все системы
```

---

## ⚙️ Настройка в Unity

### 1. Сцена — добавь объекты:
- `[GameSystems]` — DifficultyManager, DistanceTracker, YandexLeaderboard
- `[RoadManager]` — RoadManager, ObstacleSpawner, BiomeVisualTransition
- `[PlatformParent]` — PlatformMovement (объект который двигается)

### 2. ScriptableObjects (Create → Runner):
- **Road Biome Data** — для каждого биома (лес, шоссе, город и т.д.)
- **Biome Settings** — глобальные настройки
- **Obstacle Set** — для каждого набора препятствий

### 3. RoadBiomeData — заполни поля:
```
biomeIndex      = 0, 1, 2...  (уникальный)
biomeName       = "Лес"
roadPrefab      = [Prefab с компонентом Road]
skyColorTop     = синий/оранжевый/...
skyColorHorizon = цвет горизонта
fogColor        = цвет тумана
fogDensityStart = дистанция тумана (80-200)
spawnWeight     = 0.5 (вероятность)
obstacleSets    = [массив наборов препятствий]
```

### 4. BiomeVisualTransition:
- Создай материал Skybox с шейдером с полями `_TopColor` и `_BottomColor`
- Подключи его в RenderSettings → Skybox Material
- Перетащи этот же материал в поле `skyboxMaterial` компонента

### 5. Яндекс Games SDK:
```
Package Manager → Add from URL:
https://github.com/forcepusher/com.agava.yandexgames.git
```
После установки раскомментируй строки в `YandexLeaderboard.cs`

---

## 🔄 Ключевые улучшения vs старый код

| Было | Стало |
|------|-------|
| `FindObjectOfType` везде | `GameEventBus` — нет прямых ссылок |
| `List.RemoveAt(0)` — O(n) | `LinkedList.RemoveFirst()` — O(1) |
| Скорость в PlatformMovement | `DifficultyManager` — центральный источник |
| Резкая смена биомов | `BiomeVisualTransition` — плавный lerp |
| Нет препятствий | `ObstacleSpawner` — по сложности и биому |
| Нет Яндекс SDK | `YandexLeaderboard` — готов к подключению |
| `FixedUpdate` для движения | `Update` — WebGL-совместимо |

---

## 📝 Что ещё можно добавить

- `PlayerController.cs` — управление (свайп/клавиши/тач)
- `GameManager.cs` — FSM игры (Menu/Playing/GameOver)
- `UIManager.cs` — экраны меню, смерти, рекордов
- `AudioManager.cs` — звук через AudioSource pool
- `SaveSystem.cs` — сохранение через PlayerPrefs + JSON
