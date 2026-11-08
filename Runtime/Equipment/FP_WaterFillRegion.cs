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
        [Tooltip("Assign exactly one target: Coffee Maker, Kettle, or Cup.")]
        [SerializeField] private FP_EquipmentKettle kettle;
        [SerializeField] private FP_CupFill cup;
        public BoxCollider RegionCollider => GetComponent<BoxCollider>();

        /// <summary>True means the particle was caught, including when the reservoir is already full.</summary>
        public bool TryReceiveWater(float milliliters) => TryReceiveWater(milliliters, FP_CupLiquidType.Type3);

        public bool TryReceiveWater(float milliliters, FP_CupLiquidType inputType)
        {
            if (!isActiveAndEnabled ||
                !RegionCollider.enabled || !RegionCollider.isTrigger || milliliters <= 0f ||
                float.IsNaN(milliliters) || float.IsInfinity(milliliters)) return false;
            int targets = (coffeeMaker != null ? 1 : 0) + (kettle != null ? 1 : 0) + (cup != null ? 1 : 0);
            if (targets != 1) return false;
            if (cup != null) return cup.TryAddLiquid(inputType, milliliters);
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
            cup = GetComponentInParent<FP_CupFill>();
        }
    }
}
