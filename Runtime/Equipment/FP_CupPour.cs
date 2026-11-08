// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using UnityEngine;

    /// <summary>Reuses the existing tip controller with a finite cup supply.</summary>
    [RequireComponent(typeof(FP_TipParticlePour)), DisallowMultipleComponent]
    public class FP_CupPour : MonoBehaviour
    {
        [SerializeField] private FP_CupFill cup;
        [SerializeField, Min(0.01f)] private float flowMlPerSecond = 150f;
        [SerializeField] private bool autoAdvance = true;
        private FP_TipParticlePour _pour;
        private FP_CupFill _subscribedCup;

        private void Reset() => cup = GetComponentInParent<FP_CupFill>();
        private void OnEnable()
        {
            _pour = GetComponent<FP_TipParticlePour>();
            _subscribedCup = cup;
            _pour.OnPourStarted.AddListener(Started);
            _pour.OnPourStopped.AddListener(Stopped);
            if (_subscribedCup != null) _subscribedCup.OnStateChanged.AddListener(RefreshGate);
            RefreshGate(_subscribedCup);
            if (_pour.IsPouring) Started();
        }
        private void Update() { if (autoAdvance) AdvancePour(Time.deltaTime); }
        public void AdvancePour(float dt)
        {
            if (!isActiveAndEnabled || _pour == null) return;
            RefreshGate(_subscribedCup);
            if (_subscribedCup == null || !_pour.IsPouring || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            double amount = (double)flowMlPerSecond * dt;
            _subscribedCup.RemoveLiquidMl((float)System.Math.Min(amount, _subscribedCup.VolumeMl));
            RefreshGate(_subscribedCup);
        }
        private void RefreshGate(FP_CupFill source)
        {
            if (_pour != null) _pour.SetPouringAllowed(isActiveAndEnabled && source != null && source.CanPour);
            if (source != null && _pour != null) source.SetPouringState(isActiveAndEnabled && _pour.IsPouring && source.CanPour);
        }
        private void Started() { if (_subscribedCup != null) _subscribedCup.SetPouringState(true); }
        private void Stopped() { if (_subscribedCup != null) _subscribedCup.SetPouringState(false); }
        private void OnDisable()
        {
            if (_subscribedCup != null) _subscribedCup.OnStateChanged.RemoveListener(RefreshGate);
            if (_pour != null)
            {
                _pour.OnPourStarted.RemoveListener(Started);
                _pour.OnPourStopped.RemoveListener(Stopped);
                _pour.SetPouringAllowed(false);
            }
            if (_subscribedCup != null) _subscribedCup.SetPouringState(false);
            _subscribedCup = null;
        }
        private void OnValidate()
        {
            if (flowMlPerSecond <= 0f || float.IsNaN(flowMlPerSecond) || float.IsInfinity(flowMlPerSecond)) flowMlPerSecond = 150f;
        }
    }
}
