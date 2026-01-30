namespace FuzzPhyte.Tools.Samples
{
    using UnityEngine;
    using FuzzPhyte.Tools;
    using FuzzPhyte.Utility;

    public class FP_MicrowaveDebugCommands : MonoBehaviour
    {
        public FP_EquipmentMicrowave Microwave;
        [SerializeField] private EquipmentCommand _currentCommand;
        [ContextMenu("Start Button: Timer Based")]
        public void PowerOn()
        {
            _currentCommand.Type = EquipmentCommandType.StartTimerSeconds;
            if(_currentCommand.FValue <= 0)
            {
                _currentCommand.FValue = 10; // default to 10 seconds if no time set
            }
            Microwave.Execute(_currentCommand);
            
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
        [ContextMenu("10 seconds entered")]
        public void ProgramTimer10Microwave()
        {
            _currentCommand = new EquipmentCommand()
            {
                Type = EquipmentCommandType.InterfaceActions,
                FValue = 10,
            };
            Microwave.Execute(_currentCommand);
        }
        [ContextMenu("10 Second Button: Start Timer 10 Seconds")]
        public void StartTimer10Seconds()
        {
            EquipmentCommand timerTen = new EquipmentCommand()
            {
                Type = EquipmentCommandType.StartTimerSeconds,
                FValue = 10,
            };
            Microwave.Execute(timerTen);
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
