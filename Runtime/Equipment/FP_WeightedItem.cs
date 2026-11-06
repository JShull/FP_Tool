// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>Authored mass in kilograms, independent of Rigidbody mass.</summary>
    [DisallowMultipleComponent]
    public class FP_WeightedItem : MonoBehaviour, IFPContainedItem
    {
        [SerializeField] private string itemId;
        [SerializeField, Min(0f)] private float weightKg = 1f;
        private readonly HashSet<FP_EquipmentBase> _equipment = new HashSet<FP_EquipmentBase>();

        public string ItemId => string.IsNullOrEmpty(itemId) ? name : itemId;
        public float WeightKg => weightKg;
        public event Action WeightChanged;

        public void SetWeightKg(float value)
        {
            value = Sanitize(value);
            if (weightKg == value) return;
            weightKg = value;
            WeightChanged?.Invoke();
        }

        private static float Sanitize(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);

        private void OnValidate()
        {
            weightKg = Sanitize(weightKg);
            WeightChanged?.Invoke();
        }

        private void OnDisable()
        {
            foreach (var equipment in new List<FP_EquipmentBase>(_equipment))
                if (equipment != null) equipment.RemoveItemFromEquipment(this);
            _equipment.Clear();
        }

        public void OnEnteredEquipment(FP_EquipmentBase equipment) => _equipment.Add(equipment);
        public void OnExitedEquipment(FP_EquipmentBase equipment) => _equipment.Remove(equipment);
        public void OnEquipmentStateChanged(FP_EquipmentBase equipment, EquipmentStatus status) { }
        public void OnEquipmentTick(FP_EquipmentBase equipment, EquipmentStatus status, float dt) { }
    }
}
