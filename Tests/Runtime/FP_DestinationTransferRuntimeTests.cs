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

    public class FP_DestinationTransferRuntimeTests
    {
        private Scene _scene;
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
            if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene);
        }

        [UnityTest]
        public IEnumerator DifferentJarsMixOnToolAndTransferThroughOneDestinationTrigger()
        {
            _scene = SceneManager.CreateScene("Mixed food workflow", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _root = new GameObject("Mixed workflow");
            _root.SetActive(false);
            SceneManager.MoveGameObjectToScene(_root, _scene);
            for (int i = 0; i < 2; i++)
            {
                var obj = Child("Source " + i);
                obj.transform.localPosition = Vector3.right * (i * 3f);
                var jar = obj.AddComponent<FP_JarContent>();
                Set(jar, "contentType", i == 0 ? FP_ContentType.Type1 : FP_ContentType.Type2);
                Set(jar, "contentId", i == 0 ? "PeanutButter" : "Mayo");
                obj.AddComponent<BoxCollider>().isTrigger = true;
                Set(obj.AddComponent<FP_JarAccessRegion>(), "jar", jar);
            }
            var bread = Child("Bread");
            bread.transform.localPosition = Vector3.right * 6f;
            var destination = bread.AddComponent<FP_ContentDestination>();
            Set(destination, "capacityMl", 10f);
            bread.AddComponent<BoxCollider>().isTrigger = true;
            Set(bread.AddComponent<FP_ContentDestinationRegion>(), "destination", destination);
            var knife = Child("Knife");
            knife.transform.localPosition = Vector3.left * 3f;
            knife.AddComponent<Rigidbody>().isKinematic = true;
            knife.AddComponent<BoxCollider>().size = Vector3.one * 0.1f;
            var tool = knife.AddComponent<FP_JarContentTool>();
            Set(tool, "capacityMl", 10f);
            _root.SetActive(true);
            yield return null;
            int toolMixed = 0, destinationMixed = 0, mixedEmpty = 0;
            tool.OnMixed.AddListener(t => toolMixed++);
            tool.OnMixedEmpty.AddListener(t => mixedEmpty++);
            destination.OnMixed.AddListener(d => destinationMixed++);
            for (int i = 0; i < 3; i++)
            {
                knife.transform.localPosition = Vector3.right * (i * 3f);
                Physics.SyncTransforms();
                _scene.GetPhysicsScene().Simulate(0.02f);
                if (i == 0) Assert.That(tool.ContentType, Is.EqualTo(FP_ContentType.Type1));
                if (i == 1) Assert.That(tool.ContentType, Is.EqualTo(FP_ContentType.Mixed));
            }
            Assert.That(destination.AmountMl, Is.EqualTo(10f));
            Assert.That(destination.ContentType, Is.EqualTo(FP_ContentType.Mixed));
            Assert.That(tool.AmountMl, Is.Zero);
            Assert.That(tool.ContentType, Is.EqualTo(FP_ContentType.None));
            Assert.That(toolMixed, Is.EqualTo(1));
            Assert.That(destinationMixed, Is.EqualTo(1));
            Assert.That(mixedEmpty, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator JarToKnifeToBreadTransfersAndResetsThroughActualTriggers()
        {
            _scene = SceneManager.CreateScene("Destination workflow test", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _root = new GameObject("Jar knife bread workflow");
            _root.SetActive(false);
            SceneManager.MoveGameObjectToScene(_root, _scene);
            var jar = Child("Mayo jar").AddComponent<FP_JarContent>();
            Set(jar, "contentId", "Mayo");
            Set(jar, "amountMl", 20f);
            var jarRegionObject = Child("Jar access region");
            jarRegionObject.AddComponent<BoxCollider>().size = Vector3.one;
            jarRegionObject.GetComponent<BoxCollider>().isTrigger = true;
            var jarRegion = jarRegionObject.AddComponent<FP_JarAccessRegion>();
            Set(jarRegion, "jar", jar);
            var breadObject = Child("Bread");
            breadObject.transform.localPosition = Vector3.right * 3f;
            var destination = breadObject.AddComponent<FP_ContentDestination>();
            Set(destination, "capacityMl", 8f);
            breadObject.AddComponent<BoxCollider>().size = Vector3.one;
            breadObject.GetComponent<BoxCollider>().isTrigger = true;
            var destinationRegion = breadObject.AddComponent<FP_ContentDestinationRegion>();
            Set(destinationRegion, "destination", destination);
            var toolObject = Child("Knife");
            toolObject.transform.localPosition = Vector3.left * 2f;
            var body = toolObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var tool = toolObject.AddComponent<FP_JarContentTool>();
            for (int i = 0; i < 2; i++)
            {
                var colliderObject = Child("Knife collider " + i);
                colliderObject.transform.SetParent(toolObject.transform, false);
                colliderObject.transform.localPosition = Vector3.right * (i == 0 ? -0.1f : 0.1f);
                colliderObject.AddComponent<BoxCollider>().size = Vector3.one * 0.1f;
            }
            _root.SetActive(true);
            yield return null;
            int received = 0, emptied = 0, entered = 0, exited = 0;
            destination.OnContentReceived.AddListener(d =>
            {
                received++;
                Assert.That(d.ContentId, Is.EqualTo("Mayo"));
                Assert.That(d.LastSourceJar, Is.EqualTo(jar));
                Assert.That(d.LastTool, Is.EqualTo(tool));
            });
            destination.OnToolEntered.AddListener((d, t) => entered++);
            destination.OnToolExited.AddListener((d, t) => exited++);
            tool.OnEmpty.AddListener(t => emptied++);

            toolObject.transform.localPosition = Vector3.zero;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            Assert.That(tool.AmountMl, Is.EqualTo(5f));
            Assert.That(jar.AmountMl, Is.EqualTo(15f));
            toolObject.transform.localPosition = Vector3.right * 3f;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            Assert.That(destination.AmountMl, Is.EqualTo(5f));
            Assert.That(tool.AmountMl, Is.Zero);
            Assert.That(tool.ContentId, Is.Empty);
            Assert.That(tool.LastSource, Is.Null);
            Assert.That(emptied, Is.EqualTo(1));
            Assert.That(received, Is.EqualTo(1));
            Assert.That(entered, Is.EqualTo(1));

            toolObject.transform.localPosition = Vector3.zero;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            Assert.That(tool.AmountMl, Is.EqualTo(5f));
            Assert.That(jar.AmountMl, Is.EqualTo(10f));
            toolObject.transform.localPosition = Vector3.right * 3f;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            Assert.That(destination.AmountMl, Is.EqualTo(8f));
            Assert.That(tool.AmountMl, Is.EqualTo(2f));
            Assert.That(tool.ContentId, Is.EqualTo("Mayo"));
            Assert.That(emptied, Is.EqualTo(1));
            Assert.That(received, Is.EqualTo(2));
            Assert.That(entered, Is.EqualTo(2));
            destination.Empty();
            _scene.GetPhysicsScene().Simulate(0.02f);
            yield return null;
            Assert.That(tool.AmountMl, Is.EqualTo(2f), "Stay must not continuously re-transfer.");
            tool.TransferToDestination(destination);
            Assert.That(destination.AmountMl, Is.EqualTo(2f));
            Assert.That(tool.AmountMl, Is.Zero);
            Assert.That(emptied, Is.EqualTo(2));
            Assert.That(received, Is.EqualTo(3));
            toolObject.transform.localPosition = Vector3.left * 2f;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            Assert.That(exited, Is.EqualTo(2));
            Assert.That(destination.ContentId, Is.EqualTo("Mayo"));
            Assert.That(destination.LastSourceJar, Is.EqualTo(jar));
        }
    }
}
