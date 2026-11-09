// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable] public class ContentDestinationEvent : UnityEvent<FP_ContentDestination> { }
    [Serializable] public class DestinationToolEvent : UnityEvent<FP_ContentDestination, FP_JarContentTool> { }

    /// <summary>Simple bread/coating destination. Holds one content identity until emptied.</summary>
    [DisallowMultipleComponent]
    public class FP_ContentDestination : MonoBehaviour, IFPToolContentReceiver
    {
        [SerializeField, Min(0f)] private float capacityMl = 25f;
        [SerializeField] private string acceptedContentId;
        private float _amountMl;
        private string _contentId = string.Empty;
        private bool _wasFull;
        private uint _version;
        public float AmountMl => _amountMl;
        public string ContentId => _contentId;
        public float Fill01 => capacityMl > 0f ? Mathf.Clamp01(_amountMl / capacityMl) : 0f;
        public FP_JarContent LastSourceJar { get; private set; }
        public FP_JarContentTool LastTool { get; private set; }
        public float LastAcceptedAmountMl { get; private set; }
        public ContentDestinationEvent OnContentReceived = new ContentDestinationEvent();
        public ContentDestinationEvent OnContentChanged = new ContentDestinationEvent();
        public ContentDestinationEvent OnFilled = new ContentDestinationEvent();
        public ContentDestinationEvent OnEmpty = new ContentDestinationEvent();
        public DestinationToolEvent OnToolEntered = new DestinationToolEvent();
        public DestinationToolEvent OnToolExited = new DestinationToolEvent();

        private void Awake() => OnEmpty?.Invoke(this);
        public void OnContentToolEntered(FP_JarContentTool tool) => OnToolEntered?.Invoke(this, tool);
        public void OnContentToolExited(FP_JarContentTool tool) => OnToolExited?.Invoke(this, tool);
        public float ReceiveToolContent(FP_JarContentTool tool, FP_JarContent sourceJar, string id, float offeredMl)
        {
            if (!isActiveAndEnabled || string.IsNullOrEmpty(id) || offeredMl <= 0f || float.IsInfinity(offeredMl) || float.IsNaN(offeredMl) ||
                (!string.IsNullOrEmpty(acceptedContentId) && acceptedContentId != id) || (_amountMl > 0f && _contentId != id)) return 0f;
            float accepted = Mathf.Min(offeredMl, Mathf.Max(0f, capacityMl - _amountMl));
            if (accepted <= 0f) return 0f;
            _amountMl += accepted;
            _contentId = id;
            LastSourceJar = sourceJar;
            LastTool = tool;
            LastAcceptedAmountMl = accepted;
            return accepted;
        }
        public void OnToolContentReceived(FP_JarContentTool tool, FP_JarContent sourceJar, string id, float acceptedMl)
        {
            OnContentReceived?.Invoke(this);
            Publish();
        }
        public void Empty()
        {
            if (_amountMl == 0f) return;
            _amountMl = 0f;
            _contentId = string.Empty;
            LastSourceJar = null;
            LastTool = null;
            LastAcceptedAmountMl = 0f;
            Publish();
            if (_amountMl == 0f) OnEmpty?.Invoke(this);
        }
        private void Publish()
        {
            uint version = ++_version;
            bool full = capacityMl > 0f && _amountMl >= capacityMl;
            bool becameFull = full && !_wasFull;
            _wasFull = full;
            if (becameFull) OnFilled?.Invoke(this);
            if (version == _version) OnContentChanged?.Invoke(this);
        }
        private void OnValidate()
        {
            if (capacityMl < 0f || float.IsNaN(capacityMl) || float.IsInfinity(capacityMl)) capacityMl = 0f;
        }
    }
}
