// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Tests
{
    using System.Reflection;
    using NUnit.Framework;
    using UnityEngine;

    public class FP_JarContentTests
    {
        private GameObject _root;
        private FP_JarContent _jar;
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
            _root = new GameObject("Jar tests");
            _jar = _root.AddComponent<FP_JarContent>();
            Set(_jar, "contentId", "Yogurt");
            Set(_jar, "capacityMl", 10f);
            Set(_jar, "amountMl", 10f);
            Invoke(_jar, "Awake");
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void TransfersOnlyAcceptedPortionAndMetadataBeforeFeedback()
        {
            var tool = Child("Spoon").AddComponent<FP_JarContentTool>();
            Set(tool, "capacityMl", 2f);
            int received = 0, taken = 0;
            tool.OnContentReceived.AddListener(t =>
            {
                Assert.That(_jar.AmountMl, Is.EqualTo(8f));
                Assert.That(t.ContentId, Is.EqualTo("Yogurt"));
                Assert.That(t.LastSource, Is.EqualTo(_jar));
                received++;
            });
            _jar.OnContentTaken.AddListener((j, t) => { Assert.That(t, Is.EqualTo(tool)); taken++; });
            Assert.That(_jar.TryTakeContent(tool), Is.EqualTo(2f));
            Assert.That(tool.AmountMl, Is.EqualTo(2f));
            Assert.That(_jar.TryTakeContent(tool), Is.Zero);
            Assert.That(_jar.AmountMl, Is.EqualTo(8f));
            Assert.That(received, Is.EqualTo(1));
            Assert.That(taken, Is.EqualTo(1));
        }

        [Test]
        public void ToolRejectsDifferentFoodUntilEmptyAndJarKeepsItsIdentityWhenEmpty()
        {
            var tool = Child("Knife").AddComponent<FP_JarContentTool>();
            Set(tool, "capacityMl", 10f);
            _jar.TryTakeContent(tool);
            var other = Child("Other jar").AddComponent<FP_JarContent>();
            Set(other, "contentId", "Mayo");
            Invoke(other, "Awake");
            Assert.That(other.TryTakeContent(tool), Is.Zero);
            tool.Empty();
            Assert.That(tool.ContentId, Is.Empty);
            Assert.That(other.TryTakeContent(tool), Is.EqualTo(5f));
            Assert.That(tool.ContentId, Is.EqualTo("Mayo"));
            _jar.Empty();
            Assert.That(_jar.ContentId, Is.EqualTo("Yogurt"));
        }

        [Test]
        public void CustomReceiverCannotReentrantlyDrainJarDuringAcceptance()
        {
            var receiver = new Receiver { Accepted = 2f };
            receiver.OnReceive = source =>
            {
                Assert.That(source.RemoveContentMl(10f), Is.Zero);
                source.SetAmountMl(0f);
                Assert.That(source.AmountMl, Is.EqualTo(10f));
            };
            receiver.OnCommitted = source => Assert.That(source.AmountMl, Is.EqualTo(8f));
            Assert.That(_jar.TryTakeContent(receiver), Is.EqualTo(2f));
            Assert.That(_jar.AmountMl, Is.EqualTo(8f));
        }

        [Test]
        public void ReceiverFeedbackMayEmptyJarWithoutRepeatingEmptyEvent()
        {
            int empty = 0;
            _jar.OnEmpty.AddListener(j => empty++);
            var receiver = new Receiver { Accepted = 2f, OnCommitted = source => source.Empty() };
            _jar.TryTakeContent(receiver);
            Assert.That(empty, Is.EqualTo(1));
            Assert.That(_jar.IsEmpty, Is.True);
        }

        [Test]
        public void EntryIsDeduplicatedAcrossChildCollidersAndRegions()
        {
            var first = Region();
            var second = Region();
            var tool = Child("Spoon").AddComponent<FP_JarContentTool>();
            var a = Child("Collider a"); a.transform.SetParent(tool.transform, false);
            var b = Child("Collider b"); b.transform.SetParent(tool.transform, false);
            var aCollider = a.AddComponent<BoxCollider>();
            var bCollider = b.AddComponent<BoxCollider>();
            int entered = 0, exited = 0;
            _jar.OnToolEntered.AddListener((j, t) => entered++);
            _jar.OnToolExited.AddListener((j, t) => exited++);
            Invoke(first, "OnTriggerEnter", aCollider);
            Invoke(first, "OnTriggerEnter", bCollider);
            Invoke(second, "OnTriggerEnter", bCollider);
            Invoke(first, "OnTriggerStay", aCollider);
            Assert.That(entered, Is.EqualTo(1));
            Assert.That(_jar.AmountMl, Is.EqualTo(5f));
            Invoke(first, "OnTriggerExit", aCollider);
            Invoke(first, "OnTriggerExit", bCollider);
            Assert.That(exited, Is.Zero);
            Invoke(second, "OnTriggerExit", bCollider);
            Assert.That(exited, Is.EqualTo(1));
            tool.Empty();
            Invoke(first, "OnTriggerEnter", aCollider);
            Assert.That(entered, Is.EqualTo(2));
            Assert.That(_jar.IsEmpty, Is.True);
        }

        [Test]
        public void ManualTakingRequiresPresentToolAndAutoTakeCanBeOff()
        {
            Set(_jar, "takeOnToolEnter", false);
            var region = Region();
            var tool = Child("Spoon").AddComponent<FP_JarContentTool>();
            var collider = tool.gameObject.AddComponent<BoxCollider>();
            _jar.TakeWithTool(tool);
            Assert.That(_jar.AmountMl, Is.EqualTo(10f));
            Invoke(region, "OnTriggerEnter", collider);
            Assert.That(tool.AmountMl, Is.Zero);
            _jar.TakeWithTool(tool);
            Assert.That(tool.AmountMl, Is.EqualTo(5f));
            Invoke(region, "OnTriggerExit", collider);
            tool.Empty();
            _jar.TakeWithTool(tool);
            Assert.That(tool.AmountMl, Is.Zero);
        }

        [Test]
        public void DisabledColliderReleasesToolAndPouringIsOptional()
        {
            var region = Region();
            var tool = Child("Spoon").AddComponent<FP_JarContentTool>();
            var collider = tool.gameObject.AddComponent<BoxCollider>();
            int exited = 0;
            _jar.OnToolExited.AddListener((j, t) => exited++);
            Invoke(region, "OnTriggerEnter", collider);
            collider.enabled = false;
            Invoke(region, "LateUpdate");
            Assert.That(exited, Is.EqualTo(1));
            Assert.That(_jar.CanPour, Is.False);
            _jar.SetPouringEnabled(true);
            Assert.That(_jar.CanPour, Is.True);
            _jar.SetPouringEnabled(false);
            Assert.That(_jar.CanPour, Is.False);
        }

        [Test]
        public void FillVisualKeepsBottomAnchoredAndDoesNotRecaptureShrunkenScale()
        {
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(_root.transform, false);
            mesh.transform.localScale = new Vector3(1f, 2f, 1f);
            mesh.transform.localPosition = Vector3.up;
            var visual = Child("Fill visual binder").AddComponent<FP_JarFillVisual>();
            Set(visual, "jar", _jar);
            Set(visual, "contentMesh", mesh.transform);
            Invoke(visual, "OnEnable");
            _jar.SetAmountMl(5f);
            Assert.That(mesh.transform.localScale.y, Is.EqualTo(1f));
            Assert.That(mesh.transform.localPosition.y, Is.EqualTo(0.5f));
            _jar.Empty();
            Assert.That(mesh.transform.localScale.y, Is.Zero);
            Assert.That(mesh.GetComponent<Renderer>().enabled, Is.False);
            Invoke(visual, "OnDisable");
            Invoke(visual, "OnEnable");
            _jar.SetAmountMl(10f);
            Assert.That(mesh.transform.localScale.y, Is.EqualTo(2f));
            Assert.That(mesh.transform.localPosition.y, Is.EqualTo(1f));
            Assert.That(mesh.GetComponent<Renderer>().enabled, Is.True);
        }

        [Test]
        public void EmptyFullEdgesAndRefillsAreClampedAndOneShot()
        {
            int empty = 0, full = 0;
            _jar.OnEmpty.AddListener(j => empty++);
            _jar.OnFull.AddListener(j => full++);
            _jar.Empty(); _jar.Empty();
            Assert.That(empty, Is.EqualTo(1));
            _jar.AddContentMl(1000f); _jar.AddContentMl(1f);
            Assert.That(_jar.AmountMl, Is.EqualTo(10f));
            Assert.That(full, Is.EqualTo(1));
            _jar.RemoveContentMl(1f); _jar.AddContentMl(1f);
            Assert.That(full, Is.EqualTo(2));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-1f)]
        [TestCase(0f)]
        public void InvalidAddRemoveAndReceiverReturnDoNotLoseContent(float amount)
        {
            Assert.That(_jar.RemoveContentMl(amount), Is.Zero);
            _jar.AddContentMl(amount);
            Assert.That(_jar.TryTakeContent(new Receiver { Accepted = amount }), Is.Zero);
            Assert.That(_jar.AmountMl, Is.EqualTo(10f));
        }

        private FP_JarAccessRegion Region()
        {
            var obj = Child("Jar access region");
            obj.AddComponent<BoxCollider>().isTrigger = true;
            var region = obj.AddComponent<FP_JarAccessRegion>();
            Set(region, "jar", _jar);
            return region;
        }
        private class Receiver : IFPJarContentReceiver
        {
            public float Accepted;
            public System.Action<FP_JarContent> OnReceive, OnCommitted;
            public void OnJarEntered(FP_JarContent jar) { }
            public void OnJarExited(FP_JarContent jar) { }
            public float ReceiveJarContent(FP_JarContent source, string id, float offered) { OnReceive?.Invoke(source); return Accepted; }
            public void OnJarContentReceived(FP_JarContent source, string id, float amount) => OnCommitted?.Invoke(source);
        }
    }
}
