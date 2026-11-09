// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;

    public class FP_ContentTypeTests
    {
        private GameObject _root;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private T Add<T>() where T : Component
        {
            var obj = new GameObject(typeof(T).Name);
            obj.transform.SetParent(_root.transform);
            return obj.AddComponent<T>();
        }
        private FP_JarContent Jar(FP_ContentType type, string label = "Same label")
        {
            var jar = Add<FP_JarContent>();
            Set(jar, "contentType", type);
            Set(jar, "contentId", label);
            return jar;
        }
        [SetUp] public void SetUp() => _root = new GameObject("Content type tests");
        [TearDown] public void TearDown() => Object.DestroyImmediate(_root);

        [TestCase(FP_ContentType.Type1)]
        [TestCase(FP_ContentType.Type2)]
        [TestCase(FP_ContentType.Type3)]
        [TestCase(FP_ContentType.Mixed)]
        public void TypeSurvivesJarToToolToDestinationAndResetsOnlyReceivers(FP_ContentType type)
        {
            var jar = Jar(type);
            var tool = Add<FP_JarContentTool>();
            var destination = Add<FP_ContentDestination>();
            int toolFilled = 0, toolEmpty = 0, destinationFilled = 0, destinationEmpty = 0;
            switch (type)
            {
                case FP_ContentType.Type1:
                    tool.OnType1Filled.AddListener(t => toolFilled++); tool.OnType1Empty.AddListener(t => toolEmpty++);
                    destination.OnType1Filled.AddListener(d => destinationFilled++); destination.OnType1Empty.AddListener(d => destinationEmpty++); break;
                case FP_ContentType.Type2:
                    tool.OnType2Filled.AddListener(t => toolFilled++); tool.OnType2Empty.AddListener(t => toolEmpty++);
                    destination.OnType2Filled.AddListener(d => destinationFilled++); destination.OnType2Empty.AddListener(d => destinationEmpty++); break;
                case FP_ContentType.Type3:
                    tool.OnType3Filled.AddListener(t => toolFilled++); tool.OnType3Empty.AddListener(t => toolEmpty++);
                    destination.OnType3Filled.AddListener(d => destinationFilled++); destination.OnType3Empty.AddListener(d => destinationEmpty++); break;
                case FP_ContentType.Mixed:
                    tool.OnMixedFilled.AddListener(t => toolFilled++); tool.OnMixedEmpty.AddListener(t => toolEmpty++);
                    destination.OnMixedFilled.AddListener(d => destinationFilled++); destination.OnMixedEmpty.AddListener(d => destinationEmpty++); break;
            }
            Set(destination, "capacityMl", 5f);
            jar.TryTakeContent(tool);
            Assert.That(tool.ContentType, Is.EqualTo(type));
            Assert.That(toolFilled, Is.EqualTo(1));
            tool.TryTransferContent(destination);
            Assert.That(destination.ContentType, Is.EqualTo(type), "Capture the tool type before its debit clears it.");
            Assert.That(destinationFilled, Is.EqualTo(1));
            Assert.That(toolEmpty, Is.EqualTo(1));
            Assert.That(tool.ContentType, Is.EqualTo(FP_ContentType.None));
            destination.Empty(); destination.Empty();
            Assert.That(destinationEmpty, Is.EqualTo(1));
            Assert.That(destination.ContentType, Is.EqualTo(FP_ContentType.None));
            jar.Empty();
            Assert.That(jar.ContentType, Is.EqualTo(type));
        }

        [Test]
        public void ToolMixesDifferentSlotsWithSameLabelAndKeepsMixThroughPartialDrain()
        {
            var a = Jar(FP_ContentType.Type1);
            var b = Jar(FP_ContentType.Type2);
            var c = Jar(FP_ContentType.Type3);
            var tool = Add<FP_JarContentTool>();
            Set(tool, "capacityMl", 15f);
            int mixed = 0, full = 0, mixedFull = 0, mixedEmpty = 0;
            tool.OnMixed.AddListener(t => mixed++);
            tool.OnFilled.AddListener(t => full++);
            tool.OnMixedFilled.AddListener(t => mixedFull++);
            tool.OnMixedEmpty.AddListener(t => { Assert.That(t.ContentType, Is.EqualTo(FP_ContentType.None)); mixedEmpty++; });
            a.TryTakeContent(tool); b.TryTakeContent(tool);
            Assert.That(tool.ContentType, Is.EqualTo(FP_ContentType.Mixed));
            Assert.That(mixed, Is.EqualTo(1));
            Assert.That(full, Is.Zero);
            c.TryTakeContent(tool);
            Assert.That(mixed, Is.EqualTo(1));
            Assert.That(mixedFull, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(1));
            tool.RemoveContentMl(12f);
            Assert.That(tool.ContentType, Is.EqualTo(FP_ContentType.Mixed));
            var destination = Add<FP_ContentDestination>();
            tool.TryTransferContent(destination);
            Assert.That(destination.ContentType, Is.EqualTo(FP_ContentType.Mixed));
            Assert.That(mixedEmpty, Is.EqualTo(1));
            c.TryTakeContent(tool);
            Assert.That(tool.ContentType, Is.EqualTo(FP_ContentType.Type3));
        }

        [Test]
        public void DestinationAcceptsDifferentFoodsThroughSameReceiverAndMixesOnce()
        {
            var a = Jar(FP_ContentType.Type1, "Yogurt");
            var b = Jar(FP_ContentType.Type2, "Sauce");
            var tool = Add<FP_JarContentTool>();
            var destination = Add<FP_ContentDestination>();
            Set(destination, "capacityMl", 15f);
            int mixed = 0, mixedFull = 0;
            destination.OnMixed.AddListener(d => mixed++);
            destination.OnMixedFilled.AddListener(d => mixedFull++);
            a.TryTakeContent(tool); tool.TryTransferContent(destination);
            b.TryTakeContent(tool); tool.TryTransferContent(destination);
            a.TryTakeContent(tool); tool.TryTransferContent(destination);
            Assert.That(destination.ContentType, Is.EqualTo(FP_ContentType.Mixed));
            Assert.That(destination.ContentId, Is.EqualTo("Mixed"));
            Assert.That(destination.AmountMl, Is.EqualTo(15f));
            Assert.That(mixed, Is.EqualTo(1));
            Assert.That(mixedFull, Is.EqualTo(1));
            b.TryTakeContent(tool);
            Assert.That(tool.TryTransferContent(destination), Is.Zero);
            Assert.That(tool.AmountMl, Is.EqualTo(5f));
            destination.Empty();
            tool.TryTransferContent(destination);
            Assert.That(destination.ContentType, Is.EqualTo(FP_ContentType.Type2));
        }

        [Test]
        public void DifferentLabelsInSameSlotAreAcceptedWithoutMixing()
        {
            var a = Jar(FP_ContentType.Type1, "Peanut butter");
            var b = Jar(FP_ContentType.Type1, "Other label");
            var tool = Add<FP_JarContentTool>();
            Set(tool, "capacityMl", 10f);
            a.TryTakeContent(tool); b.TryTakeContent(tool);
            Assert.That(tool.AmountMl, Is.EqualTo(10f));
            Assert.That(tool.ContentType, Is.EqualTo(FP_ContentType.Type1));
        }
    }
}
