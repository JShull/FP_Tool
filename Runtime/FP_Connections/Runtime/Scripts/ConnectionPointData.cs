namespace FuzzPhyte.Tools.Connections
{
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
        [Tooltip("For the Trigger Box we will be adjusting - size")]
        public Vector3 TriggerSize;
        [Tooltip("For the Trigger box we will be adjusting - center")]
        public Vector3 TriggerCenter;
        [Tooltip("How far in what direction are we located at?")]
        public Vector3 LocalRelativePivotPosition;
        public List<Vector3> localRotationAngles;
        public float AngleTolerance=30f;
        public float InitAlignmentTolerance = 20f;
        public string TriggerTagCompare = "Part";
        [Range(0.01f, 1f)]
        public float PercentOfMeasure = 0.2f;
        [Space]
        [Tooltip("Connectors for things like bolts/screws etc.")]
        public List<Vector3> localConnectors;
    }
}
