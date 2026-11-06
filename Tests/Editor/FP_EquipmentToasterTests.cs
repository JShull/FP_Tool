// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.Events;

    public class FP_EquipmentToasterTests
    {
        private GameObject _root;
        private FP_EquipmentToaster _toaster;
        private static void Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static float Energy(FP_EnergyReactiveItem item) =>
            (float)typeof(FP_EnergyReactiveItem).GetField("energyAdded", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item);
        private GameObject Child(string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_root.transform, false);
            return obj;
        }

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Toaster tests");
            _toaster = _root.AddComponent<FP_EquipmentToaster>();
            Invoke(_toaster, "Awake");
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_root);

        private FP_ToasterContainmentVolume Volume()
        {
            var obj = Child("Slot trigger");
            obj.AddComponent<BoxCollider>().isTrigger = true;
            var region = obj.AddComponent<FP_ToasterContainmentVolume>();
            Set(region, "toaster", _toaster);
            return region;
        }

        [Test]
        public void LevelClampsAndMapsToDurationWithoutChangingActiveTimer()
        {
            int changed = 0;
            _toaster.OnToastLevelChanged.AddListener(t => changed++);
            _toaster.SetToastLevel(-1f);
            Assert.That(_toaster.ToastDurationSeconds, Is.EqualTo(5f));
            _toaster.AddItemToEquipment(new Item());
            _toaster.StartToasting();
            _toaster.SetToastLevel(2f);
            Assert.That(_toaster.ToastLevel, Is.EqualTo(1f));
            Assert.That(_toaster.ToastDurationSeconds, Is.EqualTo(30f));
            Assert.That(_toaster.Status.TimerRemainingSec, Is.EqualTo(5f));
            _toaster.SetToastLevel(float.NaN);
            _toaster.SetToastLevel(float.PositiveInfinity);
            Assert.That(changed, Is.EqualTo(2));
        }

        [Test]
        public void EmptyStartIsRejectedAndItemsReportEntryExitOnce()
        {
            int missing = 0, entered = 0, exited = 0;
            _toaster.OnNoItem.AddListener(t => missing++);
            _toaster.OnItemEntered.AddListener(t => entered++);
            _toaster.OnItemExited.AddListener(t => exited++);
            _toaster.StartToasting();
            Assert.That(missing, Is.EqualTo(1));
            Assert.That(_toaster.IsToasting, Is.False);
            var item = new Item();
            _toaster.AddItemToEquipment(item);
            _toaster.AddItemToEquipment(item);
            _toaster.RemoveItemFromEquipment(item);
            _toaster.RemoveItemFromEquipment(item);
            Assert.That(entered, Is.EqualTo(1));
            Assert.That(exited, Is.EqualTo(1));
            Assert.That(item.Entries, Is.EqualTo(1));
            Assert.That(item.Exits, Is.EqualTo(1));
            Assert.That(_toaster.Status.Fill, Is.EqualTo(EquipmentFillState.Empty));
        }

        [Test]
        public void ExistingEnergyItemsReceiveExactHeatIncludingFinalPartialTick()
        {
            var first = Child("Bread one").AddComponent<FP_EnergyReactiveItem>();
            var second = Child("Bread two").AddComponent<FP_EnergyReactiveItem>();
            first.ThresholdReachedEvent = new UnityEvent();
            int threshold = 0;
            first.ThresholdReachedEvent.AddListener(() => threshold++);
            Set(_toaster, "heatPerSecond", 10f);
            _toaster.AddItemToEquipment(first);
            _toaster.AddItemToEquipment(second);
            _toaster.StartToastingForSeconds(5f);
            _toaster.SetToastLevel(1f);
            Assert.That(Energy(first), Is.Zero, "State updates must not add implicit heat.");
            _toaster.Tick(2f);
            Assert.That(Energy(first), Is.EqualTo(20f));
            _toaster.Tick(100f);
            _toaster.Tick(100f);
            Assert.That(Energy(first), Is.EqualTo(50f));
            Assert.That(Energy(second), Is.EqualTo(50f));
            Assert.That(threshold, Is.EqualTo(1));
            Assert.That(_toaster.ToastProgress01, Is.EqualTo(1f));
            Assert.That(_toaster.Status.EmitPowerUnit, Is.Zero);
        }

        [Test]
        public void LifecycleEventsAndUserStopAreDistinctAndIdempotent()
        {
            var events = new List<string>();
            var item = new Item();
            _toaster.AddItemToEquipment(item);
            _toaster.OnStarted.AddListener(t => events.Add("started"));
            _toaster.OnToasting.AddListener(t => events.Add("toasting"));
            _toaster.OnToastProgress.AddListener(t => events.Add("progress"));
            _toaster.OnFinished.AddListener(t => events.Add("finished"));
            _toaster.OnCancelled.AddListener(t => events.Add("cancelled"));
            _toaster.StartToastingForSeconds(1f);
            _toaster.StartToasting();
            _toaster.Tick(0.5f);
            _toaster.Tick(10f);
            Assert.That(events, Is.EqualTo(new[] { "started", "toasting", "progress", "progress", "finished" }));
            Assert.That(item.Energy, Is.EqualTo(2f));
            _toaster.StartToastingForSeconds(2f);
            _toaster.StopToasting();
            _toaster.StopToasting();
            _toaster.Tick(20f);
            Assert.That(events[events.Count - 1], Is.EqualTo("cancelled"));
            Assert.That(item.Energy, Is.EqualTo(2f));
            Assert.That(_toaster.ToastProgress01, Is.Zero);
        }

        [Test]
        public void LastItemExitCancelsButOtherItemsKeepToasting()
        {
            var first = new Item();
            var second = new Item();
            _toaster.AddItemToEquipment(first);
            _toaster.AddItemToEquipment(second);
            _toaster.StartToastingForSeconds(2f);
            _toaster.Tick(0.5f);
            _toaster.RemoveItemFromEquipment(first);
            _toaster.Tick(0.5f);
            Assert.That(_toaster.IsToasting, Is.True);
            Assert.That(first.Energy, Is.EqualTo(1f));
            Assert.That(second.Energy, Is.EqualTo(2f));
            _toaster.RemoveItemFromEquipment(second);
            Assert.That(_toaster.IsToasting, Is.False);
        }

        [Test]
        public void TriggerContactsDeduplicateChildCollidersAndOverlappingSlots()
        {
            var first = Volume();
            var second = Volume();
            var bread = Child("Bread").AddComponent<FP_EnergyReactiveItem>();
            var left = Child("Left collider");
            left.transform.SetParent(bread.transform, false);
            var leftCollider = left.AddComponent<BoxCollider>();
            var right = Child("Right collider");
            right.transform.SetParent(bread.transform, false);
            var rightCollider = right.AddComponent<BoxCollider>();
            Invoke(first, "OnTriggerEnter", leftCollider);
            Invoke(first, "OnTriggerEnter", rightCollider);
            Invoke(second, "OnTriggerEnter", rightCollider);
            Assert.That(_toaster.ItemCount, Is.EqualTo(1));
            Invoke(first, "OnTriggerExit", leftCollider);
            Assert.That(_toaster.ItemCount, Is.EqualTo(1));
            Invoke(first, "OnTriggerExit", rightCollider);
            Assert.That(_toaster.ItemCount, Is.EqualTo(1));
            Invoke(second, "OnDisable");
            Assert.That(_toaster.ItemCount, Is.Zero);
        }

        [Test]
        public void DisabledColliderAndItemAreRemovedWithoutExitCallbacks()
        {
            var region = Volume();
            var bread = Child("Bread").AddComponent<FP_EnergyReactiveItem>();
            var collider = bread.gameObject.AddComponent<BoxCollider>();
            Invoke(region, "OnTriggerEnter", collider);
            _toaster.StartToasting();
            collider.enabled = false;
            Invoke(region, "LateUpdate");
            Assert.That(_toaster.ItemCount, Is.Zero);
            Assert.That(_toaster.IsToasting, Is.False);
            collider.enabled = true;
            Invoke(region, "OnTriggerStay", collider);
            _toaster.StartToasting();
            bread.enabled = false;
            _toaster.Tick(1f);
            Assert.That(_toaster.ItemCount, Is.Zero);
            Assert.That(Energy(bread), Is.Zero);
        }

        [Test]
        public void BreakAndCommandsStopHeatAndRepairDoesNotRestart()
        {
            var item = new Item();
            _toaster.AddItemToEquipment(item);
            _toaster.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetHeatLevel01, FValue = 0.75f });
            Assert.That(_toaster.ToastLevel, Is.EqualTo(0.75f));
            _toaster.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetPowerOn });
            _toaster.Execute(new EquipmentCommand { Type = EquipmentCommandType.Break });
            _toaster.StartToasting();
            _toaster.Tick(10f);
            Assert.That(item.Energy, Is.Zero);
            Assert.That(_toaster.Status.Condition, Is.EqualTo(EquipmentConditionState.Broken));
            _toaster.Execute(new EquipmentCommand { Type = EquipmentCommandType.Repair });
            Assert.That(_toaster.IsToasting, Is.False);
            _toaster.Execute(new EquipmentCommand { Type = EquipmentCommandType.StartTimerSeconds, FValue = 1f });
            Assert.That(_toaster.IsToasting, Is.True);
            _toaster.Execute(new EquipmentCommand { Type = EquipmentCommandType.TogglePower });
            Assert.That(_toaster.IsToasting, Is.False);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidTimeDoesNotStartOrAdvance(float dt)
        {
            var item = new Item();
            _toaster.AddItemToEquipment(item);
            _toaster.StartToastingForSeconds(dt);
            Assert.That(_toaster.IsToasting, Is.False);
            _toaster.StartToastingForSeconds(1f);
            _toaster.Tick(dt);
            Assert.That(item.Energy, Is.Zero);
            Assert.That(_toaster.Status.TimerRemainingSec, Is.EqualTo(1f));
        }

        [Test]
        public void EventListenersCanCancelOrReplaceCyclesWithoutStaleCompletion()
        {
            _toaster.AddItemToEquipment(new Item());
            int toasting = 0, finished = 0;
            _toaster.OnStarted.AddListener(t => t.StopToasting());
            _toaster.OnToasting.AddListener(t => toasting++);
            _toaster.StartToasting();
            Assert.That(toasting, Is.Zero);
            _toaster.OnStarted.RemoveAllListeners();
            _toaster.OnFinished.AddListener(t => finished++);
            _toaster.OnToastProgress.AddListener(t => { t.StopToasting(); t.StartToastingForSeconds(5f); });
            _toaster.StartToastingForSeconds(1f);
            _toaster.Tick(2f);
            Assert.That(finished, Is.Zero);
            Assert.That(_toaster.Status.TimerRemainingSec, Is.EqualTo(5f));
        }

        private class Item : IFPContainedItem
        {
            public string ItemId => "bread";
            public float Energy;
            public int Entries, Exits;
            public void OnEnteredEquipment(FP_EquipmentBase equipment) => Entries++;
            public void OnExitedEquipment(FP_EquipmentBase equipment) => Exits++;
            public void OnEquipmentStateChanged(FP_EquipmentBase equipment, EquipmentStatus status) =>
                Assert.That(status.EmitPowerUnit, Is.Zero);
            public void OnEquipmentTick(FP_EquipmentBase equipment, EquipmentStatus status, float dt) => Energy += status.EmitPowerUnit * dt;
        }
    }
}
