namespace FuzzPhyte.Tools.Connections
{
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
        public LayerMask RaycastLayerMask;
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
        [Header("Cached Parameters")]
        [SerializeField]
        [Tooltip("This will cache our current selected item")]
        protected GameObject selectedItem;
        [SerializeField]
        protected FP_MoveRotateItem selectedItemDetails;
        protected IFPUIEventListener<FP_Tool<PartData>> selectedItemInterface;
        protected bool isMoving;
        [SerializeField]
        [Tooltip("If this is true, we are rotating")]
        protected bool useRotation;

        [SerializeField]
        [Tooltip("Rotation Speed Scalar")]
        [Range(0.1f,5f)]
        protected float rotationScalar = 0.2f;

        protected Vector3 localVectorOffsetFromPlanePoint;

        protected Vector2 mouseDownStartScreenCoordinate;
        [SerializeField]
        protected Vector3 mouseForwardROTStartVector;
        //[SerializeField]
        //protected Vector3 mouseForwardZROTStartVector;
        protected Quaternion selectItemStartRotation;
        protected Vector2 mouseCurrentScreenCoordinate;
        [SerializeField]
        protected Vector3 mouseCurrentROTVector;
        [SerializeField]
        //protected Vector3 mouseCurrentZROTVector;
        [Tooltip("Angle Diff between")]
        protected float rotationAngleSigned;
        //protected float rotationZAngleSigned;
        [SerializeField]
        //protected float startingZSignedRightValue = 0;
        #region Unity Events Associated with Tools
        public UnityEvent OnToolActivatedUnityEvent;
        public UnityEvent OnToolSelectedItemUnityEvent;
        public UnityEvent OnToolDropItemUnityEvent;
        public UnityEvent OnToolDeactivatedUnityEvent;
        [Space]
        public UnityEvent PlaneMovedForwardEvent;
        public UnityEvent PlaneMovedBackwardEvent;
        public UnityEvent PlaneFailedToMoveEvent;
        #endregion
        
