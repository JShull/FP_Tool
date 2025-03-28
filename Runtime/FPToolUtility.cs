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

    public interface IFPToolListener
    {
        void OnToolActivated();
        void OnToolStarting();
        void OnToolInActiveUse();
        void OnToolEnding();
        void OnToolDeactivated();
    }
    public static class FPToolUtility
    {
    
    }
    
}
