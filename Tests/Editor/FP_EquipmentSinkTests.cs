// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using NUnit.Framework;
    using UnityEngine;

    public class FP_EquipmentSinkTests
    {
        private GameObject _object;
        private FP_EquipmentSink _sink;

        [SetUp]
        public void SetUp()
        {
            _object = new GameObject("Sink test");
            _sink = _object.AddComponent<FP_EquipmentSink>();
            // Edit Mode does not guarantee MonoBehaviour Awake execution.
            _object.SendMessage("Awake");
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_object);

        [Test]
        public void KnobsKeepFlowRunningUntilBothAreClosed()
        {
            int started = 0, stopped = 0, changed = 0;
            _sink.OnStarted.AddListener(sink => started++);
            _sink.OnStopped.AddListener(sink => stopped++);
            _sink.OnStateChanged.AddListener(sink => changed++);
            _sink.SetHotWaterOn(true);
            _sink.SetHotWaterOn(true);
            _sink.SetColdWaterOn(true);
            _sink.SetHotWaterOn(false);
            Assert.That(_sink.IsRunning, Is.True);
            _sink.SetColdWaterOn(false);
            Assert.That(_sink.Status.Power, Is.EqualTo(EquipmentPowerState.Off));
            Assert.That(started, Is.EqualTo(1));
            Assert.That(stopped, Is.EqualTo(1));
            Assert.That(changed, Is.EqualTo(4));
        }

        [Test]
        public void BreakClosesBothKnobsAndRepairDoesNotRestartFlow()
        {
            _sink.SetHotWaterOn(true);
            _sink.SetColdWaterOn(true);
            _sink.Execute(new EquipmentCommand { Type = EquipmentCommandType.Break });
            _sink.SetHotWaterOn(true);
            _sink.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetPowerOn });
            Assert.That(_sink.Status.Condition, Is.EqualTo(EquipmentConditionState.Broken));
            Assert.That(_sink.IsHotWaterOn || _sink.IsColdWaterOn || _sink.IsRunning, Is.False);
            _sink.Execute(new EquipmentCommand { Type = EquipmentCommandType.Repair });
            Assert.That(_sink.Status.Condition, Is.EqualTo(EquipmentConditionState.OK));
            Assert.That(_sink.IsRunning, Is.False);
            _sink.SetColdWaterOn(true);
            Assert.That(_sink.IsRunning, Is.True);
        }

        [Test]
        public void FillTracksItemsAndLiquidIndependentlyOfFlow()
        {
            var item = new ContainedItem();
            Assert.That(_sink.Status.Fill, Is.EqualTo(EquipmentFillState.Empty));
            _sink.AddItemToEquipment(item);
            Assert.That(_sink.Status.Fill, Is.EqualTo(EquipmentFillState.ContainsItem));
            _sink.SetContainsLiquid(true);
            Assert.That(_sink.Status.Fill, Is.EqualTo(EquipmentFillState.MixedContents));
            _sink.RemoveItemFromEquipment(item);
            Assert.That(_sink.Status.Fill, Is.EqualTo(EquipmentFillState.ContainsLiquid));
            _sink.SetContainsLiquid(false);
            Assert.That(_sink.Status.Fill, Is.EqualTo(EquipmentFillState.Empty));
        }

        [Test]
        public void ContainedItemsReceiveStateAndTickWithoutImplicitEnergy()
        {
            var item = new ContainedItem();
            _sink.AddItemToEquipment(item);
            _sink.SetHotWaterOn(true);
            _sink.Tick(0.25f);
            Assert.That(item.LastStatus.Power, Is.EqualTo(EquipmentPowerState.On));
            Assert.That(item.LastStatus.EmitPowerUnit, Is.Zero);
            Assert.That(item.TickSeconds, Is.EqualTo(0.25f));
        }

        [Test]
        public void PowerCommandsAndDisableStopFlow()
        {
            _sink.Execute(new EquipmentCommand { Type = EquipmentCommandType.SetPowerOn });
            Assert.That(_sink.IsColdWaterOn, Is.True);
            _sink.Execute(new EquipmentCommand { Type = EquipmentCommandType.TogglePower });
            Assert.That(_sink.IsRunning, Is.False);
            _sink.SetHotWaterOn(true);
            _sink.enabled = false;
            _object.SendMessage("OnDisable");
            Assert.That(_sink.IsRunning, Is.False);
            Assert.That(_sink.IsHotWaterOn, Is.False);
        }

        private class ContainedItem : IFPContainedItem
        {
            public string ItemId => "test-item";
            public EquipmentStatus LastStatus;
            public float TickSeconds;
            public void OnEnteredEquipment(FP_EquipmentBase equipment) { }
            public void OnExitedEquipment(FP_EquipmentBase equipment) { }
            public void OnEquipmentStateChanged(FP_EquipmentBase equipment, EquipmentStatus status) => LastStatus = status;
            public void OnEquipmentTick(FP_EquipmentBase equipment, EquipmentStatus status, float dt)
            {
                LastStatus = status;
                TickSeconds += dt;
            }
        }
    }
}
