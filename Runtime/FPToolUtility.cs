namespace FuzzPhyte.Tools
{
    using System;
    using UnityEngine;
    using FuzzPhyte.Utility;

    [Serializable]
    public enum FPToolState
    {
        Deactivated = 0,
        Activated = 1,
        Starting = 2,
        ActiveUse = 3,
        Ending = 9
    }
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

    public static class FPToolUtility
    {
    
    }
    
}
