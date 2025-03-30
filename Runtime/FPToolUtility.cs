using System;
using FuzzPhyte.Utility;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace FuzzPhyte.Tools
{
    [Serializable]
    public enum FPToolState
    {
        Deactivated = 0,
        Activated = 1,
        Starting = 2,
        ActiveUse = 3,
        Ending = 9
    }

    public interface IFPToolListener<T> where T : FP_Data
    {
        void OnToolActivated(FP_Tool<T> tool);
        void OnToolStarting(FP_Tool<T> tool);
        void OnToolInActiveUse(FP_Tool<T> tool);
        void OnToolEnding(FP_Tool<T> tool);
        void OnToolDeactivated(FP_Tool<T> tool);
    }

    public static class FPToolUtility
    {
    
    }
    
}
