// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public class FP_EquipmentScaleTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private FP_EquipmentScale _scale;

        private GameObject Create(string name)
        {
            var result = new GameObject(name);
            _objects.Add(result);
            return result;
        }

        private static void Invoke(object target, string method, params object[] arguments) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);

        [SetUp]
        public void SetUp()
        {
            _scale = Create("Scale").AddComponent<FP_EquipmentScale>();
            Invoke(_scale, "Awake");
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        private FP_WeightedItem Item(float kg)
        {
            var item = Create("Weighted item").AddComponent<FP_WeightedItem>();
            item.SetWeightKg(kg);
            return item;
        }

        private FP_ScaleContainmentVolume Volume()
        {
            var obj = Create("Volume");
            obj.AddComponent<BoxCollider>().isTrigger = true;
            var volume = obj.AddComponent<FP_ScaleContainmentVolume>();
            var serialized = new SerializedObject(volume);
            serialized.FindProperty("scale").objectReferenceValue = _scale;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return volume;
        }

        private Collider ChildCollider(FP_WeightedItem item)
        {
            var obj = Create("Item collider");
            obj.transform.SetParent(item.transform);
            return obj.AddComponent<BoxCollider>();
        }

        [Test]
        public void TotalsDistinctItemsAndUpdatesWhileContained()
        {
            var first = Item(1.25f);
            var second = Item(2.5f);
            _scale.AddItemToEquipment(first);
            _scale.AddItemToEquipment(second);
            _scale.AddItemToEquipment(first);
            Assert.That(_scale.TotalWeightKg, Is.EqualTo(3.75d));
            Assert.That(_scale.ItemCount, Is.EqualTo(2));
            first.SetWeightKg(0.5f);
            Assert.That(_scale.TotalWeightKg, Is.EqualTo(3d));
            _scale.RemoveItemFromEquipment(second);
            second.SetWeightKg(10f);
            Assert.That(_scale.TotalWeightKg, Is.EqualTo(0.5d));
            Assert.That(_scale.Status.EmitPowerUnit, Is.Zero);
        }

        [Test]
        public void MultipleChildCollidersRemainUntilLastExit()
        {
            var volume = Volume();
            var item = Item(3f);
            var first = ChildCollider(item);
            var second = ChildCollider(item);
            Invoke(volume, "OnTriggerEnter", first);
            Invoke(volume, "OnTriggerStay", first);
            Invoke(volume, "OnTriggerEnter", second);
            Assert.That(_scale.ItemCount, Is.EqualTo(1));
            Invoke(volume, "OnTriggerExit", first);
            Assert.That(_scale.TotalWeightKg, Is.EqualTo(3d));
            Invoke(volume, "OnTriggerExit", second);
            Assert.That(_scale.TotalWeightKg, Is.Zero);
            Assert.That(_scale.Status.Fill, Is.EqualTo(EquipmentFillState.Empty));
        }

        [Test]
        public void OverlappingVolumesShareOneItemAndDisableReleasesOnlyItsContacts()
        {
            var first = Volume();
            var second = Volume();
            var collider = ChildCollider(Item(2f));
            Invoke(first, "OnTriggerEnter", collider);
            Invoke(second, "OnTriggerEnter", collider);
            Assert.That(_scale.TotalWeightKg, Is.EqualTo(2d));
            Invoke(first, "OnDisable");
            Assert.That(_scale.TotalWeightKg, Is.EqualTo(2d));
            Invoke(second, "OnDisable");
            Assert.That(_scale.ItemCount, Is.Zero);
        }

        [Test]
        public void DisabledColliderIsPrunedAndStayRestoresContact()
        {
            var volume = Volume();
            var collider = ChildCollider(Item(2f));
            Invoke(volume, "OnTriggerEnter", collider);
            collider.enabled = false;
            Invoke(volume, "LateUpdate");
            Assert.That(_scale.ItemCount, Is.Zero);
            collider.enabled = true;
            Invoke(volume, "OnTriggerStay", collider);
            Assert.That(_scale.TotalWeightKg, Is.EqualTo(2d));
        }

        [Test]
        public void DisabledAndDestroyedItemsDoNotLeaveStaleWeight()
        {
            var volume = Volume();
            var item = Item(2f);
            var collider = ChildCollider(item);
            Invoke(volume, "OnTriggerEnter", collider);
            item.enabled = false;
            Invoke(item, "OnDisable");
            Assert.That(_scale.ItemCount, Is.Zero);
            item.enabled = true;
            Invoke(volume, "OnTriggerStay", collider);
            Assert.That(_scale.ItemCount, Is.EqualTo(1));
            Object.DestroyImmediate(item.gameObject);
            Invoke(volume, "LateUpdate");
            Assert.That(_scale.TotalWeightKg, Is.Zero);
        }

        [Test]
        public void ScaleDisableClearsSubscriptionsAndStayRestoresContents()
        {
            var volume = Volume();
            var item = Item(2f);
            var collider = ChildCollider(item);
            Invoke(volume, "OnTriggerEnter", collider);
            _scale.enabled = false;
            Invoke(_scale, "OnDisable");
            item.SetWeightKg(5f);
            Assert.That(_scale.ItemCount, Is.Zero);
            _scale.enabled = true;
            Invoke(volume, "OnTriggerStay", collider);
            Assert.That(_scale.TotalWeightKg, Is.EqualTo(5d));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidWeightBecomesZero(float value)
        {
            var item = Item(2f);
            _scale.AddItemToEquipment(item);
            item.SetWeightKg(value);
            Assert.That(_scale.TotalWeightKg, Is.Zero);
        }

        [Test]
        public void BrokenAndOffScalesKeepContentsButSuppressReadout()
        {
            _scale.AddItemToEquipment(Item(2f));
            Assert.That(_scale.IsReadingAvailable, Is.True);
            _scale.Execute(new EquipmentCommand { Type = EquipmentCommandType.Break });
            Assert.That(_scale.IsReadingAvailable, Is.False);
            _scale.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetPowerOn });
            Assert.That(_scale.IsReadingAvailable, Is.False);
            _scale.Execute(new EquipmentCommand { Type = EquipmentCommandType.Repair });
            Assert.That(_scale.IsReadingAvailable, Is.False);
            _scale.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetPowerOn });
            Assert.That(_scale.IsReadingAvailable, Is.True);
            Assert.That(_scale.TotalWeightKg, Is.EqualTo(2d));
        }

        [Test]
        public void WeightEventFiresOnlyForChangedTotalsIncludingZeroWeightItems()
        {
            int changes = 0;
            _scale.OnWeightChanged.AddListener(scale => changes++);
            var item = Item(0f);
            _scale.AddItemToEquipment(item);
            Assert.That(_scale.ItemCount, Is.EqualTo(1));
            Assert.That(changes, Is.Zero);
            item.SetWeightKg(2f);
            item.SetWeightKg(2f);
            _scale.RemoveItemFromEquipment(item);
            Assert.That(changes, Is.EqualTo(2));
        }
    }
}
