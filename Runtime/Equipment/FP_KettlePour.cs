// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using UnityEngine;

    /// <summary>Gates the existing tip pourer by kettle supply and drains its stored volume.</summary>
    [RequireComponent(typeof(FP_TipParticlePour)), DisallowMultipleComponent]
    public class FP_KettlePour : MonoBehaviour
    {
        [SerializeField] private FP_EquipmentKettle kettle;
        [SerializeField, Min(0.01f)] private float flowMlPerSecond = 150f;
        [SerializeField] private bool autoAdvance = true;
        private FP_TipParticlePour _pour;
        private FP_EquipmentKettle _subscribedKettle;

        private void Reset() => kettle = GetComponentInParent<FP_EquipmentKettle>();
        private void OnEnable()
        {
            _pour = GetComponent<FP_TipParticlePour>();
            _subscribedKettle = kettle;
            _pour.OnPourStarted.AddListener(Started);
            _pour.OnPourStopped.AddListener(Stopped);
            if (_subscribedKettle != null) _subscribedKettle.OnStateChanged.AddListener(RefreshGate);
            RefreshGate(_subscribedKettle);
            if (_pour.IsPouring) Started();
        }

        private void Update() { if (autoAdvance) AdvancePour(Time.deltaTime); }

        /// <summary>Scaled-time flow. Turn off Auto Advance before calling from a custom driver.</summary>
        public void AdvancePour(float dt)
        {
            if (!isActiveAndEnabled || _pour == null) return;
            RefreshGate(_subscribedKettle);
            if (_subscribedKettle == null || !_pour.IsPouring || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            double amount = (double)flowMlPerSecond * dt;
            _subscribedKettle.RemoveWaterMl((float)System.Math.Min(amount, _subscribedKettle.WaterMl));
            RefreshGate(_subscribedKettle);
        }

        private void RefreshGate(FP_EquipmentKettle source)
        {
            if (_pour != null) _pour.SetPouringAllowed(isActiveAndEnabled && source != null && source.CanPour);
        }
        private void Started() { if (_subscribedKettle != null) _subscribedKettle.SetPouringState(true); }
        private void Stopped() { if (_subscribedKettle != null) _subscribedKettle.SetPouringState(false); }
        private void OnDisable()
        {
            if (_subscribedKettle != null) _subscribedKettle.OnStateChanged.RemoveListener(RefreshGate);
            if (_pour != null)
            {
                _pour.OnPourStarted.RemoveListener(Started);
                _pour.OnPourStopped.RemoveListener(Stopped);
                _pour.SetPouringAllowed(false);
            }
            if (_subscribedKettle != null)
            {
                _subscribedKettle.SetPouringState(false);
            }
            _subscribedKettle = null;
        }
        private void OnValidate()
        {
            if (flowMlPerSecond <= 0f || float.IsNaN(flowMlPerSecond) || float.IsInfinity(flowMlPerSecond)) flowMlPerSecond = 150f;
        }
    }
}
