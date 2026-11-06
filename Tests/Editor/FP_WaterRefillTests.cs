// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;

    public class FP_WaterRefillTests
    {
        private GameObject _root;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        [SetUp] public void SetUp() => _root = new GameObject("Water refill tests");
        [TearDown] public void TearDown() => Object.DestroyImmediate(_root);
        private GameObject Child(string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_root.transform, false);
            return obj;
        }

        [Test]
        public void ReceiverAddsMillilitersClampsCapacityAndRejectsInvalidOrInactiveTransfers()
        {
            var maker = Child("Coffee maker").AddComponent<FP_EquipmentCoffeeMaker>();
            Invoke(maker, "Awake");
            var region = Child("Water inlet").AddComponent<FP_WaterFillRegion>();
            Invoke(region, "Awake");
            Set(region, "coffeeMaker", maker);
            Assert.That(region.TryReceiveWater(250f), Is.True);
            Assert.That(maker.WaterMl, Is.EqualTo(250f));
            Assert.That(region.TryReceiveWater(float.NaN), Is.False);
            Assert.That(region.TryReceiveWater(float.PositiveInfinity), Is.False);
            Assert.That(region.TryReceiveWater(-1f), Is.False);
            region.enabled = false;
            Assert.That(region.TryReceiveWater(250f), Is.False);
            region.enabled = true;
            Assert.That(region.TryReceiveWater(10000f), Is.True);
            Assert.That(maker.WaterMl, Is.EqualTo(1000f));
            Assert.That(region.TryReceiveWater(5f), Is.True);
            region.RegionCollider.enabled = false;
            Assert.That(region.TryReceiveWater(5f), Is.False);
        }

        [Test]
        public void WaterParticleSetupUsesEnterCallbackAndCanClearRegions()
        {
            var region = Child("Water inlet").AddComponent<FP_WaterFillRegion>();
            var obj = Child("Water stream");
            var relay = obj.AddComponent<FP_WaterParticleTrigger>();
            Set(relay, "fillRegions", new[] { region });
            relay.RefreshTriggerRegions();
            var trigger = obj.GetComponent<ParticleSystem>().trigger;
            Assert.That(trigger.enabled, Is.True);
            Assert.That(trigger.enter, Is.EqualTo(ParticleSystemOverlapAction.Callback));
            Assert.That(trigger.inside, Is.EqualTo(ParticleSystemOverlapAction.Ignore));
            Assert.That(trigger.colliderQueryMode, Is.EqualTo(ParticleSystemColliderQueryMode.All));
            Assert.That(trigger.GetCollider(0), Is.EqualTo(region.RegionCollider));
            Set(relay, "fillRegions", new FP_WaterFillRegion[0]);
            relay.RefreshTriggerRegions();
            Assert.That(trigger.GetCollider(0), Is.Null);
        }
    }
}
