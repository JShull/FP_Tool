namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using FuzzPhyte.Utility;

    /// <summary>
    /// Base class for equipment that needs periodic ticking via FP_TickSystem.
    /// Equipment remains command/event driven; ticking is opt-in for continuous behavior
    /// (countdowns, heat ramps, fill rates, containment checks, etc.).
    /// </summary>
    public abstract class FP_EquipmentTickableBase : FP_EquipmentBase, IFPTickable
    {
        [Header("Tick Settings")]
        [SerializeField] private int tickGroup = 1;
        [SerializeField] private int tickPriority = 0;

        [Tooltip("If true, registers with FP_TickSystem in OnEnable and unregisters in OnDisable.")]
        [SerializeField] private bool autoRegisterWithTickSystem = true;

        // IFPTickable
        public int TickGroup => tickGroup;
        public int TickPriority => tickPriority;

        protected virtual void OnEnable()
        {
            if (!autoRegisterWithTickSystem) return;

            if (FP_TickSystem.CCTick != null)
                FP_TickSystem.CCTick.Register(this);

            OnTickRegistered();
        }

        protected virtual void OnDisable()
        {
            if (FP_TickSystem.CCTick != null)
                FP_TickSystem.CCTick.Unregister(this);

            OnTickUnregistered();
        }

        /// <summary>
        /// Called by FP_TickSystem. Do not override Tick() unless you need to.
        /// Override OnTick(dt) instead.
        /// </summary>
        public void Tick(float dt)
        {
            OnTick(dt);
        }
        /// <summary>
        /// Override this for per-tick behavior.
        /// dt is the tick step size provided by FP_TickSystem (frame dt, fixed dt, or interval).
        /// </summary>
        protected virtual void OnTick(float dt) { }

        /// <summary>
        /// IFPTickable hook - also called after Register in OnEnable (safe for local init).
        /// </summary>
        public virtual void OnTickRegistered() { }

        /// <summary>
        /// IFPTickable hook - also called after Unregister in OnDisable (safe for cleanup).
        /// </summary>
        public virtual void OnTickUnregistered() { }

        /// <summary>
        /// Optional runtime control (e.g., debug UI) to move this equipment to another group.
        /// Note: will re-register only if currently enabled.
        /// </summary>
        public void SetTickGroup(int newGroup)
        {
            if (tickGroup == newGroup) return;

            bool wasRegistered = autoRegisterWithTickSystem && isActiveAndEnabled && FP_TickSystem.CCTick != null;

            if (wasRegistered)
                FP_TickSystem.CCTick.Unregister(this);

            tickGroup = newGroup;

            if (wasRegistered)
            {
                FP_TickSystem.CCTick.Register(this);
                // set dirty to ensure proper ordering
                FP_TickSystem.CCTick.MarkGroupDirty(tickGroup);
            }
                
        }

        public void SetTickPriority(int newPriority)
        {
            if(tickPriority==newPriority) return;

            tickPriority = newPriority;

            if (FP_TickSystem.CCTick != null)
            {
                FP_TickSystem.CCTick.MarkGroupDirty(tickGroup);
            }
        }
    }
}
