// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Collections;
    using System.Reflection;
    using FuzzPhyte.Utility;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;

    public class FP_ToasterPhysicsTests
    {
        private Scene _scene;
        private GameObject _ticks;
        private static void Set(object target, string field, object value, System.Type declaringType = null) =>
            (declaringType ?? target.GetType()).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static float Energy(FP_EnergyReactiveItem item) =>
            (float)typeof(FP_EnergyReactiveItem).GetField("energyAdded", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(item);

        private GameObject Create(string name)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, _scene);
            return obj;
        }

        [SetUp]
        public void SetUp() => _scene = SceneManager.CreateScene("Toaster physics test", new CreateSceneParameters(LocalPhysicsMode.Physics3D));

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_ticks != null) Object.Destroy(_ticks);
            if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene);
            yield return null;
        }

        private FP_EnergyReactiveItem Bread(string name, Vector3 position)
        {
            var obj = Create(name);
            obj.transform.position = position;
            var body = obj.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            return obj.AddComponent<FP_EnergyReactiveItem>();
        }

        [UnityTest]
        public IEnumerator TriggerEntryExitAndHeatReachOnlyContainedItems()
        {
            var toaster = Create("Toaster").AddComponent<FP_EquipmentToaster>();
            Set(toaster, "autoRegisterWithTickSystem", false, typeof(FP_EquipmentTickableBase));
            var volumeObject = Create("Slot trigger");
            var trigger = volumeObject.AddComponent<BoxCollider>();
            trigger.size = Vector3.one * 2f;
            trigger.isTrigger = true;
            var region = volumeObject.AddComponent<FP_ToasterContainmentVolume>();
            Set(region, "toaster", toaster);
            var first = Bread("First slice", Vector3.zero);
            var left = Create("Left collider");
            left.transform.SetParent(first.transform, false);
            left.transform.localPosition = Vector3.left * 0.2f;
            var leftCollider = left.AddComponent<BoxCollider>();
            leftCollider.size = Vector3.one * 0.2f;
            var right = Create("Right collider");
            right.transform.SetParent(first.transform, false);
            right.transform.localPosition = Vector3.right * 0.2f;
            right.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            var second = Bread("Second slice", Vector3.up * 0.6f);
            second.gameObject.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            int entered = 0, exited = 0, cancelled = 0, finished = 0;
            toaster.OnItemEntered.AddListener(t => entered++);
            toaster.OnItemExited.AddListener(t => exited++);
            toaster.OnCancelled.AddListener(t => cancelled++);
            toaster.OnFinished.AddListener(t => finished++);
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            yield return null;
            Assert.That(toaster.ItemCount, Is.EqualTo(2));
            Assert.That(entered, Is.EqualTo(2));

            toaster.StartToastingForSeconds(1f);
            toaster.Tick(0.25f);
            Assert.That(Energy(first), Is.EqualTo(0.5f));
            Assert.That(Energy(second), Is.EqualTo(0.5f));
            leftCollider.enabled = false;
            _scene.GetPhysicsScene().Simulate(0.02f);
            yield return null;
            Assert.That(toaster.ItemCount, Is.EqualTo(2));
            second.transform.position = Vector3.right * 5f;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            Assert.That(toaster.ItemCount, Is.EqualTo(1));
            toaster.Tick(0.25f);
            Assert.That(Energy(first), Is.EqualTo(1f));
            Assert.That(Energy(second), Is.EqualTo(0.5f));
            first.transform.position = Vector3.right * 5f;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            Assert.That(toaster.IsToasting, Is.False);
            Assert.That(exited, Is.EqualTo(2));
            Assert.That(cancelled, Is.EqualTo(1));

            first.transform.position = Vector3.zero;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            toaster.StartToastingForSeconds(0.5f);
            toaster.Tick(1f);
            Assert.That(finished, Is.EqualTo(1));
            Assert.That(Energy(first), Is.EqualTo(2f));
            Assert.That(toaster.Status.Power, Is.EqualTo(EquipmentPowerState.Off));
            toaster.StartToasting();
            Object.Destroy(first.gameObject);
            yield return null;
            yield return null;
            Assert.That(toaster.ItemCount, Is.Zero);
            Assert.That(toaster.IsToasting, Is.False);
        }

        [UnityTest]
        public IEnumerator DispatcherUsesToasterHeatTickAndReregistersAfterDisable()
        {
            Assert.That(FP_TickSystem.CCTick == null, Is.True, "Requires an isolated test scene.");
            _ticks = new GameObject("Toaster tick system");
            _ticks.AddComponent<FP_TickSystem>();
            var toaster = Create("Toaster").AddComponent<FP_EquipmentToaster>();
            var bread = Create("Bread").AddComponent<FP_EnergyReactiveItem>();
            toaster.AddItemToEquipment(bread);
            yield return null;
            toaster.StartToastingForSeconds(0.04f);
            float deadline = Time.realtimeSinceStartup + 2f;
            while (toaster.IsToasting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(toaster.IsToasting, Is.False);
            Assert.That(Energy(bread), Is.EqualTo(0.08f).Within(0.00001f));
            toaster.StartToastingForSeconds(10f);
            toaster.enabled = false;
            Assert.That(toaster.ItemCount, Is.Zero);
            Assert.That(toaster.IsToasting, Is.False);
            toaster.enabled = true;
            toaster.AddItemToEquipment(bread);
            toaster.StartToastingForSeconds(0.04f);
            deadline = Time.realtimeSinceStartup + 2f;
            while (toaster.IsToasting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(toaster.IsToasting, Is.False);
            Assert.That(Energy(bread), Is.EqualTo(0.16f).Within(0.00001f));
        }
    }
}
