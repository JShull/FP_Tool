// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Collections;
    using FuzzPhyte.Utility;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;

    public class FP_CoffeeMakerLifecycleTests
    {
        private GameObject _tickObject;
        private GameObject _makerObject;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_makerObject != null) Object.Destroy(_makerObject);
            if (_tickObject != null) Object.Destroy(_tickObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutomaticTicksBrewAndReregisterAfterDisable()
        {
            Assert.That(FP_TickSystem.CCTick == null, Is.True, "This test requires an isolated test scene.");
            _tickObject = new GameObject("Coffee maker test tick system");
            _tickObject.AddComponent<FP_TickSystem>();
            _makerObject = new GameObject("Coffee maker");
            var maker = _makerObject.AddComponent<FP_EquipmentCoffeeMaker>();
            maker.SetBeansGrams(60f);
            maker.SetWaterMl(1000f);
            int finished = 0, cancelled = 0;
            maker.OnFinished.AddListener(m => finished++);
            maker.OnCancelled.AddListener(m => cancelled++);
            yield return null; // Allow Start to register with the real dispatcher.
            maker.StartBrewingForSeconds(0.04f);
            float deadline = Time.realtimeSinceStartup + 2f;
            while (maker.IsBrewing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(finished, Is.EqualTo(1));

            maker.StartBrewingForSeconds(10f);
            maker.enabled = false;
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(maker.Status.Power, Is.EqualTo(EquipmentPowerState.Off));
            maker.StartBrewing();
            Assert.That(maker.IsBrewing, Is.False);
            yield return null;
            maker.enabled = true;
            maker.StartBrewingForSeconds(0.04f);
            deadline = Time.realtimeSinceStartup + 2f;
            while (maker.IsBrewing && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(finished, Is.EqualTo(2), "Re-enabled coffee makers must resume automatic ticking.");
            Assert.That(maker.LastBrewVolumeMl, Is.EqualTo(250f));
        }
    }
}
