using System.Collections.Generic;
using UnityEngine;

namespace UnityTools.EnemyPathGraph
{
    public class EnemyPathGraph : MonoBehaviour
    {
        [SerializeField] private List<Transform> _nodes = new List<Transform>();

        [SerializeField] private float _connectionDistance = 5f;
        public float ConnectionDistance => _connectionDistance;

        public int Count => _nodes.Count;

        public Transform GetNode(int index)
        {
            if (index < 0 || index >= _nodes.Count)
                return null;

            return _nodes[index];
        }

        public List<Transform> GetNeighbors(Transform node)
        {
            List<Transform> neighbors = new List<Transform>();

            foreach (var other in _nodes)
            {
                if (other == null || other == node)
                    continue;

                float dist = Vector3.Distance(node.position, other.position);

                if (dist <= _connectionDistance)
                    neighbors.Add(other);
            }

            return neighbors;
        }
    }
}