using System.Collections.Generic;
using UnityEngine;

namespace UnityTools.EnemyPath
{
    [System.Serializable]
    public class WaypointNode
    {
        public Transform point;
        public List<int> connections = new List<int>();
    }

    public class EnemyPathGraph : MonoBehaviour
    {
        [SerializeField] private List<WaypointNode> _nodes = new List<WaypointNode>();

        [SerializeField] private float _connectionRadius = 5f;

        [HideInInspector]
        [SerializeField] private bool _showPath = true;

        public bool ShowPath => _showPath;
        public int Count => _nodes.Count;
        public float ConnectionRadius => _connectionRadius;

        public Vector3 GetWaypoint(int index)
        {
            if (index < 0 || index >= _nodes.Count || _nodes[index].point == null)
                return transform.position;

            return _nodes[index].point.position;
        }

        public List<int> GetConnections(int index)
        {
            if (index < 0 || index >= _nodes.Count)
                return null;

            return _nodes[index].connections;
        }

        public void GenerateConnections()
        {
            float maxDist = _connectionRadius * 2f;

            for (int i = 0; i < _nodes.Count; i++)
                _nodes[i].connections.Clear();

            for (int i = 0; i < _nodes.Count; i++)
            {
                for (int j = i + 1; j < _nodes.Count; j++)
                {
                    if (_nodes[i].point == null || _nodes[j].point == null)
                        continue;

                    float dist = Vector3.Distance(
                        _nodes[i].point.position,
                        _nodes[j].point.position
                    );

                    if (dist <= maxDist)
                    {
                        _nodes[i].connections.Add(j);
                        _nodes[j].connections.Add(i);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            GenerateConnections();
        }
#endif
    }
}