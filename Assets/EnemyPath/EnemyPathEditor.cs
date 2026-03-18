using UnityEditor;
using UnityEngine;

namespace UnityTools.EnemyPath
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EnemyPath))]
    public class EnemyPathLinearEditor : Editor
    {
        private EnemyPath path;

        private Texture2D iconOn;
        private Texture2D iconOff;

        private void OnEnable()
        {
            path = (EnemyPath)target;
            Refresh();

            iconOn = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/eye_open.png");
            iconOff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/eye_closed.png");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ButtonStyle();

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

            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.xMax, rect.y));
            Handles.color = new Color(0f, 0f, 0f, 0.4f);
            Handles.DrawLine(new Vector3(rect.x, rect.yMax - 1), new Vector3(rect.xMax, rect.yMax - 1));
            Handles.EndGUI();

            Rect contentRect = new Rect(rect.x + 8, rect.y, rect.width - 16, rect.height);

            Texture icon = showProp.boolValue ? iconOn : iconOff;

            GUIStyle textStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            if (icon != null)
            {
                float iconSize = Mathf.Min(24f, contentRect.height - 6f);

                Rect iconRect = new Rect(contentRect.x, contentRect.y + (contentRect.height - iconSize) * 0.5f, iconSize, iconSize);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                Rect textRect = new Rect(contentRect.x + iconSize + 6, contentRect.y, contentRect.width - iconSize - 6, contentRect.height);
                EditorGUI.LabelField(textRect, showProp.boolValue ? "Path Visible" : "Path Hidden", textStyle);
            }
            else
            {
                EditorGUI.LabelField(contentRect, showProp.boolValue ? "Path Visible" : "Path Hidden", textStyle);
            }

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
                EnemyPath p = (EnemyPath)t;

                var so = new SerializedObject(p);
                var prop = so.FindProperty("_waypoints");

                prop.ClearArray();

                for (int i = 0; i < p.transform.childCount; i++)
                {
                    prop.InsertArrayElementAtIndex(i);
                    prop.GetArrayElementAtIndex(i).objectReferenceValue =
                        p.transform.GetChild(i);
                }

                so.ApplyModifiedProperties();
            }
        }

        private void OnSceneGUI()
        {
            if (path == null || path.Count == 0 || !path.ShowPath)
                return;

            Handles.color = Color.yellow;

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 pos = path.GetWaypoint(i);

                Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);

                if (i < path.Count - 1)
                {
                    Handles.DrawLine(pos, path.GetWaypoint(i + 1));
                }
            }
        }
    }

    [InitializeOnLoad]
    public static class EnemyPathSceneDrawer
    {
        static EnemyPathSceneDrawer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            var paths = Object.FindObjectsByType<EnemyPath>(FindObjectsSortMode.None);

            Handles.color = Color.yellow;

            foreach (var path in paths)
            {
                if (path == null || path.Count == 0 || !path.ShowPath)
                    continue;

                for (int i = 0; i < path.Count; i++)
                {
                    Vector3 pos = path.GetWaypoint(i);

                    Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);

                    if (i < path.Count - 1)
                    {
                        Handles.DrawLine(pos, path.GetWaypoint(i + 1));
                    }
                }
            }
        }
    }
}