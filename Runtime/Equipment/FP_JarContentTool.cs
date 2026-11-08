// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using UnityEngine.Events;
    using System;

    [Serializable] public class JarContentToolEvent : UnityEvent<FP_JarContentTool> { }

    /// <summary>Example spoon/knife receiver. Holds one content identity until emptied.</summary>
    [DisallowMultipleComponent]
    public class FP_JarContentTool : MonoBehaviour, IFPJarContentReceiver
    {
        [SerializeField, Min(0f)] private float capacityMl = 5f;
        private float _amountMl;
        private string _contentId = string.Empty;
        private bool _reportedFull;
        private uint _version;
        public float AmountMl => _amountMl;
        public string ContentId => _contentId;
        public float Fill01 => capacityMl > 0f ? Mathf.Clamp01(_amountMl / capacityMl) : 0f;
        public FP_JarContent LastSource { get; private set; }
        public JarContentToolEvent OnContentReceived = new JarContentToolEvent();
        public JarContentToolEvent OnContentChanged = new JarContentToolEvent();
        public JarContentToolEvent OnFilled = new JarContentToolEvent();
        public JarContentToolEvent OnEmpty = new JarContentToolEvent();
        public JarToolEvent OnEnteredJar = new JarToolEvent();
        public JarToolEvent OnExitedJar = new JarToolEvent();

        private void Awake() => OnEmpty?.Invoke(this);
        public void OnJarEntered(FP_JarContent jar) => OnEnteredJar?.Invoke(jar, this);
        public void OnJarExited(FP_JarContent jar) => OnExitedJar?.Invoke(jar, this);
        public float ReceiveJarContent(FP_JarContent source, string id, float offeredMl)
        {
            if (!isActiveAndEnabled || string.IsNullOrEmpty(id) || offeredMl <= 0f ||
                float.IsNaN(offeredMl) || float.IsInfinity(offeredMl) || (_amountMl > 0f && _contentId != id)) return 0f;
            float accepted = Mathf.Min(offeredMl, Mathf.Max(0f, capacityMl - _amountMl));
            if (accepted <= 0f) return 0f;
            _amountMl += accepted;
            _contentId = id;
            LastSource = source;
            return accepted;
        }
        public void OnJarContentReceived(FP_JarContent source, string id, float acceptedMl)
        {
            OnContentReceived?.Invoke(this);
            Publish();
        }
        public float RemoveContentMl(float amount)
        {
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return 0f;
            float removed = Mathf.Min(_amountMl, amount);
            if (removed <= 0f) return 0f;
            _amountMl -= removed;
            if (_amountMl == 0f) { _contentId = string.Empty; LastSource = null; }
            Publish();
            if (_amountMl == 0f) OnEmpty?.Invoke(this);
            return removed;
        }
        public void Empty() => RemoveContentMl(_amountMl);
        private void Publish()
        {
            uint version = ++_version;
            bool full = capacityMl > 0f && _amountMl >= capacityMl;
            bool becameFull = full && !_reportedFull;
            _reportedFull = full;
            if (becameFull) OnFilled?.Invoke(this);
            if (version == _version) OnContentChanged?.Invoke(this);
        }
        private void OnValidate()
        {
            if (capacityMl < 0f || float.IsNaN(capacityMl) || float.IsInfinity(capacityMl)) capacityMl = 0f;
        }
    }
}
