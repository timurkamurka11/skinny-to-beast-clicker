# Production Fat Man 2D Rig — полное production-ready ТЗ 3.7

Статус документа: обязательный контракт для арта, рига, анимаций и интеграции.
Целевая версия проекта: Unity `6000.3.19f1`.
Пакеты проекта: `com.unity.2d.animation 13.0.5`, `com.unity.2d.psdimporter 12.0.2`, URP `17.3.0`.
Целевые платформы: Android в первую очередь, затем iOS.

---

## 1. Цель

Создать оригинального толстого мужского 2D-персонажа для мобильного idle/clicker-проекта со следующими качествами:

- настоящий отдельный Unity 2D Animation rig;
- собственная иерархия костей, не связанная с процедурным манекеном проекта;
- ручная геометрия и ручная очистка весов SpriteSkin;
- объёмное дыхание и вторичная динамика живота, груди, подбородка и низа майки;
- аккуратная ходьба, движения рук и ног, повороты, реакции на тап и бытовые idle-действия;
- отдельная мимика: глаза, веки, зрачки, брови, рот;
- поддержка Front, SideLeft, SideRight и Back;
- поддержка четырёх стадий тела и последующих смен одежды;
- отсутствие белых прямоугольников, оторванных частей, щелей и пересечений;
- одна и та же production-модель используется на экране входа и в комнате.

Референс Lamar используется только как ориентир по ощущению живого idle-персонажа, частоте мелких действий, тяжести движений и читаемости силуэта. Запрещено копировать чужие изображения, конкретные кадры анимации, одежду, лицо, пропорции или иные защищённые элементы.

---

## 2. Важное уточнение о формате

Качественный 2D skeletal rig всё равно использует нарисованные текстуры. Отличие от неудачного Patch 3.6 заключается не в полном отказе от растровых изображений, а в правильном production pipeline:

- исходник — многослойный PSB;
- каждый художественный элемент имеет дорисованные скрытые области;
- Unity создаёт отдельные SpriteRenderer;
- мягкие части получают редактируемую mesh-геометрию;
- SpriteSkin деформирует mesh костями и весами;
- жёсткие детали при необходимости остаются целыми спрайтами, но двигаются собственной костью;
- никаких runtime-прямоугольных вырезок из общего PNG;
- никаких попыток восстановить суставы автоматически по плоской картинке.

Допустимый исходник: `.psb`.
Отдельные PNG допускаются только как промежуточный импорт при условии, что они изначально нарисованы раздельно, имеют прозрачность, скрытые перекрытия и затем вручную ригуются. Простая нарезка turnaround запрещена.

---

## 3. Обязательные результаты поставки

### 3.1 Исходники

```text
Assets/Art/Characters/FatManProduction/
  FatManProduction_Master.psb
  References/
  ExportNotes.md
```

### 3.2 Unity-ассеты

```text
Assets/Characters/FatManProduction/
  Prefabs/
    FatManProductionRig_Work.prefab
  Animations/
    FatManProduction.controller
    Clips/
  SpriteLibraries/
    FatMan_Base.spriteLib
    FatMan_Stage01.spriteLib
    FatMan_Stage02.spriteLib
    FatMan_Stage03.spriteLib
    FatMan_Stage04.spriteLib
  Materials/
  Tests/
```

### 3.3 Финальный Resources-prefab

```text
Assets/Resources/Characters/FatManProduction/FatManProductionRig.prefab
```

Этот путь является runtime-контрактом `ProductionFatManRigHost`.

### 3.4 Документация исполнителя

Исполнитель передаёт:

- карту костей;
- список Sprite Library Category/Label;
- перечень клипов и длительностей;
- описание нестандартных весов;
- список известных ограничений;
- коммерческие права на использование исходников и результата.

---

## 4. Художественное направление

### 4.1 Общий образ Stage 1

- взрослый толстый мужчина;
- крупный мягкий живот;
- мягкая грудь и бока;
- короткая шея и заметный второй подбородок;
- слегка сутулая расслабленная осанка;
- тяжёлые, но не болезненные движения;
- растянутая домашняя майка;
- свободные шорты;
- домашняя обувь;
- добродушно-уставшее лицо;
- читаемый силуэт на экране телефона.

