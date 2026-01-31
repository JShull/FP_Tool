namespace FuzzPhyte.Tools
{
    using FuzzPhyte.Utility;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    [Serializable] public class MicrowaveEvent : UnityEvent<FP_EquipmentMicrowave> { }


    public class FP_EquipmentMicrowave : FP_EquipmentTickableBase
    {
        private TimerData _activeTimer;
        [SerializeField] private float EPSILON = 0.01f;
        public TimerData ActiveTimer => _activeTimer;
        [SerializeField] private float _pausedRemainingTime = 0f;
        [SerializeField] private bool _isPaused = false;
        [SerializeField] private bool _doorOpen = false;
        [SerializeField] private float _powerPerEmit = 10f;
        [Header("Microwave Events")]
        public MicrowaveEvent OnStarted;
        public MicrowaveEvent OnPaused;
        public MicrowaveEvent OnResumed;
        public MicrowaveEvent OnButtonsPushed;
        public MicrowaveEvent OnCancelled;
        public MicrowaveEvent OnFinished;
        public MicrowaveEvent OnBroken;
        public MicrowaveEvent OnRepaired;
        public MicrowaveEvent OnDoorOpen;
        public MicrowaveEvent OnDoorClose;

        protected override void BuildDefaultTransitions()
        {
            powerTransitions = new()
            {
                { EquipmentPowerState.Off, new HashSet<EquipmentPowerState> { EquipmentPowerState.OnWithTimer } },
                { EquipmentPowerState.OnWithTimer, new HashSet<EquipmentPowerState> { EquipmentPowerState.Off } },
            };
            UpdateEmittedPowerUnit(_powerPerEmit);
        }
        public override void OnTickRegistered()
        {
            base.OnTickRegistered();
            Debug.Log($"{name} registered with the tickSystem");
        }
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
            // Debug.Log($"From the timers mouth: {remaining}");
            if (Mathf.Abs(_status.TimerRemainingSec - remaining) > EPSILON)
            {
                _status.TimerRemainingSec = remaining;
                Emit();
            }
        }

        protected override void HandleCommand(EquipmentCommand cmd)
        {
            switch (cmd.Type)
            {
                case EquipmentCommandType.InterfaceActions:
                    Debug.LogWarning("Microwave interactions...");
                    OnButtonsPushed?.Invoke(this);
                    break;
                case EquipmentCommandType.SetPowerOn:
                    Debug.LogWarning("Microwave requires a timer to run.");
                    //SetPower(EquipmentPowerState.On);
                    break;
                case EquipmentCommandType.SetPowerOff:
                    CancelTimerInternal();
                    SetPower(EquipmentPowerState.Off);
                    break;

                case EquipmentCommandType.StartTimerSeconds:
                    if (data != null && !data.SupportsTimer) return;
                    if (_doorOpen)
                    {
                        return;
                    }
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
                case EquipmentCommandType.PauseTimer:
                    PauseTimerInternal();
                    break;
                case EquipmentCommandType.ResumeTimer:
                    if (!_doorOpen)
                    {
                        ResumeTimerInternal();
                    }
                    break;
                case EquipmentCommandType.OpenDoor:
                    if (!_doorOpen)
                    {
                        Debug.LogWarning("Microwave door opened.");
                        PauseTimerInternal();
                        _doorOpen = true;
                        OnDoorOpen?.Invoke(this);
                    }
                    break;
                case EquipmentCommandType.CloseDoor:
                    if (_doorOpen)
                    {
                        Debug.LogWarning("Microwave door closed.");
                        _doorOpen = false;
                        OnDoorClose?.Invoke(this);
                    }
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
            OnStarted?.Invoke(this);
        }
        private void OnTimerFinished()
        {
            _activeTimer = null;
            _isPaused = false;
            _pausedRemainingTime = 0;
            _status.TimerRemainingSec = 0f;
            SetPower(EquipmentPowerState.Off);
            OnFinished?.Invoke(this);
        }
        protected override void Emit()
        {
            base.Emit();
            foreach(var item in _containedItems)
            {
                item.OnEquipmentStateChanged(this, _status);
            }
        }
        private void PauseTimerInternal()
        {
            if (_activeTimer == null || FP_Timer.CCTimer == null)
                return;

            // Capture remaining time
            _pausedRemainingTime = FP_Timer.CCTimer.GetRemainingSeconds(_activeTimer);

            // Stop timer
            FP_Timer.CCTimer.CancelTimer(_activeTimer);
            _activeTimer = null;

            _isPaused = true;

            // Power semantics: paused but not running
            SetPower(EquipmentPowerState.Off);

            _status.TimerRemainingSec = _pausedRemainingTime;
            Emit();
            OnPaused?.Invoke(this);
        }
        private void ResumeTimerInternal()
        {
            if (!_isPaused || _pausedRemainingTime <= 0f)
                return;

            _isPaused = false;

            // Restart timer with remaining time
            _activeTimer = FP_Timer.CCTimer.StartTimer(
                _pausedRemainingTime,
                OnTimerFinished
            );

            SetPower(EquipmentPowerState.OnWithTimer);

            Emit();
            OnResumed?.Invoke(this);
        }
        private void CancelTimerInternal()
        {
            if (_activeTimer != null && FP_Timer.CCTimer != null)
            {
                FP_Timer.CCTimer.CancelTimer(_activeTimer);
                _activeTimer = null;
            }

            _status.TimerRemainingSec = 0f;
            // reset pause state
            _isPaused = false;
            _pausedRemainingTime = 0f;

            if (_status.Power == EquipmentPowerState.OnWithTimer || _status.Power == EquipmentPowerState.On)
            {
                SetPower(EquipmentPowerState.Off);
            }
            OnCancelled?.Invoke(this);
            Emit();
        }

        protected override void UpdateEmittedPowerUnit(float newValue)
        {
            _status.EmitPowerUnit = newValue;
        }
    }
}
