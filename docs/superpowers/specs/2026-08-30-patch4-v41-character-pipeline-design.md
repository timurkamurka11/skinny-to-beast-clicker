# Patch 4 V41: единый character pipeline

## Цель

Patch 4 готовится скрытым и получает видимые пиксели только одним атомарным
handoff. Production activation остаётся закрытой существующим art approval;
Editor review/demo получает отдельный ограниченный override, который никогда не
меняет approval.

## Причины V40-сбоя

- `CharacterAnimationDriver` прекращал настройку, если controller уже был
  назначен prefab-ом. Кэши слоёв и параметров оставались пустыми, поэтому
  валидный legacy Animator считался неготовым.
- Review, interactive preview и visibility guard независимо меняли active state
  Patch 3.5/Patch 4. Это позволяло начать pixel suppression до завершения
  logical readiness и создавало промежуточные кадры.
- V21 body/face ожидали активный `Patch4VisualRoot`. Первый видимый кадр мог
  появиться до bind pose/deformer/face binding.
- Runtime Canvas rebuild мог оставить V21 на отложенно удаляемом старом
  `GeneratedCanvasLayers`.
- Полный rebuild сохранял базовый prefab до V21 animation/rig finalizers, поэтому
  локально сгенерированный prefab мог быть устаревшим относительно builder-кода.

## Архитектура

`Patch4CharacterRigController` — единственный владелец presentation state:

- `LegacyRollback`: Patch 4 скрыт, legacy пиксели и logical rig активны.
- `Patch4Initializing`: кандидат скрыт; Animator, Canvas, V21 limbs и face
  связываются детерминированно.
- `Patch4Production`: разрешён только technical readiness + production art gate.
- `EditorDevelopmentPreview` / `EditorReviewOverride`: технически готовый Patch 4
  показан без изменения art gate.
- `EditorPreviewBlackout`: обе презентации скрыты только для чистого background
  capture и затем возвращаются через того же владельца.

Прямой порядок forward handoff: подавить legacy renderer pixels, затем включить
Patch 4 root, синхронно применить текущий locomotion/routine signal до рендера.
Если signal bridge не принимает состояние, handoff тут же откатывается. Обратный
порядок: скрыть Patch 4 root, затем восстановить legacy pixels. Legacy
`VisualRoot`, skin, Animator и gameplay signals остаются логически активными.

## Rig и движение

- Канонические родители 31 костей проверяются вместе с уникальностью и конечными
  ненулевыми transform-ами.
- Animator единолично владеет articulated leg channels; obsolete V21 foot solver
  запрещён.
- V21 использует одну torso mesh и четыре непрерывные limb meshes. Secondary
  motion остаётся только на soft channels.
- Перемещение по комнате выполняет legacy `CharacterRoutineController` на общем
  gameplay root; Patch 4 остаётся его дочерним объектом и не имитирует travel
  локальным root sway.

## Проверка

Generated-prefab audit проверяет сохранённые Animator/controller/states,
animation bindings, skeleton hierarchy, Canvas/V21 references, отключённый V23
QA RawImage, отсутствие obsolete writers и скрытый initial visual root. EditMode
и PlayMode тесты проверяют положительный handoff, rollback negative paths,
сигналы, bone motion, mesh deformation и controlled root travel.

Unity 6000.3.19f1 остаётся обязательной внешней проверкой; статический guard не
считается заменой Unity compilation/EditMode/PlayMode.
