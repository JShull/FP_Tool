// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>Emits particles while a container's local upright axis is tipped away from world up.</summary>
    public class FP_TipParticlePour : MonoBehaviour
    {
        [SerializeField] private Transform origin;
        [SerializeField] private ParticleSystem pourParticles;
        [SerializeField] private Vector3 localUpAxis = Vector3.up;
        [SerializeField, Range(0f, 180f)] private float startAngle = 100f;
        [SerializeField, Range(0f, 180f)] private float stopAngle = 80f;
        public bool IsPouring { get; private set; }
        public float TipAngle => Vector3.Angle((origin != null ? origin : transform).TransformDirection(localUpAxis), Vector3.up);
        public UnityEvent OnPourStarted = new UnityEvent();
        public UnityEvent OnPourStopped = new UnityEvent();

        private void OnEnable()
        {
            if (pourParticles != null)
            {
                var emission = pourParticles.emission;
                emission.enabled = false;
            }
            RefreshPouring();
        }

        private void Update() => RefreshPouring();

        public void RefreshPouring()
        {
            if (!isActiveAndEnabled || pourParticles == null) return;
            SetPouring(IsPouring ? TipAngle > stopAngle : TipAngle >= startAngle);
        }

        private void SetPouring(bool pouring)
        {
            if (pourParticles != null)
            {
                var emission = pourParticles.emission;
                emission.enabled = pouring;
                if (pouring && !pourParticles.isPlaying) pourParticles.Play(false);
            }
            if (IsPouring == pouring) return;
            IsPouring = pouring;
            (pouring ? OnPourStarted : OnPourStopped)?.Invoke();
        }

        private void OnDisable() => SetPouring(false);

        private void OnValidate()
        {
            if (localUpAxis.sqrMagnitude < 0.0001f) localUpAxis = Vector3.up;
            stopAngle = Mathf.Min(stopAngle, startAngle);
        }
    }
}
