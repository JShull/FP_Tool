// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using UnityEngine.Events;
    using System;
    using System.Collections.Generic;

    [Serializable] public class JarContentToolEvent : UnityEvent<FP_JarContentTool> { }
    [Serializable] public class ToolDestinationEvent : UnityEvent<FP_JarContentTool, MonoBehaviour> { }

    /// <summary>Example spoon/knife receiver. Holds one content identity until emptied.</summary>
    [DisallowMultipleComponent]
    public class FP_JarContentTool : MonoBehaviour, IFPJarContentReceiver
    {
        [SerializeField, Min(0f)] private float capacityMl = 5f;
        private float _amountMl;
        private string _contentId = string.Empty;
        private bool _reportedFull;
        private uint _version;
        private bool _transferring;
        private readonly Dictionary<MonoBehaviour, HashSet<FP_ContentDestinationRegion>> _destinations =
            new Dictionary<MonoBehaviour, HashSet<FP_ContentDestinationRegion>>();
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
        public JarContentToolEvent OnContentTransferred = new JarContentToolEvent();
        public ToolDestinationEvent OnDestinationEntered = new ToolDestinationEvent();
        public ToolDestinationEvent OnDestinationExited = new ToolDestinationEvent();

        private void Awake() => OnEmpty?.Invoke(this);
        public void OnJarEntered(FP_JarContent jar) => OnEnteredJar?.Invoke(jar, this);
        public void OnJarExited(FP_JarContent jar) => OnExitedJar?.Invoke(jar, this);
        public float ReceiveJarContent(FP_JarContent source, string id, float offeredMl)
        {
            if (!isActiveAndEnabled || _transferring || string.IsNullOrEmpty(id) || offeredMl <= 0f ||
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
            if (_transferring || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return 0f;
            float removed = Mathf.Min(_amountMl, amount);
            if (removed <= 0f) return 0f;
            _amountMl -= removed;
            if (_amountMl == 0f) { _contentId = string.Empty; LastSource = null; }
            Publish();
            if (_amountMl == 0f) OnEmpty?.Invoke(this);
            return removed;
        }
        public void Empty() => RemoveContentMl(_amountMl);

        public float TryTransferContent(IFPToolContentReceiver receiver)
        {
            if (!isActiveAndEnabled || _transferring || _amountMl <= 0f || receiver == null ||
                (receiver is UnityEngine.Object obj && obj == null) ||
                (receiver is Behaviour behaviour && !behaviour.isActiveAndEnabled) || ReferenceEquals(receiver, this)) return 0f;
            string id = _contentId;
            var sourceJar = LastSource;
            float offered = _amountMl;
            float accepted;
            _transferring = true;
            try
            {
                accepted = receiver.ReceiveToolContent(this, sourceJar, id, offered);
                accepted = accepted > 0f && !float.IsInfinity(accepted) ? Mathf.Min(accepted, offered) : 0f;
                _amountMl -= accepted;
                if (_amountMl == 0f) { _contentId = string.Empty; LastSource = null; }
            }
            finally { _transferring = false; }
            if (accepted <= 0f) return 0f;
            uint version = _version;
            // Destination receives a source snapshot even when the tool itself is now empty.
            receiver.OnToolContentReceived(this, sourceJar, id, accepted);
            if (version == _version)
            {
                Publish();
                if (_amountMl == 0f) OnEmpty?.Invoke(this);
            }
            OnContentTransferred?.Invoke(this);
            return accepted;
        }

        public void TransferToDestination(MonoBehaviour destination)
        {
            if (destination != null && _destinations.ContainsKey(destination) && destination is IFPToolContentReceiver receiver)
                TryTransferContent(receiver);
        }

        internal void EnterDestination(MonoBehaviour destination, FP_ContentDestinationRegion region, bool transferOnEnter)
        {
            if (!isActiveAndEnabled || destination == null || !destination.isActiveAndEnabled ||
                !(destination is IFPToolContentReceiver receiver)) return;
            if (_destinations.TryGetValue(destination, out var regions)) { regions.Add(region); return; }
            _destinations.Add(destination, new HashSet<FP_ContentDestinationRegion> { region });
            receiver.OnContentToolEntered(this);
            if (!isActiveAndEnabled || destination == null || !_destinations.ContainsKey(destination)) return;
            OnDestinationEntered?.Invoke(this, destination);
            if (transferOnEnter && _destinations.ContainsKey(destination)) TryTransferContent(receiver);
        }

        internal void ExitDestination(MonoBehaviour destination, FP_ContentDestinationRegion region)
        {
            if ((object)destination == null || !_destinations.TryGetValue(destination, out var regions)) return;
            regions.Remove(region);
            if (regions.Count != 0) return;
            _destinations.Remove(destination);
            if (destination != null && destination is IFPToolContentReceiver receiver) receiver.OnContentToolExited(this);
            OnDestinationExited?.Invoke(this, destination);
        }

        private void OnDisable()
        {
            var destinations = new List<MonoBehaviour>(_destinations.Keys);
            _destinations.Clear();
            foreach (var destination in destinations)
            {
                if (destination != null && destination is IFPToolContentReceiver receiver) receiver.OnContentToolExited(this);
                OnDestinationExited?.Invoke(this, destination);
            }
        }
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
