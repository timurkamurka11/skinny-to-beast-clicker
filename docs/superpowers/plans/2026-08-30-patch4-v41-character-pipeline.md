# План реализации Patch 4 V41

1. Зафиксировать точные readiness diagnostics legacy Animator/skin и исправить
   кэширование предварительно назначенного controller.
2. Ввести единого владельца presentation state с атомарными forward/rollback
   переходами и Editor-only override.
3. Перевести installer на скрытую последовательность Canvas → V23 → Animator →
   state machine → V21 body → V21 face → legacy bridge → readiness.
4. Удалить прямые visibility writes из review, preview и guard; гарантировать
   синхронное подавление всех legacy painted surfaces.
5. Сделать production rebuild полным: animation/pose/walk/V21 finalizers перед
   generated-prefab audit.
6. Добавить независимое Editor gameplay animation demo без изменения art gate.
7. Обновить EditMode/PlayMode контракты: ровно одна презентация, hidden binding,
   Animator states/signals, continuous limb deformation, controlled root travel
   и безопасный rollback при сломанных зависимостях.
8. Выполнить static guard, структурные проверки, `git diff --check`, review diff,
   provisional commit/push. Unity compilation и тесты отметить pending до запуска
   на Unity 6000.3.19f1.
