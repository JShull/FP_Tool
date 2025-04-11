namespace FuzzPhyte.Tools.Samples
{
    using FuzzPhyte.Utility;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using FuzzPhyte.Tools;
    using FuzzPhyte.Tools.Connections;
    using System.Collections.Generic;

    [RequireComponent(typeof(RectTransform))]
    public class FP_DetachTool :FP_Tool<FP_DetachToolData>, IFPUIEventListener<FP_Tool<FP_DetachToolData>>
    {
        protected ConnectionPointUnity selectedItemCPU;
        protected ConnectionPointUnity selectedOtherItemCPU;
        public FP_AttachTool TheAttachTool;
        protected Vector3 worldSelectedLocation;
        [SerializeField] protected RectTransform measurementParentSpace;
        public Transform ForwardPlaneLocation;
        public UnityEvent OnDetachToolActivated;
        public UnityEvent OnDetachToolDown;
        public UnityEvent OnDetachToolDownError;
        public UnityEvent OnDetachToolUp;
        public void Start()
        {
            if (measurementParentSpace==null)
            {
                measurementParentSpace = GetComponent<RectTransform>();
            }
            if (toolData == null)
            {
                Debug.LogError($"missing tool data");
                return;
            }
        }
        public override void DeactivateToolFromUI()
        {
            ForceDeactivateTool();
        }
        public override bool DeactivateTool()
        {
            if (base.DeactivateTool())
            {
                if (!LoopTool)
                {
                    //we do want to turn off ToolIsCurrent
                    ToolIsCurrent = false;
                }
                return true;
            }
            Debug.LogWarning($"Didn't deactivate the tool?");
            return false;
        }
        /// <summary>
        /// This just sets our state up for being ready to use the tool
        /// </summary>
        public override bool ActivateTool()
        {
            if (base.ActivateTool())
            {
                ToolIsCurrent = true;
                OnDetachToolActivated.Invoke();
                return true;
            }
            Debug.LogWarning($"Didn't activate the tool?");
            return false;
        }
        public override bool ForceDeactivateTool()
        {
            if (base.ForceDeactivateTool())
            {
                ToolIsCurrent = false;
                return true;
            }
            return false;
        }
        public void OnUIEvent(FP_UIEventData<FP_Tool<FP_DetachToolData>> eventData)
        {
            //Debug.LogWarning($"OnUIEvent was processed {eventData.EventType} {eventData.AdditionalData} {this} {ToolIsCurrent}");
            if (!ToolIsCurrent)
            {
                return;
            }
            if (eventData.TargetObject == this.gameObject)
            {
                //it's me
                //Debug.LogWarning($"Event Data Target Object is me {eventData.TargetObject} {this}");
                switch (eventData.EventType)
                {
                    case FP_UIEventType.PointerDown:
                        PointerDown(eventData.UnityPointerEventData);
                        break;
                    case FP_UIEventType.Drag:
                        PointerDrag(eventData.UnityPointerEventData);
                        break;
                    case FP_UIEventType.PointerUp:
                        PointerUp(eventData.UnityPointerEventData);
                        break;
                }
            }
            else
            {
                //it's not me
                Debug.LogWarning($"Event Data Target Object is NOT me {eventData.TargetObject} {this}");
            }
        }
        public void PointerDown(PointerEventData eventData)
        {
            Debug.LogWarning($"Pointer down!");
            if (!ToolIsCurrent)
            {
                return;
            }
            if (RectTransformUtility.RectangleContainsScreenPoint(measurementParentSpace, eventData.position, ToolCamera))
            {
                if (StartTool())
                {
                    Plane fPlane = new Plane(ForwardPlaneLocation.transform.forward * -1, ForwardPlaneLocation.position);
                    FP_UtilityData.DrawLinePlane(ForwardPlaneLocation.position, ForwardPlaneLocation.forward * -1f, Color.green, 2, 10);
                    var PointData = FP_UtilityData.GetMouseWorldPositionOnPlane(ToolCamera, eventData.position, fPlane);
                    RaycastHit potentialHit;
                    var direction = (PointData.Item2 - ToolCamera.transform.position).normalized;
                    Ray ray = new Ray(ToolCamera.transform.position, direction);
                    Debug.LogWarning($"Ray: {ray.origin} | {ray.direction}");
                    Debug.DrawRay(ray.origin, ray.direction * toolData.RaycastMax, FP_UtilityData.ReturnColorByStatus(SequenceStatus.Unlocked), 10f);
                    Debug.DrawRay(PointData.Item2, Vector3.up, Color.red, 9f);
                    Physics.Raycast(ray, out potentialHit, toolData.RaycastMax);

                    if (potentialHit.collider != null) 
                    {
                        Debug.LogWarning($"DETACH TOOL | Potential Hit: {potentialHit.collider.gameObject.name}");
                        var collideItem = potentialHit.collider.gameObject.GetComponent<FP_CollideItem>();
                        if (collideItem != null)
                        {
                            Debug.LogWarning($"DETACH TOOL | Hit a CollideItem | Potential Hit: {collideItem.gameObject.name}");
                            var cpu = collideItem.GetComponent<ConnectionPointUnity>();
                            if (cpu != null)
                            {
                                Debug.LogWarning($"DETACH TOOL | Found a CPU | Potential Hit: {cpu.gameObject.name}, is it aligned? | {cpu.ConnectionPointStatusPt}");
                                //now lets see what our state is in
                                if (cpu.ConnectionPointStatusPt == ConnectionPointStatus.Connected)
                                {
                                    //now see if our cpu has a buddy
                                    if (cpu.OtherConnection != null)
                                    {
                                        selectedOtherItemCPU = cpu.OtherConnection;
                                        selectedItemCPU = cpu;
                                        worldSelectedLocation = potentialHit.point;
                                        Debug.LogWarning($"We are able to detach! | {selectedItemCPU.gameObject.name} is connected with {selectedOtherItemCPU.gameObject.name}");
                                        OnDetachToolDown.Invoke();
                                        if (UseTool())
                                        {
                                            //just need to force this because of how drag/works
                                        }
                                        //now wait until we mouse up
                                        return;
                                    }
                                    else
                                    { 
                                    }
                                }
                                else
                                {
                                }
                            }
                        }
                    }
                    //force it
                    if (UseTool())
                    {

                    }
                    OnDetachToolDownError.Invoke();
                }
            }
        }
        public void PointerDrag(PointerEventData eventData)
        {
            //do nothing
            if (!ToolIsCurrent)
            {
                return;
            }

            if (UseTool())
            {
                
            }
        }
        public void PointerUp(PointerEventData eventData)
        {
            Debug.Log($"Detach tool: On Pointer up");
            if (!ToolIsCurrent)
            {
                return;
            }
            //are we in the position?
            if (RectTransformUtility.RectangleContainsScreenPoint(measurementParentSpace, eventData.position, ToolCamera))
            {
                Debug.LogWarning($"In Rect Space");
                if (EndTool())
                {
                    if(selectedItemCPU!=null && selectedOtherItemCPU != null)
                    {
                        Debug.LogWarning($"Go for detach!?");
                        var UnlockSuccess = selectedItemCPU.TheConnectionPart.UIAttemptUnlockItem(selectedOtherItemCPU);
                        var success = TheAttachTool.RequestDetachDataSync(selectedItemCPU,selectedOtherItemCPU);
                        if(success)
                        {
                            Debug.LogWarning($"Detach was a success and it's been removed");
                            OnDetachToolUp.Invoke();
                        }
                        if(UnlockSuccess)
                        {
                            Debug.LogWarning($"Looks like this removal also unlocked the part!");
                        }
                    }
                    //reset
                    selectedItemCPU = null;
                    selectedOtherItemCPU = null;
                    worldSelectedLocation = Vector3.zero;
                    DeactivateTool();
                }
            }
            else
            {
                //reset
                selectedItemCPU = null;
                selectedOtherItemCPU = null;
                worldSelectedLocation = Vector3.zero;
                DeactivateTool();
            }
        }
        public void ResetVisuals()
        {
            
        }
    }
}
