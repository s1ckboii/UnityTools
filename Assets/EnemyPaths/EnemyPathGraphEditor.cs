using UnityEditor;
using UnityEngine;

namespace UnityTools.EnemyPaths
{
    [CustomEditor(typeof(EnemyGraphPath))]
    public class EnemyGraphPathEditor : Editor
    {
        private EnemyGraphPath path;

        private void OnEnable()
        {
            path = (EnemyGraphPath)target;
            Refresh();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Regenerate Connections"))
            {
                path.GenerateConnections();
                EditorUtility.SetDirty(path);
            }

            Refresh();
        }

        private void Refresh()
        {
            var so = new SerializedObject(path);
            var prop = so.FindProperty("_nodes");

            prop.ClearArray();

            for (int i = 0; i < path.transform.childCount; i++)
            {
                prop.InsertArrayElementAtIndex(i);

                var element = prop.GetArrayElementAtIndex(i);
                var pointProp = element.FindPropertyRelative("point");

                pointProp.objectReferenceValue = path.transform.GetChild(i);
            }

            so.ApplyModifiedProperties();

            path.GenerateConnections();
        }

        private void OnSceneGUI()
        {
            if (path == null || path.Count == 0 || !path.ShowPath)
                return;

            float maxDist = path.ConnectionRadius * 2f;

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 pos = path.GetWaypoint(i);

                // Node
                Handles.color = Color.yellow;
                Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);

                // Disc (keep your visual)
                Handles.color = new Color(0, 1, 1, 0.1f);
                Handles.DrawSolidDisc(pos, Vector3.up, path.ConnectionRadius);

                var connections = path.GetConnections(i);
                if (connections == null) continue;

                foreach (var targetIndex in connections)
                {
                    if (targetIndex < i) continue;

                    Vector3 targetPos = path.GetWaypoint(targetIndex);

                    float dist = Vector3.Distance(pos, targetPos);

                    float t = Mathf.InverseLerp(0f, maxDist, dist);


                    Color lineColor = Color.Lerp(Color.yellow, Color.red, t);

                    Handles.color = lineColor;
                    Handles.DrawLine(pos, targetPos);
                }
            }
        }
    }
}