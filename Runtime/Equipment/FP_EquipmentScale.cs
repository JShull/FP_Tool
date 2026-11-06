// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable] public class ScaleEvent : UnityEvent<FP_EquipmentScale> { }

    /// <summary>Totals distinct weighted items; no tick system or energy transfer.</summary>
    public class FP_EquipmentScale : FP_EquipmentBase
    {
        private readonly Dictionary<FP_WeightedItem, HashSet<FP_ScaleContainmentVolume>> _volumes =
            new Dictionary<FP_WeightedItem, HashSet<FP_ScaleContainmentVolume>>();

        public double TotalWeightKg { get; private set; }
        public int ItemCount => _containedItems.Count;
        public bool IsReadingAvailable => isActiveAndEnabled &&
            _status.Power == EquipmentPowerState.On && _status.Condition == EquipmentConditionState.OK;
        public ScaleEvent OnWeightChanged = new ScaleEvent();
        public ScaleEvent OnStateChanged = new ScaleEvent();

        protected override void InitializeStatus()
        {
            base.InitializeStatus();
            SetPower(EquipmentPowerState.On);
        }

        protected override void AddContainedItem(IFPContainedItem item)
        {
            if (!(item is FP_WeightedItem weighted) || weighted == null ||
                !weighted.isActiveAndEnabled || _containedItems.Contains(item)) return;
            weighted.WeightChanged += RefreshWeight;
            base.AddContainedItem(item);
        }

        protected override void RemoveContainedItem(IFPContainedItem item)
        {
            if (!(item is FP_WeightedItem weighted)) return;
            weighted.WeightChanged -= RefreshWeight;
            _volumes.Remove(weighted);
            base.RemoveContainedItem(item);
        }

        internal void EnterVolume(FP_WeightedItem item, FP_ScaleContainmentVolume volume)
        {
            if (!isActiveAndEnabled || item == null || !item.isActiveAndEnabled) return;
            if (!_volumes.TryGetValue(item, out var volumes))
            {
                volumes = new HashSet<FP_ScaleContainmentVolume>();
                _volumes.Add(item, volumes);
            }
            volumes.Add(volume);
            AddItemToEquipment(item);
        }

        internal void ExitVolume(FP_WeightedItem item, FP_ScaleContainmentVolume volume)
        {
            if (!_volumes.TryGetValue(item, out var volumes)) return;
            volumes.Remove(volume);
            if (volumes.Count == 0) RemoveItemFromEquipment(item);
        }

        private void RefreshWeight() => Emit();

        protected override void Emit()
        {
            double previous = TotalWeightKg;
            TotalWeightKg = 0d;
            foreach (FP_WeightedItem item in _containedItems)
                if (item != null && item.isActiveAndEnabled) TotalWeightKg += item.WeightKg;
            _status.Fill = ItemCount == 0 ? EquipmentFillState.Empty : EquipmentFillState.ContainsItem;
            _status.EmitPowerUnit = 0f;
            base.Emit();
            foreach (var item in _containedItems.ToArray()) item.OnEquipmentStateChanged(this, _status);
            if (previous != TotalWeightKg) OnWeightChanged?.Invoke(this);
            OnStateChanged?.Invoke(this);
        }

        protected override void HandleCommand(EquipmentCommand cmd)
        {
            switch (cmd.Type)
            {
                case EquipmentCommandType.SetPowerOn: SetPower(EquipmentPowerState.On); break;
                case EquipmentCommandType.SetPowerOff: SetPower(EquipmentPowerState.Off); break;
                case EquipmentCommandType.TogglePower:
                    SetPower(_status.Power == EquipmentPowerState.On ? EquipmentPowerState.Off : EquipmentPowerState.On);
                    break;
                case EquipmentCommandType.Break:
                    _status.Condition = EquipmentConditionState.Broken;
                    _status.Power = EquipmentPowerState.Off;
                    Emit();
                    break;
                case EquipmentCommandType.Repair:
                    _status.Condition = EquipmentConditionState.OK;
                    Emit();
                    break;
            }
        }

        private void OnEnable()
        {
            if (_status.ContainedItemIds != null) Emit();
        }

        private void OnDisable()
        {
            foreach (var item in _containedItems.ToArray()) RemoveItemFromEquipment(item);
            Emit();
        }

        protected override void UpdateEmittedPowerUnit(float newValue) => _status.EmitPowerUnit = 0f;
    }
}
