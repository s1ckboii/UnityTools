using UnityEditor;
using UnityEngine;

namespace UnityTools.Editor
{
    public static class UnityToolsPreferences
    {
        private const string GlobalKey = "UnityTools_GlobalThemeColor";
        private static readonly Color DefaultColor = new Color(0.30f, 0.60f, 1f);

        public static Color GlobalThemeColor
        {
            get
            {
                if (!EditorPrefs.HasKey(GlobalKey))
                    return DefaultColor;

                return StringToColor(EditorPrefs.GetString(GlobalKey));
            }
            set => EditorPrefs.SetString(GlobalKey, ColorToString(value));
        }

        public static Color GetColor(string key, bool useGlobal)
        {
            if (useGlobal)
                return GlobalThemeColor;

            if (!EditorPrefs.HasKey(key))
                return GlobalThemeColor;

            return StringToColor(EditorPrefs.GetString(key));
        }

        public static void SetColor(string key, Color color)
        {
            EditorPrefs.SetString(key, ColorToString(color));
        }

        public static bool GetUseGlobal(string key)
        {
            return EditorPrefs.GetBool(key, true);
        }

        public static void SetUseGlobal(string key, bool value)
        {
            EditorPrefs.SetBool(key, value);
        }

        private static string ColorToString(Color c)
        {
            return $"{c.r}|{c.g}|{c.b}|{c.a}";
        }

        private static Color StringToColor(string s)
        {
            var p = s.Split('|');
            return new Color(
                float.Parse(p[0]),
                float.Parse(p[1]),
                float.Parse(p[2]),
                float.Parse(p[3])
            );
        }

        private static void ForceRepaintAll()
        {
            SceneView.RepaintAll();
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                w.Repaint();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Preferences/Unity Tools", SettingsScope.User)
            {
                guiHandler = (searchContext) =>
                {
                    GUILayout.Space(10);

                    EditorGUI.BeginChangeCheck();
                    Color newGlobal = EditorGUILayout.ColorField("Global Theme Color", GlobalThemeColor);
                    if (EditorGUI.EndChangeCheck())
                    {
                        GlobalThemeColor = newGlobal;
                        ForceRepaintAll();
                    }

                    GUILayout.Space(5);

                    if (GUILayout.Button("Reset to Default"))
                    {
                        GlobalThemeColor = DefaultColor;
                        ForceRepaintAll();
                    }
                }
            };

            return provider;
        }
    }
}