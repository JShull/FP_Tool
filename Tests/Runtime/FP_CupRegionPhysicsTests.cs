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

    public class FP_CupRegionPhysicsTests
    {
        private Scene _scene;
        private static void Set(object target, string field, object value, System.Type declaringType = null) =>
            (declaringType ?? target.GetType()).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private GameObject Create(string name)
        {
            var obj = new GameObject(name);
            SceneManager.MoveGameObjectToScene(obj, _scene);
            return obj;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_scene.IsValid()) yield return SceneManager.UnloadSceneAsync(_scene);
        }

        [UnityTest]
        public IEnumerator RegionDetectsStationaryCupAndControlsOnlyRequiredBrews()
        {
            _scene = SceneManager.CreateScene("Cup region physics test", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            var regionObject = Create("Cup region");
            regionObject.AddComponent<BoxCollider>().size = Vector3.one * 2f;
            var region = regionObject.AddComponent<FP_CupRegion>();
            var maker = Create("Coffee maker").AddComponent<FP_EquipmentCoffeeMaker>();
            Set(maker, "autoRegisterWithTickSystem", false, typeof(FP_EquipmentTickableBase));
            Set(maker, "requireCup", true);
            Set(maker, "cupRegion", region);
            maker.SetBeansGrams(250f);
            maker.SetWaterMl(1000f);
            int missing = 0, cancelled = 0;
            maker.OnNoCup.AddListener(m => missing++);
            maker.OnCancelled.AddListener(m => cancelled++);
            maker.StartBrewing();
            Assert.That(missing, Is.EqualTo(1));

            var cupObject = Create("Cup");
            cupObject.AddComponent<FP_Cup>();
            var body = cupObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var firstObject = Create("First collider");
            firstObject.transform.SetParent(cupObject.transform, false);
            var first = firstObject.AddComponent<BoxCollider>();
            first.size = Vector3.one * 0.2f;
            var secondObject = Create("Second collider");
            secondObject.transform.SetParent(cupObject.transform, false);
            secondObject.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            yield return null;

            maker.StartBrewing();
            Assert.That(maker.IsBrewing, Is.True);
            first.enabled = false;
            yield return null;
            Assert.That(maker.IsBrewing, Is.True);
            cupObject.transform.position = Vector3.right * 5f;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            yield return null;
            Assert.That(maker.IsBrewing, Is.False);
            Assert.That(cancelled, Is.EqualTo(1));

            cupObject.transform.position = Vector3.zero;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(0.02f);
            maker.StartBrewing();
            Assert.That(maker.IsBrewing, Is.True);
            region.enabled = false;
            yield return null;
            Assert.That(maker.IsBrewing, Is.False);
            Assert.That(cancelled, Is.EqualTo(2));

            Set(maker, "requireCup", false);
            maker.StartBrewingForSeconds(1f);
            maker.Tick(2f);
            Assert.That(maker.LastBrewVolumeMl, Is.EqualTo(250f));
            Assert.That(missing, Is.EqualTo(3));

            Set(maker, "requireCup", true);
            region.enabled = true;
            maker.StartBrewing();
            Assert.That(maker.IsBrewing, Is.True);
            Object.Destroy(cupObject);
            yield return null;
            yield return null;
            Assert.That(maker.IsBrewing, Is.False);
            Assert.That(maker.IsCupPresent, Is.False);
        }
    }
}
