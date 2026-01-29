namespace FuzzPhyte.Tools.Samples
{
    using UnityEngine;
    using FuzzPhyte.Tools;
    using FuzzPhyte.Utility;

    public class FP_MicrowaveDebugCommands : MonoBehaviour
    {
        public FP_EquipmentMicrowave Microwave;
        [ContextMenu("Start Button: Power On")]
        public void PowerOn()
        {
            EquipmentCommand powerOn = new EquipmentCommand()
            {
                Type = EquipmentCommandType.SetPowerOn,
                 
            };
            Microwave.Execute(powerOn);
        }
        [ContextMenu("Stop Button: Power Off")]
        public void PowerOff()
        {
            EquipmentCommand powerOff = new EquipmentCommand()
            {
                Type = EquipmentCommandType.SetPowerOff,

            };
            Microwave.Execute(powerOff);
        }
        [ContextMenu("30 Second Button: Start Timer 30 Seconds")]
        public void StartTimer30Seconds()
        {
            EquipmentCommand timerThirty = new EquipmentCommand()
            {
                Type = EquipmentCommandType.StartTimerSeconds,
                FValue = 30,
            };
            Microwave.Execute(timerThirty);
        }
        [ContextMenu("Cancel Timer")]
        public void CancelTimer()
        {
            EquipmentCommand cancelTimer = new EquipmentCommand()
            {
                Type = EquipmentCommandType.CancelTimer
            };
            Microwave.Execute(cancelTimer);
        }
        [ContextMenu("Test Breaking")]
        public void BreakEquipment()
        {
            EquipmentCommand breakEquip = new EquipmentCommand()
            {
                Type = EquipmentCommandType.Break,
            };
            Microwave.Execute(breakEquip);
        }
        [ContextMenu("Test Repairing")]
        public void RepairEquipment()
        {
            EquipmentCommand repairEquip = new EquipmentCommand()
            {
                Type = EquipmentCommandType.Repair
            };
            Microwave.Execute(repairEquip);
        }

        [ContextMenu("Pause Timer")]
        public void PauseTimer()
        {
            EquipmentCommand pauseTimer = new EquipmentCommand()
            {
                Type = EquipmentCommandType.PauseTimer
            };
            Microwave.Execute(pauseTimer);
        }
        [ContextMenu("Resume Timer")]
        public void ResumeTimer()
        {
            EquipmentCommand resumeTimer = new EquipmentCommand()
            {
                Type = EquipmentCommandType.ResumeTimer
            };
            Microwave.Execute(resumeTimer);
        }
        /// <summary>
        /// Just use Late Update at the moment
        /// </summary>
        public void LateUpdate()
        {
            //Debug.Log($"Time.timeScale = {Time.timeScale}, Time.time = {Time.time}");

            if (Microwave.Status.Power == EquipmentPowerState.OnWithTimer || Microwave.Status.Power == EquipmentPowerState.On)
            {
                PrintTimerValue();
            }
        }



        protected void PrintTimerValue()
        {
            float remaining = Microwave.Status.TimerRemainingSec;
            // print to console with a 2 decimal rounding
            Debug.Log($"Microwave Timer Remaining: {remaining:F2} seconds, Condition: {Microwave.Status.Condition}, Power State: {Microwave.Status.Power}");
        }
    }
}
