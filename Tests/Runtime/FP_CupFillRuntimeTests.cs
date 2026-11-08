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

    public class FP_CupFillRuntimeTests
    {
        private GameObject _root;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
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
        public IEnumerator ThreeParticleInputsFillOneMixedCupAndEmptyResetsNone()
        {
            _root = new GameObject("Typed cup particle test");
            _root.SetActive(false);
            var cup = _root.AddComponent<FP_CupFill>();
            Set(cup, "capacityMl", 30f);
            var receiverObject = Child("Cup inlet");
            receiverObject.AddComponent<BoxCollider>().size = Vector3.one;
            var region = receiverObject.AddComponent<FP_WaterFillRegion>();
            Set(region, "cup", cup);
            var sources = new ParticleSystem[3];
            var relays = new FP_WaterParticleTrigger[3];
            for (int i = 0; i < 3; i++)
            {
                var obj = Child("Liquid source " + (i + 1));
                obj.transform.localPosition = new Vector3((i - 1) * 0.2f, 1f, 0f);
                obj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var ps = obj.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.playOnAwake = false;
                main.loop = true;
                main.startSpeed = 1f;
                main.startLifetime = 2f;
                main.startSize = 0.02f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                var shape = ps.shape;
                shape.angle = 0f;
                shape.radius = 0.001f;
                var emission = ps.emission;
                emission.enabled = false;
                var relay = obj.AddComponent<FP_WaterParticleTrigger>();
                Set(relay, "fillRegions", new[] { region });
                Set(relay, "inputType", (FP_CupLiquidType)(i + 1));
                sources[i] = ps;
                relays[i] = relay;
            }
            int mixed = 0, mixedFull = 0, mixedEmpty = 0, full = 0;
            cup.OnMixed.AddListener(c => mixed++);
            cup.OnMixedFilled.AddListener(c => mixedFull++);
            cup.OnMixedEmpty.AddListener(c => mixedEmpty++);
            cup.OnFilled.AddListener(c => full++);
            _root.SetActive(true);
            Physics.SyncTransforms();
            for (int i = 0; i < 3; i++)
            {
                sources[i].Play(false);
                sources[i].Emit(2);
                float deadline = Time.realtimeSinceStartup + 4f;
                while (relays[i].CapturedParticleCount < 2 && Time.realtimeSinceStartup < deadline) yield return null;
                sources[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                Assert.That(relays[i].CapturedParticleCount, Is.EqualTo(2));
                Assert.That(cup.VolumeMl, Is.EqualTo((i + 1) * 10f));
                Assert.That(cup.LiquidType, Is.EqualTo(i == 0 ? FP_CupLiquidType.Type1 : FP_CupLiquidType.Mixed));
            }
            Assert.That(mixed, Is.EqualTo(1));
            Assert.That(mixedFull, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(1));
            cup.Empty();
            Assert.That(mixedEmpty, Is.EqualTo(1));
            Assert.That(cup.LiquidType, Is.EqualTo(FP_CupLiquidType.None));
            Assert.That(cup.VolumeMl, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ExistingTipControllerPoursCupAndStopsAtEmptyWithoutLosingTypedEvent()
        {
            _root = new GameObject("Cup finite pour test");
            _root.SetActive(false);
            var cup = _root.AddComponent<FP_CupFill>();
            Set(cup, "capacityMl", 50f);
            Set(cup, "volumeMl", 50f);
            Set(cup, "liquidType", FP_CupLiquidType.Type2);
            var particles = Child("Cup pour particles").AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var tip = _root.AddComponent<FP_TipParticlePour>();
            Set(tip, "pourParticles", particles);
            var pour = _root.AddComponent<FP_CupPour>();
            Set(pour, "cup", cup);
            Set(pour, "flowMlPerSecond", 50f);
            Set(pour, "autoAdvance", false);
            int empty = 0, type2Empty = 0, stopped = 0;
            cup.OnEmpty.AddListener(c => empty++);
            cup.OnType2Empty.AddListener(c => { Assert.That(c.LiquidType, Is.EqualTo(FP_CupLiquidType.None)); type2Empty++; });
            cup.OnPourStopped.AddListener(c => stopped++);
            _root.SetActive(true);
            yield return null;
            _root.transform.rotation = Quaternion.Euler(125f, 0f, 0f);
            pour.AdvancePour(0.5f);
            Assert.That(cup.VolumeMl, Is.EqualTo(25f));
            Assert.That(cup.LiquidType, Is.EqualTo(FP_CupLiquidType.Type2));
            Assert.That(cup.IsPouring && tip.IsPouring, Is.True);
            pour.enabled = false;
            Assert.That(cup.IsPouring || tip.IsPouring || particles.emission.enabled, Is.False);
            Assert.That(cup.VolumeMl, Is.EqualTo(25f));
            pour.enabled = true;
            pour.AdvancePour(10f);
            Assert.That(cup.VolumeMl, Is.Zero);
            Assert.That(cup.LiquidType, Is.EqualTo(FP_CupLiquidType.None));
            Assert.That(cup.IsPouring || tip.IsPouring || particles.emission.enabled, Is.False);
            Assert.That(empty, Is.EqualTo(1));
            Assert.That(type2Empty, Is.EqualTo(1));
            Assert.That(stopped, Is.EqualTo(2));
            tip.RefreshPouring();
            Assert.That(particles.emission.enabled, Is.False);
        }
    }
}