### 4.2 Запрещённые визуальные решения

- копирование персонажа Lamar;
- фотореалистичная кожа при мультяшной комнате;
- один плоский turnaround как финальный персонаж;
- белый или цветной matte по краям;
- обрезание конечности ровно по линии сустава;
- одинаковый Front, растянутый в Side или Back;
- прямоугольные crop-слои;
- AI-артефакты: лишние пальцы, расплавленные суставы, несимметричная одежда без художественного замысла;
- мелкие детали, исчезающие на мобильном экране.

### 4.3 Размер мастер-арта

- рекомендуемая высота персонажа: `3000 px`;
- допустимый диапазон: `2500–4000 px`;
- прозрачный фон;
- цветовое пространство: sRGB;
- рабочая глубина: 8 bit/channel;
- общий canvas одинаковый для всех ракурсов и стадий;
- персонаж располагается в центре canvas с запасом минимум 12% сверху и снизу.

---

## 5. Ракурсы

Обязательные наборы:

- `Front`;
- `Side` — отдельный художественный вид;
- `Back` — отдельный художественный вид.

`SideRight` может быть зеркалом `SideLeft` только когда:

- одежда симметрична;
- причёска симметрична;
- освещение не создаёт очевидной ошибки;
- надписи и аксессуары отсутствуют либо корректно заменяются.

Для особенно качественного поворота рекомендуются дополнительные переходные изображения:

- `FrontThreeQuarter`;
- `BackThreeQuarter`.

Они используются только в коротком Turn-клипе и не являются постоянным gameplay-facing.

Front нельзя деформировать в Side или Back костями. Смена направления выполняется Sprite Library swap либо переключением совместимых view-root.

---

## 6. Структура PSB

В PSB все названия уникальны и чувствительны к регистру. Группы не должны содержать повторяющиеся имена.

```text
FatManProduction_Master
  FRONT
    BODY
    CLOTHES
    ARM_L
    ARM_R
    LEG_L
    LEG_R
    FACE
    HAIR
    FX_GUIDES
  SIDE
    ...
  BACK
    ...
  STAGE_GUIDES
  DO_NOT_IMPORT
```

`FX_GUIDES`, `STAGE_GUIDES` и `DO_NOT_IMPORT` не импортируются в runtime.

---

## 7. Полный обязательный список художественных частей

Ниже приведены логические категории. Для каждого ракурса создаётся отдельный набор, если элемент видим в этом ракурсе.

### 7.1 Базовое тело

- `Body.Pelvis`;
- `Body.Belly`;
- `Body.Torso`;
- `Body.ChestSoft`;
- `Body.Neck`;
- `Body.Head`;
- `Body.ChinSoft`;
- `Body.Ear.L`;
- `Body.Ear.R`;
- `Body.UpperArm.L`;
- `Body.Forearm.L`;
- `Body.Hand.L`;
- `Body.UpperArm.R`;
- `Body.Forearm.R`;
- `Body.Hand.R`;
- `Body.Thigh.L`;
- `Body.Shin.L`;
- `Body.Foot.L`;
- `Body.Thigh.R`;
- `Body.Shin.R`;
- `Body.Foot.R`.

### 7.2 Одежда

- `Clothes.Shirt.Torso`;
- `Clothes.Shirt.Belly`;
- `Clothes.Shirt.Hem`;
- `Clothes.Shorts.Pelvis`;
- `Clothes.Shorts.Leg.L`;
- `Clothes.Shorts.Leg.R`;
- `Clothes.Shoe.L`;
- `Clothes.Shoe.R`.

Одежда не должна быть намертво запечена в кожу, если в игре планируется её смена.

### 7.3 Волосы

- `Hair.Back`;
- `Hair.Main`;
- `Hair.Front`;
- `Hair.SideLocks` при необходимости.

### 7.4 Лицо

