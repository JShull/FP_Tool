// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Editor
{
    using FuzzPhyte.Utility;
    using UnityEditor;
    using UnityEngine;

    public static class FP_BeanPourPrototypeBuilder
    {
        [MenuItem("GameObject/FuzzPhyte/Equipment/Bean Pour Prototype", false, 11)]
        private static void Create(MenuCommand command)
        {
            var root = Build();
            GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root, "Create bean pour prototype");
            Selection.activeGameObject = root;
        }

        public static GameObject Build()
        {
            var root = new GameObject("FP Bean Pour Prototype");
            root.SetActive(false);
            var ticks = new GameObject("Sample Tick System");
            ticks.transform.SetParent(root.transform, false);
            var tickSystem = ticks.AddComponent<FP_TickSystem>();
            SetReference(tickSystem, "dontDestroyOnLoad", false);
            var makerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            makerObject.name = "Coffee Maker";
            makerObject.transform.SetParent(root.transform, false);
            // Keep the equipment root unscaled; scale the visual separately.
            var visual = new GameObject("Coffee Maker Root");
            visual.transform.SetParent(root.transform, false);
            makerObject.transform.SetParent(visual.transform, false);
            makerObject.transform.localPosition = Vector3.up * 0.15f;
            makerObject.transform.localScale = new Vector3(0.4f, 0.3f, 0.35f);
            var maker = visual.AddComponent<FP_EquipmentCoffeeMaker>();
            SetReference(maker, "waterMl", 1000f);

            var regionObject = new GameObject("Bean Fill Region");
            regionObject.transform.SetParent(visual.transform, false);
            regionObject.transform.localPosition = Vector3.up * 0.34f;
            var box = regionObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.4f, 0.1f, 0.35f);
            box.isTrigger = true;
            var region = regionObject.AddComponent<FP_BeanFillRegion>();
            SetReference(region, "coffeeMaker", maker);

            var container = new GameObject("Bean Container - rotate X to 125 degrees");
            container.transform.SetParent(root.transform, false);
            container.transform.localPosition = Vector3.up * 0.85f;
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Container Visual";
            body.transform.SetParent(container.transform, false);
            body.transform.localScale = new Vector3(0.12f, 0.24f, 0.08f);

            var emitterObject = new GameObject("Bean Particles");
            emitterObject.transform.SetParent(container.transform, false);
            emitterObject.transform.localPosition = Vector3.up * 0.14f;
            emitterObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var particles = emitterObject.AddComponent<ParticleSystem>();
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.startLifetime = 1.2f;
            main.startSpeed = 0.1f;
            main.startSize = 0.018f;
            main.startColor = new Color(0.25f, 0.1f, 0.035f, 1f);
            main.gravityModifier = 1f;
            main.maxParticles = 200;
            var emission = particles.emission;
            emission.rateOverTime = 30f;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.012f;
            var meshObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var renderer = emitterObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = meshObject.GetComponent<MeshFilter>().sharedMesh;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
            Object.DestroyImmediate(meshObject);
            var relay = emitterObject.AddComponent<FP_BeanParticleTrigger>();
            var relayData = new SerializedObject(relay);
            var regions = relayData.FindProperty("fillRegions");
            regions.arraySize = 1;
            regions.GetArrayElementAtIndex(0).objectReferenceValue = region;
            relayData.ApplyModifiedPropertiesWithoutUndo();
            relay.RefreshTriggerRegions();
            var pour = container.AddComponent<FP_TipParticlePour>();
            SetReference(pour, "origin", container.transform);
            SetReference(pour, "pourParticles", particles);
            root.SetActive(true);
            return root;
        }

        private static void SetReference(Object target, string property, object value)
        {
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(property);
            if (value is float number) field.floatValue = number;
            else if (value is bool boolean) field.boolValue = boolean;
            else field.objectReferenceValue = (Object)value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
