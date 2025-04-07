using FuzzPhyte.Utility;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace FuzzPhyte.Tools.Connections
{

    [Serializable]
    [CreateAssetMenu(fileName = "FP_PartData", menuName = "FuzzPhyte/Connections/PartData")]
    public class PartData : FP_Data
    {
        public int UniquePartID = 0;
        public float PartWidth;
        public GameObject ConnectionEndPrefab;
        [Tooltip("All Connection Points Data")]
        public List<ConnectionPointData> AllConnectionPointsForPart = new List<ConnectionPointData>();
    }
}