- `Face.Brow.L.Neutral`;
- `Face.Brow.L.Raised`;
- `Face.Brow.L.Strain`;
- `Face.Brow.R.Neutral`;
- `Face.Brow.R.Raised`;
- `Face.Brow.R.Strain`;
- `Face.Eye.L.Open`;
- `Face.Eye.L.Half`;
- `Face.Eye.L.Closed`;
- `Face.Eye.R.Open`;
- `Face.Eye.R.Half`;
- `Face.Eye.R.Closed`;
- `Face.Pupil.L`;
- `Face.Pupil.R`;
- `Face.Mouth.Neutral`;
- `Face.Mouth.OpenSmall`;
- `Face.Mouth.OpenWide`;
- `Face.Mouth.Smile`;
- `Face.Mouth.Strain`;
- `Face.Mouth.Yawn`;
- `Face.Mouth.Exhale`.

Нос и базовые складки лица могут быть частью `Body.Head`, но не должны мешать мимике.

### 7.5 Альтернативные кисти

- `Hand.L.Relaxed`;
- `Hand.L.Fist`;
- `Hand.L.Scratch`;
- `Hand.L.Grip`;
- `Hand.R.Relaxed`;
- `Hand.R.Fist`;
- `Hand.R.Scratch`;
- `Hand.R.Grip`.

### 7.6 Тени и перекрытия

- `Occlusion.Neck`;
- `Occlusion.Shoulder.L`;
- `Occlusion.Shoulder.R`;
- `Occlusion.Hip.L`;
- `Occlusion.Hip.R`;
- `Occlusion.Knee.L`;
- `Occlusion.Knee.R`.

Occlusion-элементы применяются только там, где без них при экстремальных позах образуется щель. Они должны выглядеть как естественная тень, а не как заплатка.

---

## 8. Правила дорисовки скрытых областей

Каждая часть должна иметь запас рисунка под соседним элементом:

- плечо заходит под торс минимум на 12–18% своей ширины;
- предплечье заходит под верхнюю руку минимум на 15% длины локтевой зоны;
- кисть имеет скрытое запястье;
- бедро имеет дорисованную верхнюю часть под тазом и шортами;
- голень имеет дорисованную коленную чашечку под бедром;
- стопа имеет дорисованную щиколотку;
- шея продолжается под голову и торс;
- живот продолжается под грудь, таз и майку;
- низ майки содержит внутреннюю тёмную кромку и запас ткани;
- второй подбородок имеет цельную скрытую область под головой.

В контрольных позах не допускается прозрачная щель шире одного экранного пикселя при reference resolution 1080×1920.

---

## 9. Стадии тела

Сначала полностью принимается Stage 1. Только после его Definition of Done выполняются Stage 2–4.

### Stage 1

- самый тяжёлый и расслабленный;
- большой живот;
- сутулость;
- тяжёлый walk cycle;
- растянутая домашняя одежда.

### Stage 2

- немного меньше живот;
- лучше осанка;
- более уверенный взгляд;
- та же логическая структура слоёв и костей.

### Stage 3

- более плотный торс;
- заметнее плечи и руки;
- меньше вторичная амплитуда живота;
- более энергичные idle-движения.

### Stage 4

- сильный и собранный, но стилистически тот же персонаж;
- сохраняется узнаваемость лица;
- движения быстрее и увереннее;
- полностью совместимая иерархия Sprite Library.

Все стадии используют одинаковые Category/Label, одинаковые имена костей, совместимые pivots и приблизительно одинаковые локальные bounds. Стадия меняется Sprite Library Asset или полным view-set swap, а не перестройкой runtime-рига.

---

## 10. Иерархия собственного скелета

```text
FatManProductionRig
  RigRoot
    RootMotion
      Pelvis
        Spine.01
          Spine.02
            Chest
              Clavicle.L
                UpperArm.L
                  Forearm.L
                    Hand.L
              Clavicle.R
                UpperArm.R
                  Forearm.R
                    Hand.R
              Neck
                Head
                  Jaw
                  ChinSoft.Root
                    ChinSoft.L
                    ChinSoft.R
                  EyeAim
                    Eye.L
                    Eye.R
        Thigh.L
          Shin.L
            Foot.L
              Toe.L
        Thigh.R
          Shin.R
            Foot.R
              Toe.R
        Belly.Root
          Belly.L
          Belly.Center
          Belly.R
        ShirtHem.Root
          ShirtHem.L
          ShirtHem.Center
          ShirtHem.R
        ButtSoft.L
        ButtSoft.R
```

