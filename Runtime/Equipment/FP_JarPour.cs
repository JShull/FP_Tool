// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using UnityEngine;

    /// <summary>Reuses the existing tip controller with a finite jar supply.</summary>
    [RequireComponent(typeof(FP_TipParticlePour)), DisallowMultipleComponent]
    public class FP_JarPour : MonoBehaviour
    {
        [SerializeField] private FP_JarContent jar;
        [SerializeField, Min(0.01f)] private float flowMlPerSecond = 150f;
        [SerializeField] private bool autoAdvance = true;
        private FP_TipParticlePour _pour;
        private FP_JarContent _subscribedJar;

        private void Reset() => jar = GetComponentInParent<FP_JarContent>();
        private void OnEnable()
        {
            _pour = GetComponent<FP_TipParticlePour>();
            _subscribedJar = jar;
            _pour.OnPourStarted.AddListener(Started);
            _pour.OnPourStopped.AddListener(Stopped);
            if (_subscribedJar != null) _subscribedJar.OnStateChanged.AddListener(RefreshGate);
            RefreshGate(_subscribedJar);
            if (_pour.IsPouring) Started();
        }
        private void Update() { if (autoAdvance) AdvancePour(Time.deltaTime); }
        public void AdvancePour(float dt)
        {
            if (!isActiveAndEnabled || _pour == null) return;
            RefreshGate(_subscribedJar);
            if (_subscribedJar == null || !_pour.IsPouring || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            double amount = (double)flowMlPerSecond * dt;
            _subscribedJar.RemoveContentMl((float)System.Math.Min(amount, _subscribedJar.AmountMl));
            RefreshGate(_subscribedJar);
        }
        private void RefreshGate(FP_JarContent source)
        {
            if (_pour != null) _pour.SetPouringAllowed(isActiveAndEnabled && source != null && source.CanPour);
            if (source != null && _pour != null) source.SetPouringState(isActiveAndEnabled && _pour.IsPouring && source.CanPour);
        }
        private void Started() { if (_subscribedJar != null) _subscribedJar.SetPouringState(true); }
        private void Stopped() { if (_subscribedJar != null) _subscribedJar.SetPouringState(false); }
        private void OnDisable()
        {
            if (_subscribedJar != null) _subscribedJar.OnStateChanged.RemoveListener(RefreshGate);
            if (_pour != null)
            {
                _pour.OnPourStarted.RemoveListener(Started);
                _pour.OnPourStopped.RemoveListener(Stopped);
                _pour.SetPouringAllowed(false);
            }
            if (_subscribedJar != null) _subscribedJar.SetPouringState(false);
            _subscribedJar = null;
        }
        private void OnValidate()
        {
            if (flowMlPerSecond <= 0f || float.IsNaN(flowMlPerSecond) || float.IsInfinity(flowMlPerSecond)) flowMlPerSecond = 150f;
        }
    }
}
