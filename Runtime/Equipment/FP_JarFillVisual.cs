// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using UnityEngine;

    /// <summary>Scales an authored full mesh on selected local axes using jar.Fill01, optionally preserving an edge.</summary>
    public class FP_JarFillVisual : MonoBehaviour
    {
        [SerializeField] private FP_JarContent jar;
        [SerializeField] private Transform contentMesh;
        [Tooltip("Local axis shrink weights (-1 to 1). Zero preserves an axis; magnitude 1 fully collapses it at empty. Negative anchors the maximum side instead of the minimum.")]
        [SerializeField] private Vector3 scaleDirection = Vector3.up;
        [InspectorName("Anchor Edge")]
        [SerializeField] private bool anchorBottom = true;
        [SerializeField] private bool hideWhenEmpty = true;
        private FP_JarContent _subscribedJar;
        private Transform _capturedMesh;
        private Vector3 _fullScale, _fullPosition;
        private Renderer _renderer;
        private bool _rendererWasEnabled;
        private Bounds _meshBounds;

        private void Reset() => jar = GetComponentInParent<FP_JarContent>();
        private void OnEnable()
        {
            _subscribedJar = jar;
            if (_subscribedJar != null) _subscribedJar.OnContentChanged.AddListener(RefreshVisual);
            RefreshVisual(_subscribedJar);
        }
        public void RefreshVisual(FP_JarContent source)
        {
            if (source == null || contentMesh == null) return;
            if (_capturedMesh != contentMesh)
            {
                _capturedMesh = contentMesh;
                _fullScale = contentMesh.localScale;
                _fullPosition = contentMesh.localPosition;
                _renderer = contentMesh.GetComponent<Renderer>();
                _rendererWasEnabled = _renderer != null && _renderer.enabled;
                var filter = contentMesh.GetComponent<MeshFilter>();
                _meshBounds = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds : new Bounds();
            }
            var direction = new Vector3(AxisWeight(scaleDirection.x), AxisWeight(scaleDirection.y), AxisWeight(scaleDirection.z));
            var weights = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            var scale = Vector3.Scale(_fullScale, Vector3.one - weights * (1f - source.Fill01));
            var anchor = new Vector3(direction.x < 0f ? _meshBounds.max.x : _meshBounds.min.x,
                direction.y < 0f ? _meshBounds.max.y : _meshBounds.min.y,
                direction.z < 0f ? _meshBounds.max.z : _meshBounds.min.z);
            contentMesh.localScale = scale;
            contentMesh.localPosition = _fullPosition + (anchorBottom
                ? contentMesh.localRotation * Vector3.Scale(anchor, _fullScale - scale) : Vector3.zero);
            if (_renderer != null) _renderer.enabled = _rendererWasEnabled && (!hideWhenEmpty || !source.IsEmpty);
        }
        private static float AxisWeight(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp(value, -1f, 1f);
        private void OnDisable()
        {
            if (_subscribedJar != null) _subscribedJar.OnContentChanged.RemoveListener(RefreshVisual);
            _subscribedJar = null;
        }
    }
}