### Обязательные принципы

- `RigRoot` отвечает за масштаб и техническую ориентацию;
- `RootMotion` остаётся на месте: перемещение по комнате выполняет gameplay-контроллер;
- `Pelvis` — главный центр массы;
- `Spine.01` и `Spine.02` дают мягкий изгиб корпуса;
- плечи начинаются с Clavicle, чтобы руки не вращались из центра груди;
- `Belly.*`, `ShirtHem.*`, `ChinSoft.*` — вторичные кости;
- старые `CharacterRigController` bones не являются `boneTransforms` production SpriteSkin;
- production-prefab не содержит `CharacterMeshGraphic`.

---

## 11. Geometry и веса SpriteSkin

### 11.1 Общие ограничения

- максимум 4 bone influence на вершину;
- рекомендуемо 2–3 influence;
- автоматические weights используются только как стартовая точка;
- каждая мягкая зона проверяется и правится вручную;
- вершины не должны пересекать контур соседнего спрайта в bind pose;
- mesh следует силуэту, но не создаёт чрезмерное число мелких треугольников.

### 11.2 Рекомендуемый бюджет геометрии Stage 1

- голова: 80–160 вершин;
- торс/грудь: 120–220;
- живот: 140–260;
- низ майки: 60–120;
- верхняя часть руки: 35–70 каждая;
- предплечье: 30–60 каждое;
- кисть: 20–45 каждая;
- бедро: 45–85 каждое;
- голень: 35–70 каждая;
- стопа/обувь: 20–50 каждая;
- полный активный view: ориентир 900–1800 вершин;
- жёсткий верхний бюджет: 2500 вершин на активный view.

Это целевые бюджеты проекта, а не требование Unity. При видимых заломах качество важнее минимального числа вершин.

### 11.3 Живот

- верх живота смешивается с `Spine.01`, `Spine.02` и `Belly.Root`;
- центр получает основной вес `Belly.Center`;
- края смешиваются с `Belly.L/R`;
- низ частично следует `Pelvis`;
- при наклоне живот сохраняет объём и не становится песочными часами;
- secondary motion запаздывает на 2–4 кадра относительно таза.

### 11.4 Грудь и торс

- вращение распределяется между `Spine.01`, `Spine.02`, `Chest`;
- плечевые вершины имеют широкое плавное смешивание с Clavicle;
- грудь не должна разрываться при поднятии обеих рук;
- мягкая грудь получает не более 8–12% визуального squash/stretch.

### 11.5 Плечо и локоть

- локтевая зона имеет дополнительные edge loops;
- внутренняя сторона локтя сжимается, внешняя сохраняет дугу;
- плечо не вытягивается тонкой трубкой;
- запрещён один общий вес на всю руку без переходной зоны.

### 11.6 Таз, бедро и колено

- таз сохраняет массу при переносе веса;
- бедро смешивается с тазом только в верхних 15–25%;
- колено имеет корректирующую геометрию спереди и сзади;
- при приседании не возникает острого треугольника;
- стопа и носок почти жёсткие.

### 11.7 Голова и подбородок

- основная голова почти жёсткая относительно `Head`;
- челюсть может иметь небольшой поворот для yawning;
- `ChinSoft` даёт лёгкое запаздывание, но не отделяется от лица;
- глаза, зрачки, брови и рот не получают веса корпуса.

### 11.8 Контрольные deformation-позы

Риггер обязан проверить минимум:

- руки вниз;
- обе руки вверх;
- локоть 35°, 90°, 135°;
- перенос веса на левую и правую ногу;
- колено 45° и 100°;
- наклон корпуса ±15°;
- поворот головы ±12°;
- глубокий вдох и выдох;
- максимальная tap-reaction;
- sit pose;
- один полный walk cycle.

---

## 12. Sprite Library contract

Для смены стадий, ракурсов, лица и кистей используются Sprite Library и Sprite Resolver.

