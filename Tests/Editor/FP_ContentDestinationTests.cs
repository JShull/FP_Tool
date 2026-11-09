// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;

    public class FP_ContentDestinationTests
    {
        private GameObject _root;
        private FP_JarContent _jar;
        private FP_JarContentTool _tool;
        private FP_ContentDestination _destination;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Invoke(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
        private GameObject Child(string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_root.transform, false);
            return obj;
        }
        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Destination transfer tests");
            _jar = Child("Mayo jar").AddComponent<FP_JarContent>();
            Set(_jar, "contentId", "Mayo");
            Set(_jar, "amountMl", 50f);
            Invoke(_jar, "Awake");
            _tool = Child("Knife").AddComponent<FP_JarContentTool>();
            _jar.TryTakeContent(_tool);
            _destination = Child("Bread").AddComponent<FP_ContentDestination>();
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(_root);

        private FP_ContentDestinationRegion Region()
        {
            var obj = Child("Bread trigger");
            obj.AddComponent<BoxCollider>().isTrigger = true;
            var region = obj.AddComponent<FP_ContentDestinationRegion>();
            Set(region, "destination", _destination);
            return region;
        }

        [Test]
        public void FullTransferResetsToolAndRetainsSourceMetadataBeforeVisualReset()
        {
            bool cleared = false;
            int empty = 0, received = 0;
            _tool.OnEmpty.AddListener(t => { cleared = true; empty++; });
            _destination.OnContentReceived.AddListener(d =>
            {
                Assert.That(_tool.AmountMl, Is.Zero);
                Assert.That(_tool.LastSource, Is.Null);
                Assert.That(d.LastSourceJar, Is.EqualTo(_jar));
                Assert.That(d.ContentId, Is.EqualTo("Mayo"));
                Assert.That(d.LastAcceptedAmountMl, Is.EqualTo(5f));
                Assert.That(cleared, Is.False, "Copy custom tool color/VFX data here before OnEmpty clears it.");
                received++;
            });
            Assert.That(_tool.TryTransferContent(_destination), Is.EqualTo(5f));
            Assert.That(_tool.ContentId, Is.Empty);
            Assert.That(_destination.AmountMl, Is.EqualTo(5f));
            Assert.That(received, Is.EqualTo(1));
            Assert.That(empty, Is.EqualTo(1));
            Assert.That(_tool.TryTransferContent(_destination), Is.Zero);
            Assert.That(empty, Is.EqualTo(1));
        }

        [Test]
        public void PartialFullAndDisabledTargetsDoNotLoseRemainingContent()
        {
            _destination.enabled = false;
            Assert.That(_tool.TryTransferContent(_destination), Is.Zero);
            _destination.enabled = true;
            Set(_destination, "capacityMl", 2f);
            Assert.That(_tool.TryTransferContent(_destination), Is.EqualTo(2f));
            Assert.That(_tool.AmountMl, Is.EqualTo(3f));
            Assert.That(_tool.ContentId, Is.EqualTo("Mayo"));
            Assert.That(_tool.LastSource, Is.EqualTo(_jar));
            Assert.That(_tool.TryTransferContent(_destination), Is.Zero);
            Assert.That(_tool.AmountMl, Is.EqualTo(3f));
        }

        [Test]
        public void CompoundCollidersAndOverlappingRegionsTransferOnlyOnFirstEntry()
        {
            var first = Region();
            var second = Region();
            var a = Child("Collider a"); a.transform.SetParent(_tool.transform, false);
            var b = Child("Collider b"); b.transform.SetParent(_tool.transform, false);
            var ac = a.AddComponent<BoxCollider>();
            var bc = b.AddComponent<BoxCollider>();
            int entered = 0, exited = 0;
            _destination.OnToolEntered.AddListener((d, t) => entered++);
            _destination.OnToolExited.AddListener((d, t) => exited++);
            Invoke(first, "OnTriggerEnter", ac);
            _jar.TryTakeContent(_tool); // Refill while still touching: Stay/another collider must not transfer again.
            Invoke(first, "OnTriggerEnter", bc);
            Invoke(second, "OnTriggerEnter", bc);
            Invoke(first, "OnTriggerStay", ac);
            Assert.That(_destination.AmountMl, Is.EqualTo(5f));
            Assert.That(_tool.AmountMl, Is.EqualTo(5f));
            Assert.That(entered, Is.EqualTo(1));
            Invoke(first, "OnTriggerExit", ac);
            Invoke(first, "OnTriggerExit", bc);
            Assert.That(exited, Is.Zero);
            Invoke(second, "OnDisable");
            Assert.That(exited, Is.EqualTo(1));
        }

        [Test]
        public void ManualTransferRequiresPresenceAndReentryCanTransferNewPortion()
        {
            var region = Region();
            Set(region, "transferOnEnter", false);
            var collider = _tool.gameObject.AddComponent<BoxCollider>();
            _tool.TransferToDestination(_destination);
            Assert.That(_tool.AmountMl, Is.EqualTo(5f));
            Invoke(region, "OnTriggerEnter", collider);
            Assert.That(_destination.AmountMl, Is.Zero);
            _tool.TransferToDestination(_destination);
            Assert.That(_destination.AmountMl, Is.EqualTo(5f));
            Invoke(region, "OnTriggerExit", collider);
            _jar.TryTakeContent(_tool);
            _tool.TransferToDestination(_destination);
            Assert.That(_tool.AmountMl, Is.EqualTo(5f));
            Set(region, "transferOnEnter", true);
            Invoke(region, "OnTriggerEnter", collider);
            Assert.That(_destination.AmountMl, Is.EqualTo(10f));
        }

        [Test]
        public void CustomReceiverCannotDrainOrRefillToolDuringAcceptance()
        {
            var receiver = new Receiver { Accepted = 2f };
            receiver.Accepting = tool =>
            {
                Assert.That(tool.RemoveContentMl(5f), Is.Zero);
                Assert.That(_jar.TryTakeContent(tool), Is.Zero);
                Assert.That(tool.AmountMl, Is.EqualTo(5f));
            };
            receiver.Committed = (tool, jar, id) =>
            {
                Assert.That(tool.AmountMl, Is.EqualTo(3f));
                Assert.That(jar, Is.EqualTo(_jar));
                Assert.That(id, Is.EqualTo("Mayo"));
            };
            Assert.That(_tool.TryTransferContent(receiver), Is.EqualTo(2f));
            Assert.That(_jar.AmountMl, Is.EqualTo(45f));
        }

        [Test]
        public void CallbackEmptyDoesNotRepeatToolEmptyNotification()
        {
            Set(_destination, "capacityMl", 2f);
            int empty = 0;
            _tool.OnEmpty.AddListener(t => empty++);
            _destination.OnContentReceived.AddListener(d => _tool.Empty());
            _tool.TryTransferContent(_destination);
            Assert.That(empty, Is.EqualTo(1));
            Assert.That(_destination.AmountMl, Is.EqualTo(2f));
        }

        [Test]
        public void DestroyedDestinationAndDisabledColliderReleaseContactOnce()
        {
            var region = Region();
            var collider = _tool.gameObject.AddComponent<BoxCollider>();
            int exited = 0;
            _tool.OnDestinationExited.AddListener((t, d) => exited++);
            Invoke(region, "OnTriggerEnter", collider);
            collider.enabled = false;
            Invoke(region, "LateUpdate");
            Assert.That(exited, Is.EqualTo(1));
            collider.enabled = true;
            _jar.TryTakeContent(_tool);
            Invoke(region, "OnTriggerStay", collider);
            Object.DestroyImmediate(_destination);
            Invoke(region, "LateUpdate");
            Invoke(region, "LateUpdate");
            Assert.That(exited, Is.EqualTo(2));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        [TestCase(0f)]
        public void InvalidReceiverAcceptancePreservesTool(float accepted)
        {
            Assert.That(_tool.TryTransferContent(new Receiver { Accepted = accepted }), Is.Zero);
            Assert.That(_tool.AmountMl, Is.EqualTo(5f));
            Assert.That(_tool.ContentId, Is.EqualTo("Mayo"));
        }

        private class Receiver : IFPToolContentReceiver
        {
            public float Accepted;
            public System.Action<FP_JarContentTool> Accepting;
            public System.Action<FP_JarContentTool, FP_JarContent, string> Committed;
            public void OnContentToolEntered(FP_JarContentTool tool) { }
            public void OnContentToolExited(FP_JarContentTool tool) { }
            public float ReceiveToolContent(FP_JarContentTool tool, FP_JarContent jar, string id, float offered)
            { Accepting?.Invoke(tool); return Accepted; }
            public void OnToolContentReceived(FP_JarContentTool tool, FP_JarContent jar, string id, float accepted) => Committed?.Invoke(tool, jar, id);
        }
    }
}
