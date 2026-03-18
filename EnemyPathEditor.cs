using PlaceholderName.EnemyScripts;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PlaceholderName.EditorScripts
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EnemyPath))]
    public class EnemyPathEditor : Editor
    {
        private EnemyPath path;

        private Texture2D iconEyeOn;
        private Texture2D iconEyeOff;
        private Texture2D iconLockLocked;
        private Texture2D iconLockUnlocked;

        private ReorderableList waypointList;

        private static readonly string ThemeKey = "EnemyPathEditor_ThemeColor";

        private static Color ThemeColor
        {
            get
            {
                if (!EditorPrefs.HasKey(ThemeKey))
                    return new Color(0.30f, 0.60f, 1f);

                return StringToColor(EditorPrefs.GetString(ThemeKey));
            }
            set
            {
                EditorPrefs.SetString(ThemeKey, ColorToString(value));
            }
        }
        private static string ColorToString(Color colorTheme)
        {
            return $"{colorTheme.r}|{colorTheme.g}|{colorTheme.b}|{colorTheme.a}";
        }

        private static Color StringToColor(string s)
        {
            var parts = s.Split('|');
            return new Color(
                float.Parse(parts[0]),
                float.Parse(parts[1]),
                float.Parse(parts[2]),
                float.Parse(parts[3])
            );
        }
        private static Color GetContrastColor(Color c)
        {
            float luminance = (0.299f * c.r + 0.587f * c.g + 0.114f * c.b);

            if (luminance > 0.85f)
                return Color.green;

            return Color.white;
        }
        private string AddSpacing(string input)
        {
            return string.Join(" ", input.ToUpper().ToCharArray());
        }

        private readonly System.Collections.Generic.Dictionary<EnemyPath, string> hierarchySignatures = new System.Collections.Generic.Dictionary<EnemyPath, string>();

        private void OnEnable()
        {
            path = (EnemyPath)target;
            Refresh();

            hierarchySignatures.Clear();

            foreach (var t in targets)
            {
                EnemyPath p = (EnemyPath)t;
                if (p != null)
                {
                    hierarchySignatures[p] = GetHierarchySignature(p);
                }
            }

            iconEyeOn = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/eye_open.png");
            iconEyeOff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/eye_closed.png");

            iconLockLocked = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/lock_locked.png");
            iconLockUnlocked = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Icons/lock_unlocked.png");

            SerializedProperty waypointsProp = serializedObject.FindProperty("_waypoints");
            SetupWaypointList(waypointsProp);

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }
        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            if (waypointList != null)
                waypointList.index = -1;
        }
        private void OnHierarchyChanged()
        {
            bool needsRefresh = false;

            foreach (var t in targets)
            {
                EnemyPath p = (EnemyPath)t;
                if (p == null) continue;

                string newSignature = GetHierarchySignature(p);

                if (!hierarchySignatures.TryGetValue(p, out string oldSignature))
                {
                    hierarchySignatures[p] = newSignature;
                    needsRefresh = true;
                    continue;
                }

                if (newSignature != oldSignature)
                {
                    hierarchySignatures[p] = newSignature;
                    needsRefresh = true;
                }
            }

            if (!needsRefresh)
                return;

            Refresh();
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty waypointsProp = serializedObject.FindProperty("_waypoints");
            SerializedProperty lockProp = serializedObject.FindProperty("_lockWaypoints");

            DrawWaypointInfo(lockProp);
            DrawWaypointLock(lockProp);
            DrawWaypointList(waypointsProp, lockProp);

            if (Event.current.type == EventType.MouseDown)
            {
                Rect listRect = GUILayoutUtility.GetLastRect();

                if (!listRect.Contains(Event.current.mousePosition))
                {
                    waypointList.index = -1;
                    Repaint();
                }
            }

            ButtonStyle();

            EditorGUILayout.Space(5);

            Color newColor = EditorGUILayout.ColorField("Theme Color", ThemeColor);

            if (newColor != ThemeColor)
            {
                ThemeColor = newColor;
                Repaint();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(5);

            serializedObject.ApplyModifiedProperties();
        }

        private void SetupWaypointList(SerializedProperty waypointsProp)
        {
            waypointList = new ReorderableList(serializedObject, waypointsProp, true, true, false, false);

            waypointList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Waypoints");
            };

            waypointList.drawElementCallback = (rect, index, active, focused) =>
            {
                DrawWaypointElement(rect, index, active);
            };

            waypointList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                DrawWaypointBackground(rect, index, active);
            };

            waypointList.onReorderCallback = list =>
            {
                serializedObject.ApplyModifiedProperties();
                ReorderChildren(list.serializedProperty);
            };
        }

        private void DrawWaypointInfo(SerializedProperty lockProp)
        {
            EditorGUILayout.HelpBox(
                lockProp.boolValue
                    ? "Waypoints are auto-synced from child objects. Modify the hierarchy to change the path."
                    : "Manual mode: reorder waypoints here to update child object order.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);
        }

        private void DrawWaypointLock(SerializedProperty lockProp)
        {
            Rect full = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));

            float labelWidth = EditorGUIUtility.labelWidth;

            Rect labelRect = new Rect(full.x, full.y, labelWidth, full.height);
            EditorGUI.LabelField(labelRect, "Waypoints");

            float remainingWidth = full.width - labelWidth;
            float buttonWidth = Mathf.Clamp(remainingWidth, 160f, 320f);

            Rect rect = new Rect(
                full.x + labelWidth + (remainingWidth - buttonWidth) * 0.5f,
                full.y,
                buttonWidth,
                full.height
            );

            bool hovered = rect.Contains(Event.current.mousePosition);

            bool isLocked = lockProp.boolValue;

            Color top = isLocked
                ? new Color(0.70f, 0.35f, 0.35f)
                : ThemeColor;

            Color bottom = isLocked
                ? new Color(0.40f, 0.18f, 0.18f)
                : ThemeColor * 0.6f;

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

            Texture icon = isLocked ? iconLockLocked : iconLockUnlocked;

            Color baseColor = isLocked
                ? new Color(0.70f, 0.35f, 0.35f)
                : ThemeColor;

            Color textColor = GetContrastColor(baseColor);

            GUIStyle textStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                normal = { textColor = textColor }
            };
            if (icon != null)
            {
                float iconSize = Mathf.Min(24f, contentRect.height - 6f);

                Rect iconRect = new Rect(contentRect.x, contentRect.y + (contentRect.height - iconSize) * 0.5f, iconSize, iconSize);

                Color prev = GUI.color;
                Color shadow = new Color(0f, 0f, 0f, 0.9f);

                GUI.color = shadow;
                GUI.DrawTexture(new Rect(iconRect.x + 1, iconRect.y + 1, iconRect.width, iconRect.height), icon, ScaleMode.ScaleToFit);

                GUI.color = textColor;
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                GUI.color = prev;

                Rect textRect = new Rect(contentRect.x + iconSize + 6, contentRect.y, contentRect.width - iconSize - 6, contentRect.height);

                DrawTextWithOutline(textRect, AddSpacing(isLocked ? "Locked" : "Unlocked"), textStyle, textColor);
            }
            else
            {
                EditorGUI.LabelField(contentRect, isLocked ? "Locked" : "Unlocked", textStyle);
            }

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                lockProp.boolValue = !lockProp.boolValue;
                serializedObject.ApplyModifiedProperties();
                Event.current.Use();
            }

            EditorGUILayout.Space(5);
        }

        private void DrawTextWithOutline(Rect rect, string text, GUIStyle style, Color textColor)
        {
            Color prev = GUI.color;

            Color shadow = new Color(0f, 0f, 0f, 0.9f);

            GUI.color = shadow;
            EditorGUI.LabelField(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);

            GUI.color = textColor;
            EditorGUI.LabelField(rect, text, style);

            GUI.color = prev;
        }

        private void DrawWaypointList(SerializedProperty waypointsProp, SerializedProperty lockProp)
        {
            EditorGUI.BeginDisabledGroup(lockProp.boolValue);

            waypointList.DoLayoutList();

            EditorGUI.EndDisabledGroup();
        }

        private void DrawWaypointElement(Rect rect, int index, bool active)
        {
            SerializedProperty element = waypointList.serializedProperty.GetArrayElementAtIndex(index);

            rect.y += 2;

            Rect indexRect = new Rect(rect.x + 6, rect.y, 28, EditorGUIUtility.singleLineHeight);
            Rect fieldRect = new Rect(rect.x + 38, rect.y, rect.width - 44, EditorGUIUtility.singleLineHeight);

            bool isLocked = serializedObject.FindProperty("_lockWaypoints").boolValue;

            Color baseColor = active ? ThemeColor : new Color(0.25f, 0.25f, 0.25f);
            Color textColor = GetContrastColor(baseColor);

            Color prev = GUI.color;

            Color badgeTop = active ? ThemeColor : new Color(0.18f, 0.18f, 0.18f);
            Color badgeBottom = active ? ThemeColor * 0.6f : new Color(0.12f, 0.12f, 0.12f);

            EditorGUI.DrawRect(indexRect, badgeBottom);
            EditorGUI.DrawRect(new Rect(indexRect.x, indexRect.y, indexRect.width, indexRect.height * 0.5f), badgeTop);

            GUIStyle badgeStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 11,
                normal = { textColor = textColor }
            };

            DrawTextWithOutline(indexRect, index.ToString(), badgeStyle, textColor);

            if (isLocked)
                GUI.color = new Color(1f, 1f, 1f, 0.6f);

            EditorGUI.PropertyField(fieldRect, element, GUIContent.none);

            GUI.color = prev;
        }
        private void DrawWaypointBackground(Rect rect, int index, bool active)
        {
            bool isLocked = serializedObject.FindProperty("_lockWaypoints").boolValue;

            Color top;
            Color bottom;

            if (!isLocked && active)
            {
                top = ThemeColor;
                bottom = ThemeColor * 0.6f;
            }
            else
            {
                if (index % 2 == 0)
                {
                    top = new Color(0.22f, 0.22f, 0.22f);
                    bottom = new Color(0.16f, 0.16f, 0.16f);
                }
                else
                {
                    top = new Color(0.26f, 0.26f, 0.26f);
                    bottom = new Color(0.18f, 0.18f, 0.18f);
                }
            }

            if (isLocked)
            {
                top = Color.Lerp(top, new Color(0.35f, 0.2f, 0.2f), 0.2f);
                bottom = Color.Lerp(bottom, new Color(0.2f, 0.1f, 0.1f), 0.2f);
            }

            EditorGUI.DrawRect(rect, bottom);

            Rect topRect = new Rect(rect.x, rect.y, rect.width, rect.height * 0.5f);
            EditorGUI.DrawRect(topRect, top);

            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.05f);
            Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.xMax, rect.y));
            Handles.color = new Color(0f, 0f, 0f, 0.5f);
            Handles.DrawLine(new Vector3(rect.x, rect.yMax - 1), new Vector3(rect.xMax, rect.yMax - 1));
            Handles.EndGUI();
        }

        private void ReorderChildren(SerializedProperty waypointsProp)
        {
            for (int i = 0; i < waypointsProp.arraySize; i++)
            {
                var element = waypointsProp.GetArrayElementAtIndex(i);
                Transform t = element.objectReferenceValue as Transform;

                if (t != null)
                {
                    t.SetSiblingIndex(i);
                }
            }
        }
        private string GetHierarchySignature(EnemyPath p)
        {
            if (p == null) return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < p.transform.childCount; i++)
            {
                Transform child = p.transform.GetChild(i);
                sb.Append(child.GetInstanceID());
                sb.Append("|");
            }

            return sb.ToString();
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

            Color baseColor = showProp.boolValue
                ? ThemeColor
                : new Color(0.32f, 0.32f, 0.32f);

            Color top = showProp.boolValue
                ? baseColor
                : new Color(0.32f, 0.32f, 0.32f);

            Color bottom = showProp.boolValue
                ? baseColor * 0.6f
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

            Texture icon = showProp.boolValue ? iconEyeOn : iconEyeOff;

            Color textColor = GetContrastColor(baseColor);

            GUIStyle textStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                normal = { textColor = textColor }
            };

            if (icon != null)
            {
                float iconSize = Mathf.Min(24f, contentRect.height - 6f);

                Rect iconRect = new Rect(contentRect.x, contentRect.y + (contentRect.height - iconSize) * 0.5f, iconSize, iconSize);

                Color prev = GUI.color;
                Color shadow = new Color(0f, 0f, 0f, 0.6f);

                GUI.color = shadow;
                GUI.DrawTexture(new Rect(iconRect.x + 2, iconRect.y + 2, iconRect.width, iconRect.height), icon, ScaleMode.ScaleToFit);

                GUI.color = textColor;
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                GUI.color = prev;

                Rect textRect = new Rect(contentRect.x + iconSize + 6, contentRect.y, contentRect.width - iconSize - 6, contentRect.height);
                DrawTextWithOutline(textRect, AddSpacing(showProp.boolValue ? "Path Visible" : "Path Hidden"), textStyle, textColor);
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

        public static void DrawPath(EnemyPath path)
        {
            if (path == null || path.Count == 0 || !path.ShowPath)
                return;

            float time = (float)EditorApplication.timeSinceStartup * 0.5f;

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 pos = path.GetWaypoint(i);

                Handles.color = Color.yellow;
                Handles.SphereHandleCap(
                    0,
                    pos,
                    Quaternion.identity,
                    HandleUtility.GetHandleSize(pos) * 0.1f,
                    EventType.Repaint
                );

                if (i >= path.Count - 1)
                    continue;

                Vector3 start = pos;
                Vector3 end = path.GetWaypoint(i + 1);

                DrawSegment(path, start, end, i, time);
            }
        }

        private static void DrawSegment(EnemyPath path, Vector3 start, Vector3 end, int index, float time)
        {
            float tColor = index / (float)(path.Count - 1);

            Color baseColor = Color.Lerp(new Color(0.3f, 0.8f, 0.3f), new Color(0.9f, 0.3f, 0.3f), tColor);

            Handles.color = baseColor;
            Handles.DrawAAPolyLine(3f, start, end);

            int segments = 1;

            for (int s = 0; s < segments; s++)
            {
                float t = (s / (float)segments + time) % 1f;

                Vector3 point = Vector3.Lerp(start, end, t);

                float size = HandleUtility.GetHandleSize(point) * 0.04f;

                float fade = Mathf.Sin(t * Mathf.PI);
                Handles.color = new Color(baseColor.r, baseColor.g, baseColor.b, fade * 0.6f);

                Handles.SphereHandleCap(0, point, Quaternion.identity, size, EventType.Repaint);
            }
        }

        private void OnSceneGUI()
        {
            DrawPath(path);
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
                EnemyPathEditor.DrawPath(path);
            }
        }
    }
}