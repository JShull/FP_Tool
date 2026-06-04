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
    [RequireComponent(typeof(Collider))]
    public class FP_EquipmentContainmentVolume : MonoBehaviour
    {
        [SerializeField] private FP_EquipmentBase equipment;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<IFPContainedItem>(out var item))
                equipment.AddItemToEquipment(item);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<IFPContainedItem>(out var item))
                equipment.RemoveItemFromEquipment(item);
        }
    }
}
