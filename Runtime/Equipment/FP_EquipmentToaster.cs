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

    [Serializable] public class ToasterEvent : UnityEvent<FP_EquipmentToaster> { }

    /// <summary>One shared toast cycle; contained items receive heat through equipment ticks.</summary>
    public class FP_EquipmentToaster : FP_EquipmentTickableBase
    {
        [SerializeField, Range(0f, 1f)] private float toastLevel = 0.5f;
        [SerializeField, Min(0.01f)] private float lightToastSeconds = 5f;
        [SerializeField, Min(0.01f)] private float darkToastSeconds = 30f;
        [SerializeField, Min(0f)] private float heatPerSecond = 2f;
        private float _cycleDuration;
        private float _cycleHeat;
        private uint _cycleVersion;
        private bool _started;
        private bool _stopping;
        private readonly Dictionary<IFPContainedItem, HashSet<FP_ToasterContainmentVolume>> _volumes =
            new Dictionary<IFPContainedItem, HashSet<FP_ToasterContainmentVolume>>();

        public float ToastLevel => toastLevel;
        public float ToastDurationSeconds => Mathf.Lerp(lightToastSeconds, darkToastSeconds, toastLevel);
        public bool IsToasting => _status.Power == EquipmentPowerState.OnWithTimer;
        public int ItemCount => _containedItems.Count;
        public float ToastProgress01 => _cycleDuration > 0f
            ? Mathf.Clamp01(1f - _status.TimerRemainingSec / _cycleDuration) : 0f;

        [Header("Toaster Events")]
        public ToasterEvent OnToastLevelChanged = new ToasterEvent();
        public ToasterEvent OnStarted = new ToasterEvent();
        public ToasterEvent OnToasting = new ToasterEvent();
        public ToasterEvent OnToastProgress = new ToasterEvent();
        public ToasterEvent OnFinished = new ToasterEvent();
        public ToasterEvent OnCancelled = new ToasterEvent();
        public ToasterEvent OnNoItem = new ToasterEvent();
        public ToasterEvent OnContentsChanged = new ToasterEvent();
        public ToasterEvent OnItemEntered = new ToasterEvent();
        public ToasterEvent OnItemExited = new ToasterEvent();
        public ToasterEvent OnBroken = new ToasterEvent();
        public ToasterEvent OnRepaired = new ToasterEvent();
        public ToasterEvent OnStateChanged = new ToasterEvent();

        /// <summary>Changes the next cycle's duration; an active cycle keeps its accepted recipe.</summary>
        public void SetToastLevel(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            value = Mathf.Clamp01(value);
            if (toastLevel == value) return;
            toastLevel = value;
            Emit();
            OnToastLevelChanged?.Invoke(this);
        }

        public void StartToasting() => StartToastingForSeconds(ToastDurationSeconds);

        public void StartToastingForSeconds(float seconds)
        {
            if (!isActiveAndEnabled || _stopping || IsToasting ||
                _status.Condition == EquipmentConditionState.Broken || !PositiveFinite(seconds)) return;
            PruneItems();
            if (IsToasting || _stopping || !isActiveAndEnabled || _status.Condition == EquipmentConditionState.Broken) return;
            if (ItemCount == 0) { OnNoItem?.Invoke(this); return; }
            _cycleDuration = seconds;
            _cycleHeat = heatPerSecond;
            _status.TimerRemainingSec = seconds;
            _status.Power = EquipmentPowerState.OnWithTimer;
            uint cycle = ++_cycleVersion;
            Emit();
            if (!IsToasting || cycle != _cycleVersion) return;
            OnStarted?.Invoke(this);
            if (IsToasting && cycle == _cycleVersion) OnToasting?.Invoke(this);
        }

        public void StopToasting()
        {
            if (_stopping || !IsToasting) return;
            _stopping = true;
            try
            {
                ++_cycleVersion;
                _status.Power = EquipmentPowerState.Off;
                _status.TimerRemainingSec = 0f;
                _cycleDuration = 0f;
                Emit();
                OnCancelled?.Invoke(this);
            }
            finally { _stopping = false; }
        }

        public override void Tick(float dt)
        {
            if (!isActiveAndEnabled || !PositiveFinite(dt)) return;
            PruneItems();
            if (!IsToasting) return;
            uint cycle = _cycleVersion;
            // Heat the final partial slice before turning off; never heat for an oversized dt.
            float slice = Mathf.Min(dt, _status.TimerRemainingSec);
            _status.TimerRemainingSec = Mathf.Max(0f, _status.TimerRemainingSec - slice);
            var heatStatus = _status;
            heatStatus.EmitPowerUnit = _cycleHeat;
            foreach (var item in _containedItems.ToArray())
            {
                if (!IsToasting || cycle != _cycleVersion) return;
                if (_containedItems.Contains(item) && IsUsable(item)) item.OnEquipmentTick(this, heatStatus, slice);
            }
            if (!IsToasting || cycle != _cycleVersion) return;
            Emit();
            if (!IsToasting || cycle != _cycleVersion) return;
            OnToastProgress?.Invoke(this);
            if (!IsToasting || cycle != _cycleVersion || _status.TimerRemainingSec > 0f) return;
            _status.Power = EquipmentPowerState.Off;
            Emit();
            OnFinished?.Invoke(this);
        }

        private static bool IsUsable(IFPContainedItem item)
        {
            if (item == null) return false;
            if (item is UnityEngine.Object obj && obj == null) return false;
            return !(item is Behaviour behaviour) || behaviour.isActiveAndEnabled;
        }

        private void PruneItems()
        {
            foreach (var item in _containedItems.ToArray())
                if (!IsUsable(item)) RemoveItemFromEquipment(item);
        }

        protected override void AddContainedItem(IFPContainedItem item)
        {
            if (!IsUsable(item) || _containedItems.Contains(item)) return;
            base.AddContainedItem(item);
            OnItemEntered?.Invoke(this);
            OnContentsChanged?.Invoke(this);
        }

        protected override void RemoveContainedItem(IFPContainedItem item)
        {
            if (item == null || !_containedItems.Contains(item)) return;
            _volumes.Remove(item);
            base.RemoveContainedItem(item);
            if (ItemCount == 0) StopToasting();
            OnItemExited?.Invoke(this);
            OnContentsChanged?.Invoke(this);
        }

        internal void EnterVolume(FP_EnergyReactiveItem item, FP_ToasterContainmentVolume volume)
        {
            if (!isActiveAndEnabled || !IsUsable(item)) return;
            if (!_volumes.TryGetValue(item, out var volumes))
            {
                volumes = new HashSet<FP_ToasterContainmentVolume>();
                _volumes.Add(item, volumes);
            }
            volumes.Add(volume);
            AddItemToEquipment(item);
        }

        internal void ExitVolume(FP_EnergyReactiveItem item, FP_ToasterContainmentVolume volume)
        {
            if (!_volumes.TryGetValue(item, out var volumes)) return;
            volumes.Remove(volume);
            if (volumes.Count == 0) RemoveItemFromEquipment(item);
        }

        protected override void Emit()
        {
            _status.NormalizedLevel = toastLevel;
            _status.EmitPowerUnit = IsToasting ? _cycleHeat : 0f;
            _status.Fill = ItemCount == 0 ? EquipmentFillState.Empty : EquipmentFillState.ContainsItem;
            base.Emit();
            // Legacy energy items also add energy on state notifications. Heat is tick-only here.
            var notification = _status;
            notification.EmitPowerUnit = 0f;
            foreach (var item in _containedItems.ToArray())
                if (IsUsable(item)) item.OnEquipmentStateChanged(this, notification);
            OnStateChanged?.Invoke(this);
        }

        protected override void HandleCommand(EquipmentCommand cmd)
        {
            switch (cmd.Type)
            {
                case EquipmentCommandType.SetPowerOn:
                case EquipmentCommandType.InterfaceActions: StartToasting(); break;
                case EquipmentCommandType.StartTimerSeconds: StartToastingForSeconds(cmd.FValue); break;
                case EquipmentCommandType.SetPowerOff:
                case EquipmentCommandType.CancelTimer: StopToasting(); break;
                case EquipmentCommandType.TogglePower:
                    if (IsToasting) StopToasting(); else StartToasting();
                    break;
                case EquipmentCommandType.SetHeatLevel01: SetToastLevel(cmd.FValue); break;
                case EquipmentCommandType.Break:
                    _status.Condition = EquipmentConditionState.Broken;
                    StopToasting();
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
        protected override void OnEnable() { base.OnEnable(); if (_started) base.Start(); }
        protected override void OnDisable()
        {
            StopToasting();
            foreach (var item in _containedItems.ToArray()) RemoveItemFromEquipment(item);
            base.OnDisable();
        }
        protected override void UpdateEmittedPowerUnit(float newValue) => _status.EmitPowerUnit = IsToasting ? _cycleHeat : 0f;
        private static bool PositiveFinite(float value) => value > 0f && !float.IsInfinity(value);
        private void OnValidate()
        {
            toastLevel = float.IsNaN(toastLevel) || float.IsInfinity(toastLevel) ? 0.5f : Mathf.Clamp01(toastLevel);
            lightToastSeconds = PositiveFinite(lightToastSeconds) ? lightToastSeconds : 5f;
            darkToastSeconds = PositiveFinite(darkToastSeconds) ? Mathf.Max(lightToastSeconds, darkToastSeconds) : Mathf.Max(lightToastSeconds, 30f);
            heatPerSecond = PositiveFinite(heatPerSecond) ? heatPerSecond : 0f;
        }
    }
}
