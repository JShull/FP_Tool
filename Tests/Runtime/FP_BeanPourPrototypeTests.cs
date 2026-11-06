// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

#if UNITY_EDITOR
namespace FuzzPhyte.Tools.Tests
{
    using System.Collections;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;

    public class FP_BeanPourPrototypeTests
    {
        private GameObject _instance;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_instance != null) Object.Destroy(_instance);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PrefabPoursThroughActualParticleTriggerWithoutDoubleFilling()
        {
            GameObject prefab = null;
            foreach (var guid in AssetDatabase.FindAssets("FP_BeanPourPrototype t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("/Samples/BeanPour/FP_BeanPourPrototype.prefab"))
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            Assert.That(prefab, Is.Not.Null, "Import/open the BeanPour sample prefab to run its integration test.");
            _instance = Object.Instantiate(prefab);
            var maker = _instance.GetComponentInChildren<FP_EquipmentCoffeeMaker>();
            var pour = _instance.GetComponentInChildren<FP_TipParticlePour>();
            var relay = _instance.GetComponentInChildren<FP_BeanParticleTrigger>();
            var particles = relay.GetComponent<ParticleSystem>();
            var region = _instance.GetComponentInChildren<FP_BeanFillRegion>();
            var extraObject = new GameObject("Overlapping receiver");
            extraObject.transform.SetParent(region.transform.parent, false);
            extraObject.transform.position = region.transform.position;
            extraObject.AddComponent<BoxCollider>().size = region.RegionCollider.size;
            var extra = extraObject.AddComponent<FP_BeanFillRegion>();
            Set(extra, "coffeeMaker", maker);
            Set(relay, "fillRegions", new[] { region, extra });
            relay.RefreshTriggerRegions();
            yield return null;
            Assert.That(pour.IsPouring, Is.False);
            Assert.That(maker.BeansGrams, Is.Zero);
            pour.transform.localRotation = Quaternion.Euler(125f, 0f, 0f);
            pour.RefreshPouring();
            float deadline = Time.realtimeSinceStartup + 4f;
            while (relay.CapturedParticleCount < 3 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(relay.CapturedParticleCount, Is.GreaterThanOrEqualTo(3), "Real OnParticleTrigger callbacks must refill the hopper.");
            pour.transform.localRotation = Quaternion.identity;
            pour.RefreshPouring();
            Assert.That(particles.emission.enabled, Is.False);
            yield return new WaitForSeconds(1.4f);
            Assert.That(maker.BeansGrams, Is.EqualTo(relay.CapturedParticleCount * 0.2f).Within(0.0001f));
            float settled = maker.BeansGrams;
            yield return null;
            yield return null;
            Assert.That(maker.BeansGrams, Is.EqualTo(settled));

            region.enabled = false;
            extra.enabled = false;
            pour.transform.localRotation = Quaternion.Euler(125f, 0f, 0f);
            pour.RefreshPouring();
            yield return new WaitForSeconds(0.6f);
            Assert.That(maker.BeansGrams, Is.EqualTo(settled));
            Assert.That(pour.IsPouring, Is.True);
        }
    }
}
#endif
