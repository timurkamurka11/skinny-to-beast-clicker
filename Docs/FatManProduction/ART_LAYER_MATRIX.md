# Production Fat Man — Art Layer Matrix 3.7

Использовать как checklist при рисовании PSB и при приёмке импорта.

## Naming rules

- только латиница, цифры и точки;
- регистр важен;
- никаких `Layer 1`, `copy`, `final2`;
- каждое имя уникально во всём PSB;
- suffix вида: `.Front`, `.Side`, `.Back`;
- stage хранится в Sprite Library labels, а не обязательно в имени GameObject.

Пример:

```text
Body.Belly.Front
Body.Belly.Side
Body.Belly.Back
Face.Mouth.Yawn.Front
```

## Матрица обязательных частей

| Категория | Front | Side | Back | SpriteSkin | Swap states | Главная кость |
|---|---:|---:|---:|---:|---|---|
| Body.Pelvis | ✓ | ✓ | ✓ | ✓ | Stage01–04 | Pelvis |
| Body.Belly | ✓ | ✓ | ✓ | ✓ | Stage01–04 | Belly.Root/L/C/R |
| Body.Torso | ✓ | ✓ | ✓ | ✓ | Stage01–04 | Spine.01/02/Chest |
| Body.ChestSoft | ✓ | ✓ | ✓ | ✓ | Stage01–04 | Chest |
| Body.Neck | ✓ | ✓ | ✓ | optional | Stage01–04 | Neck |
| Body.Head | ✓ | ✓ | ✓ | optional | Stage01–04 | Head |
| Body.ChinSoft | ✓ | ✓ | optional | ✓ | Stage01–04 | ChinSoft.Root/L/R |
| Body.Ear.L/R | ✓ | visible ear | optional | no | Stage01–04 | Head |
| UpperArm.L/R | ✓ | ✓ | ✓ | ✓ | Stage01–04 | UpperArm.L/R |
| Forearm.L/R | ✓ | ✓ | ✓ | ✓ | Stage01–04 | Forearm.L/R |
| Hand.L/R | ✓ | ✓ | ✓ | rigid/optional | pose swaps | Hand.L/R |
| Thigh.L/R | ✓ | ✓ | ✓ | ✓ | Stage01–04 | Thigh.L/R |
| Shin.L/R | ✓ | ✓ | ✓ | ✓ | Stage01–04 | Shin.L/R |
| Foot.L/R | ✓ | ✓ | ✓ | rigid/optional | Stage01–04 | Foot.L/R |
| Shirt.Torso | ✓ | ✓ | ✓ | ✓ | Stage/outfit | Chest/Spine |
| Shirt.Belly | ✓ | ✓ | ✓ | ✓ | Stage/outfit | Belly bones |
| Shirt.Hem | ✓ | ✓ | ✓ | ✓ | Stage/outfit | ShirtHem bones |
| Shorts.Pelvis | ✓ | ✓ | ✓ | ✓ | Stage/outfit | Pelvis |
| Shorts.Leg.L/R | ✓ | ✓ | ✓ | ✓ | Stage/outfit | Thigh.L/R |
| Shoe.L/R | ✓ | ✓ | ✓ | rigid | outfit | Foot.L/R |
| Hair.Back | ✓ | ✓ | ✓ | no/optional | hairstyle | Head |
| Hair.Main | ✓ | ✓ | ✓ | no/optional | hairstyle | Head |
| Hair.Front | ✓ | ✓ | no | no/optional | hairstyle | Head |
| Eye.L/R | ✓ | visible eye | no | no | Open/Half/Closed | Head/EyeAim |
| Pupil.L/R | ✓ | visible pupil | no | no | direction | Eye.L/R |
| Brow.L/R | ✓ | visible brow | no | no | Neutral/Raised/Strain | Head |
| Mouth | ✓ | ✓ | no | no | facial states | Jaw/Head |
| Occlusion joints | as needed | as needed | as needed | no | view | corresponding joint |

## Hidden overlap checklist

### Head and neck

- neck extends at least 20% under head;
- neck extends under shirt/torso;
- chin art has no hard lower crop;
- hair back extends under main hair and head contour.

### Shoulders

- upper arm contains rounded shoulder cap;
- cap extends under torso;
- torso contains natural armpit shadow;
- arm-up pose does not reveal empty area.

### Elbows

- upper arm and forearm overlap;
- inner elbow has darker crease only in bent state or neutral subtle fold;
- no straight transparent cut line.

### Wrists

- hand includes hidden wrist section;
- forearm includes cuff-side overlap;
- alternative hand poses share same pivot and approximate bounds.

### Hips

- thigh extends under pelvis and shorts;
- body pelvis is complete beneath clothes where outfit changes are planned;
- left/right thighs do not share one flattened sprite.

### Knees

- shin includes upper knee volume;
- thigh includes lower knee overlap;
- bent pose remains rounded.

### Ankles

- foot/shoe includes hidden ankle;
- shin extends into shoe;
- no floating shoe.

### Belly and clothes

- body belly exists beneath shirt;
- shirt belly is separate from torso shirt;
- shirt hem has inside shadow and hidden upper fabric;
- shirt hem can move without opening a transparent gap.

## Canvas alignment

Все view-группы должны использовать:

- одинаковый master canvas;
- одну ground line;
- одинаковую высоту головы;
- одинаковую позицию Pelvis;
- одинаковый масштаб;
- neutral pose, совместимую с skeleton bind pose.

Допустимое отличие общей высоты Front/Side/Back: до 3%.

## Pivot/bind locations

| Часть | Pivot/Bind |
|---|---|
| Pelvis | центр массы таза |
| Torso | нижняя середина Spine.01 |
| Belly | верхне-средняя область у Belly.Root |
| ShirtHem | верхняя внутренняя линия ткани |
| Head | основание черепа/шея |
| ChinSoft | верхняя центральная точка мягкого подбородка |
| UpperArm | центр плечевого сустава |
| Forearm | центр локтя |
| Hand | центр запястья |
| Thigh | тазобедренный сустав |
| Shin | центр колена |
| Foot | щиколотка |
| Eye/Brow/Mouth | локальный центр элемента относительно головы |

## Stage compatibility

Для Stage01–04:

- Category names полностью совпадают;
- labels различаются только Stage-prefix;
- pivots не прыгают;
- лицо сохраняет идентичность;
- одежда не меняет sorting без отдельной настройки;
- границы мягких mesh не должны радикально отличаться;
- один skeleton должен оставаться применимым либо должен использоваться документированный shared skeleton workflow.

## Запрещённые дефекты исходника

- непрозрачный фон;
- белый fringe;
- premultiplied-alpha ореол;
- полупрозрачная прямоугольная область вокруг части;
- лишние пальцы;
- слитые кисти и бёдра;
- видимые куски соседнего ракурса;
- crop из turnaround без дорисовки;
- разные источники света между слоями;
- line-art, который не совпадает на перекрытии;
- неуникальные layer names.

## Art acceptance renders

До рига художник предоставляет PNG-preview:

1. собранный Front neutral;
2. собранный Side neutral;
3. собранный Back neutral;
4. разобранные слои на сером фоне;
5. arm-up overlap test;
6. elbow 90° mock-up;
7. knee 90° mock-up;
8. belly bend mock-up;
9. лицо со всеми eye/mouth states;
10. силуэт на фоне комнаты в масштабе gameplay.

Только после утверждения этих previews начинается manual skinning.
