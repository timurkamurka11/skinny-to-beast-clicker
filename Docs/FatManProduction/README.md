# Fat Man Production Rig 3.7 — documentation index

Основные документы:

1. `PRODUCTION_RIG_ASSET_CONTRACT.md` — полный контракт на арт, PSB, skeleton, SpriteSkin, weights, Animator, импорт, производительность, QA и Definition of Done.
2. `ART_LAYER_MATRIX.md` — точная матрица художественных слоёв, pivots, overlaps, Stage/view compatibility и checklist исходника.
3. `ANIMATION_CLIP_MATRIX.md` — точный список клипов, длительности, параметры Animator, blend settings и критерии приёмки.

Runtime-путь финального prefab:

```text
Assets/Resources/Characters/FatManProduction/FatManProductionRig.prefab
```

Проверка в Unity:

```text
Tools > Skinny to Beast > Validate Production Fat Man Rig 3.7
```

Без настоящего authored PSB/SpriteSkin prefab проект обязан использовать только цельный безопасный fallback и не показывать generated cut-outs Patch 3.6.

Порядок производства:

```text
Concept Lock
→ Layered PSB Stage 1
→ Skeleton / Geometry / Manual Weights
→ Idle / Blink / Walk / Tap
→ Entry + Room Integration
→ Secondary Motion + QA
→ Stage 2–4
```

Главный принцип: сначала полностью принимается Stage 1. Массовое производство Stage 2–4 запрещено до прохождения deformation QA, entry test, room test и Android profiling для Stage 1.
