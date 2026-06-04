// Copyright (c) 2026 John B. Shull
// FuzzPhyte LLC is a company associated with John B. Shull
// This file is part of FP_Tool Package.
//
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
//
// See LICENSE.md COMMERCIAL-LICENSE.md, and NOTICE.md.

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
