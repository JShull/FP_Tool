// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Collections;
    using System.Reflection;
    using NUnit.Framework;
    using TMPro;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;

    public class FP_ScalePhysicsTests
    {
        private Scene _scene;
        private PhysicsScene _physics;

        [SetUp]
        public void SetUp()
        {
            _scene = SceneManager.CreateScene("Scale physics test", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            _physics = _scene.GetPhysicsScene();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return SceneManager.UnloadSceneAsync(_scene);
        }

        private GameObject Create(string name)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, _scene);
            return obj;
        }

        private static void SetReference(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [UnityTest]
        public IEnumerator PhysicsAndDisplayFollowCompoundItemsAndLifecycleChanges()
        {
            var root = Create("Scale");
            var scale = root.AddComponent<FP_EquipmentScale>();
            var volumeObject = Create("Trigger");
            var trigger = volumeObject.AddComponent<BoxCollider>();
            trigger.size = Vector3.one * 2f;
            trigger.isTrigger = true;
            var volume = volumeObject.AddComponent<FP_ScaleContainmentVolume>();
            SetReference(volume, "scale", scale);

            var panel = Create("Weight panel");
            panel.SetActive(false);
            var label = panel.AddComponent<TextMeshPro>();
            var display = panel.AddComponent<FP_ScaleDisplay>();
            SetReference(display, "scale", scale);
            SetReference(display, "weightLabel", label);
            panel.SetActive(true);

            var firstObject = Create("First item");
            var first = firstObject.AddComponent<FP_WeightedItem>();
            first.SetWeightKg(1.25f);
            var body = firstObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            var left = Create("Left collider");
            left.transform.SetParent(firstObject.transform, false);
            left.transform.localPosition = Vector3.left * 0.2f;
            left.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            var right = Create("Right collider");
            right.transform.SetParent(firstObject.transform, false);
            right.transform.localPosition = Vector3.right * 0.2f;
            var rightCollider = right.AddComponent<BoxCollider>();
            rightCollider.size = Vector3.one * 0.2f;
            var secondObject = Create("Second item");
            secondObject.transform.position = Vector3.up * 0.5f;
            var second = secondObject.AddComponent<FP_WeightedItem>();
            second.SetWeightKg(2.5f);
            secondObject.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            secondObject.AddComponent<Rigidbody>().useGravity = false;

            Physics.SyncTransforms();
            _physics.Simulate(0.02f);
            yield return null;
            Assert.That(scale.ItemCount, Is.EqualTo(2));
            Assert.That(label.text, Is.EqualTo("3.750 kg"));

            first.SetWeightKg(0.5f);
            Assert.That(label.text, Is.EqualTo("3.000 kg"));
            left.transform.localPosition = Vector3.left * 3f;
            Physics.SyncTransforms();
            _physics.Simulate(0.02f);
            Assert.That(scale.TotalWeightKg, Is.EqualTo(3d));
            rightCollider.enabled = false;
            yield return null;
            Assert.That(scale.TotalWeightKg, Is.EqualTo(2.5d));
            second.enabled = false;
            Assert.That(label.text, Is.EqualTo("0.000 kg"));
            second.enabled = true;
            yield return null;
            Assert.That(label.text, Is.EqualTo("2.500 kg"));

            Object.Destroy(secondObject);
            yield return null;
            Assert.That(scale.ItemCount, Is.Zero);
            Assert.That(label.text, Is.EqualTo("0.000 kg"));
            scale.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetPowerOff });
            Assert.That(label.text, Is.EqualTo("-- kg"));
        }
    }
}
