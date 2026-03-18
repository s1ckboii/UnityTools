using System.Collections.Generic;
using UnityEngine;

namespace UnityTools.EnemyPath
{
    public class EnemyPath : MonoBehaviour
    {
        [SerializeField] private List<Transform> _waypoints = new List<Transform>();
        [SerializeField] private bool _showPath = true;
        public bool ShowPath => _showPath;

        public int Count => _waypoints.Count;

        [SerializeField] private bool _lockWaypoints = true;
        public bool LockWaypoints => _lockWaypoints;

        public Vector3 GetWaypoint(int index)
        {
            if (index < 0 || index >= _waypoints.Count)
                return transform.position;

            return _waypoints[index].position;
        }
    }
}
