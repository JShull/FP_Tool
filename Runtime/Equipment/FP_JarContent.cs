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

    [Serializable] public class JarEvent : UnityEvent<FP_JarContent> { }
    [Serializable] public class JarToolEvent : UnityEvent<FP_JarContent, MonoBehaviour> { }

    /// <summary>Fixed content identity with finite stored volume and interface-based tool extraction.</summary>
    [DisallowMultipleComponent]
    public class FP_JarContent : MonoBehaviour
    {
        [SerializeField] private string contentId = "PeanutButter";
        [SerializeField] private FP_ContentType contentType = FP_ContentType.Type1;
        [SerializeField, Min(0f)] private float capacityMl = 500f;
        [SerializeField, Min(0f)] private float amountMl = 500f;
        [SerializeField, Min(0.01f)] private float takeAmountMl = 5f;
        [SerializeField] private bool takeOnToolEnter = true;
        [SerializeField] private bool pouringEnabled;
        private bool _transferring;
        private bool _initialized;
        private uint _version;
        private readonly Dictionary<MonoBehaviour, HashSet<FP_JarAccessRegion>> _tools =
            new Dictionary<MonoBehaviour, HashSet<FP_JarAccessRegion>>();

        public string ContentId => contentId;
        public FP_ContentType ContentType => contentType;
        public float AmountMl => amountMl;
        public float CapacityMl => capacityMl;
        public float Fill01 => capacityMl > 0f ? Mathf.Clamp01(amountMl / capacityMl) : 0f;
        public bool IsEmpty => amountMl <= 0f;
        public bool IsFull => capacityMl > 0f && amountMl >= capacityMl;
        public bool PouringEnabled => pouringEnabled;
        public bool CanPour => isActiveAndEnabled && pouringEnabled && !IsEmpty && !_transferring;
        public bool IsPouring { get; private set; }

        public JarEvent OnEmpty = new JarEvent();
        public JarEvent OnFull = new JarEvent();
        public JarEvent OnContentChanged = new JarEvent();
        public JarEvent OnStateChanged = new JarEvent();
        public JarEvent OnPourStarted = new JarEvent();
        public JarEvent OnPourStopped = new JarEvent();
        public JarToolEvent OnToolEntered = new JarToolEvent();
        public JarToolEvent OnToolExited = new JarToolEvent();
        public JarToolEvent OnContentTaken = new JarToolEvent();

        private void Awake()
        {
            Sanitize();
            _initialized = true;
            Publish(0f, false, true);
        }

        public void SetAmountMl(float amount)
        {
            if (_transferring) return;
            float next = ClampAmount(amount);
            if (next == amountMl) return;
            float previous = amountMl;
            bool wasFull = IsFull;
            amountMl = next;
            bool stopped = IsEmpty && IsPouring;
            if (stopped) IsPouring = false;
            Publish(previous, wasFull, false);
            if (stopped && !IsPouring) OnPourStopped?.Invoke(this);
        }

        public void AddContentMl(float amount)
        {
            if (PositiveFinite(amount)) SetAmountMl((float)Math.Min((double)amountMl + amount, capacityMl));
        }
        public float RemoveContentMl(float amount)
        {
            if (_transferring || !PositiveFinite(amount)) return 0f;
            float removed = Mathf.Min(amountMl, amount);
            SetAmountMl(amountMl - removed);
            return removed;
        }
        public void Empty() => SetAmountMl(0f);

        /// <summary>Debits only the amount accepted by the tool. Returns the transferred milliliters.</summary>
        public float TryTakeContent(IFPJarContentReceiver receiver)
        {
            if (!isActiveAndEnabled || _transferring || IsEmpty || receiver == null ||
                (receiver is UnityEngine.Object obj && obj == null) ||
                (receiver is Behaviour behaviour && !behaviour.isActiveAndEnabled)) return 0f;
            float offered = Mathf.Min(amountMl, takeAmountMl);
            if (!PositiveFinite(offered)) return 0f;
            float previous = amountMl;
            bool wasFull = IsFull;
            float accepted;
            _transferring = true;
            try
            {
                accepted = receiver.ReceiveJarContent(this, contentId, offered);
                accepted = PositiveFinite(accepted) ? Mathf.Min(accepted, offered) : 0f;
                amountMl -= accepted;
            }
            finally { _transferring = false; }
            if (accepted <= 0f) return 0f;
            bool stopped = IsEmpty && IsPouring;
            if (stopped) IsPouring = false;
            // Both stores are updated before any public transfer feedback.
            uint version = _version;
            receiver.OnJarContentReceived(this, contentId, accepted);
            if (version == _version) Publish(previous, wasFull, false);
            if (stopped && !IsPouring) OnPourStopped?.Invoke(this);
            OnContentTaken?.Invoke(this, receiver as MonoBehaviour);
            return accepted;
        }

        public void TakeWithTool(MonoBehaviour tool)
        {
            if (tool != null && _tools.ContainsKey(tool) && tool is IFPJarContentReceiver receiver) TryTakeContent(receiver);
        }

        internal void EnterRegion(MonoBehaviour tool, FP_JarAccessRegion region)
        {
            if (!isActiveAndEnabled || tool == null || !tool.isActiveAndEnabled || !(tool is IFPJarContentReceiver receiver)) return;
            if (_tools.TryGetValue(tool, out var regions)) { regions.Add(region); return; }
            _tools.Add(tool, new HashSet<FP_JarAccessRegion> { region });
            receiver.OnJarEntered(this);
            if (!isActiveAndEnabled || tool == null || !_tools.ContainsKey(tool)) return;
            OnToolEntered?.Invoke(this, tool);
            if (takeOnToolEnter && _tools.ContainsKey(tool)) TryTakeContent(receiver);
        }

        internal void ExitRegion(MonoBehaviour tool, FP_JarAccessRegion region)
        {
            if (!_tools.TryGetValue(tool, out var regions)) return;
            regions.Remove(region);
            if (regions.Count != 0) return;
            _tools.Remove(tool);
            if (tool != null && tool is IFPJarContentReceiver receiver) receiver.OnJarExited(this);
            OnToolExited?.Invoke(this, tool);
        }

        public void SetPouringEnabled(bool enabled)
        {
            if (pouringEnabled == enabled) return;
            pouringEnabled = enabled;
            if (!enabled) SetPouringState(false);
            OnStateChanged?.Invoke(this);
        }
        internal void SetPouringState(bool pouring)
        {
            pouring = pouring && CanPour;
            if (IsPouring == pouring) return;
            IsPouring = pouring;
            (pouring ? OnPourStarted : OnPourStopped)?.Invoke(this);
            OnStateChanged?.Invoke(this);
        }

        private void Publish(float previous, bool wasFull, bool initial)
        {
            uint version = ++_version;
            if (IsEmpty && (initial || previous > 0f)) OnEmpty?.Invoke(this);
            if (version != _version) return;
            if (IsFull && (initial || !wasFull)) OnFull?.Invoke(this);
            if (version != _version) return;
            if (initial || previous != amountMl) OnContentChanged?.Invoke(this);
            if (version == _version) OnStateChanged?.Invoke(this);
        }
        private void OnEnable() { if (_initialized) OnStateChanged?.Invoke(this); }
        private void OnDisable()
        {
            var tools = new List<MonoBehaviour>(_tools.Keys);
            _tools.Clear();
            foreach (var tool in tools)
            {
                if (tool != null && tool is IFPJarContentReceiver receiver) receiver.OnJarExited(this);
                OnToolExited?.Invoke(this, tool);
            }
            SetPouringState(false);
            OnStateChanged?.Invoke(this);
        }
        private static bool PositiveFinite(float value) => value > 0f && !float.IsInfinity(value);
        private float ClampAmount(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp(value, 0f, capacityMl);
        private void Sanitize()
        {
            if (contentType < FP_ContentType.Type1 || contentType > FP_ContentType.Mixed) contentType = FP_ContentType.Type1;
            if (string.IsNullOrWhiteSpace(contentId)) contentId = "Content";
            capacityMl = PositiveFinite(capacityMl) ? capacityMl : 0f;
            amountMl = ClampAmount(amountMl);
            takeAmountMl = PositiveFinite(takeAmountMl) ? takeAmountMl : 5f;
        }
        private void OnValidate() => Sanitize();
    }
}
