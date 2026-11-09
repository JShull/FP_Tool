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

    /// <summary>Simple bread/coating destination. Combines food types until emptied.</summary>
    [DisallowMultipleComponent]
    public class FP_ContentDestination : MonoBehaviour, IFPToolContentReceiver
    {
        [SerializeField, Min(0f)] private float capacityMl = 25f;
        private float _amountMl;
        private string _contentId = string.Empty;
        private FP_ContentType _contentType;
        private FP_ContentType _reportedType;
        private bool _wasFull;
        private uint _version;
        public float AmountMl => _amountMl;
        public string ContentId => _contentId;
        public FP_ContentType ContentType => _contentType;
        public float Fill01 => capacityMl > 0f ? Mathf.Clamp01(_amountMl / capacityMl) : 0f;
        public FP_JarContent LastSourceJar { get; private set; }
        public FP_JarContentTool LastTool { get; private set; }
        public float LastAcceptedAmountMl { get; private set; }
        public ContentDestinationEvent OnContentReceived = new ContentDestinationEvent();
        public ContentDestinationEvent OnContentChanged = new ContentDestinationEvent();
        public ContentDestinationEvent OnFilled = new ContentDestinationEvent();
        public ContentDestinationEvent OnEmpty = new ContentDestinationEvent();
        public ContentDestinationEvent OnType1Filled = new ContentDestinationEvent();
        public ContentDestinationEvent OnType2Filled = new ContentDestinationEvent();
        public ContentDestinationEvent OnType3Filled = new ContentDestinationEvent();
        public ContentDestinationEvent OnMixedFilled = new ContentDestinationEvent();
        public ContentDestinationEvent OnType1Empty = new ContentDestinationEvent();
        public ContentDestinationEvent OnType2Empty = new ContentDestinationEvent();
        public ContentDestinationEvent OnType3Empty = new ContentDestinationEvent();
        public ContentDestinationEvent OnMixedEmpty = new ContentDestinationEvent();
        public ContentDestinationEvent OnMixed = new ContentDestinationEvent();
        public ContentDestinationEvent OnContentTypeChanged = new ContentDestinationEvent();
        public DestinationToolEvent OnToolEntered = new DestinationToolEvent();
        public DestinationToolEvent OnToolExited = new DestinationToolEvent();

        private void Awake() => OnEmpty?.Invoke(this);
        public void OnContentToolEntered(FP_JarContentTool tool) => OnToolEntered?.Invoke(this, tool);
        public void OnContentToolExited(FP_JarContentTool tool) => OnToolExited?.Invoke(this, tool);
        public float ReceiveToolContent(FP_JarContentTool tool, FP_JarContent sourceJar, string id, float offeredMl)
        {
            if (!isActiveAndEnabled || offeredMl <= 0f || float.IsInfinity(offeredMl) || float.IsNaN(offeredMl)) return 0f;
            float accepted = Mathf.Min(offeredMl, Mathf.Max(0f, capacityMl - _amountMl));
            if (accepted <= 0f) return 0f;
            _amountMl += accepted;
            var incoming = tool != null && tool.ContentType != FP_ContentType.None ? tool.ContentType :
                sourceJar != null ? sourceJar.ContentType : FP_ContentType.Type1;
            _contentType = _contentType == FP_ContentType.None ? incoming :
                _contentType == incoming ? _contentType : FP_ContentType.Mixed;
            _contentId = _contentType == FP_ContentType.Mixed ? "Mixed" : id;
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
            _contentType = FP_ContentType.None;
            LastSourceJar = null;
            LastTool = null;
            LastAcceptedAmountMl = 0f;
            Publish();
            if (_amountMl == 0f) OnEmpty?.Invoke(this);
        }
        private void Publish()
        {
            uint version = ++_version;
            var previousType = _reportedType;
            _reportedType = _contentType;
            bool full = capacityMl > 0f && _amountMl >= capacityMl;
            bool becameFull = full && !_wasFull;
            _wasFull = full;
            PublishTypeEvents(previousType, full, becameFull, version);
            if (version != _version) return;
            if (becameFull) OnFilled?.Invoke(this);
            if (version == _version) OnContentChanged?.Invoke(this);
        }
        private void PublishTypeEvents(FP_ContentType previous, bool full, bool becameFull, uint version)
        {
            bool changed = previous != _contentType;
            if (changed && _contentType == FP_ContentType.Mixed) OnMixed?.Invoke(this);
            if (version != _version) return;
            if (changed && _contentType == FP_ContentType.None)
                TypeEvent(previous, false)?.Invoke(this);
            if (version != _version) return;
            if (full && (becameFull || changed)) TypeEvent(_contentType, true)?.Invoke(this);
            if (version != _version) return;
            if (changed) OnContentTypeChanged?.Invoke(this);
        }
        private ContentDestinationEvent TypeEvent(FP_ContentType type, bool filled)
        {
            switch (type)
            {
                case FP_ContentType.Type1: return filled ? OnType1Filled : OnType1Empty;
                case FP_ContentType.Type2: return filled ? OnType2Filled : OnType2Empty;
                case FP_ContentType.Type3: return filled ? OnType3Filled : OnType3Empty;
                case FP_ContentType.Mixed: return filled ? OnMixedFilled : OnMixedEmpty;
                default: return null;
            }
        }
        private void OnValidate()
        {
            if (capacityMl < 0f || float.IsNaN(capacityMl) || float.IsInfinity(capacityMl)) capacityMl = 0f;
        }
    }
}
