namespace FuzzPhyte.Tools
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using FuzzPhyte.Utility;

    #region Interfaces
    /// <summary>
    /// Generic Tool Listener setup with Functions tied to state of the tool
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFPToolListener<T> where T : FP_Data
    {
        void OnToolActivated(FP_Tool<T> tool);
        void OnToolStarting(FP_Tool<T> tool);
        void OnToolInActiveUse(FP_Tool<T> tool);
        void OnToolEnding(FP_Tool<T> tool);
        void OnToolDeactivated(FP_Tool<T> tool);
    }
    /// <summary>
    /// Two Point Interface making sure we utilize the functions tied to a generic 'line' with 2 points
    /// </summary>
    public interface IFPTwoPoint
    {
        void DropFirstPoint(Vector3 pos);
        void DropSecondPoint(Vector3 pos);
    }
    #endregion
    #region Equipment Related

    public enum EquipmentPowerState
    {
        NA,
        Off,
        On,
        OnWithTimer,
    }
    public enum EquipmentConditionState
    {
        NA,
        OK,
        Broken,
        NeedsMaintenance,
    }
    public enum EquipmentFillState
    {
        NA,
        Empty,
        ContainsItem,
        ContainsLiquid,
        MixedContents
    }
    public enum EquipmentCommandType
    {
        TogglePower,
        SetPowerOn,
        SetPowerOff,
        SetHeatLevel01,
        SetHeatLevel02,
        SetHeatLevel03,
        StartTimerSeconds,
        CancelTimer,
        SetFillLevel01,
        SetFillLevel02,
        SetFillLevel03,
        InsertItem,
        RemoveItem,
        Break,
        Repair,
        PauseTimer,
        ResumeTimer,
        InterfaceActions,
        OpenDoor,
        CloseDoor
    }
    [System.Serializable]
    public struct EquipmentCommand
    {
        public EquipmentCommandType Type;
        public float FValue;
        public string SValue;
        [Tooltip("Just so we can track if the command is active or not")]
        public bool ActiveState;
    }
    public struct EquipmentEvent
    {
        public string EquipmentId;
        public EquipmentStatus Status;
    }
    [System.Serializable]
    public struct EquipmentStatus
    {
        public EquipmentPowerState Power;
        public EquipmentConditionState Condition;

        [Range(0,1)]
        public float NormalizedLevel;
        public float TimerRemainingSec;
        public EquipmentFillState Fill;
        public List<string> ContainedItemIds;
    }
    #endregion
    public static class FPToolUtility
    {
    
    }
    
}
