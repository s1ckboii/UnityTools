using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityTools.Runtime.EnemyPaths;

namespace UnityTools.Editor.EnemyPaths
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EnemyGraphPath))]
    public class EnemyGraphPathEditor : UnityEditor.Editor
    {
        private EnemyGraphPath graph;

        private Texture2D iconEyeOn;
        private Texture2D iconEyeOff;
        private Texture2D iconLockLocked;
        private Texture2D iconLockUnlocked;

        private ReorderableList nodeList;

        private static readonly string ShowGraphKey = "EnemyPathGraph_ShowGraph";
        private static readonly string LockNodesKey = "EnemyPathGraph_LockNodes";
        private static readonly string ShowDiscsKey = "EnemyPathGraph_ShowDiscs";
        private static readonly string ShowLinesKey = "EnemyPathGraph_ShowLines";
        private static readonly string ThemeKey = "EnemyPathGraphEditor_ThemeColor";
        private static bool ShowGraph
        {
            get => EditorPrefs.GetBool(ShowGraphKey, true);
            set => EditorPrefs.SetBool(ShowGraphKey, value);
        }

        private static bool LockNodes
        {
            get => EditorPrefs.GetBool(LockNodesKey, true);
            set => EditorPrefs.SetBool(LockNodesKey, value);
        }
        private static bool ShowDiscs
        {
            get => EditorPrefs.GetBool(ShowDiscsKey, true);
            set => EditorPrefs.SetBool(ShowDiscsKey, value);
        }

        private static bool ShowLines
        {
            get => EditorPrefs.GetBool(ShowLinesKey, true);
            set => EditorPrefs.SetBool(ShowLinesKey, value);
        }
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

        private readonly System.Collections.Generic.Dictionary<EnemyGraphPath, string> hierarchySignatures =
            new System.Collections.Generic.Dictionary<EnemyGraphPath, string>();

        private void OnEnable()
        {
            graph = (EnemyGraphPath)target;
            Refresh();

            hierarchySignatures.Clear();

            foreach (var t in targets)
            {
                EnemyGraphPath g = (EnemyGraphPath)t;
                if (g != null)
                {
                    hierarchySignatures[g] = GetHierarchySignature(g);
                }
            }

            iconEyeOn = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/eye_open.png");
            iconEyeOff = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/eye_closed.png");

            iconLockLocked = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/lock_locked.png");
            iconLockUnlocked = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/Icons/lock_unlocked.png");

            SerializedProperty nodesProp = serializedObject.FindProperty("_nodes");
            SetupNodeList(nodesProp);

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            if (nodeList != null)
                nodeList.index = -1;
        }

        private void OnHierarchyChanged()
        {
            bool needsRefresh = false;

            foreach (var t in targets)
            {
                EnemyGraphPath g = (EnemyGraphPath)t;
                if (g == null) continue;

                string newSignature = GetHierarchySignature(g);

                if (!hierarchySignatures.TryGetValue(g, out string oldSignature))
                {
                    hierarchySignatures[g] = newSignature;
                    needsRefresh = true;
                    continue;
                }

                if (newSignature != oldSignature)
                {
                    hierarchySignatures[g] = newSignature;
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

            SerializedProperty nodesProp = serializedObject.FindProperty("_nodes");
            bool lockValue = LockNodes;
            SerializedProperty distProp = serializedObject.FindProperty("_connectionDistance");

            EditorGUILayout.PropertyField(distProp);

            DrawNodeInfo();
            DrawNodeLock();
            DrawNodeList(nodesProp);

            if (Event.current.type == EventType.MouseDown)
            {
                Rect listRect = GUILayoutUtility.GetLastRect();

                if (!listRect.Contains(Event.current.mousePosition))
                {
                    nodeList.index = -1;
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

        private void SetupNodeList(SerializedProperty nodesProp)
        {
            nodeList = new ReorderableList(serializedObject, nodesProp, true, true, false, false);

            nodeList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Nodes");
            };

            nodeList.drawElementCallback = (rect, index, active, focused) =>
            {
                DrawNodeElement(rect, index, active);
            };

            nodeList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                DrawNodeBackground(rect, index, active);
            };

            nodeList.onReorderCallback = list =>
            {
                serializedObject.ApplyModifiedProperties();
                ReorderChildren(list.serializedProperty);
            };
        }

        private void DrawNodeInfo()
        {
            EditorGUILayout.HelpBox(LockNodes
                    ? "Nodes are auto-synced from child objects. Modify the hierarchy to change the graph."
                    : "Manual mode: reorder nodes here to update child object order.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);
        }

        private void DrawNodeLock()
        {
            Rect full = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));

            float labelWidth = EditorGUIUtility.labelWidth;

            Rect labelRect = new Rect(full.x, full.y, labelWidth, full.height);
            EditorGUI.LabelField(labelRect, "Nodes");

            float remainingWidth = full.width - labelWidth;
            float buttonWidth = Mathf.Clamp(remainingWidth, 160f, 320f);

            Rect rect = new Rect(
                full.x + labelWidth + (remainingWidth - buttonWidth) * 0.5f,
                full.y,
                buttonWidth,
                full.height
            );

            bool hovered = rect.Contains(Event.current.mousePosition);

            bool isLocked = LockNodes;

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
                LockNodes = !LockNodes;
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

        private void DrawNodeList(SerializedProperty nodesProp)
        {
            EditorGUI.BeginDisabledGroup(LockNodes);
            nodeList.DoLayoutList();
            EditorGUI.EndDisabledGroup();
        }

        private void DrawNodeElement(Rect rect, int index, bool active)
        {
            SerializedProperty element = nodeList.serializedProperty.GetArrayElementAtIndex(index);

            rect.y += 2;

            Rect indexRect = new Rect(rect.x + 6, rect.y, 28, EditorGUIUtility.singleLineHeight);
            Rect fieldRect = new Rect(rect.x + 38, rect.y, rect.width - 44, EditorGUIUtility.singleLineHeight);

            bool isLocked = LockNodes;

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

        private void DrawNodeBackground(Rect rect, int index, bool active)
        {
            bool isLocked = LockNodes;

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

        private void ReorderChildren(SerializedProperty nodesProp)
        {
            for (int i = 0; i < nodesProp.arraySize; i++)
            {
                var element = nodesProp.GetArrayElementAtIndex(i);
                Transform t = element.objectReferenceValue as Transform;

                if (t != null)
                {
                    t.SetSiblingIndex(i);
                }
            }
        }

        private string GetHierarchySignature(EnemyGraphPath g)
        {
            if (g == null) return "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < g.transform.childCount; i++)
            {
                Transform child = g.transform.GetChild(i);
                sb.Append(child.GetInstanceID());
                sb.Append("|");
            }

            return sb.ToString();
        }

        private void ButtonStyle()
        {
            EditorGUILayout.Space(10);

            bool showGraph = ShowGraph;

            Rect full = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));

            float labelWidth = EditorGUIUtility.labelWidth;

            Rect labelRect = new Rect(full.x, full.y, labelWidth, full.height);
            EditorGUI.LabelField(labelRect, "Visualization");

            float remainingWidth = full.width - labelWidth;
            float buttonWidth = Mathf.Clamp(remainingWidth, 160f, 320f);

            Rect rect = new Rect(full.x + labelWidth + (remainingWidth - buttonWidth) * 0.5f, full.y, buttonWidth, full.height);

            bool hovered = rect.Contains(Event.current.mousePosition);

            Color baseColor = showGraph
                ? ThemeColor
                : new Color(0.32f, 0.32f, 0.32f);

            Color top = showGraph
                ? baseColor
                : new Color(0.32f, 0.32f, 0.32f);

            Color bottom = showGraph
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

            Texture icon = showGraph ? iconEyeOn : iconEyeOff;

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
                DrawTextWithOutline(textRect, AddSpacing(showGraph ? "Graph Visible" : "Graph Hidden"), textStyle, textColor);
            }
            else
            {
                EditorGUI.LabelField(contentRect, showGraph ? "Graph Visible" : "Graph Hidden", textStyle);
            }

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                ShowGraph = !ShowGraph;
                SceneView.RepaintAll();
                Event.current.Use();
            }

            EditorGUILayout.Space(5);

            Rect row = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));

            float startX = row.x + labelWidth + (remainingWidth - buttonWidth) * 0.5f;

            float half = (buttonWidth - 6f) * 0.5f;

            Rect left = new Rect(startX, row.y, half, row.height);
            Rect right = new Rect(startX + half + 6f, row.y, half, row.height);

            DrawMiniToggle(left, () => ShowDiscs, v => ShowDiscs = v, "Discs");
            DrawMiniToggle(right, () => ShowLines, v => ShowLines = v, "Lines");
        }

        private void DrawMiniToggle(Rect rect, System.Func<bool> getter, System.Action<bool> setter, string label)
        {
            bool value = getter();

            bool hovered = rect.Contains(Event.current.mousePosition);

            Color baseColor = value ? ThemeColor : new Color(0.28f, 0.28f, 0.28f);

            Color top = value ? baseColor : new Color(0.28f, 0.28f, 0.28f);
            Color bottom = value ? baseColor * 0.6f : new Color(0.16f, 0.16f, 0.16f);

            if (hovered)
            {
                top *= 1.1f;
                bottom *= 1.1f;
            }

            EditorGUI.DrawRect(rect, bottom);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height * 0.5f), top);

            Color textColor = GetContrastColor(baseColor);

            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                normal = { textColor = textColor }
            };

            DrawTextWithOutline(rect, label.ToUpper(), style, textColor);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                setter(!value);
                SceneView.RepaintAll();
                Event.current.Use();
            }
        }

        private void Refresh()
        {
            foreach (var t in targets)
            {
                EnemyGraphPath g = (EnemyGraphPath)t;

                var so = new SerializedObject(g);
                var prop = so.FindProperty("_nodes");

                prop.ClearArray();

                for (int i = 0; i < g.transform.childCount; i++)
                {
                    prop.InsertArrayElementAtIndex(i);
                    prop.GetArrayElementAtIndex(i).objectReferenceValue =
                        g.transform.GetChild(i);
                }

                so.ApplyModifiedProperties();
            }
        }

        public static void DrawGraph(EnemyGraphPath graph)
        {
            if (graph == null || graph.Count == 0 || !ShowGraph)
                return;

            float dist = graph.ConnectionDistance;

            for (int i = 0; i < graph.Count; i++)
            {
                Transform node = graph.GetNode(i);
                if (node == null) continue;

                Vector3 pos = node.position;

                Color theme = ThemeColor;

                Color discFill = new Color(theme.r, theme.g, theme.b, 0.08f);
                Color discWire = new Color(theme.r, theme.g, theme.b, 0.6f);

                if (ShowDiscs)
                {
                    Handles.color = discFill;
                    Handles.DrawSolidDisc(pos, Vector3.up, dist);

                    Handles.color = discWire;
                    Handles.DrawWireDisc(pos, Vector3.up, dist);
                }

                Handles.color = Color.yellow;
                Handles.SphereHandleCap(
                    0,
                    pos,
                    Quaternion.identity,
                    HandleUtility.GetHandleSize(pos) * 0.1f,
                    EventType.Repaint
                );

                if (!ShowLines)
                    continue;

                for (int j = i + 1; j < graph.Count; j++)
                {
                    Transform other = graph.GetNode(j);
                    if (other == null) continue;

                    float d = Vector3.Distance(pos, other.position);

                    if (d <= dist * 2f)
                    {
                        Color baseColor;

                        if (d <= dist)
                        {
                            float t = d / dist;
                            baseColor = Color.Lerp(Color.green, Color.yellow, t);
                        }
                        else
                        {
                            float t = (d - dist) / dist;
                            baseColor = Color.Lerp(Color.yellow, Color.red, t);
                        }

                        Handles.color = baseColor;
                        Handles.DrawAAPolyLine(3f, pos, other.position);
                    }
                }
            }
        }

        private void OnSceneGUI()
        {
            DrawGraph(graph);
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
            var graphs = Object.FindObjectsByType<EnemyGraphPath>(FindObjectsSortMode.None);

            foreach (var graph in graphs)
            {
                EnemyGraphPathEditor.DrawGraph(graph);
            }
        }
    }
}