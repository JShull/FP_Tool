// Copyright (c) 2026 John B. Shull
// FuzzPhyte LLC is a company associated with John B. Shull
// This file is part of FP_Tool Package.
//
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
//
// See LICENSE.md COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Samples
{
    using UnityEngine;
    using FuzzPhyte.Utility;
    [CreateAssetMenu(fileName = "FP_DetachToolData", menuName = "FuzzPhyte/Tools/FPDetachData")]
    public class FP_DetachToolData : FP_Data
    {
        public float RaycastMax = 15;
    }
}