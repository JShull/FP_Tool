// Copyright (c) 2026 John B. Shull
// FuzzPhyte LLC is a company associated with John B. Shull
// This file is part of FP_Tool Package.
//
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
//
// See LICENSE.md COMMERCIAL-LICENSE.md, and NOTICE.md.

using FuzzPhyte.Utility;
using UnityEngine;

namespace FuzzPhyte.Tools.Samples
{
    [CreateAssetMenu(fileName = "FP_AttachToolData", menuName = "FuzzPhyte/Tools/FPAttachData")]
    public class FP_AttachToolData : FP_Data
    {
        public GameObject AttachVisual;
        public float RaycastMax = 15;
    }
}
