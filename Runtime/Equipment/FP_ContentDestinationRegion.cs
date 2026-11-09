// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System.Collections.Generic;
    using UnityEngine;

    [RequireComponent(typeof(Collider)), DisallowMultipleComponent]
    public class FP_ContentDestinationRegion : MonoBehaviour
    {
        [Tooltip("A component implementing IFPToolContentReceiver, such as FP_ContentDestination.")]
        [SerializeField] private MonoBehaviour destination;
        [SerializeField] private bool transferOnEnter = true;
        private readonly Dictionary<Collider, FP_JarContentTool> _contacts = new Dictionary<Collider, FP_JarContentTool>();
        private readonly List<Collider> _stale = new List<Collider>();
        private readonly List<KeyValuePair<Collider, FP_JarContentTool>> _snapshot = new List<KeyValuePair<Collider, FP_JarContentTool>>();
        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            destination = GetComponentInParent<IFPToolContentReceiver>() as MonoBehaviour;
        }
        private void OnTriggerEnter(Collider other) => Track(other);
        private void OnTriggerStay(Collider other) => Track(other);
        private void OnTriggerExit(Collider other) => Remove(other);
        private void Track(Collider other)
        {
            if (!isActiveAndEnabled || destination == null || !destination.isActiveAndEnabled || !(destination is IFPToolContentReceiver)) return;
            var tool = other.GetComponentInParent<FP_JarContentTool>();
            if (tool == null || !tool.isActiveAndEnabled) return;
            if (_contacts.TryGetValue(other, out var previous) && previous != tool) Remove(other);
            _contacts[other] = tool;
            tool.EnterDestination(destination, this, transferOnEnter);
        }
        private void Remove(Collider collider)
        {
            if (!_contacts.TryGetValue(collider, out var tool)) return;
            _contacts.Remove(collider);
            if (tool != null && !_contacts.ContainsValue(tool)) tool.ExitDestination(destination, this);
        }
        private void LateUpdate()
        {
            var trigger = GetComponent<Collider>();
            if (!trigger.enabled || !trigger.isTrigger) { Clear(); return; }
            _stale.Clear();
            foreach (var pair in _contacts)
                if (pair.Key == null || !pair.Key.enabled || !pair.Key.gameObject.activeInHierarchy ||
                    pair.Value == null || pair.Key.GetComponentInParent<FP_JarContentTool>() != pair.Value) _stale.Add(pair.Key);
            foreach (var collider in _stale) Remove(collider);
            _snapshot.Clear();
            _snapshot.AddRange(_contacts);
            foreach (var pair in _snapshot)
            {
                if (!isActiveAndEnabled || pair.Value == null) continue;
                if (pair.Value.isActiveAndEnabled && destination != null && destination.isActiveAndEnabled)
                    pair.Value.EnterDestination(destination, this, transferOnEnter);
                else pair.Value.ExitDestination(destination, this);
            }
        }
        private void OnDisable() => Clear();
        private void Clear()
        {
            var tools = new HashSet<FP_JarContentTool>(_contacts.Values);
            _contacts.Clear();
            foreach (var tool in tools) if (tool != null) tool.ExitDestination(destination, this);
        }
    }
}
