// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>Trigger enter/exit containment for energy-reactive items, including child colliders.</summary>
    [RequireComponent(typeof(Collider)), DisallowMultipleComponent]
    public class FP_ToasterContainmentVolume : MonoBehaviour
    {
        [SerializeField] private FP_EquipmentToaster toaster;
        private readonly Dictionary<Collider, FP_EnergyReactiveItem> _contacts = new Dictionary<Collider, FP_EnergyReactiveItem>();
        private readonly List<Collider> _stale = new List<Collider>();
        private readonly List<KeyValuePair<Collider, FP_EnergyReactiveItem>> _snapshot = new List<KeyValuePair<Collider, FP_EnergyReactiveItem>>();

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            toaster = GetComponentInParent<FP_EquipmentToaster>();
        }

        private void OnTriggerEnter(Collider other) => Track(other);
        private void OnTriggerStay(Collider other) => Track(other);
        private void OnTriggerExit(Collider other) => RemoveContact(other);

        private void Track(Collider other)
        {
            if (!isActiveAndEnabled || toaster == null || !toaster.isActiveAndEnabled) return;
            var item = other.GetComponentInParent<FP_EnergyReactiveItem>();
            if (item == null || !item.isActiveAndEnabled) return;
            if (_contacts.TryGetValue(other, out var previous) && previous != item) RemoveContact(other);
            _contacts[other] = item;
            toaster.EnterVolume(item, this, other.attachedRigidbody);
        }

        private void RemoveContact(Collider collider)
        {
            if (!_contacts.TryGetValue(collider, out var item)) return;
            _contacts.Remove(collider);
            if (!_contacts.ContainsValue(item) && toaster != null) toaster.ExitVolume(item, this);
        }

        private void LateUpdate()
        {
            var trigger = GetComponent<Collider>();
            if (!trigger.enabled || !trigger.isTrigger) { ClearContents(); return; }
            _stale.Clear();
            foreach (var contact in _contacts)
                if (contact.Key == null || !contact.Key.enabled || !contact.Key.gameObject.activeInHierarchy ||
                    contact.Value == null || contact.Key.GetComponentInParent<FP_EnergyReactiveItem>() != contact.Value)
                    _stale.Add(contact.Key);
            foreach (var collider in _stale) RemoveContact(collider);
            // Recover tracked sleeping items after enabling their marker or toaster.
            if (toaster == null) return;
            _snapshot.Clear();
            _snapshot.AddRange(_contacts);
            foreach (var contact in _snapshot)
            {
                if (!isActiveAndEnabled || toaster == null) break;
                if (contact.Key != null && contact.Value != null && contact.Value.isActiveAndEnabled)
                    toaster.EnterVolume(contact.Value, this, contact.Key.attachedRigidbody);
                else toaster.ExitVolume(contact.Value, this);
            }
        }

        private void OnDisable() => ClearContents();
        private void ClearContents()
        {
            var items = new HashSet<FP_EnergyReactiveItem>(_contacts.Values);
            _contacts.Clear();
            if (toaster != null) foreach (var item in items) toaster.ExitVolume(item, this);
        }
    }
}
