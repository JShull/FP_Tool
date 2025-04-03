namespace FuzzPhyte.Connections
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using FuzzPhyte.Utility;
    [CreateAssetMenu(fileName = "FP_ConnectionPointData", menuName = "FuzzPhyte/Connections/ConnectionPoint")]
    public class ConnectionPointData : FP_Data
    {
        public string connectionType;
        public float width;
        [Tooltip("For scaling gizmos")]
        public UnitOfMeasure ConnectionUnitOfMeasure;
        [Tooltip("Will be used for alignment and Connectors")]
        public Vector3 localForward;
        public List<Vector3> localRotationAngles;

        [Space]
        [Tooltip("Connectors for things like bolts/screws etc.")]
        public List<Vector3> localConnectors;
    }
}
