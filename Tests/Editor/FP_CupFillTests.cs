// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;

    public class FP_CupFillTests
    {
        private GameObject _root;
        private FP_CupFill _cup;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Cup fill tests");
            _cup = _root.AddComponent<FP_CupFill>();
            Set(_cup, "capacityMl", 10f);
            Invoke(_cup, "Awake");
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(_root);

        private void ListenFilled(int[] counts)
        {
            _cup.OnType1Filled.AddListener(c => counts[1]++);
            _cup.OnType2Filled.AddListener(c => counts[2]++);
            _cup.OnType3Filled.AddListener(c => counts[3]++);
            _cup.OnMixedFilled.AddListener(c => counts[4]++);
        }
        private void ListenEmpty(int[] counts)
        {
            _cup.OnType1Empty.AddListener(c => counts[1]++);
            _cup.OnType2Empty.AddListener(c => counts[2]++);
            _cup.OnType3Empty.AddListener(c => counts[3]++);
            _cup.OnMixedEmpty.AddListener(c => counts[4]++);
        }

        [TestCase(FP_CupLiquidType.Type1)]
        [TestCase(FP_CupLiquidType.Type2)]
        [TestCase(FP_CupLiquidType.Type3)]
        public void PureTypesFireOnlyMatchingFullAndEmptyEvents(FP_CupLiquidType type)
        {
            int full = 0, empty = 0;
            var filledCounts = new int[5];
            var emptyCounts = new int[5];
            ListenFilled(filledCounts);
            ListenEmpty(emptyCounts);
            _cup.OnFilled.AddListener(c => full++);
            _cup.OnEmpty.AddListener(c => { Assert.That(c.LiquidType, Is.EqualTo(FP_CupLiquidType.None)); empty++; });
            Assert.That(_cup.TryAddLiquid(type, 5f), Is.True);
            Assert.That(full, Is.Zero);
            _cup.TryAddLiquid(type, 100f);
            _cup.TryAddLiquid(type, 100f);
            Assert.That(full, Is.EqualTo(1));
            Assert.That(filledCounts[(int)type], Is.EqualTo(1));
            Assert.That(_cup.VolumeMl, Is.EqualTo(10f));
            _cup.RemoveLiquidMl(5f);
            Assert.That(empty, Is.Zero);
            Assert.That(_cup.LiquidType, Is.EqualTo(type));
            _cup.Empty();
            _cup.Empty();
            Assert.That(empty, Is.EqualTo(1));
            Assert.That(emptyCounts[(int)type], Is.EqualTo(1));
            for (int i = 1; i <= 4; i++)
                if (i != (int)type) Assert.That(filledCounts[i] + emptyCounts[i], Is.Zero);
            Assert.That(_root.GetComponent<FP_Cup>(), Is.Not.Null);
        }

        [Test]
        public void DifferentInputsMixImmediatelyRemainMixedAndResetToNoneOnEmpty()
        {
            int mixed = 0, mixedFull = 0, mixedEmpty = 0, full = 0;
            _cup.OnMixed.AddListener(c => { Assert.That(c.LiquidType, Is.EqualTo(FP_CupLiquidType.Mixed)); mixed++; });
            _cup.OnMixedFilled.AddListener(c => mixedFull++);
            _cup.OnMixedEmpty.AddListener(c => { Assert.That(c.LiquidType, Is.EqualTo(FP_CupLiquidType.None)); mixedEmpty++; });
            _cup.OnFilled.AddListener(c => full++);
            _cup.TryAddLiquid(FP_CupLiquidType.Type1, 2f);
            _cup.TryAddLiquid(FP_CupLiquidType.Type2, 2f);
            Assert.That(mixed, Is.EqualTo(1));
            Assert.That(mixedFull, Is.Zero);
            Assert.That(_cup.VolumeMl, Is.EqualTo(4f));
            _cup.TryAddLiquid(FP_CupLiquidType.Type3, 100f);
            _cup.TryAddLiquid(FP_CupLiquidType.Type1, 1f);
            Assert.That(mixed, Is.EqualTo(1));
            Assert.That(mixedFull, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(1));
            _cup.RemoveLiquidMl(9f);
            Assert.That(_cup.LiquidType, Is.EqualTo(FP_CupLiquidType.Mixed));
            _cup.Empty();
            Assert.That(mixedEmpty, Is.EqualTo(1));
            Assert.That(_cup.IsEmpty, Is.True);
            Assert.That(_cup.LiquidType, Is.EqualTo(FP_CupLiquidType.None));
            _cup.TryAddLiquid(FP_CupLiquidType.Type3, 1f);
            Assert.That(_cup.LiquidType, Is.EqualTo(FP_CupLiquidType.Type3));
            _cup.TryAddLiquid(FP_CupLiquidType.Type1, 1f);
            Assert.That(mixed, Is.EqualTo(2));
        }

        [Test]
        public void OverflowDoesNotChangeFullPureCupAndFullEventsRearmAfterPartialDrain()
        {
            int full = 0, mixed = 0;
            _cup.OnFilled.AddListener(c => full++);
            _cup.OnMixed.AddListener(c => mixed++);
            _cup.TryAddLiquid(FP_CupLiquidType.Type1, 10f);
            Assert.That(_cup.TryAddLiquid(FP_CupLiquidType.Type2, 10f), Is.True);
            Assert.That(_cup.LiquidType, Is.EqualTo(FP_CupLiquidType.Type1));
            Assert.That(mixed, Is.Zero);
            _cup.RemoveLiquidMl(1f);
            _cup.TryAddLiquid(FP_CupLiquidType.Type1, 1f);
            Assert.That(full, Is.EqualTo(2));
        }

        [Test]
        public void InitialEmptyHasNoTypedEmptyAndInitialMixedFullReportsItsState()
        {
            int empty = 0, mixed = 0, mixedFull = 0;
            var emptyCounts = new int[5];
            ListenEmpty(emptyCounts);
            _cup.OnEmpty.AddListener(c => empty++);
            _cup.OnMixed.AddListener(c => mixed++);
            _cup.OnMixedFilled.AddListener(c => mixedFull++);
            Invoke(_cup, "Awake");
            Assert.That(empty, Is.EqualTo(1));
            Assert.That(emptyCounts, Is.EqualTo(new int[5]));
            Set(_cup, "volumeMl", 10f);
            Set(_cup, "liquidType", FP_CupLiquidType.Mixed);
            Invoke(_cup, "Awake");
            Assert.That(mixed, Is.EqualTo(1));
            Assert.That(mixedFull, Is.EqualTo(1));
            _cup.enabled = false;
            _cup.enabled = true;
            Assert.That(mixedFull, Is.EqualTo(1));
            Assert.That(_cup.VolumeMl, Is.EqualTo(10f));
        }

        [Test]
        public void TypedEmptyCleanupPrecedesGenericAutoRefill()
        {
            int type1Empty = 0, type2Filled = 0;
            _cup.OnType1Empty.AddListener(c => type1Empty++);
            _cup.OnType2Filled.AddListener(c => type2Filled++);
            _cup.OnEmpty.AddListener(c => c.TryAddLiquid(FP_CupLiquidType.Type2, 10f));
            _cup.TryAddLiquid(FP_CupLiquidType.Type1, 10f);
            _cup.Empty();
            Assert.That(type1Empty, Is.EqualTo(1));
            Assert.That(type2Filled, Is.EqualTo(1));
            Assert.That(_cup.LiquidType, Is.EqualTo(FP_CupLiquidType.Type2));
        }

        [Test]
        public void ExistingWaterRegionUsesSourceTypeAndRetainsDefaultWaterApi()
        {
            var obj = new GameObject("Cup inlet");
            obj.transform.SetParent(_root.transform);
            var region = obj.AddComponent<FP_WaterFillRegion>();
            Invoke(region, "Awake");
            Set(region, "cup", _cup);
            Assert.That(region.TryReceiveWater(3f, FP_CupLiquidType.Type1), Is.True);
            Assert.That(_cup.LiquidType, Is.EqualTo(FP_CupLiquidType.Type1));
            Assert.That(region.TryReceiveWater(3f, FP_CupLiquidType.Type2), Is.True);
            Assert.That(_cup.LiquidType, Is.EqualTo(FP_CupLiquidType.Mixed));
            _cup.Empty();
            Assert.That(region.TryReceiveWater(5f), Is.True);
            Assert.That(_cup.LiquidType, Is.EqualTo(FP_CupLiquidType.Type3));
            var coffee = obj.AddComponent<FP_EquipmentCoffeeMaker>();
            Invoke(coffee, "Awake");
            Set(region, "coffeeMaker", coffee);
            Assert.That(region.TryReceiveWater(5f), Is.False);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidVolumeDoesNotFillOrDrain(float amount)
        {
            Assert.That(_cup.TryAddLiquid(FP_CupLiquidType.Type1, amount), Is.False);
            _cup.TryAddLiquid(FP_CupLiquidType.Type2, 5f);
            Assert.That(_cup.RemoveLiquidMl(amount), Is.Zero);
            Assert.That(_cup.VolumeMl, Is.EqualTo(5f));
        }

        [Test]
        public void NoneUnknownTypesDisabledAndZeroCapacityCannotReceive()
        {
            Assert.That(_cup.TryAddLiquid(FP_CupLiquidType.None, 1f), Is.False);
            Assert.That(_cup.TryAddLiquid((FP_CupLiquidType)99, 1f), Is.False);
            _cup.enabled = false;
            Assert.That(_cup.TryAddLiquid(FP_CupLiquidType.Type1, 1f), Is.False);
            _cup.enabled = true;
            Set(_cup, "capacityMl", 0f);
            Assert.That(_cup.TryAddLiquid(FP_CupLiquidType.Type1, 1f), Is.False);
            Assert.That(_cup.IsFilled || _cup.CanPour, Is.False);
        }
    }
}
