// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    public enum FP_CupLiquidType { None = 0, Type1 = 1, Type2 = 2, Type3 = 3, Mixed = 4 }
    [Serializable] public class CupFillEvent : UnityEvent<FP_CupFill> { }

    /// <summary>One shared volume. Different accepted inputs form a sticky Mixed state until empty.</summary>
    [RequireComponent(typeof(FP_Cup)), DisallowMultipleComponent]
    public class FP_CupFill : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float capacityMl = 250f;
        [SerializeField, Min(0f)] private float volumeMl;
        [SerializeField] private FP_CupLiquidType liquidType = FP_CupLiquidType.Type3;
        private bool _initialized;
        private uint _changeVersion;

        public float VolumeMl => volumeMl;
        public float CapacityMl => capacityMl;
        public float Fill01 => capacityMl > 0f ? Mathf.Clamp01(volumeMl / capacityMl) : 0f;
        public FP_CupLiquidType LiquidType => liquidType;
        public bool IsEmpty => volumeMl <= 0f;
        public bool IsFilled => capacityMl > 0f && volumeMl >= capacityMl;
        public bool CanPour => isActiveAndEnabled && !IsEmpty;
        public bool IsPouring { get; private set; }

        [Header("Cup Fill Events")]
        public CupFillEvent OnEmpty = new CupFillEvent();
        public CupFillEvent OnFilled = new CupFillEvent();
        public CupFillEvent OnType1Filled = new CupFillEvent();
        public CupFillEvent OnType2Filled = new CupFillEvent();
        public CupFillEvent OnType3Filled = new CupFillEvent();
        public CupFillEvent OnType1Empty = new CupFillEvent();
        public CupFillEvent OnType2Empty = new CupFillEvent();
        public CupFillEvent OnType3Empty = new CupFillEvent();
        public CupFillEvent OnMixed = new CupFillEvent();
        public CupFillEvent OnMixedFilled = new CupFillEvent();
        public CupFillEvent OnMixedEmpty = new CupFillEvent();
        public CupFillEvent OnFillChanged = new CupFillEvent();
        public CupFillEvent OnLiquidTypeChanged = new CupFillEvent();
        public CupFillEvent OnPourStarted = new CupFillEvent();
        public CupFillEvent OnPourStopped = new CupFillEvent();
        public CupFillEvent OnStateChanged = new CupFillEvent();

        private void Awake()
        {
            Sanitize();
            _initialized = true;
            Publish(0f, FP_CupLiquidType.None, false, true);
        }

        /// <summary>True means this input is accepted. Volume clamps at capacity, but incoming types can still mix a full cup.</summary>
        public bool TryAddLiquid(FP_CupLiquidType type, float milliliters)
        {
            if (!isActiveAndEnabled || !ValidType(type) || capacityMl <= 0f ||
                !PositiveFinite(milliliters)) return false;
            float next = (float)Math.Min((double)volumeMl + milliliters, capacityMl);
            var nextType = IsEmpty ? type : liquidType == type ? type : FP_CupLiquidType.Mixed;
            if (next == volumeMl && nextType == liquidType) return true;
            float previous = volumeMl;
            var previousType = liquidType;
            bool wasFull = IsFilled;
            volumeMl = next;
            liquidType = nextType;
            Publish(previous, previousType, wasFull, false);
            return true;
        }

        public float RemoveLiquidMl(float milliliters)
        {
            if (!PositiveFinite(milliliters) || IsEmpty) return 0f;
            float previous = volumeMl;
            var previousType = liquidType;
            bool wasFull = IsFilled;
            bool stoppedPouring = false;
            float removed = Mathf.Min(volumeMl, milliliters);
            volumeMl -= removed;
            if (IsEmpty)
            {
                liquidType = FP_CupLiquidType.None;
                stoppedPouring = IsPouring;
                IsPouring = false;
            }
            Publish(previous, previousType, wasFull, false);
            if (stoppedPouring && !IsPouring) OnPourStopped?.Invoke(this);
            return removed;
        }

        public void Empty() => RemoveLiquidMl(volumeMl);

        internal void SetPouringState(bool pouring)
        {
            pouring = pouring && CanPour;
            if (IsPouring == pouring) return;
            IsPouring = pouring;
            (pouring ? OnPourStarted : OnPourStopped)?.Invoke(this);
            OnStateChanged?.Invoke(this);
        }

        private void Publish(float previousVolume, FP_CupLiquidType previousType, bool wasFull, bool initial)
        {
            uint version = ++_changeVersion;
            bool becameEmpty = IsEmpty && (initial || previousVolume > 0f);
            bool becameFull = IsFilled && (initial || !wasFull);
            if (liquidType == FP_CupLiquidType.Mixed && previousType != FP_CupLiquidType.Mixed)
            {
                OnMixed?.Invoke(this);
                if (version != _changeVersion) return;
            }
            // Typed edges precede generic edges, so effect cleanup/setup happens before general completion handlers.
            if (becameEmpty)
            {
                if (previousVolume > 0f) TypedEmpty(previousType)?.Invoke(this);
                if (version != _changeVersion) return;
                OnEmpty?.Invoke(this);
            }
            if (version != _changeVersion) return;
            if (IsFilled && (becameFull || previousType != liquidType))
            {
                TypedFilled(liquidType)?.Invoke(this);
                if (version != _changeVersion) return;
            }
            if (becameFull) OnFilled?.Invoke(this);
            if (version != _changeVersion) return;
            if (previousType != liquidType) OnLiquidTypeChanged?.Invoke(this);
            if (version != _changeVersion) return;
            if (initial || previousVolume != volumeMl) OnFillChanged?.Invoke(this);
            if (version == _changeVersion) OnStateChanged?.Invoke(this);
        }

        private CupFillEvent TypedFilled(FP_CupLiquidType type) =>
            type == FP_CupLiquidType.Type1 ? OnType1Filled : type == FP_CupLiquidType.Type2 ? OnType2Filled :
            type == FP_CupLiquidType.Type3 ? OnType3Filled : type == FP_CupLiquidType.Mixed ? OnMixedFilled : null;
        private CupFillEvent TypedEmpty(FP_CupLiquidType type) =>
            type == FP_CupLiquidType.Type1 ? OnType1Empty : type == FP_CupLiquidType.Type2 ? OnType2Empty :
            type == FP_CupLiquidType.Type3 ? OnType3Empty : type == FP_CupLiquidType.Mixed ? OnMixedEmpty : null;
        private static bool PositiveFinite(float value) => value > 0f && !float.IsInfinity(value);
        private static bool ValidType(FP_CupLiquidType type) => type >= FP_CupLiquidType.Type1 && type <= FP_CupLiquidType.Mixed;
        private void Sanitize()
        {
            capacityMl = PositiveFinite(capacityMl) ? capacityMl : 0f;
            volumeMl = float.IsNaN(volumeMl) || float.IsInfinity(volumeMl) ? 0f : Mathf.Clamp(volumeMl, 0f, capacityMl);
            if (IsEmpty) liquidType = FP_CupLiquidType.None;
            else if (!ValidType(liquidType)) liquidType = FP_CupLiquidType.Type3;
        }
        private void OnValidate() => Sanitize();
        private void OnEnable() { if (_initialized) OnStateChanged?.Invoke(this); }
        private void OnDisable() { SetPouringState(false); OnStateChanged?.Invoke(this); }
    }
}
