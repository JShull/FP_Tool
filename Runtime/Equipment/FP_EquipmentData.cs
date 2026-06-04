// Copyright (c) 2026 John B. Shull
// FuzzPhyte LLC is a company associated with John B. Shull
// This file is part of FP_Tool Package.
//
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
//
// See LICENSE.md COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using FuzzPhyte.Utility;
    [CreateAssetMenu(fileName = "FP Equipment Data", menuName = "FuzzPhyte/Tools/Equipment", order = 20)]
    public class FP_EquipmentData:FP_Data
    {
        public string DisplayName;

        [Header("Capabilities")]
        public bool SupportsTimer;
        public bool SupportsFill;     // sink/kettle/blender
        public bool SupportsHeat;     // burner/oven/toaster
        public int SubUnitCount;      // stove burners count, etc.

        [Header("Fill/Capacity")]
        public float MaxVolumeLiters = 1f;

        [Header("Heat")]
        public float MaxHeatLevel = 1f; // normalized scale mapping
    }
}
