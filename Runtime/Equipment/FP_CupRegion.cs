// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>Queries a box for active cup markers; queried only by consumers that need cup detection.</summary>
    [RequireComponent(typeof(BoxCollider)), DisallowMultipleComponent]
    public class FP_CupRegion : MonoBehaviour
    {
        [SerializeField] private LayerMask cupLayers = ~0;
        private BoxCollider _box;
        private Collider[] _overlaps = new Collider[8];

        public bool HasCup
        {
            get
            {
                if (!isActiveAndEnabled) return false;
                if (_box == null) _box = GetComponent<BoxCollider>();
                if (_box == null || !_box.enabled || !_box.isTrigger) return false;
                var scale = transform.lossyScale;
                var halfExtents = Vector3.Scale(_box.size * 0.5f,
                    new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                var physics = gameObject.scene.GetPhysicsScene();
                if (!physics.IsValid()) return false;
                while (true)
                {
                    int count = physics.OverlapBox(transform.TransformPoint(_box.center), halfExtents,
                        _overlaps, transform.rotation, cupLayers, QueryTriggerInteraction.Collide);
                    bool found = false;
                    for (int i = 0; i < count; i++)
                    {
                        if (_overlaps[i] == _box) continue;
                        var cup = _overlaps[i].GetComponentInParent<FP_Cup>();
                        if (cup != null && cup.isActiveAndEnabled) { found = true; break; }
                    }
                    Array.Clear(_overlaps, 0, count);
                    if (found) return true;
                    if (count < _overlaps.Length) return false;
                    // Dense regions must not silently miss a cup beyond the first buffer.
                    Array.Resize(ref _overlaps, _overlaps.Length * 2);
                }
            }
        }

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
        }

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            var previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = previous;
        }
    }
}
