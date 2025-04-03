namespace FuzzPhyte.Connections
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.Events;

    //pipe = part

    public class ConnectionToolTrigger : MonoBehaviour
    {
        public ConnectionPointUnity MyPoint;
        [SerializeField]
        protected bool active = true;
        public void SetActiveTrigger(bool status)
        {
            active = status;
        }
        public delegate void ToolTriggerAction(Collider item, ConnectionPointUnity myPoint, ConnectionPointUnity otherPoint);
        public ToolTriggerAction OnPartTriggerEnterAction;
        public ToolTriggerAction OnPartTriggerExitAction;
        public ToolTriggerAction OnPartTriggerStayAction;
        public delegate void TriggerAction(Collider item);
        public TriggerAction OnTriggerEnterAction;
        public TriggerAction OnTriggerExitAction;
        public TriggerAction OnTriggerStayAction;
        public string TagToCompare;
        [Header("Unity Events")]
        public UnityEvent OnTriggerEnterValid;
        public UnityEvent OnTriggerExitValid;
        public UnityEvent OnTriggerStayValid;
        
        
        public void OnTriggerEnter(Collider other)
        {
            if (!active) return;
            //Debug.LogWarning($"Collided with {other.gameObject.name}");
            if (other.gameObject.tag == TagToCompare)
            {
                if (other.GetComponent<ConnectionToolTrigger>() != null)
                {
                    var otherPt = other.GetComponent<ConnectionToolTrigger>().MyPoint;
                    
                    OnPartTriggerEnterAction?.Invoke(other, MyPoint, otherPt);
                }
                else
                {
                    OnTriggerEnterAction?.Invoke(other);
                }


                OnTriggerEnterValid.Invoke();
            }
        }
        public void OnTriggerExit(Collider other)
        {
            if (!active) return;
            if (other.GetComponent<ConnectionToolTrigger>() != null)
            {
                var otherPt = other.GetComponent<ConnectionToolTrigger>().MyPoint;
                OnPartTriggerExitAction?.Invoke(other, MyPoint, otherPt);
            }
            else
            {
                OnTriggerExitAction?.Invoke(other);
            }
            OnTriggerExitValid.Invoke();
        }
        public void OnTriggerStay(Collider other)
        {
            if (!active) return;
            if (other.GetComponent<ConnectionToolTrigger>() != null)
            {
                var otherPt = other.GetComponent<ConnectionToolTrigger>().MyPoint;
                OnPartTriggerStayAction?.Invoke(other, MyPoint, otherPt);
            }
            else
            {
                OnTriggerStayAction?.Invoke(other);
            }
            OnTriggerStayValid.Invoke();
        }
    }
}
