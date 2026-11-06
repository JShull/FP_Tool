// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools.Editor
{
    using TMPro;
    using UnityEditor;
    using UnityEngine;

    /// <summary>Creates an editable scale with a wired world-space UI panel.</summary>
    public static class FP_ScaleBuilder
    {
        [MenuItem("GameObject/FuzzPhyte/Equipment/Scale", false, 10)]
        private static void Create(MenuCommand command)
        {
            var root = Build();
            GameObjectUtility.SetParentAndAlign(root, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(root, "Create scale");
            Selection.activeGameObject = root;
        }

        public static GameObject Build()
        {
            var root = new GameObject("FP Scale");
            var scale = root.AddComponent<FP_EquipmentScale>();
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform";
            platform.transform.SetParent(root.transform, false);
            platform.transform.localScale = new Vector3(0.4f, 0.04f, 0.3f);

            var volumeObject = new GameObject("Weighing Volume");
            volumeObject.transform.SetParent(root.transform, false);
            volumeObject.transform.localPosition = new Vector3(0f, 0.17f, 0f);
            var trigger = volumeObject.AddComponent<BoxCollider>();
            trigger.size = new Vector3(0.4f, 0.3f, 0.3f);
            trigger.isTrigger = true;
            var volume = volumeObject.AddComponent<FP_ScaleContainmentVolume>();
            SetReference(volume, "scale", scale);

            var panel = new GameObject("Weight Panel", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.Image));
            panel.SetActive(false);
            panel.transform.SetParent(root.transform, false);
            panel.transform.localPosition = new Vector3(0f, 0.08f, -0.19f);
            panel.transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            panel.transform.localScale = Vector3.one * 0.001f;
            panel.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 80f);
            var image = panel.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.6f, 1f, 0.8f, 1f);
            image.raycastTarget = false;

            var labelObject = new GameObject("Weight", typeof(RectTransform));
            labelObject.transform.SetParent(panel.transform, false);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "0.000 kg";
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 38f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.025f, 0.055f, 0.065f, 1f);
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(8f, 8f);
            label.rectTransform.offsetMax = new Vector2(-8f, -8f);

            var display = panel.AddComponent<FP_ScaleDisplay>();
            SetReference(display, "scale", scale);
            SetReference(display, "weightLabel", label);
            panel.SetActive(true);
            return root;
        }

        private static void SetReference(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