### Категории

```text
View
Body.Pelvis
Body.Belly
Body.Torso
Body.ChestSoft
Body.Neck
Body.Head
Body.ChinSoft
Body.UpperArm.L
Body.Forearm.L
Body.Hand.L
Body.UpperArm.R
Body.Forearm.R
Body.Hand.R
Body.Thigh.L
Body.Shin.L
Body.Foot.L
Body.Thigh.R
Body.Shin.R
Body.Foot.R
Clothes.Shirt.Torso
Clothes.Shirt.Belly
Clothes.Shirt.Hem
Clothes.Shorts.Pelvis
Clothes.Shorts.Leg.L
Clothes.Shorts.Leg.R
Clothes.Shoe.L
Clothes.Shoe.R
Hair.Back
Hair.Main
Hair.Front
Face.Eye.L
Face.Eye.R
Face.Brow.L
Face.Brow.R
Face.Mouth
HandPose.L
HandPose.R
```

### Labels

Для body/clothes/hair:

```text
Stage01.Front
Stage01.Side
Stage01.Back
Stage02.Front
...
Stage04.Back
```

Для лица:

```text
Open
Half
Closed
Neutral
Raised
Strain
OpenSmall
OpenWide
Smile
Yawn
Exhale
```

Для кистей:

```text
Relaxed
Fist
Scratch
Grip
```

---

## 13. Сортировка

На корне production-персонажа находится `SortingGroup`.

Рекомендуемые относительные orders внутри одного view:

```text
-40  Hair.Back
-35  дальняя рука
-30  дальняя нога
-20  задняя часть одежды
-10  Body.Pelvis
  0  Body.Belly / Body.Torso
 10  Clothes.Shirt
 20  ближняя нога
 25  ближняя рука
 30  Body.Neck / Body.Head
 35  Hair.Main
 40  Face base
 45  Eyes / Pupils / Brows / Mouth
 50  Hair.Front
```

Порядок ближних и дальних конечностей меняется для Front, Side и Back. Нельзя использовать один sorting order для всех ракурсов.

---

## 14. Animator contract

`ProductionFatManRigHost` уже передаёт параметры, если они присутствуют и имеют правильный тип:

```text
Facing : int
Stage  : int
Speed  : float
Tap    : bool
Action : int
```

### Значения Facing

```text
0 = Front
1 = SideLeft
2 = SideRight
3 = Back
```

### Значения Stage

```text
0 = Stage01
1 = Stage02
2 = Stage03
3 = Stage04
```

### Значения Action

Соответствуют `CharacterRoutineAction` проекта:

```text
0  None
1  ShiftWeight
2  LookAround
3  Scratch
4  Yawn
5  Stretch
6  Flex
7  AdjustClothes
8  WarmShoulders
9  SitDown
10 SitLoop
11 StandUp
12 Sit
```

### Animator Layers

1. `BaseLocomotion` — Idle/Walk/Sit;
2. `UpperBodyAction` — бытовые действия, Avatar Mask по верхней части тела;
3. `Face` — blink, eye aim, mouth swaps;
4. `SecondaryMotion` — мягкие кости либо animation curves с небольшой амплитудой;
5. `TurnView` — короткий переход и переключение Sprite Library view.

`Root Motion` выключен.

---

## 15. Полный список анимаций

Точный тайминг находится также в `ANIMATION_CLIP_MATRIX.md`.

### Обязательный MVP Stage 1

- `Idle_Breathe`;
- `Idle_ShiftWeight_L`;
- `Idle_ShiftWeight_R`;
- `Blink_Single`;
- `Blink_Double`;
- `Walk`;
- `TapReact_01`;
- `TapReact_02`;
- `TapReact_03`;
- `Turn_Front_Side`;
- `Turn_Side_Back`;
- `LookAround`;
- `Scratch`;
- `Yawn`;
- `Stretch`;
- `Flex`;
- `AdjustClothes`;
- `WarmShoulders`;
- `SitDown`;
- `SitLoop`;
- `StandUp`.

### Требования к ощущению движения

