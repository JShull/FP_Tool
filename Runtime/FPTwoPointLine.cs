using FuzzPhyte.Utility;
using UnityEngine;

namespace FuzzPhyte.Tools
{
    public abstract class FPTwoPointLine<T> : MonoBehaviour, IFPToolListener<T>, IFPTwoPoint where T : FP_Data 
    {
        protected FP_Tool<T> myTool;
        public abstract void Setup(FP_Tool<T> tool);
        
        public abstract void UpdateText(string text);
        public virtual void OnDestroy()
        {
            if (myTool != null)
            {
                myTool.OnActivated -= OnToolActivated;
                myTool.OnStarting -= OnToolStarting;
                myTool.OnActiveUse -= OnToolInActiveUse;
                myTool.OnEnding -= OnToolEnding;
                myTool.OnDeactivated -= OnToolDeactivated;
            }
        }
        #region Interface Requirements
        /// <summary>
        /// We first activate/establish this tool
        /// </summary>
        /// <param name="tool"></param>
        public virtual void OnToolActivated(FP_Tool<T> tool)
        { 
        }
        /// <summary>
        /// When we first start the tool, e.g. ON Mouse DOWN
        /// </summary>
        /// <param name="tool"></param>
        public virtual void OnToolStarting(FP_Tool<T> tool)
        {
        }
        /// <summary>
        /// When the tool is in an activate state of running, e.g. Mouse Down
        /// </summary>
        /// <param name="tool"></param>
        public virtual void OnToolInActiveUse(FP_Tool<T> tool)
        {
        }
        /// <summary>
        /// When the tool use stops, e.g. ON Mouse UP
        /// </summary>
        /// <param name="tool"></param>
        public virtual void OnToolEnding(FP_Tool<T> tool)
        {
        }
        /// <summary>
        /// When we deactivate it, end end state, we kill it off
        /// </summary>
        /// <param name="tool"></param>
        public virtual void OnToolDeactivated(FP_Tool<T> tool)
        {
        }

        public virtual void DropFirstPoint(Vector3 pos)
        {
        }

        public virtual void DropSecondPoint(Vector3 pos)
        {
        }

        #endregion
    }
}
