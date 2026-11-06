// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;

    public class FP_BeanPourTests
    {
        private GameObject _root;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        [SetUp] public void SetUp() => _root = new GameObject("Bean pour tests");
        [TearDown] public void TearDown() => Object.DestroyImmediate(_root);
        private GameObject Child(string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_root.transform, false);
            return obj;
        }

        [Test]
        public void TipThresholdsHaveHysteresisAndPreserveFlyingParticles()
        {
            var ps = Child("Particles").AddComponent<ParticleSystem>();
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var pour = _root.AddComponent<FP_TipParticlePour>();
            Set(pour, "pourParticles", ps);
            int starts = 0, stops = 0;
            pour.OnPourStarted.AddListener(() => starts++);
            pour.OnPourStopped.AddListener(() => stops++);
            pour.RefreshPouring();
            Assert.That(ps.emission.enabled, Is.False);
            _root.transform.rotation = Quaternion.Euler(110f, 0f, 0f);
            pour.RefreshPouring();
            Assert.That(pour.IsPouring, Is.True);
            ps.Emit(1);
            _root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pour.RefreshPouring();
            Assert.That(pour.IsPouring, Is.True);
            _root.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
            pour.RefreshPouring();
            Assert.That(ps.emission.enabled, Is.False);
            Assert.That(ps.particleCount, Is.GreaterThan(0));
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(1));
        }

        [Test]
        public void PourUsesAssignedOriginAndUprightAxisAndStopsOnDisable()
        {
            var ps = Child("Particles").AddComponent<ParticleSystem>();
            ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var origin = Child("Origin").transform;
            var pour = _root.AddComponent<FP_TipParticlePour>();
            Set(pour, "origin", origin);
            Set(pour, "localUpAxis", Vector3.right);
            Set(pour, "pourParticles", ps);
            origin.rotation = Quaternion.Euler(0f, 0f, -90f);
            pour.RefreshPouring();
            Assert.That(pour.IsPouring, Is.True);
            Invoke(pour, "OnDisable");
            Assert.That(pour.IsPouring, Is.False);
            Assert.That(ps.emission.enabled, Is.False);
        }

        [Test]
        public void ReceiverClampsSupplyAndRejectsInactiveOrInvalidTransfers()
        {
            var maker = Child("Coffee maker").AddComponent<FP_EquipmentCoffeeMaker>();
            Invoke(maker, "Awake");
            var receiver = Child("Receiver").AddComponent<FP_BeanFillRegion>();
            Invoke(receiver, "Awake");
            Set(receiver, "coffeeMaker", maker);
            Assert.That(receiver.TryReceiveBeans(15f), Is.True);
            Assert.That(maker.BeansGrams, Is.EqualTo(15f));
            Assert.That(receiver.TryReceiveBeans(float.NaN), Is.False);
            Assert.That(receiver.TryReceiveBeans(float.PositiveInfinity), Is.False);
            Assert.That(receiver.TryReceiveBeans(-1f), Is.False);
            receiver.enabled = false;
            Assert.That(receiver.TryReceiveBeans(15f), Is.False);
            receiver.enabled = true;
            Assert.That(receiver.TryReceiveBeans(1000f), Is.True);
            Assert.That(maker.BeansGrams, Is.EqualTo(250f));
            Assert.That(receiver.TryReceiveBeans(1f), Is.True);
            Assert.That(maker.BeansGrams, Is.EqualTo(250f));
            receiver.RegionCollider.enabled = false;
            Assert.That(receiver.TryReceiveBeans(1f), Is.False);
        }

        [Test]
        public void TriggerConfigurationUsesEnterCallbackAndColliderData()
        {
            var receiver = Child("Receiver").AddComponent<FP_BeanFillRegion>();
            var emitter = Child("Emitter");
            var relay = emitter.AddComponent<FP_BeanParticleTrigger>();
            Set(relay, "fillRegions", new[] { receiver });
            relay.RefreshTriggerRegions();
            var trigger = emitter.GetComponent<ParticleSystem>().trigger;
            Assert.That(trigger.enabled, Is.True);
            Assert.That(trigger.enter, Is.EqualTo(ParticleSystemOverlapAction.Callback));
            Assert.That(trigger.inside, Is.EqualTo(ParticleSystemOverlapAction.Ignore));
            Assert.That(trigger.colliderQueryMode, Is.EqualTo(ParticleSystemColliderQueryMode.All));
            Assert.That(trigger.GetCollider(0), Is.EqualTo(receiver.RegionCollider));
            Set(relay, "fillRegions", new FP_BeanFillRegion[0]);
            relay.RefreshTriggerRegions();
            Assert.That(trigger.GetCollider(0), Is.Null);
        }
    }
}