- движения имеют вес и инерцию;
- живот не повторяет таз синхронно, а немного запаздывает;
- руки во время ходьбы движутся в противофазе ногам;
- голова стабилизируется, но имеет небольшую вертикальную реакцию;
- tap action читается за первые 2–3 кадра;
- idle не должен выглядеть как постоянное одинаковое качание;
- случайные действия не повторяются подряд чаще двух раз;
- взгляд и blink работают независимо от корпуса;
- все циклы имеют бесшовный первый/последний кадр.

---

## 16. Рекомендуемые длительности и частота

- `Idle_Breathe`: 3.6–4.8 сек, loop;
- blink single: 0.10–0.15 сек;
- blink double: 0.22–0.32 сек;
- случайный blink: каждые 2.2–5.5 сек;
- `Walk`: 0.72–0.92 сек на полный двухшаговый цикл;
- tap reaction: 0.28–0.48 сек;
- `LookAround`: 2.0–3.0 сек;
- `Scratch`: 1.8–2.8 сек;
- `Yawn`: 3.0–4.2 сек;
- `Stretch`: 2.5–3.5 сек;
- `Flex`: 1.8–2.8 сек;
- `AdjustClothes`: 1.6–2.4 сек;
- `WarmShoulders`: 2.0–3.0 сек;
- `SitDown`: 0.75–1.05 сек;
- `SitLoop`: 2.5–4.0 сек loop;
- `StandUp`: 0.75–1.10 сек;
- turn transition: 0.16–0.30 сек.

---

## 17. Мимика

### Blink

- веки закрывают глаз по форме, а не накладываются прямоугольником;
- pupils скрываются при закрытых глазах;
- допускается sprite swap Open → Half → Closed → Half → Open;
- при yawn blink может быть длиннее;
- при tap допускается краткое расширение глаз.

### Eye aim

- зрачки перемещаются в пределах маски/формы глаза;
- максимальное отклонение ограничено, чтобы зрачок не выходил за глаз;
- LookAround использует отдельные curves;
- idle micro-look выполняется редко и с малой амплитудой.

### Mouth

- Neutral используется в idle;
- OpenSmall — короткая реакция;
- OpenWide/Yawn — зевок;
- Strain — flex/stretch;
- Smile — успешный upgrade или stage change;
- Exhale — окончание тяжёлого действия.

---

## 18. Secondary motion

Вторичная анимация создаётся либо ключами в клипах, либо отдельным безопасным spring-контроллером после принятия базового рига.

Целевые ограничения:

- Belly rotation: ±2.5° idle, до ±5° walk/tap;
- Belly translation: 0.5–2.0% высоты живота;
- ShirtHem rotation: ±3°;
- ShirtHem delay: 2–4 кадра;
- ChinSoft rotation: ±1.5° idle, до ±3° yawn/tap;
- ChestSoft scale: не более 1.08 по одной оси;
- никакой бесконечной физической раскачки после остановки;
- secondary motion отключается или стабилизируется в меню паузы.

---

## 19. View switching

Смена Facing выполняется дискретно:

1. запустить Turn-клип;
2. слегка сжать/повернуть Root визуально;
3. на 45–55% клипа переключить Sprite Library labels;
4. переставить sorting orders ближних/дальних конечностей;
5. завершить Turn в bind-compatible позе нового вида.

SideRight зеркалируется отрицательным scale только на view-root и только при подтверждённой симметрии. Нельзя зеркалировать текст, логотип или асимметричный свет.

---

## 20. Экран входа

Экран входа использует тот же `FatManProductionRig.prefab`:

- Facing = Back;
- Speed = 1;
- отдельный entry camera framing;
- персонаж уменьшается по мере движения к двери за счёт UI/world placement, а не изменения пропорций костей;
- белые или procedural части запрещены;
- при отсутствии production-prefab используется цельный безопасный fallback;
- ошибка production-rig не может блокировать переход в комнату чёрным экраном.

---

## 21. Комната и gameplay

