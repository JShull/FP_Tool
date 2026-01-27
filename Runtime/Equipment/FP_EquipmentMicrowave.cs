namespace FuzzPhyte.Tools
{
    using FuzzPhyte.Utility;
    using UnityEngine;

    public class FP_EquipmentMicrowave : FP_EquipmentTickableBase
    {
        private TimerData _activeTimer;
        
        protected override void OnTick(float dt)
        {
            if (_activeTimer == null || FP_Timer.CCTimer == null)
                return;

            // If the timer is no longer scheduled, clear out state.
            if (!FP_Timer.CCTimer.IsScheduled(_activeTimer))
            {
                _activeTimer = null;

                if (_status.TimerRemainingSec != 0f)
                {
                    _status.TimerRemainingSec = 0f;
                    Emit();
                }
                return;
            }

            // Pull remaining time from central timer and emit only if it changed.
            float remaining = FP_Timer.CCTimer.GetRemainingSeconds(_activeTimer);
            if (!Mathf.Approximately(_status.TimerRemainingSec, remaining))
            {
                _status.TimerRemainingSec = remaining;
                Emit();
            }
        }

        protected override void HandleCommand(EquipmentCommand cmd)
        {
            switch (cmd.Type)
            {
                case EquipmentCommandType.SetPowerOn:
                    SetPower(EquipmentPowerState.On);
                    break;

                case EquipmentCommandType.SetPowerOff:
                    CancelTimerInternal();
                    SetPower(EquipmentPowerState.Off);
                    break;

                case EquipmentCommandType.StartTimerSeconds:
                    if (data != null && !data.SupportsTimer) return;
                    StartTimer(cmd.FValue);
                    break;

                case EquipmentCommandType.CancelTimer:
                    CancelTimerInternal();
                    if (_status.Power == EquipmentPowerState.OnWithTimer)
                        SetPower(EquipmentPowerState.On);
                    break;

                case EquipmentCommandType.Break:
                    CancelTimerInternal();
                    _status.Condition = EquipmentConditionState.Broken;
                    SetPower(EquipmentPowerState.Off);
                    Emit();
                    break;

                case EquipmentCommandType.Repair:
                    _status.Condition = EquipmentConditionState.OK;
                    Emit();
                    break;
            }
        }

        private void StartTimer(float seconds)
        {
            CancelTimerInternal();

            seconds = Mathf.Max(0f, seconds);
            if (seconds <= 0f)
            {
                _status.TimerRemainingSec = 0f;
                SetPower(EquipmentPowerState.Off);
                return;
            }

            if (SetPower(EquipmentPowerState.OnWithTimer))
            {
                _status.TimerRemainingSec = seconds;
                Emit();

                _activeTimer = FP_Timer.CCTimer.StartTimer(seconds, OnTimerFinished);
            }
        }

        private void OnTimerFinished()
        {
            _activeTimer = null;
            _status.TimerRemainingSec = 0f;
            SetPower(EquipmentPowerState.Off);
        }

        private void CancelTimerInternal()
        {
            if (_activeTimer != null && FP_Timer.CCTimer != null)
                FP_Timer.CCTimer.CancelTimer(_activeTimer);

            _activeTimer = null;

            if (_status.TimerRemainingSec != 0f)
            {
                _status.TimerRemainingSec = 0f;
                Emit();
            }
        }
    }
}
