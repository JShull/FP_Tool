// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;

    public class FP_EquipmentCoffeeMakerTests
    {
        private GameObject _object;
        private FP_EquipmentCoffeeMaker _maker;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("Coffee maker test");
            _maker = _object.AddComponent<FP_EquipmentCoffeeMaker>();
            typeof(FP_EquipmentCoffeeMaker).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_maker, null);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_object);

        private void Fill()
        {
            _maker.SetBeansGrams(60f);
            _maker.SetWaterMl(1000f);
        }

        [Test]
        public void MissingSuppliesReportBothReasonsWithoutStarting()
        {
            int beans = 0, water = 0, started = 0;
            _maker.OnNoBeans.AddListener(m => beans++);
            _maker.OnNoWater.AddListener(m => water++);
            _maker.OnStarted.AddListener(m => started++);
            _maker.StartBrewing();
            Assert.That(beans, Is.EqualTo(1));
            Assert.That(water, Is.EqualTo(1));
            Assert.That(started, Is.Zero);
            Assert.That(_maker.IsBrewing, Is.False);
            _maker.SetBeansGrams(14f);
            _maker.SetWaterMl(249f);
            _maker.StartBrewing();
            Assert.That(beans, Is.EqualTo(2));
            Assert.That(water, Is.EqualTo(2));
        }

        [Test]
        public void BrewConsumesRecipeOnceAndReportsOrderedLifecycle()
        {
            Fill();
            var events = new List<string>();
            _maker.OnStarted.AddListener(m => events.Add("started"));
            _maker.OnBrewing.AddListener(m => events.Add("brewing"));
            _maker.OnBrewProgress.AddListener(m => events.Add("progress"));
            _maker.OnFinished.AddListener(m => events.Add("finished"));
            _maker.StartBrewingForSeconds(2f);
            _maker.StartBrewing();
            Assert.That(_maker.BeansGrams, Is.EqualTo(45f));
            Assert.That(_maker.WaterMl, Is.EqualTo(750f));
            _maker.Tick(1f);
            Assert.That(_maker.BrewProgress01, Is.EqualTo(0.5f));
            Assert.That(_maker.Status.TimerRemainingSec, Is.EqualTo(1f));
            _maker.Tick(10f);
            _maker.Tick(10f);
            Assert.That(events, Is.EqualTo(new[] { "started", "brewing", "progress", "progress", "finished" }));
            Assert.That(_maker.IsBrewing, Is.False);
            Assert.That(_maker.Status.Power, Is.EqualTo(EquipmentPowerState.On));
            Assert.That(_maker.BrewProgress01, Is.EqualTo(1f));
            Assert.That(_maker.LastBrewVolumeMl, Is.EqualTo(250f));
            Assert.That(_maker.Status.EmitPowerUnit, Is.Zero);
        }

        [Test]
        public void CancelIsIdempotentAndDoesNotRefundSuppliesOrFinish()
        {
            Fill();
            int cancelled = 0, finished = 0;
            _maker.OnCancelled.AddListener(m => cancelled++);
            _maker.OnFinished.AddListener(m => finished++);
            _maker.StartBrewing();
            _maker.Tick(1f);
            _maker.CancelBrewing();
            _maker.CancelBrewing();
            _maker.Tick(60f);
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(finished, Is.Zero);
            Assert.That(_maker.BeansGrams, Is.EqualTo(45f));
            Assert.That(_maker.WaterMl, Is.EqualTo(750f));
            Assert.That(_maker.LastBrewVolumeMl, Is.Zero);
            Assert.That(_maker.BrewProgress01, Is.Zero);
        }

        [Test]
        public void OptionalCupBlocksStartAndRemovalCancelsBrew()
        {
            Fill();
            Set(_maker, "requireCup", true);
            int missing = 0;
            _maker.OnNoCup.AddListener(m => missing++);
            _maker.StartBrewing();
            Assert.That(missing, Is.EqualTo(1));
            Assert.That(_maker.BeansGrams, Is.EqualTo(60f));
            _maker.SetCupPresent(true);
            _maker.StartBrewing();
            Assert.That(_maker.IsBrewing, Is.True);
            _maker.SetCupPresent(false);
            Assert.That(missing, Is.EqualTo(2));
            Assert.That(_maker.IsBrewing, Is.False);
            _maker.Tick(60f);
            Assert.That(_maker.LastBrewVolumeMl, Is.Zero);
        }

        [Test]
        public void BreakCancelsAndRepairDoesNotRestart()
        {
            Fill();
            int broken = 0, repaired = 0, cancelled = 0;
            _maker.OnBroken.AddListener(m => broken++);
            _maker.OnRepaired.AddListener(m => repaired++);
            _maker.OnCancelled.AddListener(m => cancelled++);
            _maker.StartBrewing();
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.Break });
            _maker.StartBrewing();
            Assert.That(_maker.Status.Power, Is.EqualTo(EquipmentPowerState.Off));
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(broken, Is.EqualTo(1));
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.Repair });
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.Repair });
            Assert.That(repaired, Is.EqualTo(1));
            Assert.That(_maker.IsBrewing, Is.False);
            _maker.StartBrewing();
            Assert.That(_maker.IsBrewing, Is.True);
        }

        [Test]
        public void RefillsClampToCapacityAndIgnoreInvalidAdditions()
        {
            _maker.SetBeansGrams(1000f);
            _maker.SetWaterMl(10000f);
            Assert.That(_maker.BeansGrams, Is.EqualTo(250f));
            Assert.That(_maker.WaterMl, Is.EqualTo(1000f));
            _maker.SetBeansGrams(-1f);
            _maker.SetWaterMl(float.NaN);
            _maker.AddBeansGrams(-1f);
            _maker.AddWaterMl(float.PositiveInfinity);
            Assert.That(_maker.BeansGrams, Is.Zero);
            Assert.That(_maker.WaterMl, Is.Zero);
            _maker.AddBeansGrams(30f);
            _maker.AddWaterMl(500f);
            Assert.That(_maker.HasBeans && _maker.HasWater, Is.True);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidDurationAndTickDoNotConsumeOrAdvance(float value)
        {
            Fill();
            _maker.StartBrewingForSeconds(value);
            Assert.That(_maker.BeansGrams, Is.EqualTo(60f));
            Assert.That(_maker.IsBrewing, Is.False);
            _maker.StartBrewingForSeconds(2f);
            _maker.Tick(value);
            Assert.That(_maker.Status.TimerRemainingSec, Is.EqualTo(2f));
        }

        [Test]
        public void CommandsStartCancelAndPowerOff()
        {
            Fill();
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetPowerOn });
            Assert.That(_maker.IsBrewing, Is.False);
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.InterfaceActions });
            Assert.That(_maker.IsBrewing, Is.True);
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.CancelTimer });
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.StartTimerSeconds, FValue = 2f });
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.TogglePower });
            Assert.That(_maker.Status.Power, Is.EqualTo(EquipmentPowerState.Off));
            Assert.That(_maker.Status.TimerRemainingSec, Is.Zero);
        }

        [Test]
        public void StartedListenerCanCancelWithoutStartingBrewEffects()
        {
            Fill();
            int brewing = 0;
            _maker.OnStarted.AddListener(m => m.CancelBrewing());
            _maker.OnBrewing.AddListener(m => brewing++);
            _maker.StartBrewing();
            Assert.That(brewing, Is.Zero);
            Assert.That(_maker.IsBrewing, Is.False);
        }

        [Test]
        public void ProgressListenerCanReplaceCycleWithoutFinishingNewBrew()
        {
            Fill();
            int finished = 0;
            _maker.OnFinished.AddListener(m => finished++);
            _maker.OnBrewProgress.AddListener(m => { m.CancelBrewing(); m.StartBrewingForSeconds(5f); });
            _maker.StartBrewingForSeconds(1f);
            _maker.Tick(2f);
            Assert.That(finished, Is.Zero);
            Assert.That(_maker.IsBrewing, Is.True);
            Assert.That(_maker.Status.TimerRemainingSec, Is.EqualTo(5f));
        }

        [Test]
        public void PowerOffCannotBeUndoneByCancellationListener()
        {
            Fill();
            _maker.OnCancelled.AddListener(m => m.StartBrewing());
            _maker.StartBrewing();
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetPowerOff });
            Assert.That(_maker.Status.Power, Is.EqualTo(EquipmentPowerState.Off));
            Assert.That(_maker.Status.TimerRemainingSec, Is.Zero);
            Assert.That(_maker.BeansGrams, Is.EqualTo(45f));
        }
    }
}