- перемещение персонажа выполняет существующий `CharacterRoutineController`;
- production Animator получает Speed/Facing/Action;
- tap вызывает одно из трёх реакций;
- при сильной очереди тапов reactions не складываются в разрушительную амплитуду;
- персонаж перемещается между room anchors целиком через RootTransform;
- ноги анимируются локально и не определяют world root motion;
- после завершения walk цикл заканчивается в нейтральной опорной фазе;
- SitDown разрешён только у подходящего room anchor.

---

## 22. Импорт PSB в Unity

Обязательные настройки:

- Import Mode: `Individual Sprites (Mosaic)`;
- Texture Type: `Sprite (2D and UI)`;
- Sprite Mode: `Multiple`;
- `Use as Rig`: включено;
- `Use Layer Name`: включено, все имена уникальны;
- `Character Rig`: включено;
- Include Hidden Layers: выключено для production, если скрытые группы не помечены как runtime;
- Mosaic Padding: минимум 4 px, рекомендуется 8 px для master;
- Sprite Padding: достаточный для предотвращения texture bleeding;
- Generate Physics Shape: выключено;
- Mip Maps: выключено;
- Wrap Mode: Clamp;
- Filter Mode: Bilinear;
- Alpha Is Transparency: включено;
- Compression на этапе рига: None;
- Android compression выбирается только после visual QA.

Нельзя менять имена PSB-слоёв после настройки Sprite Library без контролируемой миграции.

---

## 23. Prefab hierarchy

```text
FatManProductionRig
  ProductionRigMarker
  Animator
  SortingGroup
  SpriteLibrary
  Views
    FrontRoot
      Renderers...
    SideRoot
      Renderers...
    BackRoot
      Renderers...
  Skeleton
    RigRoot...
  FaceController
  ViewController
  StageController
```

Допускается другая физическая иерархия, если соблюдены логические имена, Animator contract и validator.

---

## 24. Render и производительность

Проект использует URP, а SpriteSkin поддерживает CPU и GPU deformation. Начальная настройка проекта:

- сначала CPU deformation для предсказуемого тестирования и динамического batching;
- затем профиль Android-сборки;
- GPU deformation допускается после сравнения CPU/GPU времени и draw calls;
- одновременно активен только один полноценный view;
- неактивные Front/Side/Back renderers выключены;
- RenderTexture production host: 768×1280, ARGB32, MSAA 2x;
- целевой активный SpriteRenderer count: 18–36;
- целевой bones count: 30–48;
- целевой draw calls персонажа: до 12 после batching, допускается больше при доказанной необходимости качества;
- целевая память текстур Stage 1: до 32 MB после platform compression;
- Animation Culling отключается только там, где это необходимо для offscreen entry rendering.

Финальный выбор CPU/GPU deformation принимается только по Unity Profiler на реальном Android-устройстве.

---

## 25. Автоматический validator

`Tools > Skinny to Beast > Validate Production Fat Man Rig 3.7` обязан проверять:

- prefab существует по Resources-пути;
- присутствует Animator;
- присутствуют SpriteRenderer;
- присутствует хотя бы один валидный SpriteSkin;
- каждый SpriteSkin имеет boneTransforms;
- нет CharacterMeshGraphic;
- нет Patch 3.6 generated cut-outs;
- нет renderer с белой непрозрачной прямоугольной текстурой;
- параметры Animator имеют правильные типы;
- обязательные clips существуют;
- active view только один;
- bounds не пустые;
- sorting group присутствует;
- Stage 1 categories полностью заполнены;
- отсутствуют дублирующиеся имена костей и PSB-слоёв.

Без прохождения validator production-режим не считается готовым.

---

## 26. Ручная QA-матрица

Обязательные разрешения:

- 1080×1920 portrait;
- 720×1600 portrait;
- 1440×3200 portrait;
- устройство с display cutout;
- Unity Game View с изменением aspect ratio.

Обязательные проверки:

- вход Back-view;
- переход без чёрного экрана;
- Front idle 30 секунд;
- SideLeft и SideRight;
- Back;
- 100 быстрых тапов;
- полный цикл всех random actions;
- walk между всеми anchors;
- SitDown/SitLoop/StandUp;
- смена Stage 1→2→3→4;
- пауза/возобновление;
- возврат в меню и повторный вход;
- Android development build;
- отсутствие Console Error и NaN в transforms.

