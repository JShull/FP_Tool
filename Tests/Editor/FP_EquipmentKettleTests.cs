// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;

    public class FP_EquipmentKettleTests
    {
        private GameObject _root;
        private FP_EquipmentKettle _kettle;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Kettle tests");
            _kettle = _root.AddComponent<FP_EquipmentKettle>();
            Set(_kettle, "coolingSeconds", 3f);
            Invoke(_kettle, "Awake");
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void EmptyCannotHeatAndFillEventsAreTransitionBased()
        {
            int noWater = 0, available = 0, full = 0, empty = 0, cold = 0;
            _kettle.OnNoWater.AddListener(k => noWater++);
            _kettle.OnWaterAvailable.AddListener(k => available++);
            _kettle.OnWaterFull.AddListener(k => full++);
            _kettle.OnWaterEmpty.AddListener(k => empty++);
            _kettle.OnWaterCold.AddListener(k => cold++);
            _kettle.StartHeating();
            Assert.That(noWater, Is.EqualTo(1));
            Assert.That(_kettle.IsHeating || _kettle.CanPour, Is.False);
            _kettle.AddWaterMl(100f);
            _kettle.AddWaterMl(100f);
            Assert.That(available, Is.EqualTo(1));
            Assert.That(cold, Is.EqualTo(1));
            _kettle.AddWaterMl(10000f);
            _kettle.AddWaterMl(100f);
            Assert.That(_kettle.WaterMl, Is.EqualTo(1000f));
            Assert.That(full, Is.EqualTo(1));
            _kettle.SetWaterMl(0f);
            _kettle.SetWaterMl(0f);
            Assert.That(empty, Is.EqualTo(1));
            _kettle.SetWaterMl(1000f);
            Assert.That(full, Is.EqualTo(2));
        }

        [Test]
        public void HeatingCompletesAndWaterStaysHotUntilCoolingTimerExpires()
        {
            int started = 0, finished = 0, hot = 0, cold = 0;
            _kettle.SetWaterMl(500f);
            _kettle.OnStarted.AddListener(k => started++);
            _kettle.OnFinished.AddListener(k => finished++);
            _kettle.OnWaterHot.AddListener(k => hot++);
            _kettle.OnWaterCold.AddListener(k => cold++);
            _kettle.StartHeatingForSeconds(2f);
            _kettle.StartHeating();
            _kettle.Tick(1f);
            Assert.That(_kettle.IsWaterCold, Is.True);
            Assert.That(_kettle.HeatingProgress01, Is.EqualTo(0.5f));
            _kettle.Tick(1f);
            Assert.That(started, Is.EqualTo(1));
            Assert.That(finished, Is.EqualTo(1));
            Assert.That(hot, Is.EqualTo(1));
            Assert.That(_kettle.IsWaterHot, Is.True);
            Assert.That(_kettle.CoolingRemainingSeconds, Is.EqualTo(3f));
            _kettle.Tick(2.5f);
            Assert.That(_kettle.IsWaterHot, Is.True);
            _kettle.Tick(0.5f);
            _kettle.Tick(10f);
            Assert.That(_kettle.IsWaterCold, Is.True);
            Assert.That(cold, Is.EqualTo(1));
            Assert.That(_kettle.WaterMl, Is.EqualTo(500f));
        }

        [Test]
        public void OversizedTickIncludesCoolingTimeAfterHeatingCompletes()
        {
            _kettle.SetWaterMl(100f);
            _kettle.StartHeatingForSeconds(2f);
            _kettle.Tick(4f);
            Assert.That(_kettle.IsWaterHot, Is.True);
            Assert.That(_kettle.CoolingRemainingSeconds, Is.EqualTo(1f));
            _kettle.Tick(1f);
            Assert.That(_kettle.IsWaterCold, Is.True);
        }

        [Test]
        public void PouringSomeHotWaterPreservesHeatAndEmptyingClearsIt()
        {
            _kettle.SetWaterMl(500f);
            _kettle.StartHeatingForSeconds(1f);
            _kettle.Tick(1f);
            Assert.That(_kettle.RemoveWaterMl(200f), Is.EqualTo(200f));
            Assert.That(_kettle.IsWaterHot, Is.True);
            Assert.That(_kettle.CoolingRemainingSeconds, Is.EqualTo(3f));
            Assert.That(_kettle.RemoveWaterMl(1000f), Is.EqualTo(300f));
            Assert.That(_kettle.HasWater || _kettle.IsWaterHot || _kettle.IsWaterCold || _kettle.CanPour, Is.False);
            Assert.That(_kettle.CoolingRemainingSeconds, Is.Zero);
        }

        [Test]
        public void ColdRefillResetsHeatAndCancelsActiveHeating()
        {
            _kettle.SetWaterMl(200f);
            _kettle.StartHeatingForSeconds(1f);
            _kettle.Tick(1f);
            _kettle.AddWaterMl(50f);
            Assert.That(_kettle.IsWaterCold, Is.True);
            Assert.That(_kettle.CoolingRemainingSeconds, Is.Zero);
            int cancelled = 0;
            _kettle.OnCancelled.AddListener(k => cancelled++);
            _kettle.StartHeatingForSeconds(2f);
            _kettle.AddWaterMl(50f);
            Assert.That(_kettle.IsHeating, Is.False);
            Assert.That(cancelled, Is.EqualTo(1));
        }

        [Test]
        public void CancelDoesNotHeatAndBreakRepairRetainWaterWithoutRestart()
        {
            _kettle.SetWaterMl(500f);
            _kettle.StartHeatingForSeconds(2f);
            _kettle.CancelHeating();
            _kettle.Tick(10f);
            Assert.That(_kettle.IsWaterCold, Is.True);
            _kettle.StartHeating();
            _kettle.Execute(new EquipmentCommand { Type = EquipmentCommandType.Break });
            _kettle.StartHeating();
            Assert.That(_kettle.IsHeating || _kettle.CanPour, Is.False);
            Assert.That(_kettle.WaterMl, Is.EqualTo(500f));
            _kettle.Execute(new EquipmentCommand { Type = EquipmentCommandType.Repair });
            Assert.That(_kettle.CanPour, Is.True);
            Assert.That(_kettle.IsHeating, Is.False);
        }

        [Test]
        public void InitialHotWaterReportsStateAndDisableRetainsCoolingTime()
        {
            Set(_kettle, "waterMl", 1000f);
            Set(_kettle, "waterHot", true);
            int full = 0, hot = 0;
            _kettle.OnWaterFull.AddListener(k => full++);
            _kettle.OnWaterHot.AddListener(k => hot++);
            Invoke(_kettle, "Awake");
            Assert.That(full, Is.EqualTo(1));
            Assert.That(hot, Is.EqualTo(1));
            _kettle.Tick(1f);
            _kettle.enabled = false;
            Invoke(_kettle, "OnDisable");
            _kettle.Tick(10f);
            Assert.That(_kettle.CoolingRemainingSeconds, Is.EqualTo(2f));
            _kettle.enabled = true;
            _kettle.Tick(2f);
            Assert.That(_kettle.IsWaterCold, Is.True);
        }

        [Test]
        public void SharedWaterRegionTargetsKettleAndRejectsAmbiguousTargets()
        {
            var obj = new GameObject("Water inlet");
            obj.transform.SetParent(_root.transform);
            var region = obj.AddComponent<FP_WaterFillRegion>();
            Invoke(region, "Awake");
            Set(region, "kettle", _kettle);
            Assert.That(region.TryReceiveWater(250f), Is.True);
            Assert.That(_kettle.WaterMl, Is.EqualTo(250f));
            var coffee = obj.AddComponent<FP_EquipmentCoffeeMaker>();
            Invoke(coffee, "Awake");
            Set(region, "coffeeMaker", coffee);
            Assert.That(region.TryReceiveWater(250f), Is.False);
            Assert.That(_kettle.WaterMl, Is.EqualTo(250f));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        [TestCase(0f)]
        public void InvalidInputsCannotHeatDrainOrAdvance(float value)
        {
            _kettle.SetWaterMl(500f);
            _kettle.StartHeatingForSeconds(value);
            Assert.That(_kettle.IsHeating, Is.False);
            Assert.That(_kettle.RemoveWaterMl(value), Is.Zero);
            _kettle.StartHeatingForSeconds(2f);
            _kettle.Tick(value);
            Assert.That(_kettle.Status.TimerRemainingSec, Is.EqualTo(2f));
        }
    }
}
