using FuzzPhyte.Utility;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace FuzzPhyte.Tools
{
    public enum FPToolState
    {
        Deactivated,
        Activated,
        Starting,
        ActiveUse,
        Ending
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
