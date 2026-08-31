using UnityEditor;
using UnityEngine;

namespace SkinnyToBeast.Gameplay.Patch4.Editor
{
    public sealed class Patch4GameplayAnimationDemoWindow : EditorWindow
    {
        [MenuItem("Tools/GameWork/Patch 4.0/Gameplay Animation Demo")]
        public static void Open()
        {
            Patch4GameplayAnimationDemoWindow window =
                GetWindow<Patch4GameplayAnimationDemoWindow>();
            window.titleContent = new GUIContent("Patch 4 Gameplay Demo");
            window.minSize = new Vector2(420f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update -= Repaint;
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Patch 4 — реальная игровая комната",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Демонстрация работает только в Unity Editor, не меняет " +
                "production art approval и показывает V21 Canvas rig через " +
                "тот же Animator и handoff, что использует gameplay.",
                MessageType.Info);

            if (!EditorApplication.isPlaying)
            {
                if (GUILayout.Button("Запустить скрытую инициализацию и демо", GUILayout.Height(36f)))
                {
                    Patch4InteractiveGameplayPreview.StartDevelopmentDemo();
                }
                DrawAuditStatus();
                return;
            }

            Patch4InteractiveGameplayPreviewDriver driver =
                Patch4InteractiveGameplayPreview.ActiveDriver;
            if (driver == null || !driver.IsActive)
            {
                EditorGUILayout.HelpBox(
                    "Ожидание валидного LivingGameplayScene и атомарного handoff…",
                    MessageType.Warning);
                if (GUILayout.Button("Остановить Play Mode"))
                    EditorApplication.isPlaying = false;
                return;
            }

            EditorGUILayout.LabelField("Animator", driver.AnimatorReady ? "готов" : "не готов");
            EditorGUILayout.LabelField("Legacy rollback", driver.LegacyReady ? "готов" : "не готов");
            EditorGUILayout.LabelField("Видимых презентаций", driver.VisiblePresentationCount.ToString());
            EditorGUILayout.LabelField("Текущий клип", driver.CurrentDevelopmentClip);
            if (!string.IsNullOrEmpty(driver.LastError))
                EditorGUILayout.HelpBox(driver.LastError, MessageType.Error);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Анимации", EditorStyles.boldLabel);
            for (int i = 0; i < Patch4RigContract.RequiredClipNames.Count; i++)
            {
                string clip = Patch4RigContract.RequiredClipNames[i];
                if (GUILayout.Button(clip)) driver.PlayDevelopmentClip(clip);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Контролируемое перемещение корня", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Идти влево")) driver.WalkLeft();
            if (GUILayout.Button("Сброс")) driver.ResetDevelopmentDemo();
            if (GUILayout.Button("Идти вправо")) driver.WalkRight();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            if (GUILayout.Button("Остановить и восстановить Patch 3.5", GUILayout.Height(30f)))
                EditorApplication.isPlaying = false;
        }

        private static void DrawAuditStatus()
        {
            bool valid = Patch4GeneratedPrefabAudit.ValidateGeneratedPrefab(out string error);
            EditorGUILayout.HelpBox(
                valid ? "Сгенерированный prefab прошёл структурный аудит."
                    : "Перед запуском prefab будет детерминированно перестроен.\n" + error,
                valid ? MessageType.Info : MessageType.Warning);
        }
    }
}
