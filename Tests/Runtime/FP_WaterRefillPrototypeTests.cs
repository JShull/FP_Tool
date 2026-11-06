// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

#if UNITY_EDITOR
namespace FuzzPhyte.Tools.Tests
{
    using System.Collections;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;

    public class FP_WaterRefillPrototypeTests
    {
        private GameObject _instance;
        private string _temporaryPrefabPath;
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_instance != null) Object.Destroy(_instance);
            yield return null;
            if (!string.IsNullOrEmpty(_temporaryPrefabPath)) AssetDatabase.DeleteAsset(_temporaryPrefabPath);
        }

        [UnityTest]
        public IEnumerator StationaryWaterParticlesRefillOnceAndReportThresholds()
        {
            GameObject prefab = null;
            foreach (var guid in AssetDatabase.FindAssets("FP_WaterRefillPrototype t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == "FP_WaterRefillPrototype.prefab") prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            if (prefab == null)
                foreach (var guid in AssetDatabase.FindAssets("FP_WaterRefillPrototypeBuilder t:MonoScript"))
                {
                    var builderPath = AssetDatabase.GUIDToAssetPath(guid);
                    var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(builderPath);
                    var packageRoot = package != null ? package.resolvedPath : Path.GetDirectoryName(Path.GetDirectoryName(builderPath));
                    var samplePath = Path.Combine(packageRoot, "Samples~", "WaterRefill", "FP_WaterRefillPrototype.prefab");
                    if (!File.Exists(samplePath)) continue;
                    _temporaryPrefabPath = "Assets/FP_WaterRefillTest_" + System.Guid.NewGuid().ToString("N") + ".prefab";
                    File.Copy(samplePath, Path.Combine(Application.dataPath, Path.GetFileName(_temporaryPrefabPath)));
                    AssetDatabase.ImportAsset(_temporaryPrefabPath, ImportAssetOptions.ForceSynchronousImport);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_temporaryPrefabPath);
                    break;
                }
            Assert.That(prefab, Is.Not.Null);
            _instance = Object.Instantiate(prefab);
            Assert.That(_instance.GetComponentInChildren<FP_TipParticlePour>(), Is.Null);
            var maker = _instance.GetComponentInChildren<FP_EquipmentCoffeeMaker>();
            var region = _instance.GetComponentInChildren<FP_WaterFillRegion>();
            var relay = _instance.GetComponentInChildren<FP_WaterParticleTrigger>();
            var particles = relay.GetComponent<ParticleSystem>();
            int enough = 0, full = 0;
            maker.OnEnoughWater.AddListener(m => enough++);
            maker.OnWaterFull.AddListener(m => full++);
            Assert.That(maker.WaterMl, Is.Zero);
            maker.SetWaterMl(247.5f);
            var extraObject = new GameObject("Overlapping water inlet");
            extraObject.transform.SetParent(region.transform.parent, false);
            extraObject.transform.position = region.transform.position;
            extraObject.AddComponent<BoxCollider>().size = region.RegionCollider.size;
            var extra = extraObject.AddComponent<FP_WaterFillRegion>();
            Set(extra, "coffeeMaker", maker);
            Set(relay, "fillRegions", new[] { region, extra });
            relay.RefreshTriggerRegions();
            float deadline = Time.realtimeSinceStartup + 4f;
            while (relay.CapturedParticleCount < 3 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(relay.CapturedParticleCount, Is.GreaterThanOrEqualTo(3));
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            yield return new WaitForSeconds(1.4f);
            Assert.That(maker.WaterMl, Is.EqualTo(247.5f + relay.CapturedParticleCount * 5f).Within(0.0001f));
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(full, Is.Zero);
            maker.SetWaterMl(999f);
            region.TryReceiveWater(5f);
            region.TryReceiveWater(5f);
            Assert.That(full, Is.EqualTo(1));
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(maker.WaterMl, Is.EqualTo(1000f));
            maker.SetWaterMl(0f);
            region.enabled = false;
            extra.enabled = false;
            particles.Play(false);
            yield return new WaitForSeconds(0.6f);
            Assert.That(maker.WaterMl, Is.Zero);
            Assert.That(enough, Is.EqualTo(1));
            Assert.That(full, Is.EqualTo(1));
        }
    }
}
#endif
