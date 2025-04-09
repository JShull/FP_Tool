namespace FuzzPhyte.Tools.Connections
{
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using FuzzPhyte.Utility;
    using UnityEngine.EventSystems;
    using UnityEngine.Events;
    public class FP_PanMove : FP_Tool<PartData>, IFPUIEventListener<FP_Tool<PartData>>
    {
        public RectTransform movePanRectParentPanel;
        [Tooltip("Max raycast distance for when we fire")]
        public float RaycastMaxDistance = 15;
        [SerializeField]
        [Tooltip("This will cache our current selected item")]
        protected GameObject selectedItem;
        [Space]
        [Header("Forward Plane Details")]
        public Transform ForwardPlaneLocation;
        [Tooltip("If true we will only adjust by the grid size value")]
        public bool UseOffsetGridSnapping = true;
        [Tooltip("Amount to increment the offset by")]
        [Range(0.01f,1f)]
        public float OffsetGridSize = 0.025f;
        [Tooltip("This will double in size, as we have to go in both the forward/backward direction")]
        public float BoundsMaxOffsetFromStart = 1f;
        [Tooltip("The box we will build around our plane starting position")]
        protected Bounds planeBounds;
        protected Vector3 startingPlaneLocation;
        protected Vector3 cameraForwardDirection;
       
        [SerializeField]
        private FP_MoveRotateItem selectedItemDetails;
        private IFPUIEventListener<FP_Tool<PartData>> selectedItemInterface;
        #region Unity Events Associated with Tools
        public UnityEvent OnToolActivatedUnityEvent;
        public UnityEvent OnToolSelectedItemUnityEvent;
        public UnityEvent OnToolDropItemUnityEvent;
        [Space]
        public UnityEvent PlaneMovedForwardEvent;
        public UnityEvent PlaneMovedBackwardEvent;
        public UnityEvent PlaneFailedToMoveEvent;
        #endregion
        [Tooltip("Cached Values")]
        [SerializeField]
        private bool isMoving;
        protected Vector3 originalLocationOnActive;
        protected Vector3 originalHitPoint;
        protected Vector3 localVectorOffsetFromPlanePoint;
        
        public void Start()
        {
            cameraForwardDirection = ToolCamera.transform.forward;  
            startingPlaneLocation = ForwardPlaneLocation.position;
            planeBounds = new Bounds(startingPlaneLocation, BoundsMaxOffsetFromStart * 2 * Vector3.one);
        }
        #region testing
        public void TestingDebug(string message)
        {
            Debug.LogWarning($"Testing Debug: {message}");
        }
        [ContextMenu("Forward Plane One Snap Unit")]
        public void ForwardPlaneOneSnapUnit()
        {
            UseOffsetGridSnapping=true;
            UpdateForwardPlaneLocation(1f);
        }
        [ContextMenu("Forward Plane One Snap Unit Back")]
        public void ForwardPlaneOneSnapUnitBack()
        {
            UseOffsetGridSnapping = true;
            UpdateForwardPlaneLocation(-1f);
        }
        #endregion
        /// <summary>
        /// tweak our z offset from another user input like forward/backward key
        /// </summary>
        /// <param name="passedZOffset"></param>
        public void UpdateForwardPlaneLocation(float passedZOffset)
        {
            Vector3 newPosition = ForwardPlaneLocation.position;
            bool isForward = false;
            isForward = passedZOffset > 0 ? true : false;
           
            if (UseOffsetGridSnapping)
            {
                if (passedZOffset > 0)
                {
                    newPosition = ForwardPlaneLocation.position + (cameraForwardDirection * OffsetGridSize);
                }
                else
                {
                    newPosition = ForwardPlaneLocation.position - (cameraForwardDirection * OffsetGridSize);
                }
            }else
            {
                newPosition=ForwardPlaneLocation.position + (cameraForwardDirection * passedZOffset);
            }
            //check if we are in bounds and fire off events
            PlaneWithinBounds(newPosition,isForward);
        }
        /// <summary>
        /// Will return true if we are within the bounds of the plane, and fire off events
        /// </summary>
        /// <param name="positionToCheck"></param>
        /// <param name="forward"></param>
        /// <returns></returns>
        protected bool PlaneWithinBounds(Vector3 positionToCheck, bool forward)
        {
            //use bounds to check inside unit sphere
            if (planeBounds.Contains(positionToCheck))
            {
                ForwardPlaneLocation.position = positionToCheck;
                if(forward)
                {
                    PlaneMovedForwardEvent.Invoke();
                }
                else
                {
                    PlaneMovedBackwardEvent.Invoke();
                }
                return true;
            }
            PlaneFailedToMoveEvent.Invoke();
            return false;
        }
        #region Interface Requirements
        public void OnUIEvent(FP_UIEventData<FP_Tool<PartData>> eventData)
        {
            switch (eventData.EventType)
            {
                case FP_UIEventType.PointerDown:
                    PointerDown(eventData.UnityPointerEventData);
                    break;
                case FP_UIEventType.PointerUp:
                    PointerUp(eventData.UnityPointerEventData);
                    break;
                case FP_UIEventType.Drag:
                    PointerDrag(eventData.UnityPointerEventData);
                    break;
            }
        }
        public void PointerDown(PointerEventData eventData)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(movePanRectParentPanel, eventData.position,ToolCamera))
            {
                if(StartTool())
                {
                    Debug.LogWarning($"Pointer DOWN Coordinates: {eventData.position}");
                    Plane fPlane = new Plane(ForwardPlaneLocation.transform.forward*-1, ForwardPlaneLocation.position);
                    FP_UtilityData.DrawLinePlane(ForwardPlaneLocation.position, ForwardPlaneLocation.forward * -1f,Color.green,2,10);
                    var PointData = FP_UtilityData.GetMouseWorldPositionOnPlane(ToolCamera,eventData.position,fPlane);
                    RaycastHit potentialHit;
                    var direction = (PointData.Item2 - ToolCamera.transform.position).normalized;
                    Ray ray = new Ray(ToolCamera.transform.position, direction);
                    Debug.LogWarning($"Ray: {ray.origin} | {ray.direction}");
                    Debug.DrawRay(ray.origin, ray.direction * RaycastMaxDistance, FP_UtilityData.ReturnColorByStatus(SequenceStatus.Unlocked), 10f);
                    Debug.DrawRay(PointData.Item2,Vector3.up,Color.red,9f);
                    Physics.Raycast(ray, out potentialHit, RaycastMaxDistance);
                    if(potentialHit.collider!=null)
                    {
                        Debug.Log($"Hit: {potentialHit.collider.name}");
                        originalHitPoint = potentialHit.point;
                        var FPMRItem = potentialHit.collider.gameObject.GetComponent<FP_CollideItem>();
                        if(FPMRItem!=null)
                        {
                            selectedItemDetails=FPMRItem.MoveRotateItem;
                            selectedItemInterface = selectedItemDetails.gameObject.GetComponent<IFPUIEventListener<FP_Tool<PartData>>>();
                            localVectorOffsetFromPlanePoint = selectedItemDetails.transform.position - PointData.Item2;
                            Debug.DrawLine(PointData.Item2, PointData.Item2 + localVectorOffsetFromPlanePoint, Color.magenta, 10f);
                            if (selectedItemInterface != null)
                            {
                                selectedItemInterface.PointerDown(eventData);
                            }
                            var item = selectedItemDetails.gameObject;
                            if(item==null)
                            {
                                return;
                            }
                            if (!item.GetComponent<FP_MoveRotateItem>())
                            {
                                Debug.LogError($"Missing FP_MoveRotateItem on {item.name}");
                                return;
                            }
                            //set up our managers cached data by item and item data
                            selectedItem = item;
                            Debug.LogWarning($"Selected Item: {selectedItem.name}");
                            selectedItemDetails = item.GetComponent<FP_MoveRotateItem>();
                            originalLocationOnActive =item.transform.position;
                            //confirm camera
                            if(ToolCamera==null)
                            {
                                Debug.LogError($"No camera set: using main camera");
                                return;
                                //currentCam = Camera.main;
                            }
                            //fin
                            isMoving = true;
                            selectedItemDetails.MoveStarted();
                            OnToolSelectedItemUnityEvent.Invoke();
                        }
                    }
                }
            }
        }        
        public void PointerDrag(PointerEventData eventData)
        {
            if (!isMoving)
            {
                return;
            }
            if(!ToolIsCurrent)
            {
                return;
            }
            if(UseTool())
            {
                if (selectedItemInterface != null)
                {
                    selectedItemInterface.PointerDrag(eventData);
                }
                //now update our position
                if (selectedItem == null) return;
                
                Plane fPlane = new Plane(ToolCamera.transform.forward, ForwardPlaneLocation.position);
                var PointData = FP_UtilityData.GetMouseWorldPositionOnPlane(ToolCamera, eventData.position, fPlane);
                var direction = (PointData.Item2 - ToolCamera.transform.position).normalized;
                Ray rayMouse = new Ray(ToolCamera.transform.position, direction);
                
                Debug.DrawRay(rayMouse.origin, rayMouse.direction * RaycastMaxDistance, FP_UtilityData.ReturnColorByStatus(SequenceStatus.Active), 5f);
                //now the offset
                Vector3 returnLocation = PointData.Item2 + localVectorOffsetFromPlanePoint;
                selectedItem.transform.position = returnLocation;
            }
        }
        public void PointerUp(PointerEventData eventData)
        {
            //throw new NotImplementedException();
            Debug.Log($"On Pointer up");
            if (!ToolIsCurrent)
            {
                return;
            }
            if (EndTool())
            {
                if (selectedItemInterface != null)
                {
                    selectedItemInterface.PointerUp(eventData);
                }
                OnToolDropItemUnityEvent.Invoke();
                DeactivateTool();
            }
            else
            {
                DeactivateTool();
            }
            //clean up parameters
            selectedItem = null;
            selectedItemDetails = null;
            isMoving = false;
        }
        #endregion
        #region Base Overrides
        /// <summary>
        /// This just sets our state up for being ready to use the tool
        /// </summary>
        public override bool ActivateTool()
        {
            if (base.ActivateTool())
            {
                ToolIsCurrent = true;
                OnToolActivatedUnityEvent.Invoke();
                return true;
            }
            return false;
        }
        public override bool DeactivateTool()
        {
            if (base.DeactivateTool())
            {
                if(!LoopTool)
                {
                    //we do want to turn off ToolIsCurrent
                    ToolIsCurrent = false;
                }
                return true;
            }
            return false;
        }
        /// <summary>
        /// Ideally called from a game manager / ui to really turn off the system
        /// this forces ToolIsCurrent as well as deactivating the tool
        /// </summary>
        /// <returns></returns>
        public override bool ForceDeactivateTool()
        {
            if(base.ForceDeactivateTool())
            {
                ToolIsCurrent = false;
                selectedItem = null;
                selectedItemDetails = null;
                isMoving = false;
                return true;
            }
            return false;
        }
        #endregion
        /// <summary>
        /// Some additional UI reference to deactivate something if we needed it
        /// </summary>
        public void DeactivateToolFromUI()
        {
            ForceDeactivateTool();
        }
    }
}
