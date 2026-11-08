// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Collections;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    public class FP_KettleRuntimeTests
    {
        private GameObject _root;
        private static void Set(object target, string field, object value, System.Type declaringType = null) =>
            (declaringType ?? target.GetType()).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private GameObject Child(string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_root.transform, false);
            return obj;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExistingTipPourerDrainsOnlyAvailableWaterAndRetainsHeat()
        {
            _root = new GameObject("Kettle pour test");
            _root.SetActive(false);
            var kettle = _root.AddComponent<FP_EquipmentKettle>();
            Set(kettle, "autoRegisterWithTickSystem", false, typeof(FP_EquipmentTickableBase));
            Set(kettle, "waterMl", 200f);
            Set(kettle, "coolingSeconds", 3f);
            var particles = Child("Spout particles").AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.startLifetime = 1f;
            main.loop = true;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = particles.emission;
            emission.rateOverTime = 30f;
            var tip = _root.AddComponent<FP_TipParticlePour>();
            Set(tip, "pourParticles", particles);
            var pour = _root.AddComponent<FP_KettlePour>();
            Set(pour, "kettle", kettle);
            Set(pour, "flowMlPerSecond", 100f);
            Set(pour, "autoAdvance", false);
            _root.SetActive(true);
            yield return null;
            int started = 0, stopped = 0;
            kettle.OnPourStarted.AddListener(k => started++);
            kettle.OnPourStopped.AddListener(k => stopped++);
            kettle.StartHeatingForSeconds(1f);
            kettle.Tick(1f);
            Assert.That(kettle.IsWaterHot, Is.True);
            _root.transform.rotation = Quaternion.Euler(125f, 0f, 0f);
            pour.AdvancePour(0.5f);
            Assert.That(kettle.WaterMl, Is.EqualTo(150f));
            Assert.That(kettle.IsWaterHot, Is.True);
            Assert.That(kettle.IsPouring && tip.IsPouring, Is.True);
            Assert.That(started, Is.EqualTo(1));
            yield return new WaitForSeconds(0.15f);
            Assert.That(particles.particleCount, Is.GreaterThan(0));
            pour.enabled = false;
            Assert.That(kettle.IsPouring || tip.IsPouring || particles.emission.enabled, Is.False);
            Assert.That(kettle.WaterMl, Is.EqualTo(150f));
            pour.enabled = true;
            Assert.That(kettle.IsPouring && tip.IsPouring, Is.True);
            Assert.That(started, Is.EqualTo(2));
            pour.AdvancePour(100f);
            Assert.That(kettle.WaterMl, Is.Zero);
            Assert.That(kettle.IsPouring || tip.IsPouring || particles.emission.enabled, Is.False);
            Assert.That(stopped, Is.EqualTo(2));
            tip.RefreshPouring();
            yield return null;
            Assert.That(particles.emission.enabled, Is.False);
            _root.transform.rotation = Quaternion.identity;
            kettle.AddWaterMl(100f);
            kettle.StartHeatingForSeconds(1f);
            kettle.Tick(1f);
            kettle.Tick(3f);
            Assert.That(kettle.IsWaterCold, Is.True);
        }

        [UnityTest]
        public IEnumerator ExistingWaterParticleTriggerRefillsKettle()
        {
            _root = new GameObject("Kettle water particle test");
            _root.SetActive(false);
            var kettle = _root.AddComponent<FP_EquipmentKettle>();
            Set(kettle, "autoRegisterWithTickSystem", false, typeof(FP_EquipmentTickableBase));
            var regionObject = Child("Inlet");
            regionObject.AddComponent<BoxCollider>().size = Vector3.one;
            var region = regionObject.AddComponent<FP_WaterFillRegion>();
            Set(region, "kettle", kettle);
            var source = Child("Water source");
            source.transform.localPosition = Vector3.up;
            source.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var particles = source.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startSpeed = 1f;
            main.startLifetime = 2f;
            main.startSize = 0.02f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var shape = particles.shape;
            shape.angle = 0f;
            shape.radius = 0.001f;
            var emission = particles.emission;
            emission.rateOverTime = 30f;
            var relay = source.AddComponent<FP_WaterParticleTrigger>();
            Set(relay, "fillRegions", new[] { region });
            _root.SetActive(true);
            Physics.SyncTransforms();
            particles.Play(false);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (relay.CapturedParticleCount < 3 && Time.realtimeSinceStartup < deadline) yield return null;
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            Assert.That(relay.CapturedParticleCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(kettle.WaterMl, Is.EqualTo(relay.CapturedParticleCount * 5f));
            Assert.That(kettle.IsWaterCold, Is.True);
        }
    }
}
