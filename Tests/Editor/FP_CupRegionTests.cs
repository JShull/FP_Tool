// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;

    public class FP_CupRegionTests
    {
        private GameObject _root;
        private FP_CupRegion _region;
        private FP_EquipmentCoffeeMaker _maker;

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        private GameObject Create(string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_root.transform, false);
            return obj;
        }

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Cup region tests");
            _root.transform.position = new Vector3(10000f, 10000f, 10000f);
            var regionObject = Create("Cup region");
            regionObject.AddComponent<BoxCollider>().size = Vector3.one * 2f;
            _region = regionObject.AddComponent<FP_CupRegion>();
            Invoke(_region, "Awake");
            _maker = Create("Coffee maker").AddComponent<FP_EquipmentCoffeeMaker>();
            Invoke(_maker, "Awake");
            Set(_maker, "requireCup", true);
            Set(_maker, "cupRegion", _region);
            _maker.SetBeansGrams(250f);
            _maker.SetWaterMl(1000f);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        private FP_Cup Cup()
        {
            var cup = Create("Cup").AddComponent<FP_Cup>();
            var first = Create("First cup collider");
            first.transform.SetParent(cup.transform, false);
            first.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            var second = Create("Second cup collider");
            second.transform.SetParent(cup.transform, false);
            second.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            Physics.SyncTransforms();
            return cup;
        }

        [Test]
        public void ExistingCupIsDetectedBeforeAnyTriggerCallbackAndLastColliderMatters()
        {
            var cup = Cup();
            Assert.That(_region.HasCup, Is.True);
            _maker.StartBrewing();
            Assert.That(_maker.IsBrewing, Is.True);
            var colliders = cup.GetComponentsInChildren<Collider>();
            colliders[0].enabled = false;
            Invoke(_maker, "LateUpdate");
            Assert.That(_maker.IsBrewing, Is.True);
            colliders[1].enabled = false;
            Invoke(_maker, "LateUpdate");
            Assert.That(_maker.IsBrewing, Is.False);
            Assert.That(_maker.IsCupPresent, Is.False);
        }

        [Test]
        public void OrdinaryItemsAndDisabledMarkersDoNotCount()
        {
            Create("Not a cup").AddComponent<BoxCollider>();
            var cup = Cup();
            cup.enabled = false;
            Assert.That(_region.HasCup, Is.False);
            int missing = 0;
            _maker.OnNoCup.AddListener(m => missing++);
            _maker.StartBrewing();
            Assert.That(missing, Is.EqualTo(1));
            Assert.That(_maker.BeansGrams, Is.EqualTo(250f));
            cup.enabled = true;
            _maker.StartBrewing();
            Assert.That(_maker.IsBrewing, Is.True);
        }

        [Test]
        public void RegionReportsCupArrivalOnceAndIgnoresRegionWhenCupNotRequired()
        {
            int present = 0;
            _maker.OnCupPresent.AddListener(m => present++);
            var cup = Cup();
            Set(_maker, "requireCup", false);
            Invoke(_maker, "LateUpdate");
            Assert.That(present, Is.Zero);
            Set(_maker, "requireCup", true);
            Invoke(_maker, "LateUpdate");
            Invoke(_maker, "LateUpdate");
            Assert.That(present, Is.EqualTo(1));
            cup.transform.localPosition = Vector3.right * 5f;
            Physics.SyncTransforms();
            Invoke(_maker, "LateUpdate");
            cup.transform.localPosition = Vector3.zero;
            Physics.SyncTransforms();
            Invoke(_maker, "LateUpdate");
            Assert.That(present, Is.EqualTo(2));
        }

        [Test]
        public void MultipleCupsRemainPresentUntilAllLeave()
        {
            var first = Cup();
            var second = Cup();
            _maker.StartBrewing();
            first.transform.localPosition = Vector3.right * 5f;
            Physics.SyncTransforms();
            Invoke(_maker, "LateUpdate");
            Assert.That(_maker.IsBrewing, Is.True);
            second.transform.localPosition = Vector3.right * 5f;
            Physics.SyncTransforms();
            Invoke(_maker, "LateUpdate");
            Assert.That(_maker.IsBrewing, Is.False);
        }

        [Test]
        public void DisabledOrDestroyedRegionCannotLeaveStaleCupPresence()
        {
            Cup();
            _maker.StartBrewing();
            _region.enabled = false;
            Invoke(_maker, "LateUpdate");
            Assert.That(_maker.IsBrewing, Is.False);
            _region.enabled = true;
            _maker.StartBrewing();
            Assert.That(_maker.IsBrewing, Is.True);
            Object.DestroyImmediate(_region.gameObject);
            Invoke(_maker, "LateUpdate");
            Assert.That(_maker.IsBrewing, Is.False);
            Assert.That(_maker.IsCupPresent, Is.False);
            _maker.SetCupPresent(true);
            _maker.StartBrewing();
            Assert.That(_maker.IsBrewing, Is.False);
        }

        [Test]
        public void UncheckedRequireCupIgnoresEmptyOrDisabledRegion()
        {
            Set(_maker, "requireCup", false);
            _region.enabled = false;
            int missing = 0;
            _maker.OnNoCup.AddListener(m => missing++);
            _maker.StartBrewingForSeconds(1f);
            Invoke(_maker, "LateUpdate");
            _maker.Tick(2f);
            Assert.That(_maker.LastBrewVolumeMl, Is.EqualTo(250f));
            Assert.That(missing, Is.Zero);
        }

        [Test]
        public void DestroyedCupAndDisabledTriggerAreDetectedWithoutExitCallbacks()
        {
            var cup = Cup();
            _maker.StartBrewing();
            Object.DestroyImmediate(cup.gameObject);
            _maker.Tick(100f); // Check cup before completing, even before LateUpdate.
            Assert.That(_maker.LastBrewVolumeMl, Is.Zero);
            Assert.That(_maker.IsBrewing, Is.False);
            Cup();
            _maker.StartBrewing();
            _region.GetComponent<BoxCollider>().enabled = false;
            Invoke(_maker, "LateUpdate");
            Assert.That(_maker.IsBrewing, Is.False);
        }

        [Test]
        public void DenseRegionDoesNotMissCupBehindUnrelatedColliders()
        {
            for (int i = 0; i < 24; i++) Create("Other collider").AddComponent<BoxCollider>();
            Cup();
            Physics.SyncTransforms();
            Assert.That(_region.HasCup, Is.True);
        }

        [Test]
        public void RegionUsesConfiguredCenterRotationScaleAndLayerMask()
        {
            var cup = Cup();
            var box = _region.GetComponent<BoxCollider>();
            box.center = Vector3.right * 3f;
            _region.transform.localScale = new Vector3(2f, 1f, 1f);
            _region.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Physics.SyncTransforms();
            Assert.That(_region.HasCup, Is.False);
            cup.transform.position = _region.transform.TransformPoint(box.center);
            Physics.SyncTransforms();
            Assert.That(_region.HasCup, Is.True);
            Set(_region, "cupLayers", (LayerMask)0);
            Assert.That(_region.HasCup, Is.False);
        }
    }
}
