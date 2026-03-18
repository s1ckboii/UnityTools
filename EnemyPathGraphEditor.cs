using UnityEditor;
using UnityEngine;

namespace UnityTools.EnemyPath
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EnemyPathGraph))]
    public class EnemyPathGraphEditor : Editor
    {
        private EnemyPathGraph path;

        private Texture2D iconOn;
        private Texture2D iconOff;

        private void OnEnable()
        {
            path = (EnemyPathGraph)target;
            Refresh();

            iconOn = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/eye_open.png");
            iconOff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/eye_closed.png");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ButtonStyle();

            if (GUILayout.Button("Regenerate Connections"))
            {
                foreach (var t in targets)
                {
                    EnemyPathGraph p = (EnemyPathGraph)t;
                    p.GenerateConnections();
                    EditorUtility.SetDirty(p);
                }
            }

            Refresh();
        }

        private void ButtonStyle()
        {
            EditorGUILayout.Space(10);

            SerializedProperty showProp = serializedObject.FindProperty("_showPath");

            Rect full = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));

            float labelWidth = EditorGUIUtility.labelWidth;

            Rect labelRect = new Rect(full.x, full.y, labelWidth, full.height);
            EditorGUI.LabelField(labelRect, "Visualization");

            float remainingWidth = full.width - labelWidth;
            float buttonWidth = Mathf.Clamp(remainingWidth, 160f, 320f);

            Rect rect = new Rect(full.x + labelWidth + (remainingWidth - buttonWidth) * 0.5f, full.y, buttonWidth, full.height);

            bool hovered = rect.Contains(Event.current.mousePosition);

            Color top = showProp.boolValue
                ? new Color(0.30f, 0.60f, 1f)
                : new Color(0.32f, 0.32f, 0.32f);

            Color bottom = showProp.boolValue
                ? new Color(0.12f, 0.35f, 0.85f)
                : new Color(0.18f, 0.18f, 0.18f);

            if (hovered)
            {
                top *= 1.1f;
                bottom *= 1.1f;
            }

            EditorGUI.DrawRect(rect, bottom);

            Rect topRect = new Rect(rect.x, rect.y, rect.width, rect.height * 0.5f);
            EditorGUI.DrawRect(topRect, top);

            Rect contentRect = new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height);

            GUIStyle textStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            EditorGUI.LabelField(contentRect, showProp.boolValue ? "Path Visible" : "Path Hidden", textStyle);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                showProp.boolValue = !showProp.boolValue;
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll();
                Event.current.Use();
            }
        }

        private void Refresh()
        {
            foreach (var t in targets)
            {
                EnemyPathGraph p = (EnemyPathGraph)t;

                var so = new SerializedObject(p);
                var prop = so.FindProperty("_nodes");

                prop.ClearArray();

                for (int i = 0; i < p.transform.childCount; i++)
                {
                    prop.InsertArrayElementAtIndex(i);

                    var element = prop.GetArrayElementAtIndex(i);
                    var pointProp = element.FindPropertyRelative("point");

                    pointProp.objectReferenceValue = p.transform.GetChild(i);
                }

                so.ApplyModifiedProperties();

                p.GenerateConnections();
            }
        }

        private void OnSceneGUI()
        {
            DrawPath(path);
        }

        public static void DrawPath(EnemyPathGraph path)
        {
            if (path == null || path.Count == 0 || !path.ShowPath)
                return;

            Handles.color = Color.yellow;

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 pos = path.GetWaypoint(i);

                Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);

                Handles.color = new Color(0, 1, 1, 0.08f);
                Handles.DrawSolidDisc(pos, Vector3.up, path.ConnectionRadius);

                Handles.color = Color.yellow;

                var connections = path.GetConnections(i);
                if (connections == null) continue;

                foreach (var targetIndex in connections)
                {
                    if (targetIndex <= i) continue;

                    Vector3 targetPos = path.GetWaypoint(targetIndex);
                    Handles.DrawLine(pos, targetPos);
                }
            }
        }
    }

    [InitializeOnLoad]
    public static class EnemyPathGraphSceneDrawer
    {
        static EnemyPathGraphSceneDrawer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            var paths = Object.FindObjectsByType<EnemyPathGraph>(FindObjectsSortMode.None);

            foreach (var path in paths)
            {
                EnemyPathGraphEditor.DrawPath(path);
            }
        }
    }
}