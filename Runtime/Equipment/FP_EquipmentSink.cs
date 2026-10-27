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
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable] public class SinkEvent : UnityEvent<FP_EquipmentSink> { }

    /// <summary>Two independent water knobs; equipment power represents water flow.</summary>
    public class FP_EquipmentSink : FP_EquipmentTickableBase
    {
        private bool _hotWaterOn;
        private bool _coldWaterOn;
        private bool _containsLiquid;

        public bool IsHotWaterOn => _hotWaterOn;
        public bool IsColdWaterOn => _coldWaterOn;
        public bool IsRunning => _status.Power == EquipmentPowerState.On;

        [Header("Sink Events")]
        public SinkEvent OnStarted = new SinkEvent();
        public SinkEvent OnStopped = new SinkEvent();
        public SinkEvent OnHotWaterOn = new SinkEvent();
        public SinkEvent OnHotWaterOff = new SinkEvent();
        public SinkEvent OnColdWaterOn = new SinkEvent();
        public SinkEvent OnColdWaterOff = new SinkEvent();
        public SinkEvent OnBroken = new SinkEvent();
        public SinkEvent OnRepaired = new SinkEvent();
        public SinkEvent OnStateChanged = new SinkEvent();

        protected override void BuildDefaultTransitions()
        {
            powerTransitions = new Dictionary<EquipmentPowerState, HashSet<EquipmentPowerState>>
            {
                { EquipmentPowerState.Off, new HashSet<EquipmentPowerState> { EquipmentPowerState.On } },
                { EquipmentPowerState.On, new HashSet<EquipmentPowerState> { EquipmentPowerState.Off } }
            };
        }

        public void SetHotWaterOn(bool on) => SetKnobs(on, _coldWaterOn);
        public void SetColdWaterOn(bool on) => SetKnobs(_hotWaterOn, on);
        public void ToggleHotWater() => SetHotWaterOn(!_hotWaterOn);
        public void ToggleColdWater() => SetColdWaterOn(!_coldWaterOn);

        private void SetKnobs(bool hot, bool cold)
        {
            if (!isActiveAndEnabled && (hot || cold)) return;
            if (_status.Condition == EquipmentConditionState.Broken && (hot || cold)) return;
            if (_hotWaterOn == hot && _coldWaterOn == cold) return;

            bool hotChanged = _hotWaterOn != hot;
            bool coldChanged = _coldWaterOn != cold;
            bool wasRunning = IsRunning;
            _hotWaterOn = hot;
            _coldWaterOn = cold;

            var power = hot || cold ? EquipmentPowerState.On : EquipmentPowerState.Off;
            if (!SetPower(power)) Emit(); // A knob can change while overall flow stays on.

            if (hotChanged) (hot ? OnHotWaterOn : OnHotWaterOff)?.Invoke(this);
            if (coldChanged) (cold ? OnColdWaterOn : OnColdWaterOff)?.Invoke(this);
            if (wasRunning != IsRunning) (IsRunning ? OnStarted : OnStopped)?.Invoke(this);
        }

        /// <summary>Tracks basin contents explicitly; running water does not accumulate automatically.</summary>
        public void SetContainsLiquid(bool containsLiquid)
        {
            if (_containsLiquid == containsLiquid) return;
            _containsLiquid = containsLiquid;
            Emit();
        }

        protected override void HandleCommand(EquipmentCommand cmd)
        {
            switch (cmd.Type)
            {
                case EquipmentCommandType.SetPowerOn:
                    SetColdWaterOn(true);
                    break;
                case EquipmentCommandType.SetPowerOff:
                    SetKnobs(false, false);
                    break;
                case EquipmentCommandType.TogglePower:
                    if (IsRunning) SetKnobs(false, false);
                    else SetColdWaterOn(true);
                    break;
                case EquipmentCommandType.Break:
                    _status.Condition = EquipmentConditionState.Broken;
                    if (_hotWaterOn || _coldWaterOn) SetKnobs(false, false);
                    else Emit();
                    OnBroken?.Invoke(this);
                    break;
                case EquipmentCommandType.Repair:
                    if (_status.Condition == EquipmentConditionState.OK) return;
                    _status.Condition = EquipmentConditionState.OK;
                    Emit();
                    OnRepaired?.Invoke(this);
                    break;
            }
        }

        protected override void Emit()
        {
            _status.EmitPowerUnit = 0f;
            _status.Fill = _containsLiquid
                ? (_containedItems.Count > 0 ? EquipmentFillState.MixedContents : EquipmentFillState.ContainsLiquid)
                : (_containedItems.Count > 0 ? EquipmentFillState.ContainsItem : EquipmentFillState.Empty);
            base.Emit();
            foreach (var item in _containedItems.ToArray())
                item.OnEquipmentStateChanged(this, _status);
            OnStateChanged?.Invoke(this);
        }

        protected override void OnDisable()
        {
            SetKnobs(false, false);
            base.OnDisable();
        }

        // Flow does not imply heating/energy transfer to existing energy-reactive items.
        protected override void UpdateEmittedPowerUnit(float newValue) => _status.EmitPowerUnit = 0f;
    }
}
