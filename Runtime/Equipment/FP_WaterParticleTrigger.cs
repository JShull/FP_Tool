// Copyright (c) 2026 John B. Shull
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
// See LICENSE.md, COMMERCIAL-LICENSE.md, and NOTICE.md.

namespace FuzzPhyte.Tools
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>Must live on the ParticleSystem GameObject to receive OnParticleTrigger.</summary>
    [RequireComponent(typeof(ParticleSystem)), DisallowMultipleComponent]
    public class FP_WaterParticleTrigger : MonoBehaviour
    {
        [SerializeField, Min(0.0001f)] private float millilitersPerParticle = 5f;
        [SerializeField] private FP_WaterFillRegion[] fillRegions = new FP_WaterFillRegion[0];
        [Tooltip("Cup input channel. Coffee-maker and kettle targets ignore this classification. Keep fixed while particles are alive.")]
        [SerializeField] private FP_CupLiquidType inputType = FP_CupLiquidType.Type3;
        private ParticleSystem _particles;
        private readonly List<ParticleSystem.Particle> _entered = new List<ParticleSystem.Particle>();
        public int CapturedParticleCount { get; private set; }

        private void OnEnable() => RefreshTriggerRegions();

        [ContextMenu("Refresh Trigger Regions")]
        public void RefreshTriggerRegions()
        {
            if (_particles == null) _particles = GetComponent<ParticleSystem>();
            var trigger = _particles.trigger;
            trigger.enabled = true;
            trigger.enter = ParticleSystemOverlapAction.Callback;
            trigger.inside = ParticleSystemOverlapAction.Ignore;
            trigger.exit = ParticleSystemOverlapAction.Ignore;
            trigger.outside = ParticleSystemOverlapAction.Ignore;
            trigger.colliderQueryMode = ParticleSystemColliderQueryMode.All;
            for (int i = 0; i < trigger.colliderCount; i++) trigger.SetCollider(i, null);
            if (fillRegions == null) return;
            for (int i = 0; i < fillRegions.Length; i++)
                if (fillRegions[i] != null) trigger.SetCollider(i, fillRegions[i].RegionCollider);
        }

        private void OnParticleTrigger()
        {
            if (!isActiveAndEnabled || millilitersPerParticle <= 0f || float.IsNaN(millilitersPerParticle) ||
                float.IsInfinity(millilitersPerParticle)) return;
            int count = _particles.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, _entered, out var colliderData);
            for (int i = 0; i < count; i++)
            {
                var particle = _entered[i];
                if (particle.remainingLifetime <= 0f) continue;
                for (int j = 0; j < colliderData.GetColliderCount(i); j++)
                {
                    var collider = colliderData.GetCollider(i, j);
                    var region = collider != null ? collider.GetComponent<FP_WaterFillRegion>() : null;
                    if (region == null || !region.TryReceiveWater(millilitersPerParticle, inputType)) continue;
                    particle.remainingLifetime = 0f;
                    _entered[i] = particle;
                    CapturedParticleCount++;
                    break;
                }
            }
            _particles.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, _entered);
        }
    }
}
