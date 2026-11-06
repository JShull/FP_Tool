// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Editor
{
    using UnityEditor;
    using UnityEngine;

    /// <summary>A stationary particle stream wired to a coffee maker's water reservoir.</summary>
    public static class FP_WaterRefillPrototypeBuilder
    {
        [MenuItem("GameObject/FuzzPhyte/Equipment/Water Refill Prototype", false, 12)]
        private static void Create(MenuCommand command)
        {
            var root = Build();
            GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root, "Create water refill prototype");
            Selection.activeGameObject = root;
        }

        public static GameObject Build()
        {
            // Reuse the sample geometry/material configuration, then replace the refill wiring.
            var root = FP_BeanPourPrototypeBuilder.Build();
            root.SetActive(false);
            root.name = "FP Water Refill Prototype";
            var maker = root.GetComponentInChildren<FP_EquipmentCoffeeMaker>(true);
            var makerData = new SerializedObject(maker);
            makerData.FindProperty("beansGrams").floatValue = 60f;
            makerData.FindProperty("waterMl").floatValue = 0f;
            makerData.ApplyModifiedPropertiesWithoutUndo();
            var oldRegion = root.GetComponentInChildren<FP_BeanFillRegion>(true);
            var regionObject = oldRegion.gameObject;
            Object.DestroyImmediate(oldRegion);
            regionObject.name = "Water Fill Region";
            var region = regionObject.AddComponent<FP_WaterFillRegion>();
            var regionData = new SerializedObject(region);
            regionData.FindProperty("coffeeMaker").objectReferenceValue = maker;
            regionData.ApplyModifiedPropertiesWithoutUndo();

            var pour = root.GetComponentInChildren<FP_TipParticlePour>(true);
            var containerObject = pour.gameObject;
            var oldRelay = root.GetComponentInChildren<FP_BeanParticleTrigger>(true);
            var emitterObject = oldRelay.gameObject;
            emitterObject.transform.SetParent(root.transform, false);
            emitterObject.transform.localPosition = Vector3.up * 0.75f;
            emitterObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            emitterObject.name = "Water Stream";
            Object.DestroyImmediate(containerObject);
            Object.DestroyImmediate(oldRelay);
            var particles = emitterObject.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = true;
            main.startColor = new Color(0.15f, 0.55f, 1f, 1f);
            main.startSize = 0.012f;
            var emission = particles.emission;
            emission.enabled = true;
            var relay = emitterObject.AddComponent<FP_WaterParticleTrigger>();
            var relayData = new SerializedObject(relay);
            var regions = relayData.FindProperty("fillRegions");
            regions.arraySize = 1;
            regions.GetArrayElementAtIndex(0).objectReferenceValue = region;
            relayData.ApplyModifiedPropertiesWithoutUndo();
            relay.RefreshTriggerRegions();
            root.SetActive(true);
            return root;
        }
    }
}
