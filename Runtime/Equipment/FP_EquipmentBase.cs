namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using FuzzPhyte.Utility;

    public abstract class FP_EquipmentBase: MonoBehaviour,IFPEventSource<EquipmentEvent>,IFPCommandTarget<EquipmentCommand>
    {
        [SerializeField] protected FP_EquipmentData data;

        public event Action<EquipmentEvent> OnEvent;

        public EquipmentStatus Status => _status;

        protected EquipmentStatus _status;

        // Similar to FP_Tool: CurrentState + allowed transitions
        protected Dictionary<EquipmentPowerState, HashSet<EquipmentPowerState>> powerTransitions;

        protected virtual void Awake()
        {
            BuildDefaultTransitions();
            InitializeStatus();
        }
        protected virtual void BuildDefaultTransitions()
        {
            powerTransitions = new()
            {
                { EquipmentPowerState.Off, new HashSet<EquipmentPowerState> { EquipmentPowerState.On, EquipmentPowerState.OnWithTimer } },
                { EquipmentPowerState.On, new HashSet<EquipmentPowerState> { EquipmentPowerState.Off, EquipmentPowerState.OnWithTimer } },
                { EquipmentPowerState.OnWithTimer, new HashSet<EquipmentPowerState> { EquipmentPowerState.Off, EquipmentPowerState.On } },
            };
        }
        protected virtual void InitializeStatus()
        {
            _status = new EquipmentStatus
            {
                Power = EquipmentPowerState.Off,
                Condition = EquipmentConditionState.OK,
                NormalizedLevel = 0f,
                TimerRemainingSec = 0f,
                Fill = EquipmentFillState.Empty,
                ContainedItemIds= new List<string>()
            };

            Emit();
        }

        public void Execute(EquipmentCommand cmd)
        {
            if (_status.Condition == EquipmentConditionState.Broken && cmd.Type != EquipmentCommandType.Repair)
                return;

            HandleCommand(cmd);
        }

        protected abstract void HandleCommand(EquipmentCommand cmd);

        protected bool CanPowerTransitionTo(EquipmentPowerState target)
        {
            // Mirrors FP_Tool.CanTransitionTo(...) dictionary check :contentReference[oaicite:13]{index=13}
            return powerTransitions.TryGetValue(_status.Power, out var allowed) && allowed.Contains(target);
        }

        protected bool SetPower(EquipmentPowerState next)
        {
            if (!CanPowerTransitionTo(next)) return false;
            _status.Power = next;
            Emit();
            return true;
        }

        protected void Emit()
        {
            OnEvent?.Invoke(new EquipmentEvent
            {
                EquipmentId = data != null ? data.UniqueID : name,
                Status = _status
            });
        }
    }
}
