# Generated Fat Man Bone Rig 3.8

## Цель

Patch 3.8 создаёт нового оригинального толстого 2D-персонажа без runtime-нарезки PNG и без использования старого процедурного манекена как deformation rig.

Персонаж визуально основан на утверждённом направлении:

- взрослый полный мужчина;
- растрёпанные тёмно-коричневые волосы;
- красная полосатая майка;
- тёмно-серые шорты;
- синие домашние туфли;
- тяжёлая расслабленная пластика;
- отдельные Front, Side и Back.

## Техническая архитектура

`GeneratedFatManRigActor` создаёт три независимых view-rig:

- `Front.View`;
- `Side.View`;
- `Back.View`.

У каждого view собственные bones:

- `RigRoot`;
- `Pelvis`;
- `Spine.01`;
- `Chest`;
- `Neck`;
- `Head`;
- `ChinSoft`;
- `Belly`;
- `ShirtHem`;
- обе ключицы;
- плечи, предплечья и кисти;
- бёдра, голени и стопы.

Все видимые детали создаются как `SkinnedMeshRenderer`. Для живота, майки, таза и груди используются multi-bone weights. Конечности и детали также являются skinned surfaces, но могут иметь один основной bone influence.

Старые `CharacterMeshGraphic` и `CharacterSpriteRigController` остаются невидимыми. Старый `CharacterRigController` используется только как источник gameplay-состояния:

- facing;
- stage;
- walking;
- tap;
- routine action.

Его transforms не входят в `SkinnedMeshRenderer.bones` нового персонажа.

## Реализованные движения

- idle breathing;
- pelvis sway;
- delayed belly motion;
- delayed shirt hem motion;
- chin secondary motion;
- Front/Side/Back walk cycles;
- randomized blink;
- three tap reactions;
- shift weight;
- look around;
- scratch;
- yawn;
- stretch;
- flex;
- adjust clothes;
- warm shoulders;
- sit down;
- sit loop;
- stand up;
- Stage 1–4 body scaling.

## Рендеринг

Новый actor находится в изолированном world-space слое и снимается прозрачной orthographic camera в `RenderTexture`. В существующий uGUI layout передаётся только `RawImage`.

Это позволяет использовать настоящий `SkinnedMeshRenderer`, не перестраивая существующую UI-комнату.

## Защита от регрессий

`CharacterVisibilityGate`:

1. сначала проверяет `GeneratedFatManRigHost`;
2. калибрует его экранный размер;
3. никогда не оставляет комнату чёрной навсегда;
4. через 2.75 секунды выполняет диагностический fail-open, если render validation не завершилась.

## Проверка

В Unity:

`Tools > Skinny to Beast > Validate Generated Fat Man Bone Rig 3.8`

Ожидается:

- 3 view rigs;
- не менее 45 независимых bones;
- не менее 45 `SkinnedMeshRenderer`;
- у каждого renderer есть mesh, bones и bone weights;
- 0 `CharacterMeshGraphic` внутри нового actor.

Затем в Play Mode проверить:

1. экран входа;
2. Front/Side/Back;
3. idle и blink;
4. walking;
5. 100 быстрых tap;
6. все routine actions;
7. Stage 1–4;
8. отсутствие белых обрезков;
9. отсутствие старого красного робота;
10. отсутствие вечного чёрного экрана.

## Честная граница первой версии

Это настоящий независимый bone rig, но его art создаётся процедурными цветными mesh-поверхностями, а не вручную нарисованным PSB. Поэтому он решает архитектурную проблему и даёт костную анимацию без PNG-обрезков, однако финальная художественная полировка всё равно потребует корректировки по Play Mode-видео:

- пропорции;
- амплитуды;
- локти и колени;
- facial placement;
- sorting;
- timing;
- mobile readability.
