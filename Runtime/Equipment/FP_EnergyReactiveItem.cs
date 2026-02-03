namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using UnityEngine.Events;
    public class FP_EnergyReactiveItem : MonoBehaviour,IFPContainedItem
    {
        [SerializeField] private string itemId;
        public string ItemId => itemId;
        [SerializeField] protected float energyAdded = 0f;
        [SerializeField] protected float energyTickFrame = 0.016f;
        [SerializeField] protected float maxEnergyAdded = 100f;
        [SerializeField] protected float foodStateChangeThreshold = 50f;
        public bool ThresholdReached => energyAdded >= foodStateChangeThreshold;
        [SerializeField] protected bool activatedThresholdEvent = false;
        [SerializeField] protected bool activatedMaxThresholdEvent = false;
        public UnityEvent ThresholdReachedEvent;
        public UnityEvent MaxThresholdReachedEvent;

        public virtual void Start()
        {
            //always make sure state change threshold is less than max energy
            foodStateChangeThreshold = Mathf.Min(foodStateChangeThreshold, maxEnergyAdded);
        }
        public void OnEnteredEquipment(FP_EquipmentBase equipment)
        {
            Debug.Log($"{itemId} entered {equipment.name}");
        }

        public void OnExitedEquipment(FP_EquipmentBase equipment)
        {
            Debug.Log($"{itemId} exited {equipment.name}");
        }

        public virtual void OnEquipmentStateChanged(FP_EquipmentBase equipment, EquipmentStatus status)
        {
            if (status.Power == EquipmentPowerState.OnWithTimer || status.Power == EquipmentPowerState.On)
            {
                energyAdded += energyTickFrame * status.EmitPowerUnit;
            }
        }
        public virtual void OnEquipmentTick(FP_EquipmentBase equipment, EquipmentStatus status, float dt)
        {
            if (status.Power != EquipmentPowerState.OnWithTimer && status.Power != EquipmentPowerState.On) return;
            float energyThisTick = dt * status.EmitPowerUnit;
            energyAdded += energyThisTick;
            energyAdded = Mathf.Min(energyAdded, maxEnergyAdded);
            CheckThresholds();
        }
        protected virtual void CheckThresholds()
        {
            if (!activatedThresholdEvent && energyAdded >= foodStateChangeThreshold)
            {
                ThresholdReachedEvent?.Invoke();
                activatedThresholdEvent = true;
            }

            if (!activatedMaxThresholdEvent && energyAdded >= maxEnergyAdded)
            {
                MaxThresholdReachedEvent?.Invoke();
                activatedMaxThresholdEvent = true;
            }
        }

    }
}
