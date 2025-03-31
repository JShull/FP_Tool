namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using FuzzPhyte.Utility;
    using System;
    using System.Collections.Generic;
    public abstract class FP_Tool<T> : MonoBehaviour 
        where T : FP_Data
    {
        [SerializeField]
        protected T toolData;
        public Camera ToolCamera;
        public FPToolState CurrentState;
        public event Action<FP_Tool<T>> OnActivated;
        public event Action<FP_Tool<T>> OnStarting;
        public event Action<FP_Tool<T>> OnActiveUse;
        public event Action<FP_Tool<T>> OnEnding;
        public event Action<FP_Tool<T>> OnDeactivated;

        [Tooltip("This is an interrupt flag to let us ignore")]
        public bool ToolIsCurrent;

        public T ToolData => toolData;
        // Dictionary defining allowed transitions
        protected Dictionary<FPToolState, HashSet<FPToolState>> allowedTransitions = new Dictionary<FPToolState, HashSet<FPToolState>>
        {
            { FPToolState.Deactivated, new HashSet<FPToolState> { FPToolState.Activated,    FPToolState.Deactivated } },
            { FPToolState.Activated,   new HashSet<FPToolState> { FPToolState.Starting,     FPToolState.Deactivated } },
            { FPToolState.Starting,    new HashSet<FPToolState> { FPToolState.ActiveUse,    FPToolState.Deactivated } },
            { FPToolState.ActiveUse,   new HashSet<FPToolState> { FPToolState.ActiveUse,    FPToolState.Ending,         FPToolState.Deactivated } },
            { FPToolState.Ending,      new HashSet<FPToolState> { FPToolState.Activated,    FPToolState.Deactivated } },
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
            Debug.Log($"Starting tool {this}");
            return SetState(FPToolState.Starting);
        }

        public virtual bool UseTool()
        {
            Debug.Log($"Using tool {this}");
            return SetState(FPToolState.ActiveUse);
        }

        public virtual bool EndTool()
        {
            return SetState(FPToolState.Ending);
        }

        public virtual bool DeactivateTool()
        {
            return SetState(FPToolState.Deactivated);
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