---

## 27. Критерии качества деформации

Патч отклоняется, если наблюдается хотя бы один пункт:

- сустав отделяется от тела;
- виден прозрачный просвет;
- белая или цветная подложка;
- живот становится тонким или инвертируется;
- локоть/колено превращается в острый треугольник;
- лицо плавает относительно головы;
- глаза выходят за контур;
- одежда пересекает кожу без естественного перекрытия;
- при SideRight зеркалируется надпись;
- голова дёргается при view swap;
- ступни скользят по полу более чем на 4 px во время опорной фазы;
- персонаж меняет общий рост при Front/Side/Back более чем на 3%;
- entry и room используют разные визуальные версии персонажа.

---

## 28. Definition of Done — Stage 1

Stage 1 считается принятым, когда:

- PSB импортируется без ошибок;
- validator проходит;
- присутствует собственный production skeleton;
- старый procedural mannequin не виден;
- Patch 3.6 cut-outs не создаются;
- Front/Side/Back отображаются чисто;
- Idle, Blink, Walk и 3 Tap reactions приняты;
- живот, майка и подбородок имеют аккуратную secondary motion;
- вход и комната используют один prefab;
- чёрный экран невозможен при ошибке персонажа;
- 100 быстрых тапов не ломают позу;
- Android-профиль не показывает неконтролируемых allocation spikes;
- пользователь подтверждает внешний вид по видео из Play Mode.

Только после этого выполняются дополнительные idle-actions и Stage 2–4.

---

## 29. Этапы производства

### Milestone A — Concept Lock

- Front/Side/Back цветные концепты;
- утверждение лица, одежды и пропорций;
- проверка силуэта в размере 20–30% высоты экрана.

### Milestone B — Layered PSB Stage 1

- полная структура слоёв;
- скрытые области;
- проверка прозрачности;
- импорт Mosaic без потерь.

### Milestone C — Rig and Weights

- собственные кости;
- geometry;
- manual weights;
- deformation pose sheet.

### Milestone D — Core Animation

- Idle;
- Blink;
- Walk;
- TapReact 01–03;
- Turn.

### Milestone E — Gameplay Integration

- entry;
- room movement;
- actions;
- stage parameter;
- audio event hooks;
- validator.

### Milestone F — Polish

- secondary motion;
- facial micro-animation;
- performance profiling;
- Android QA;
- дополнительные Stage 2–4.

---

## 30. Что должен предоставить художник/риггер

Минимальный финальный handoff:

```text
FatManProduction_Master.psb
FatManProductionRig.prefab
FatManProduction.controller
FatMan_Base.spriteLib
FatMan_Stage01.spriteLib
AnimationClips/
Textures/
Materials/
RigMap.md
License.txt
```

В PSB должны оставаться редактируемые слои, а не только rasterized merged groups.

---

## 31. Что делает ChatGPT/GitHub-патч после получения ассета

- проверяет структуру prefab;
- подключает prefab к `ProductionFatManRigHost`;
- связывает Facing, Stage, Speed, Tap, Action;
- отключает safe fallback;
- подключает entry и room;
- добавляет автоматические проверки;
- исправляет camera framing, scale, sorting и runtime lifecycle;
- по видео Play Mode корректирует кодовые амплитуды и переходы.

Ручную художественную настройку mesh/weights внутри Skinning Editor нельзя надёжно заменить одним runtime-скриптом. Она является частью production-ассета.

---

## 32. Ссылочная техническая база

Проект следует официальному Unity pipeline:

- 2D PSD Importer 12.x импортирует `.psb`, создаёт Individual Sprites/Mosaic и Character Rig;
- Skinning Editor используется для bones, geometry, influences и weights;
- SpriteSkin деформирует SpriteRenderer mesh через boneTransforms;
- Sprite Library/Resolver используются для стадий, ракурсов и facial/hand swaps;
- CPU и GPU deformation выбираются по профилированию проекта.

Публичная презентация Lamar используется только для определения жанрового уровня живости idle-персонажа. Внутренняя технология Lamar неизвестна и не предполагается в этом документе.
