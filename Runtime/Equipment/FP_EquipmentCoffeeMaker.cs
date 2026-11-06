// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable] public class CoffeeMakerEvent : UnityEvent<FP_EquipmentCoffeeMaker> { }

    /// <summary>A timed brew cycle with explicit supplies and Inspector-driven effects.</summary>
    public class FP_EquipmentCoffeeMaker : FP_EquipmentTickableBase
    {
        [Header("Recipe")]
        [SerializeField, Min(0.01f)] private float brewDurationSeconds = 30f;
        [SerializeField, Min(0.01f)] private float beansPerBrewGrams = 15f;
        [SerializeField, Min(0.01f)] private float waterPerBrewMl = 250f;
        [Header("Supplies")]
        [SerializeField, Min(0f)] private float beanCapacityGrams = 250f;
        [SerializeField, Min(0f)] private float waterCapacityMl = 1000f;
        [SerializeField, Min(0f)] private float beansGrams;
        [SerializeField, Min(0f)] private float waterMl;
        [Header("Cup")]
        [SerializeField] private bool requireCup;
        [Tooltip("Used only when Require Cup is enabled. Place this region beneath the spout.")]
        [SerializeField] private FP_CupRegion cupRegion;
        [SerializeField, HideInInspector] private bool cupPresent;
        private bool _hasUsedCupRegion;

        private float _cycleDuration;
        private float _cycleVolumeMl;
        private bool _started;
        private bool _poweringOff;
        private uint _cycleVersion;
        private bool _hadEnoughBeans;
        private bool _wasBeanHopperFull;
        private uint _beanThresholdVersion;
        private bool _hadEnoughWater;
        private bool _wasWaterTankFull;
        private uint _waterThresholdVersion;
        public bool IsBrewing => _status.Power == EquipmentPowerState.OnWithTimer;
        public float BeansGrams => beansGrams;
        public float WaterMl => waterMl;
        public bool HasBeans => beansGrams >= beansPerBrewGrams;
        public bool IsBeanHopperFull => beanCapacityGrams > 0f && beansGrams >= beanCapacityGrams;
        public bool HasWater => waterMl >= waterPerBrewMl;
        public bool IsWaterTankFull => waterCapacityMl > 0f && waterMl >= waterCapacityMl;
        public bool IsCupPresent => cupPresent;
        public float BrewProgress01 => _status.NormalizedLevel;
        public float LastBrewVolumeMl { get; private set; }

        [Header("Coffee Maker Events")]
        public CoffeeMakerEvent OnStarted = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnBrewing = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnBrewProgress = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnFinished = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnCancelled = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnNoBeans = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnNoWater = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnNoCup = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnCupPresent = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnSuppliesChanged = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnEnoughBeans = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnBeansFull = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnEnoughWater = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnWaterFull = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnBroken = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnRepaired = new CoffeeMakerEvent();
        public CoffeeMakerEvent OnStateChanged = new CoffeeMakerEvent();

        public void StartBrewing() => StartBrewingForSeconds(brewDurationSeconds);

        /// <summary>Supplies are consumed on acceptance, including subsequently cancelled brews.</summary>
        public void StartBrewingForSeconds(float seconds)
        {
            if (!isActiveAndEnabled || _poweringOff || IsBrewing || _status.Condition == EquipmentConditionState.Broken ||
                !IsPositiveFinite(seconds)) return;
            RefreshCupPresence();
            if (IsBrewing || _poweringOff || !isActiveAndEnabled ||
                _status.Condition == EquipmentConditionState.Broken) return;
            bool missingBeans = !HasBeans;
            bool missingWater = !HasWater;
            bool missingCup = requireCup && !cupPresent;
            if (missingBeans || missingWater || missingCup)
            {
                if (missingBeans) OnNoBeans?.Invoke(this);
                if (missingWater) OnNoWater?.Invoke(this);
                if (missingCup) OnNoCup?.Invoke(this);
                return;
            }

            beansGrams -= beansPerBrewGrams;
            waterMl -= waterPerBrewMl;
            _cycleDuration = seconds;
            _cycleVolumeMl = waterPerBrewMl;
            LastBrewVolumeMl = 0f;
            _status.TimerRemainingSec = seconds;
            _status.NormalizedLevel = 0f;
            _status.Power = EquipmentPowerState.OnWithTimer;
            uint cycle = ++_cycleVersion;
            Emit();
            OnSuppliesChanged?.Invoke(this);
            // A listener may cancel/disable the equipment during any callback.
            if (!IsBrewing || cycle != _cycleVersion) return;
            OnStarted?.Invoke(this);
            if (IsBrewing && cycle == _cycleVersion) OnBrewing?.Invoke(this);
        }

        public void CancelBrewing()
        {
            if (!IsBrewing) return;
            ++_cycleVersion;
            _status.Power = EquipmentPowerState.On;
            _status.TimerRemainingSec = 0f;
            _status.NormalizedLevel = 0f;
            Emit();
            OnCancelled?.Invoke(this);
        }

        public void SetBeansGrams(float grams)
        {
            float next = ClampSupply(grams, beanCapacityGrams);
            if (beansGrams == next) return;
            beansGrams = next;
            Emit();
            OnSuppliesChanged?.Invoke(this);
        }

        public void SetWaterMl(float milliliters)
        {
            float next = ClampSupply(milliliters, waterCapacityMl);
            if (waterMl == next) return;
            waterMl = next;
            Emit();
            OnSuppliesChanged?.Invoke(this);
        }

        public void AddBeansGrams(float grams)
        {
            if (IsPositiveFinite(grams)) SetBeansGrams((float)Math.Min((double)beansGrams + grams, beanCapacityGrams));
        }

        public void AddWaterMl(float milliliters)
        {
            if (IsPositiveFinite(milliliters)) SetWaterMl((float)Math.Min((double)waterMl + milliliters, waterCapacityMl));
        }

        public void SetCupPresent(bool present)
        {
            // Preserve manual detector integrations only when no region owns detection.
            if (cupRegion != null || _hasUsedCupRegion) return;
            UpdateCupPresence(present);
        }

        private void RefreshCupPresence()
        {
            if (!isActiveAndEnabled || !requireCup) return;
            if (cupRegion != null)
            {
                _hasUsedCupRegion = true;
                UpdateCupPresence(cupRegion.HasCup);
            }
            else if (_hasUsedCupRegion) UpdateCupPresence(false);
        }

        private void LateUpdate()
        {
            RefreshCupPresence();
            if (isActiveAndEnabled)
            {
                CheckBeanThresholds();
                CheckWaterThresholds();
            }
        }

        private void CheckBeanThresholds()
        {
            bool enough = HasBeans;
            bool full = IsBeanHopperFull;
            if (enough == _hadEnoughBeans && full == _wasBeanHopperFull) return;
            bool becameEnough = enough && !_hadEnoughBeans;
            bool becameFull = full && !_wasBeanHopperFull;
            // Commit both latches before callbacks, which may consume or refill beans.
            _hadEnoughBeans = enough;
            _wasBeanHopperFull = full;
            uint version = ++_beanThresholdVersion;
            if (becameEnough) OnEnoughBeans?.Invoke(this);
            if (becameFull && version == _beanThresholdVersion) OnBeansFull?.Invoke(this);
        }

        protected override void InitializeStatus()
        {
            _hadEnoughBeans = false;
            _wasBeanHopperFull = false;
            _hadEnoughWater = false;
            _wasWaterTankFull = false;
            base.InitializeStatus();
        }

        private void CheckWaterThresholds()
        {
            bool enough = HasWater;
            bool full = IsWaterTankFull;
            if (enough == _hadEnoughWater && full == _wasWaterTankFull) return;
            bool becameEnough = enough && !_hadEnoughWater;
            bool becameFull = full && !_wasWaterTankFull;
            _hadEnoughWater = enough;
            _wasWaterTankFull = full;
            uint version = ++_waterThresholdVersion;
            if (becameEnough) OnEnoughWater?.Invoke(this);
            if (becameFull && version == _waterThresholdVersion) OnWaterFull?.Invoke(this);
        }

        private void UpdateCupPresence(bool present)
        {
            if (cupPresent == present) return;
            cupPresent = present;
            if (present) OnCupPresent?.Invoke(this);
            if (requireCup && !present && IsBrewing)
            {
                CancelBrewing();
                OnNoCup?.Invoke(this);
            }
            Emit();
        }

        protected override void OnTick(float dt)
        {
            RefreshCupPresence();
            if (!isActiveAndEnabled || !IsBrewing || !IsPositiveFinite(dt)) return;
            uint cycle = _cycleVersion;
            _status.TimerRemainingSec = Mathf.Max(0f, _status.TimerRemainingSec - dt);
            _status.NormalizedLevel = 1f - _status.TimerRemainingSec / _cycleDuration;
            bool finished = _status.TimerRemainingSec == 0f;
            Emit();
            if (!IsBrewing || cycle != _cycleVersion) return;
            OnBrewProgress?.Invoke(this);
            if (!IsBrewing || cycle != _cycleVersion || !finished) return;
            LastBrewVolumeMl = _cycleVolumeMl;
            _status.Power = EquipmentPowerState.On;
            Emit();
            OnFinished?.Invoke(this);
        }

        protected override void HandleCommand(EquipmentCommand cmd)
        {
            switch (cmd.Type)
            {
                case EquipmentCommandType.SetPowerOn:
                    if (isActiveAndEnabled && !IsBrewing) SetPower(EquipmentPowerState.On);
                    break;
                case EquipmentCommandType.SetPowerOff: PowerOff(); break;
                case EquipmentCommandType.TogglePower:
                    if (_status.Power == EquipmentPowerState.Off)
                    {
                        if (isActiveAndEnabled) SetPower(EquipmentPowerState.On);
                    }
                    else PowerOff();
                    break;
                case EquipmentCommandType.InterfaceActions: StartBrewing(); break;
                case EquipmentCommandType.StartTimerSeconds: StartBrewingForSeconds(cmd.FValue); break;
                case EquipmentCommandType.CancelTimer: CancelBrewing(); break;
                case EquipmentCommandType.Break:
                    _status.Condition = EquipmentConditionState.Broken;
                    PowerOff();
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

        private void PowerOff()
        {
            if (_poweringOff) return;
            _poweringOff = true;
            try
            {
                CancelBrewing();
                _status.Power = EquipmentPowerState.Off;
                Emit();
            }
            finally { _poweringOff = false; }
        }

        protected override void Emit()
        {
            _status.EmitPowerUnit = 0f;
            _status.Fill = waterMl > 0f
                ? (_containedItems.Count > 0 ? EquipmentFillState.MixedContents : EquipmentFillState.ContainsLiquid)
                : (_containedItems.Count > 0 ? EquipmentFillState.ContainsItem : EquipmentFillState.Empty);
            CheckBeanThresholds();
            CheckWaterThresholds();
            base.Emit();
            foreach (var item in _containedItems.ToArray()) item.OnEquipmentStateChanged(this, _status);
            OnStateChanged?.Invoke(this);
        }

        protected override void Start()
        {
            _started = true;
            base.Start();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // The tickable base registers only in Start, which Unity runs once.
            if (_started) base.Start();
        }

        protected override void OnDisable()
        {
            PowerOff();
            base.OnDisable();
        }

        protected override void UpdateEmittedPowerUnit(float newValue) => _status.EmitPowerUnit = 0f;
        private static bool IsPositiveFinite(float value) => value > 0f && !float.IsInfinity(value);
        private static float ClampSupply(float value, float capacity) =>
            float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp(value, 0f, capacity);

        private void OnValidate()
        {
            brewDurationSeconds = IsPositiveFinite(brewDurationSeconds) ? brewDurationSeconds : 30f;
            beansPerBrewGrams = IsPositiveFinite(beansPerBrewGrams) ? beansPerBrewGrams : 15f;
            waterPerBrewMl = IsPositiveFinite(waterPerBrewMl) ? waterPerBrewMl : 250f;
            beanCapacityGrams = IsPositiveFinite(beanCapacityGrams) ? beanCapacityGrams : 0f;
            waterCapacityMl = IsPositiveFinite(waterCapacityMl) ? waterCapacityMl : 0f;
            beansGrams = ClampSupply(beansGrams, beanCapacityGrams);
            waterMl = ClampSupply(waterMl, waterCapacityMl);
        }
    }
}
