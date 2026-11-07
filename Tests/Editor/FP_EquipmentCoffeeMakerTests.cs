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

        [Test]
        public void BeanEventsFireOnceAtExactThresholdsAndIgnoreFurtherRefills()
        {
            int enough = 0, full = 0;
            _maker.OnEnoughBeans.AddListener(m => enough++);
            _maker.OnBeansFull.AddListener(m => full++);
            _maker.SetBeansGrams(14f);
            Assert.That(enough, Is.Zero);
            _maker.AddBeansGrams(1f);
            _maker.AddBeansGrams(1f);
            _maker.SetWaterMl(1000f);
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(full, Is.Zero);
            _maker.SetBeansGrams(250f);
            _maker.AddBeansGrams(1000f);
            _maker.SetBeansGrams(250f);
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetPowerOn });
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(1));
            Assert.That(_maker.IsBeanHopperFull, Is.True);
        }

        [Test]
        public void WaterEventsFireAtRecipeAndCapacityAndRearmAfterConsumption()
        {
            int enough = 0, full = 0;
            _maker.OnEnoughWater.AddListener(m => enough++);
            _maker.OnWaterFull.AddListener(m => full++);
            _maker.SetBeansGrams(60f);
            _maker.SetWaterMl(249f);
            Assert.That(enough, Is.Zero);
            _maker.AddWaterMl(1f);
            _maker.AddWaterMl(1f);
            Assert.That(enough, Is.EqualTo(1));
            _maker.SetWaterMl(1000f);
            _maker.AddWaterMl(1000f);
            _maker.SetWaterMl(1000f);
            Assert.That(full, Is.EqualTo(1));
            Assert.That(_maker.IsWaterTankFull, Is.True);
            _maker.StartBrewing();
            _maker.CancelBrewing();
            _maker.AddWaterMl(250f);
            Assert.That(full, Is.EqualTo(2));
            Assert.That(enough, Is.EqualTo(1));
            _maker.SetWaterMl(250f);
            _maker.StartBrewing();
            _maker.CancelBrewing();
            _maker.AddWaterMl(250f);
            Assert.That(enough, Is.EqualTo(2));
            Assert.That(full, Is.EqualTo(2));
        }

        [Test]
        public void LastRecipeReportsBothShortagesAtCompletionWithoutAnotherStartAttempt()
        {
            _maker.SetBeansGrams(15f);
            _maker.SetWaterMl(250f);
            int beans = 0, water = 0, finished = 0;
            _maker.OnNoBeans.AddListener(m => { Assert.That(m.IsBrewing, Is.False); beans++; });
            _maker.OnNoWater.AddListener(m => { Assert.That(m.IsBrewing, Is.False); water++; });
            _maker.OnFinished.AddListener(m => finished++);
            _maker.StartBrewingForSeconds(1f);
            Assert.That(_maker.HasBeans || _maker.HasWater, Is.False);
            Assert.That(beans + water, Is.Zero, "The accepted drink must not be interrupted by its supply deduction.");
            _maker.Tick(0.5f);
            Assert.That(beans + water, Is.Zero);
            _maker.Tick(0.5f);
            Assert.That(finished, Is.EqualTo(1));
            Assert.That(beans, Is.EqualTo(1));
            Assert.That(water, Is.EqualTo(1));
            _maker.Tick(1f);
            _maker.SetCupPresent(true);
            _maker.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetPowerOff });
            Assert.That(beans, Is.EqualTo(1));
            Assert.That(water, Is.EqualTo(1));
        }

        [Test]
        public void CompletionReportsOnlyDepletedSupplyAndRearmsAfterRefill()
        {
            _maker.SetBeansGrams(60f);
            _maker.SetWaterMl(250f);
            int beans = 0, water = 0;
            _maker.OnNoBeans.AddListener(m => beans++);
            _maker.OnNoWater.AddListener(m => water++);
            _maker.StartBrewingForSeconds(1f);
            _maker.Tick(1f);
            Assert.That(beans, Is.Zero);
            Assert.That(water, Is.EqualTo(1));
            _maker.AddWaterMl(250f);
            _maker.StartBrewingForSeconds(1f);
            _maker.Tick(1f);
            Assert.That(beans, Is.Zero);
            Assert.That(water, Is.EqualTo(2));
        }

        [Test]
        public void RefillingDuringBrewClearsPendingShortagesAndCancelPublishesRemainingOnes()
        {
            _maker.SetBeansGrams(15f);
            _maker.SetWaterMl(250f);
            int beans = 0, water = 0;
            _maker.OnNoBeans.AddListener(m => beans++);
            _maker.OnNoWater.AddListener(m => water++);
            _maker.StartBrewingForSeconds(1f);
            _maker.AddBeansGrams(15f);
            _maker.AddWaterMl(250f);
            _maker.Tick(1f);
            Assert.That(beans + water, Is.Zero);
            _maker.StartBrewingForSeconds(1f);
            _maker.AddWaterMl(250f);
            _maker.CancelBrewing();
            Assert.That(beans, Is.EqualTo(1));
            Assert.That(water, Is.Zero);
        }

        [Test]
        public void IdleDepletionNotifiesOnceAndNoBeansListenerCanResolveWaterShortage()
        {
            Fill();
            int beans = 0, water = 0;
            _maker.OnNoBeans.AddListener(m => { beans++; m.AddWaterMl(250f); });
            _maker.OnNoWater.AddListener(m => water++);
            // Change both through serialized state as an Inspector/external-state update.
            Set(_maker, "beansGrams", 0f);
            Set(_maker, "waterMl", 0f);
            typeof(FP_EquipmentCoffeeMaker).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_maker, null);
            Assert.That(beans, Is.EqualTo(1));
            Assert.That(water, Is.Zero);
            _maker.SetWaterMl(0f);
            _maker.SetWaterMl(0f);
            Assert.That(water, Is.EqualTo(1));
        }

        [Test]
        public void InitialWaterSupplyReportsThresholdsOnceWithoutEnableDuplicates()
        {
            int enough = 0, full = 0;
            _maker.OnEnoughWater.AddListener(m => enough++);
            _maker.OnWaterFull.AddListener(m => full++);
            Set(_maker, "waterMl", 1000f);
            typeof(FP_EquipmentCoffeeMaker).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_maker, null);
            _maker.enabled = false;
            _maker.enabled = true;
            _maker.SetBeansGrams(30f);
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(1));
        }

        [Test]
        public void WaterCallbackCanDrainAndRefillWithoutRepeatingStaleFullEvent()
        {
            int enough = 0, full = 0;
            _maker.OnEnoughWater.AddListener(m =>
            {
                enough++;
                if (enough != 1) return;
                m.SetWaterMl(0f);
                m.SetWaterMl(1000f);
            });
            _maker.OnWaterFull.AddListener(m => full++);
            _maker.SetWaterMl(1000f);
            Assert.That(enough, Is.EqualTo(2));
            Assert.That(full, Is.EqualTo(1));
        }

        [Test]
        public void InspectorWaterRecipeAndCapacityChangesAreMonitored()
        {
            int enough = 0, full = 0;
            _maker.OnEnoughWater.AddListener(m => enough++);
            _maker.OnWaterFull.AddListener(m => full++);
            _maker.SetWaterMl(100f);
            Set(_maker, "waterPerBrewMl", 100f);
            Set(_maker, "waterCapacityMl", 100f);
            var lateUpdate = typeof(FP_EquipmentCoffeeMaker).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            lateUpdate.Invoke(_maker, null);
            lateUpdate.Invoke(_maker, null);
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(1));
            Set(_maker, "waterCapacityMl", 0f);
            _maker.SetWaterMl(0f);
            Assert.That(_maker.IsWaterTankFull, Is.False);
            Assert.That(full, Is.EqualTo(1));
        }

        [Test]
        public void CupPresentEventFiresOncePerArrivalAndRearmsAfterRemoval()
        {
            Set(_maker, "requireCup", true);
            int present = 0;
            _maker.OnCupPresent.AddListener(m => present++);
            _maker.SetCupPresent(false);
            _maker.SetCupPresent(true);
            _maker.SetCupPresent(true);
            _maker.SetWaterMl(1000f);
            Assert.That(present, Is.EqualTo(1));
            _maker.SetCupPresent(false);
            _maker.SetCupPresent(true);
            Assert.That(present, Is.EqualTo(2));
        }

        [Test]
        public void CupRemovedReportsEachDepartureOnceWhileIdleAndBrewing()
        {
            Set(_maker, "requireCup", true);
            int removed = 0, missing = 0, cancelled = 0;
            _maker.OnCupRemoved.AddListener(m => { Assert.That(m.IsCupPresent, Is.False); removed++; });
            _maker.OnNoCup.AddListener(m => missing++);
            _maker.OnCancelled.AddListener(m => cancelled++);
            _maker.SetCupPresent(false);
            Assert.That(removed, Is.Zero, "Starting empty is not a departure.");
            _maker.SetCupPresent(true);
            _maker.SetCupPresent(false);
            _maker.SetCupPresent(false);
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(missing + cancelled, Is.Zero, "Idle departure does not imply a rejected brew.");
            Fill();
            _maker.SetCupPresent(true);
            _maker.StartBrewing();
            _maker.SetCupPresent(false);
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(missing, Is.EqualTo(1));
            Assert.That(cancelled, Is.EqualTo(1));
            Assert.That(_maker.IsBrewing, Is.False);
        }

        [Test]
        public void BrewingAndManualDepletionRearmBeanEventsIndependently()
        {
            int enough = 0, full = 0;
            _maker.OnEnoughBeans.AddListener(m => enough++);
            _maker.OnBeansFull.AddListener(m => full++);
            Fill(); // Enough for several drinks.
            _maker.SetBeansGrams(250f);
            _maker.StartBrewing(); // Drops below capacity, still enough for a drink.
            _maker.CancelBrewing();
            _maker.AddBeansGrams(15f);
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(2));
            _maker.SetBeansGrams(15f);
            _maker.StartBrewing(); // Consume last recipe and re-arm enough-beans event.
            _maker.CancelBrewing();
            _maker.AddBeansGrams(15f);
            Assert.That(enough, Is.EqualTo(2));
            Assert.That(full, Is.EqualTo(2));
            _maker.SetBeansGrams(0f);
            _maker.SetBeansGrams(1000f); // Clamp and cross both in one refill.
            Assert.That(enough, Is.EqualTo(3));
            Assert.That(full, Is.EqualTo(3));
        }

        [Test]
        public void InitialBeanSupplyReportsReadinessOnceAndEnableDoesNotRepeatIt()
        {
            int enough = 0, full = 0;
            _maker.OnEnoughBeans.AddListener(m => enough++);
            _maker.OnBeansFull.AddListener(m => full++);
            Set(_maker, "beansGrams", 250f);
            typeof(FP_EquipmentCoffeeMaker).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_maker, null);
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(1));
            _maker.enabled = false;
            _maker.enabled = true;
            _maker.SetWaterMl(250f);
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(1));
        }

        [Test]
        public void BeanThresholdCallbackCanDrainAndRefillWithoutStaleFullEvent()
        {
            int enough = 0, full = 0;
            _maker.OnEnoughBeans.AddListener(m =>
            {
                enough++;
                if (enough != 1) return;
                m.SetBeansGrams(0f);
                m.SetBeansGrams(250f);
            });
            _maker.OnBeansFull.AddListener(m => full++);
            _maker.SetBeansGrams(250f);
            Assert.That(enough, Is.EqualTo(2));
            Assert.That(full, Is.EqualTo(1));
        }

        [Test]
        public void RecipeAndCapacityInspectorChangesAreMonitoredAndZeroCapacityIsNotFull()
        {
            int enough = 0, full = 0;
            _maker.OnEnoughBeans.AddListener(m => enough++);
            _maker.OnBeansFull.AddListener(m => full++);
            _maker.SetBeansGrams(10f);
            Set(_maker, "beansPerBrewGrams", 10f);
            Set(_maker, "beanCapacityGrams", 10f);
            var lateUpdate = typeof(FP_EquipmentCoffeeMaker).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            lateUpdate.Invoke(_maker, null);
            lateUpdate.Invoke(_maker, null);
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(1));
            Set(_maker, "beanCapacityGrams", 0f);
            _maker.SetBeansGrams(0f);
            Assert.That(_maker.IsBeanHopperFull, Is.False);
            Assert.That(full, Is.EqualTo(1));
        }
    }
}
