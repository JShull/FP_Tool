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
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;

    public class FP_JarRuntimeTests
    {
        private GameObject _root;
        private Scene _scene;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private GameObject Child(string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_root.transform, false);
            return obj;
        }
        [SetUp]
        public void SetUp()
        {
            _scene = SceneManager.CreateScene("Jar interaction test", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _root = new GameObject("Jar test root");
            _root.SetActive(false);
            SceneManager.MoveGameObjectToScene(_root, _scene);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene);
        }

        [UnityTest]
        public IEnumerator ToolTriggerTransfersOncePerEntryAndKeepsInformationAfterExit()
        {
            var jarObject = Child("Yogurt jar");
            var jar = jarObject.AddComponent<FP_JarContent>();
            Set(jar, "contentId", "Yogurt");
            Set(jar, "capacityMl", 20f);
            Set(jar, "amountMl", 20f);
            var regionObject = Child("Jar access trigger");
            regionObject.AddComponent<BoxCollider>().size = Vector3.one * 2f;
            regionObject.GetComponent<BoxCollider>().isTrigger = true;
            var region = regionObject.AddComponent<FP_JarAccessRegion>();
            Set(region, "jar", jar);
            var toolObject = Child("Spoon");
            toolObject.transform.localPosition = Vector3.right * 3f;
            var body = toolObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var tool = toolObject.AddComponent<FP_JarContentTool>();
            Set(tool, "capacityMl", 10f);
            var a = Child("Spoon collider a"); a.transform.SetParent(toolObject.transform, false);
            a.transform.localPosition = Vector3.left * 0.1f;
            var aCollider = a.AddComponent<BoxCollider>();
            aCollider.size = Vector3.one * 0.1f;
            var b = Child("Spoon collider b"); b.transform.SetParent(toolObject.transform, false);
            b.transform.localPosition = Vector3.right * 0.1f;
            b.AddComponent<BoxCollider>().size = Vector3.one * 0.1f;
            int entered = 0, exited = 0, receipts = 0;
            jar.OnToolEntered.AddListener((j, t) => entered++);
            jar.OnToolExited.AddListener((j, t) => exited++);
            tool.OnContentReceived.AddListener(t =>
            {
                receipts++;
                Assert.That(jar.AmountMl, Is.EqualTo(20f - receipts * 5f));
                Assert.That(t.ContentId, Is.EqualTo("Yogurt"));
                Assert.That(t.AmountMl, Is.EqualTo(5f));
            });
            _root.SetActive(true);
            yield return null;
            toolObject.transform.localPosition = Vector3.zero;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            yield return null;
            _scene.GetPhysicsScene().Simulate(0.02f);
            Assert.That(entered, Is.EqualTo(1));
            Assert.That(receipts, Is.EqualTo(1));
            Assert.That(jar.AmountMl, Is.EqualTo(15f));
            aCollider.enabled = false;
            _scene.GetPhysicsScene().Simulate(0.02f);
            yield return null;
            Assert.That(exited, Is.Zero);
            toolObject.transform.localPosition = Vector3.right * 3f;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            Assert.That(exited, Is.EqualTo(1));
            Assert.That(tool.AmountMl, Is.EqualTo(5f));
            Assert.That(tool.ContentId, Is.EqualTo("Yogurt"));
            tool.Empty();
            toolObject.transform.localPosition = Vector3.zero;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            Assert.That(entered, Is.EqualTo(2));
            Assert.That(receipts, Is.EqualTo(2));
            Assert.That(jar.AmountMl, Is.EqualTo(10f));
            Object.Destroy(toolObject);
            yield return null;
            yield return null;
            Assert.That(exited, Is.EqualTo(2));
            Assert.That(jar.AmountMl, Is.EqualTo(10f));
        }

        [UnityTest]
        public IEnumerator OptionalJarPourUsesExistingTipAndToolsStillWorkWhenPourIsOff()
        {
            var jar = _root.AddComponent<FP_JarContent>();
            Set(jar, "capacityMl", 50f);
            Set(jar, "amountMl", 50f);
            var ps = Child("Jar pour particles").AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var tip = _root.AddComponent<FP_TipParticlePour>();
            Set(tip, "pourParticles", ps);
            var pour = _root.AddComponent<FP_JarPour>();
            Set(pour, "jar", jar);
            Set(pour, "flowMlPerSecond", 25f);
            Set(pour, "autoAdvance", false);
            int started = 0, stopped = 0;
            jar.OnPourStarted.AddListener(j => started++);
            jar.OnPourStopped.AddListener(j => stopped++);
            _root.SetActive(true);
            yield return null;
            _root.transform.rotation = Quaternion.Euler(125f, 0f, 0f);
            pour.AdvancePour(1f);
            Assert.That(jar.AmountMl, Is.EqualTo(50f));
            Assert.That(ps.emission.enabled, Is.False);
            Assert.That(started, Is.Zero);
            jar.SetPouringEnabled(true);
            pour.AdvancePour(1f);
            Assert.That(jar.AmountMl, Is.EqualTo(25f));
            Assert.That(jar.IsPouring && tip.IsPouring, Is.True);
            jar.SetPouringEnabled(false);
            Assert.That(jar.IsPouring || tip.IsPouring || ps.emission.enabled, Is.False);
            Assert.That(jar.AmountMl, Is.EqualTo(25f));
            jar.SetPouringEnabled(true);
            pour.AdvancePour(10f);
            Assert.That(jar.IsEmpty, Is.True);
            Assert.That(ps.emission.enabled, Is.False);
            Assert.That(stopped, Is.EqualTo(2));
            jar.SetPouringEnabled(false);
            jar.SetAmountMl(10f);
            var tool = Child("Spoon").AddComponent<FP_JarContentTool>();
            Assert.That(jar.TryTakeContent(tool), Is.EqualTo(5f));
            Assert.That(jar.AmountMl, Is.EqualTo(5f));
        }
    }
}
