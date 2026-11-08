// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>Reverse containment: entering receivers take content out of the jar.</summary>
    [RequireComponent(typeof(Collider)), DisallowMultipleComponent]
    public class FP_JarAccessRegion : MonoBehaviour
    {
        [SerializeField] private FP_JarContent jar;
        private readonly Dictionary<Collider, MonoBehaviour> _contacts = new Dictionary<Collider, MonoBehaviour>();
        private readonly List<Collider> _stale = new List<Collider>();
        private readonly List<KeyValuePair<Collider, MonoBehaviour>> _snapshot = new List<KeyValuePair<Collider, MonoBehaviour>>();
        private void Reset() { GetComponent<Collider>().isTrigger = true; jar = GetComponentInParent<FP_JarContent>(); }
        private void OnTriggerEnter(Collider other) => Track(other);
        private void OnTriggerStay(Collider other) => Track(other);
        private void OnTriggerExit(Collider other) => Remove(other);
        private void Track(Collider other)
        {
            if (!isActiveAndEnabled || jar == null || !jar.isActiveAndEnabled) return;
            var tool = other.GetComponentInParent<IFPJarContentReceiver>() as MonoBehaviour;
            if (tool == null || !tool.isActiveAndEnabled) return;
            if (_contacts.TryGetValue(other, out var previous) && previous != tool) Remove(other);
            _contacts[other] = tool;
            jar.EnterRegion(tool, this);
        }
        private void Remove(Collider collider)
        {
            if (!_contacts.TryGetValue(collider, out var tool)) return;
            _contacts.Remove(collider);
            if (!_contacts.ContainsValue(tool) && jar != null) jar.ExitRegion(tool, this);
        }
        private void LateUpdate()
        {
            var trigger = GetComponent<Collider>();
            if (!trigger.enabled || !trigger.isTrigger) { Clear(); return; }
            _stale.Clear();
            foreach (var pair in _contacts)
                if (pair.Key == null || !pair.Key.enabled || !pair.Key.gameObject.activeInHierarchy ||
                    pair.Value == null || pair.Key.GetComponentInParent<IFPJarContentReceiver>() as MonoBehaviour != pair.Value)
                    _stale.Add(pair.Key);
            foreach (var collider in _stale) Remove(collider);
            _snapshot.Clear();
            _snapshot.AddRange(_contacts);
            foreach (var pair in _snapshot)
            {
                if (!isActiveAndEnabled || jar == null) break;
                if (pair.Value != null && pair.Value.isActiveAndEnabled) jar.EnterRegion(pair.Value, this);
                else jar.ExitRegion(pair.Value, this);
            }
        }
        private void OnDisable() => Clear();
        private void Clear()
        {
            var tools = new HashSet<MonoBehaviour>(_contacts.Values);
            _contacts.Clear();
            if (jar != null) foreach (var tool in tools) jar.ExitRegion(tool, this);
        }
    }
}
