// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using UnityEngine;

    /// <summary>Scales an authored full mesh along local Y using jar.Fill01, optionally preserving its bottom.</summary>
    public class FP_JarFillVisual : MonoBehaviour
    {
        [SerializeField] private FP_JarContent jar;
        [SerializeField] private Transform contentMesh;
        [SerializeField] private bool anchorBottom = true;
        [SerializeField] private bool hideWhenEmpty = true;
        private FP_JarContent _subscribedJar;
        private Transform _capturedMesh;
        private Vector3 _fullScale, _fullPosition;
        private Renderer _renderer;
        private bool _rendererWasEnabled;
        private float _meshBottomY;

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
                _meshBottomY = filter != null && filter.sharedMesh != null ? filter.sharedMesh.bounds.min.y : 0f;
            }
            var scale = _fullScale;
            scale.y *= source.Fill01;
            contentMesh.localScale = scale;
            contentMesh.localPosition = _fullPosition + (anchorBottom
                ? contentMesh.localRotation * Vector3.up * (_meshBottomY * (_fullScale.y - scale.y)) : Vector3.zero);
            if (_renderer != null) _renderer.enabled = _rendererWasEnabled && (!hideWhenEmpty || !source.IsEmpty);
        }
        private void OnDisable()
        {
            if (_subscribedJar != null) _subscribedJar.OnContentChanged.RemoveListener(RefreshVisual);
            _subscribedJar = null;
        }
    }
}
