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
        public BoxCollider RegionCollider => GetComponent<BoxCollider>();

        /// <summary>True means the particle was caught, including when the reservoir is already full.</summary>
        public bool TryReceiveWater(float milliliters)
        {
            if (!isActiveAndEnabled || coffeeMaker == null || !coffeeMaker.isActiveAndEnabled ||
                !RegionCollider.enabled || !RegionCollider.isTrigger || milliliters <= 0f ||
                float.IsNaN(milliliters) || float.IsInfinity(milliliters)) return false;
            coffeeMaker.AddWaterMl(milliliters);
            return true;
        }

        private void Awake() => RegionCollider.isTrigger = true;
        private void Reset()
        {
            RegionCollider.isTrigger = true;
            coffeeMaker = GetComponentInParent<FP_EquipmentCoffeeMaker>();
        }
    }
}
