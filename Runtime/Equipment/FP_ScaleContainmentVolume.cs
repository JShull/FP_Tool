// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>One trigger collider per volume; overlapping volumes may share a scale.</summary>
    [RequireComponent(typeof(Collider)), DisallowMultipleComponent]
    public class FP_ScaleContainmentVolume : MonoBehaviour
    {
        [SerializeField] private FP_EquipmentScale scale;
        private readonly Dictionary<Collider, FP_WeightedItem> _contacts = new Dictionary<Collider, FP_WeightedItem>();
        private readonly List<Collider> _stale = new List<Collider>();

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            scale = GetComponentInParent<FP_EquipmentScale>();
        }

        private void OnTriggerEnter(Collider other) => Track(other);
        private void OnTriggerStay(Collider other) => Track(other);

        private void Track(Collider other)
        {
            if (!isActiveAndEnabled || scale == null || !scale.isActiveAndEnabled) return;
            var item = other.GetComponentInParent<FP_WeightedItem>();
            if (item == null || !item.isActiveAndEnabled) return;
            if (_contacts.TryGetValue(other, out var previous) && previous != item) RemoveContact(other);
            _contacts[other] = item;
            scale.EnterVolume(item, this);
        }

        private void OnTriggerExit(Collider other) => RemoveContact(other);

        private void RemoveContact(Collider collider)
        {
            if (!_contacts.TryGetValue(collider, out var item)) return;
            _contacts.Remove(collider);
            if (!_contacts.ContainsValue(item) && scale != null) scale.ExitVolume(item, this);
        }

        private void LateUpdate()
        {
            var trigger = GetComponent<Collider>();
            if (!trigger.enabled || !trigger.isTrigger)
            {
                ClearContacts();
                return;
            }
            _stale.Clear();
            foreach (var contact in _contacts)
                if (contact.Key == null || !contact.Key.enabled || !contact.Key.gameObject.activeInHierarchy ||
                    contact.Value == null ||
                    contact.Key.GetComponentInParent<FP_WeightedItem>() != contact.Value)
                    _stale.Add(contact.Key);
            foreach (var collider in _stale) RemoveContact(collider);
            // Stay is not guaranteed for sleeping bodies. Keep known contacts so an
            // item or scale can be re-enabled without requiring physical movement.
            if (scale != null)
                foreach (var contact in _contacts)
                    if (contact.Value.isActiveAndEnabled) scale.EnterVolume(contact.Value, this);
        }

        private void OnDisable() => ClearContacts();

        private void ClearContacts()
        {
            if (scale != null)
                foreach (var item in new HashSet<FP_WeightedItem>(_contacts.Values)) scale.ExitVolume(item, this);
            _contacts.Clear();
        }
    }
}
