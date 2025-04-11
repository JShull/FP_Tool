namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using FuzzPhyte.Utility;
    using System;
    using System.Collections.Generic;
    public abstract class FP_Tool<T> : MonoBehaviour, IFPTool 
        where T : FP_Data
    {
        [SerializeField]
        protected T toolData;
        public Camera ToolCamera;
        public FPToolState CurrentState;
        [Space]
        public bool LoopTool = false;
        public event Action<FP_Tool<T>> OnActivated;
        public event Action<FP_Tool<T>> OnStarting;
        public event Action<FP_Tool<T>> OnActiveUse;
        public event Action<FP_Tool<T>> OnEnding;
        public event Action<FP_Tool<T>> OnDeactivated;
        public event Action<FP_Tool<T>> OnLocked;
        public event Action<FP_Tool<T>> OnUnlocked;

        [Tooltip("This is an interrupt flag to let us ignore")]
        public bool ToolIsCurrent;

        public T ToolData => toolData;
        // Dictionary defining allowed transitions
        protected Dictionary<FPToolState, HashSet<FPToolState>> allowedTransitions = new Dictionary<FPToolState, HashSet<FPToolState>>
        {
            { FPToolState.Unlocked,    new HashSet<FPToolState> { FPToolState.Activated,    FPToolState.Locked,     FPToolState.Deactivated } },
            { FPToolState.Locked,      new HashSet<FPToolState> { FPToolState.Unlocked,     FPToolState.Locked,     FPToolState.Deactivated } },
            { FPToolState.Deactivated, new HashSet<FPToolState> { FPToolState.Activated,    FPToolState.Locked,     FPToolState.Deactivated } },
            { FPToolState.Activated,   new HashSet<FPToolState> { FPToolState.Starting,     FPToolState.Locked,     FPToolState.Deactivated } },
            { FPToolState.Starting,    new HashSet<FPToolState> { FPToolState.ActiveUse,    FPToolState.Locked,     FPToolState.Deactivated } },
            { FPToolState.ActiveUse,   new HashSet<FPToolState> { FPToolState.ActiveUse,    FPToolState.Locked,     FPToolState.Ending,     FPToolState.Deactivated } },
            { FPToolState.Ending,      new HashSet<FPToolState> { FPToolState.Activated,    FPToolState.Locked,     FPToolState.Deactivated } },
        };

        public virtual void Initialize(T data)
        {
            toolData = data;
        }
        public virtual bool ActivateTool()
        {
            Debug.Log($"Activating tool {this}");
            return SetState(FPToolState.Activated);
        }
        public virtual bool StartTool()
        {
            //Debug.Log($"Starting tool {this}");
            return SetState(FPToolState.Starting);
        }
        public virtual bool UseTool()
        {
            //Debug.Log($"Using tool {this}");
            return SetState(FPToolState.ActiveUse);
        }
        public virtual bool EndTool()
        {
            return SetState(FPToolState.Ending);
        }
        public virtual bool LockTool()
        {
            return SetState(FPToolState.Locked);
        }
        public virtual bool UnlockTool() 
        {
            return SetState(FPToolState.Unlocked);
        }
        public virtual bool DeactivateTool()
        {
            bool confirmDeactivation = SetState(FPToolState.Deactivated);
            if(LoopTool)
            {
                if(confirmDeactivation)
                {
                    return SetState(FPToolState.Activated);
                }
            }
            return confirmDeactivation;
        }
        public virtual bool ForceDeactivateTool()
        {
            return SetState(FPToolState.Deactivated);
        }
        public virtual void DeactivateToolFromUI(){}
        public virtual FPToolState ReturnState()
        {
            return CurrentState;
        }

        protected virtual bool SetState(FPToolState newState)
        {
            if (!CanTransitionTo(newState))
            {
                Debug.LogWarning($"Invalid state transition from {CurrentState} to {newState}");
                return false;
            }
            CurrentState = newState;
            switch (CurrentState)
            {
                case FPToolState.Activated:
                    OnActivated?.Invoke(this);
                    return true;
                case FPToolState.Starting:
                    OnStarting?.Invoke(this);
                    return true;
                case FPToolState.ActiveUse:
                    OnActiveUse?.Invoke(this);
                    return true;
                case FPToolState.Ending:
                    OnEnding?.Invoke(this);
                    return true;
                case FPToolState.Deactivated:
                    OnDeactivated?.Invoke(this);
                    return true;
                case FPToolState.Locked:
                    OnLocked?.Invoke(this);
                    return true;
                case FPToolState.Unlocked:
                    OnUnlocked?.Invoke(this);
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Will confirm if the transition is valid based on the current state and the target state.
        /// </summary>
        /// <param name="targetState">Next potential target state?</param>
        /// <returns></returns>
        protected virtual bool CanTransitionTo(FPToolState targetState)
        {
            if (allowedTransitions.TryGetValue(CurrentState, out var allowedNextStates))
            {
                return allowedNextStates.Contains(targetState);
            }
            return false;
        }
    }
}