        /// <summary>
        /// Setup our parameters and stuff
        /// </summary>
        public void Start()
        {
            cameraForwardDirection = ToolCamera.transform.forward;  
            startingPlaneLocation = ForwardPlaneLocation.position;
            planeBounds = new Bounds(startingPlaneLocation, BoundsMaxOffsetFromStart * 2 * Vector3.one);
            if (ToolCamera == null)
            {
                Debug.LogError($"No camera set: using main camera");
                ToolCamera = Camera.main;
            }
        }
        /// <summary>
        /// Whatever you pass in will double in bounds, half size
        /// </summary>
        /// <param name="newBoundsMaxOffsetFromStart">Halfsize</param>
        public void UpdateBoundsRemote(float newBoundsMaxOffsetFromStart)
        {
            BoundsMaxOffsetFromStart = newBoundsMaxOffsetFromStart;
            planeBounds = new Bounds(startingPlaneLocation, BoundsMaxOffsetFromStart * 2 * Vector3.one);
        }
        #region testing
        public void TestingDebug(string message)
        {
            //Debug.LogWarning($"Testing Debug: {message}");
        }
        [ContextMenu("Forward Plane One Snap Unit")]
        public void ForwardPlaneOneSnapUnit()
        {
            UseOffsetGridSnapping=true;
            UpdateForwardPlaneLocation(1f,UseOffsetGridSnapping);
        }
        [ContextMenu("Forward Plane One Snap Unit Back")]
        public void ForwardPlaneOneSnapUnitBack()
        {
            UseOffsetGridSnapping = true;
            UpdateForwardPlaneLocation(-1f, UseOffsetGridSnapping);
        }
        #endregion
        /// <summary>
        /// tweak our z offset from another user input like forward/backward key
        /// </summary>
        /// <param name="passedZOffset"></param>
        public virtual void UpdateForwardPlaneLocation(float passedZOffset,bool useSnap=true, bool useUnityEvent = true)
        {
            UseOffsetGridSnapping = useSnap;
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
            PlaneWithinBounds(newPosition,isForward, useUnityEvent);
        }
        /// <summary>
        /// Will return true if we are within the bounds of the plane, and fire off events
        /// </summary>
        /// <param name="positionToCheck"></param>
        /// <param name="forward"></param>
        /// <returns></returns>
        protected bool PlaneWithinBounds(Vector3 positionToCheck, bool forward, bool useUnityEvent = true)
        {
            //use bounds to check inside unit sphere
            if (planeBounds.Contains(positionToCheck))
            {
                ForwardPlaneLocation.position = positionToCheck;
                if(forward)
                {
                    if (useUnityEvent)
                    {
                        PlaneMovedForwardEvent.Invoke();
                    }
                }
                else
                {
                    if (useUnityEvent)
                    {
                        PlaneMovedBackwardEvent.Invoke();
                    }
                    
                }
                return true;
            }
            PlaneFailedToMoveEvent.Invoke();
            return false;
        }
        #region Interface Requirements
        public virtual void OnUIEvent(FP_UIEventData<FP_Tool<PartData>> eventData)
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
        public virtual void PointerDown(PointerEventData eventData)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(movePanRectParentPanel, eventData.position,ToolCamera))
            {
                if(StartTool())
                {
                    
                    //Debug.LogWarning($"Pointer DOWN Coordinates: {eventData.position} by pointer ID: {eventData.button}");
                    Plane fPlane = new Plane(ForwardPlaneLocation.forward*-1, ForwardPlaneLocation.position);
                    FP_UtilityData.DrawLinePlane(ForwardPlaneLocation.position, ForwardPlaneLocation.forward * -1f,Color.green,2,10);
                    var PointData = FP_UtilityData.GetMouseWorldPositionOnPlane(ToolCamera,eventData.position,fPlane);
                    RaycastHit potentialHit;
                    var direction = (PointData.Item2 - ToolCamera.transform.position).normalized;
                    Ray ray = new Ray(ToolCamera.transform.position, direction);
                    //Debug.LogWarning($"Ray: {ray.origin} | {ray.direction}");
                    Debug.DrawRay(ray.origin, ray.direction * RaycastMaxDistance, FP_UtilityData.ReturnColorByStatus(SequenceStatus.Unlocked), 10f);
                    Debug.DrawRay(PointData.Item2,Vector3.up,Color.red,9f);
                    Physics.Raycast(ray, out potentialHit, RaycastMaxDistance, RaycastLayerMask);
                    
                    if (potentialHit.collider!=null)
                    {
                        mouseDownStartScreenCoordinate = eventData.position;
                        
                        //mouseLastFrameCoordinate = eventData.position;
                        var FPMRItem = potentialHit.collider.gameObject.GetComponent<FP_CollideItem>();
                        if(FPMRItem!=null)
                        {
                            selectedItemDetails=FPMRItem.MoveRotateItem;
                            if (selectedItemDetails == null)
                            {
                                Debug.LogError($"We hit something, {FPMRItem.gameObject.name}, and it didn't have a FP_MoveRotateItem component, we need one to continue!");
                                return;
                            }
                            selectedItem = selectedItemDetails.gameObject;
                            //update cached parameters
                            mouseForwardROTStartVector = ToolCamera.ScreenPointToRay(mouseDownStartScreenCoordinate).direction.normalized;
                            
                            
                            
                            //we don't exactly know what sort of part we have here but we know that we can check the interface to pass our eventData now to the connection logic on the part
                            //this in theory grabs the one/only 'IFPUIEventListener' interface that is being used on the selectedItem object
                            //at this moment, this is casting through the interface and hitting 'ConnectionPart.cs' and we're just sending/passing our information over there
                            
                            // can we move this item?
                            var toolInterface = selectedItem.GetComponent<IFPTool>();
                            if(toolInterface != null)
                            {
                                //if this is using the IFPTool interface we want to confirm we aren't in a locked state - everything else is fine
                                if (toolInterface.ReturnState() == FPToolState.Locked)
                                {
                                    selectedItem = null;
                                    return;
                                }
                            }
                            // did we find a tool/connection on this item part?
                            selectedItemInterface = selectedItem.GetComponent<IFPUIEventListener<FP_Tool<PartData>>>();
                            selectedItem.transform.position = new Vector3(selectedItem.transform.position.x, selectedItem.transform.position.y, ForwardPlaneLocation.position.z);
                            selectItemStartRotation = selectedItem.transform.rotation;
                            localVectorOffsetFromPlanePoint = selectedItem.transform.position - PointData.Item2;
                            if (selectedItemInterface != null)
                            {
                                //basically this cast is going ot hit 'ConnectionPart.cs' and pass our event data directly into the PointerDown function
                                selectedItemInterface.PointerDown(eventData);
                            }
                            //assuming we are facing z Forward going to jump the part forward
                            
                            //wrap up
                            isMoving = true;
                            selectedItemDetails.InteractionStarted();
                            //if we are rotating or moving?
                            if (eventData.button == PointerEventData.InputButton.Left) 
                            {
                                useRotation = false;
                            }
                            if (eventData.button == PointerEventData.InputButton.Right || eventData.button == PointerEventData.InputButton.Middle) 
                            {
                                useRotation = true;
                            }
                            OnToolSelectedItemUnityEvent.Invoke();
                        }
                    }
                }
            }
        }        
        public virtual void PointerDrag(PointerEventData eventData)
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
                //check lock state
                if (selectedItem != null)
                {
                    var toolInterface = selectedItem.GetComponent<IFPTool>();
                    if (toolInterface != null)
                    {
                        //if this is using the IFPTool interface we want to confirm we aren't in a locked state - everything else is fine
                        if (toolInterface.ReturnState() == FPToolState.Locked)
                        {
                            selectedItem = null;
                            selectedItemInterface = null;
                            return;
                        }
                    }
                }
                else
                {
                    return;
                }

                if (selectedItemInterface != null)
                {
                    selectedItemInterface.PointerDrag(eventData);
                }
                //now update our position
                mouseCurrentScreenCoordinate = eventData.position;
                Plane fPlane = new Plane(ToolCamera.transform.forward, ForwardPlaneLocation.position);
                var PointData = FP_UtilityData.GetMouseWorldPositionOnPlane(ToolCamera, eventData.position, fPlane);
                var direction = (PointData.Item2 - ToolCamera.transform.position).normalized;
                Ray rayMouse = new Ray(ToolCamera.transform.position, direction);
                
                Debug.DrawRay(rayMouse.origin, rayMouse.direction * RaycastMaxDistance, FP_UtilityData.ReturnColorByStatus(SequenceStatus.Active), 5f);
                //now the offset motion or rotation
                if (useRotation)
                {
                    //Vector3 currentMousePos = Input.mousePosition;
                    mouseCurrentROTVector = ToolCamera.ScreenPointToRay(mouseCurrentScreenCoordinate).direction.normalized;
                    //mouseCurrentZROTVector = ToolCamera.ScreenPointToRay(mouseCurrentScreenCoordinate).direction.normalized;

                    if (selectedItemDetails.UseRotationSnap)
                    {

                        float snapValue = 0;
                        Vector3 rotationAxis = Vector3.up;
                        float snappedAngleRot = 0;
                        float changeDirection = 1;
                        if(eventData.button== PointerEventData.InputButton.Right)
                        {
                            rotationAxis = ToolCamera.transform.forward;
                            snapValue = selectedItemDetails.RotationSnap.z;
                            changeDirection = 1;
                        }
                        else
                        {
                            rotationAxis = ToolCamera.transform.up;
                            snapValue = selectedItemDetails.RotationSnap.y;
                            changeDirection = -1f;
                        }
                        rotationAngleSigned = Vector3.SignedAngle(mouseForwardROTStartVector, mouseCurrentROTVector, rotationAxis);
                        snappedAngleRot = SnapToIncrement(rotationAngleSigned * rotationScalar, snapValue);
                        Quaternion RotationDelta = Quaternion.AngleAxis(snappedAngleRot* changeDirection, rotationAxis);
                        selectedItem.transform.rotation = RotationDelta * selectItemStartRotation;
                    }
                    else
                    {
                       
                        //Quaternion RotationDeltaZ = Quaternion.AngleAxis(rotationZAngleSigned*rotationScalar,ToolCamera.transform.forward);
                        if (eventData.button == PointerEventData.InputButton.Right)
                        {
                            //selectedItem.transform.rotation = RotationDeltaZ * selectItemStartRotation;
                        }
                        else
                        {
                            Quaternion RotationDelta = Quaternion.AngleAxis(rotationAngleSigned * rotationScalar, ToolCamera.transform.up);
                            selectedItem.transform.rotation = RotationDelta * selectItemStartRotation;
                        }
                        
                    }
                }
                else
                {
                    var (snapped, newPosition) = selectedItemDetails.MoveItem(PointData.Item2 + localVectorOffsetFromPlanePoint);
                    if (snapped)
                    {
                        //using snap - anything we need to do here?
                    }
                    else
                    {
                        //not using snap - anything we need to do here?
                    }
                    selectedItem.transform.position = newPosition;
                }
            }
        }
        public virtual void PointerUp(PointerEventData eventData)
        {
            //throw new NotImplementedException();
            Debug.Log($"On Pointer up");
            if (!ToolIsCurrent)
            {
                return;
            }
            if (EndTool())
            {
                //check our actual item to see if we somehow got locked while in some sort of manuever - HIGHLY Unlikely but still possible
                if (selectedItem != null)
                {
                    var toolInterface = selectedItem.GetComponent<IFPTool>();
                    if (toolInterface != null)
                    {
                        //if this is using the IFPTool interface we want to confirm we aren't in a locked state - everything else is fine
                        if (toolInterface.ReturnState() == FPToolState.Locked)
                        {
                            DeactivateTool();
                            selectedItem = null;
                            selectedItemDetails = null;
                            isMoving = false;
                            useRotation = false;
                            return;
                        }
                    }
                }
                else
                {
                    DeactivateTool();
                    selectedItem = null;
                    selectedItemDetails = null;
                    isMoving = false;
                    useRotation = false;
                    return;
                }
                // check if we have our interface
                if (selectedItemInterface != null)
                {
                    selectedItemInterface.PointerUp(eventData);
                }
                else
                {
                    DeactivateTool();
                    selectedItem = null;
                    selectedItemDetails = null;
                    isMoving = false;
                    useRotation = false;
                    return;
                }
                selectedItemDetails.InteractionEnded();
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
            useRotation = false;
        }
        public virtual void ResetVisuals()
        {
            //do nothing
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
                    OnToolDeactivatedUnityEvent.Invoke();
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
                OnToolDeactivatedUnityEvent.Invoke();
                return true;
            }
            return false;
        }
        #endregion
        float SnapToIncrement(float angle, float increment)
        {
            float sign = Mathf.Sign(angle);
            float abs = Mathf.Abs(angle);
            int snappedStep = Mathf.FloorToInt(abs / increment);

            // Snap *only if we're above the lower bound
            if (snappedStep == 0)
                return 0f;

            return sign * snappedStep * increment;
        }
        #region UI Methods
        /// <summary>
        /// Some additional UI reference to deactivate something if we needed it
        /// </summary>
        public void DeactivateToolFromUI()
        {
            ForceDeactivateTool();
        }
        #endregion
    }
}
