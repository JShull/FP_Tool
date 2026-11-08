// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using UnityEngine;

    [RequireComponent(typeof(BoxCollider)), DisallowMultipleComponent]
    public class FP_WaterFillRegion : MonoBehaviour
    {
        [SerializeField] private FP_EquipmentCoffeeMaker coffeeMaker;
        [Tooltip("Assign either Coffee Maker or Kettle, not both.")]
        [SerializeField] private FP_EquipmentKettle kettle;
        public BoxCollider RegionCollider => GetComponent<BoxCollider>();

        /// <summary>True means the particle was caught, including when the reservoir is already full.</summary>
        public bool TryReceiveWater(float milliliters)
        {
            if (!isActiveAndEnabled ||
                !RegionCollider.enabled || !RegionCollider.isTrigger || milliliters <= 0f ||
                float.IsNaN(milliliters) || float.IsInfinity(milliliters)) return false;
            if (coffeeMaker != null && kettle != null) return false;
            if (kettle != null && kettle.isActiveAndEnabled) { kettle.AddWaterMl(milliliters); return true; }
            if (coffeeMaker != null && coffeeMaker.isActiveAndEnabled) { coffeeMaker.AddWaterMl(milliliters); return true; }
            return false;
        }

        private void Awake() => RegionCollider.isTrigger = true;
        private void Reset()
        {
            RegionCollider.isTrigger = true;
            coffeeMaker = GetComponentInParent<FP_EquipmentCoffeeMaker>();
            kettle = GetComponentInParent<FP_EquipmentKettle>();
        }
    }
}
