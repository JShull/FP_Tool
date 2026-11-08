// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable] public class KettleEvent : UnityEvent<FP_EquipmentKettle> { }

    /// <summary>Timed heating and binary hot/cold water; cooling advances through the tick system.</summary>
    public class FP_EquipmentKettle : FP_EquipmentTickableBase
    {
        [Header("Water")]
        [SerializeField, Min(0f)] private float waterCapacityMl = 1000f;
        [SerializeField, Min(0f)] private float waterMl;
        [SerializeField] private bool waterHot;
        [Header("Timers")]
        [SerializeField, Min(0.01f)] private float heatingSeconds = 10f;
        [SerializeField, Min(0.01f)] private float coolingSeconds = 120f;
        private float _heatDuration;
        private float _coolingRemaining;
        private bool _started;
        private bool _stopping;
        private uint _cycleVersion;
        private bool _reported;
        private float _lastWater;
        private bool _lastHot, _lastFull;
        private uint _notificationVersion;

        public float WaterMl => waterMl;
        public bool HasWater => waterMl > 0f;
        public bool IsWaterFull => waterCapacityMl > 0f && waterMl >= waterCapacityMl;
        public bool IsWaterHot => HasWater && waterHot;
        public bool IsWaterCold => HasWater && !waterHot;
        public bool IsHeating => _status.Power == EquipmentPowerState.OnWithTimer;
        public bool IsPouring { get; private set; }
        public bool CanPour => isActiveAndEnabled && HasWater && _status.Condition == EquipmentConditionState.OK;
        public float CoolingRemainingSeconds => _coolingRemaining;
        public float HeatingProgress01 => _status.NormalizedLevel;

        [Header("Kettle Events")]
        public KettleEvent OnStarted = new KettleEvent();
        public KettleEvent OnHeating = new KettleEvent();
        public KettleEvent OnHeatingProgress = new KettleEvent();
        public KettleEvent OnFinished = new KettleEvent();
        public KettleEvent OnCancelled = new KettleEvent();
        public KettleEvent OnNoWater = new KettleEvent();
        public KettleEvent OnWaterEmpty = new KettleEvent();
        public KettleEvent OnWaterAvailable = new KettleEvent();
        public KettleEvent OnWaterFull = new KettleEvent();
        public KettleEvent OnWaterChanged = new KettleEvent();
        public KettleEvent OnWaterHot = new KettleEvent();
        public KettleEvent OnWaterCold = new KettleEvent();
        public KettleEvent OnPourStarted = new KettleEvent();
        public KettleEvent OnPourStopped = new KettleEvent();
        public KettleEvent OnBroken = new KettleEvent();
        public KettleEvent OnRepaired = new KettleEvent();
        public KettleEvent OnStateChanged = new KettleEvent();

        protected override void InitializeStatus()
        {
            waterMl = ClampWater(waterMl);
            waterHot = HasWater && waterHot;
            _coolingRemaining = waterHot ? coolingSeconds : 0f;
            _reported = false;
            base.InitializeStatus();
        }

        public void AddWaterMl(float amount)
        {
            if (PositiveFinite(amount)) SetWaterMl((float)Math.Min((double)waterMl + amount, waterCapacityMl));
        }

        public void SetWaterMl(float amount)
        {
            float next = ClampWater(amount);
            if (waterMl == next) return;
            bool added = next > waterMl;
            waterMl = next;
            if (added || !HasWater)
            {
                waterHot = false;
                _coolingRemaining = 0f;
                CancelHeating();
            }
            if (!HasWater) SetPouringState(false);
            Emit();
        }

        public float RemoveWaterMl(float amount)
        {
            if (!PositiveFinite(amount)) return 0f;
            float removed = Mathf.Min(waterMl, amount);
            SetWaterMl(waterMl - removed);
            return removed;
        }

        public void StartHeating() => StartHeatingForSeconds(heatingSeconds);

        public void StartHeatingForSeconds(float seconds)
        {
            if (!isActiveAndEnabled || _stopping || IsHeating || IsPouring ||
                _status.Condition != EquipmentConditionState.OK || !PositiveFinite(seconds)) return;
            if (!HasWater) { OnNoWater?.Invoke(this); return; }
            _heatDuration = seconds;
            _status.TimerRemainingSec = seconds;
            _status.NormalizedLevel = 0f;
            _status.Power = EquipmentPowerState.OnWithTimer;
            uint cycle = ++_cycleVersion;
            Emit();
            if (!IsHeating || cycle != _cycleVersion) return;
            OnStarted?.Invoke(this);
            if (IsHeating && cycle == _cycleVersion) OnHeating?.Invoke(this);
        }

        public void CancelHeating()
        {
            if (!IsHeating || _stopping) return;
            _stopping = true;
            try
            {
                ++_cycleVersion;
                _status.Power = EquipmentPowerState.Off;
                _status.TimerRemainingSec = 0f;
                _status.NormalizedLevel = 0f;
                Emit();
                OnCancelled?.Invoke(this);
            }
            finally { _stopping = false; }
        }

        internal void SetPouringState(bool pouring)
        {
            pouring = pouring && CanPour;
            if (IsPouring == pouring) return;
            IsPouring = pouring;
            if (pouring) CancelHeating();
            Emit();
            (pouring ? OnPourStarted : OnPourStopped)?.Invoke(this);
        }

        protected override void OnTick(float dt)
        {
            if (!isActiveAndEnabled || !PositiveFinite(dt)) return;
            if (IsHeating)
            {
                uint cycle = _cycleVersion;
                float slice = Mathf.Min(dt, _status.TimerRemainingSec);
                _status.TimerRemainingSec = Mathf.Max(0f, _status.TimerRemainingSec - slice);
                _status.NormalizedLevel = 1f - _status.TimerRemainingSec / _heatDuration;
                Emit();
                if (!IsHeating || cycle != _cycleVersion) return;
                OnHeatingProgress?.Invoke(this);
                if (!IsHeating || cycle != _cycleVersion || _status.TimerRemainingSec > 0f) return;
                _status.Power = EquipmentPowerState.Off;
                waterHot = true;
                _coolingRemaining = coolingSeconds;
                Emit();
                OnFinished?.Invoke(this);
                if (cycle != _cycleVersion || IsHeating) return;
                dt -= slice;
            }
            if (!IsWaterHot || dt <= 0f) return;
            _coolingRemaining = Mathf.Max(0f, _coolingRemaining - dt);
            if (_coolingRemaining == 0f) waterHot = false;
            Emit();
        }

        protected override void Emit()
        {
            _status.EmitPowerUnit = 0f;
            _status.Fill = HasWater ? EquipmentFillState.ContainsLiquid : EquipmentFillState.Empty;
            bool first = !_reported;
            bool changed = first || _lastWater != waterMl;
            bool becameAvailable = HasWater && (first || _lastWater <= 0f);
            bool becameEmpty = !HasWater && (first || _lastWater > 0f);
            bool becameFull = IsWaterFull && (first || !_lastFull);
            bool becameHot = IsWaterHot && (first || !_lastHot);
            bool becameCold = IsWaterCold && (first || _lastHot || _lastWater <= 0f);
            _reported = true;
            _lastWater = waterMl;
            _lastHot = IsWaterHot;
            _lastFull = IsWaterFull;
            uint version = ++_notificationVersion;
            if (changed) OnWaterChanged?.Invoke(this);
            if (version != _notificationVersion) return;
            if (becameAvailable) OnWaterAvailable?.Invoke(this);
            if (version != _notificationVersion) return;
            if (becameEmpty) OnWaterEmpty?.Invoke(this);
            if (version != _notificationVersion) return;
            if (becameFull) OnWaterFull?.Invoke(this);
            if (version != _notificationVersion) return;
            if (becameHot) OnWaterHot?.Invoke(this);
            if (version != _notificationVersion) return;
            if (becameCold) OnWaterCold?.Invoke(this);
            if (version != _notificationVersion) return;
            base.Emit();
            OnStateChanged?.Invoke(this);
        }

        protected override void HandleCommand(EquipmentCommand cmd)
        {
            switch (cmd.Type)
            {
                case EquipmentCommandType.SetPowerOn:
                case EquipmentCommandType.InterfaceActions: StartHeating(); break;
                case EquipmentCommandType.StartTimerSeconds: StartHeatingForSeconds(cmd.FValue); break;
                case EquipmentCommandType.SetPowerOff:
                case EquipmentCommandType.CancelTimer: CancelHeating(); break;
                case EquipmentCommandType.TogglePower:
                    if (IsHeating) CancelHeating(); else StartHeating();
                    break;
                case EquipmentCommandType.Break:
                    _status.Condition = EquipmentConditionState.Broken;
                    CancelHeating();
                    SetPouringState(false);
                    Emit();
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

        protected override void Start() { _started = true; base.Start(); }
        protected override void OnEnable() { base.OnEnable(); if (_started) { base.Start(); Emit(); } }
        protected override void OnDisable() { CancelHeating(); SetPouringState(false); base.OnDisable(); }
        protected override void UpdateEmittedPowerUnit(float value) => _status.EmitPowerUnit = 0f;
        private static bool PositiveFinite(float value) => value > 0f && !float.IsInfinity(value);
        private float ClampWater(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp(value, 0f, waterCapacityMl);
        private void OnValidate()
        {
            waterCapacityMl = PositiveFinite(waterCapacityMl) ? waterCapacityMl : 0f;
            waterMl = ClampWater(waterMl);
            heatingSeconds = PositiveFinite(heatingSeconds) ? heatingSeconds : 10f;
            coolingSeconds = PositiveFinite(coolingSeconds) ? coolingSeconds : 120f;
            if (!HasWater) waterHot = false;
        }
    }
}
