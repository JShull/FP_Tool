// Copyright (c) 2026 John B. Shull
// FuzzPhyte LLC is a company associated with John B. Shull
// This file is part of FP_Tool Package.
//
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
//
// See LICENSE.md COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Connections
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Class to hold data for my ConnectableItem.cs Mono
    /// </summary>
    [Serializable]
    public class ConnectableData 
    {
        public GameObject Prefab;
        public int StoredUniqueIndex;
        public Vector3 LocalPivotPosition;
        public List<Vector3> LocalConnectionPointLocations = new List<Vector3>();
        public float ConnectionDistance = 1.5f;
        public ConnectableData(GameObject prefab, int storedIndexPosition, Vector3 localPivotPosition, List<Vector3> localConnectionPointLocations, float connectionDistance)
        {
            Prefab = prefab;
            StoredUniqueIndex = storedIndexPosition;
            LocalPivotPosition = localPivotPosition;
            LocalConnectionPointLocations = localConnectionPointLocations;
            ConnectionDistance = connectionDistance;
        }
    }
}
