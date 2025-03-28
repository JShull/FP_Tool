namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using FuzzPhyte.Utility;
    using System;
    public abstract class FP_Tool<T> : MonoBehaviour 
        where T : FP_Data
    {
        [SerializeField]
        protected T toolData;

        public FPToolState CurrentState { get; protected set; } = FPToolState.Deactivated;
        public event Action OnActivated;
        public event Action OnStarting;
        public event Action OnActiveUse;
        public event Action OnEnding;
        public event Action OnDeactivated;

        public virtual void Initialize(T data)
        {
            toolData = data;
        }

        public virtual void ActivateTool()
        {
            SetState(FPToolState.Activated);
        }

        public virtual void StartTool()
        {
            SetState(FPToolState.Starting);
        }

        public virtual void UseTool()
        {
            SetState(FPToolState.ActiveUse);
        }

        public virtual void EndTool()
        {
            SetState(FPToolState.Ending);
        }

        public virtual void DeactivateTool()
        {
            SetState(FPToolState.Deactivated);
        }

        protected virtual void SetState(FPToolState newState)
        {
            CurrentState = newState;
            switch (CurrentState)
            {
                case FPToolState.Activated:
                    OnActivated?.Invoke();
                    break;
                case FPToolState.Starting:
                    OnStarting?.Invoke();
                    break;
                case FPToolState.ActiveUse:
                    OnActiveUse?.Invoke();
                    break;
                case FPToolState.Ending:
                    OnEnding?.Invoke();
                    break;
                case FPToolState.Deactivated:
                    OnDeactivated?.Invoke();
                    break;
            }
        }
    }
}
