namespace FuzzPhyte.Tools.Connections
{
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using FuzzPhyte.Utility;
    using UnityEngine.EventSystems;

    public class FP_PanMove : FP_Tool<PartData>, IFPUIEventListener<FP_Tool<PartData>>
    {
        public RectTransform movePanRectParentPanel;
        public Transform ForwardPlaneLocation;
        public float RaycastMaxDistance = 15;
        public List<Collider> SnapPoints = new List<Collider>();
        [SerializeField]
        protected List<Collider> UsedSnapPoints = new List<Collider>();
        protected Collider lastHitSnapPoint;
        [SerializeField]
        protected GameObject selectedItem;
        [SerializeField]
        private FP_MoveRotateItem selectedItemDetails;
        
        protected Vector2 mouseScreenPos;
        protected Vector3 originalLocationOnActive;
        /// <summary>
        /// this we might adjust with other input information as needed
        /// </summary>
        protected float originalDistanceFromCam;
        protected float zOffsetDistance = 0f;
        public bool UseOffsetGridSnapping = true;
        public float OffsetGridSize = 0.025f;
        [SerializeField]
        private bool isMoving;
        [Tooltip("If we are further from this distance on drop we just return to original location")]
        [Range(1,10)]
        public float MaxDistanceBoundsCatch=4;
        [Header("Movement Locked")]
        public bool ManagerOverride;
        public bool IsXLocked;
        public bool IsYLocked;
        public bool IsZLocked;
        [Tooltip("Quick fix")]
        public bool UseGameObjectPositionNotCollider;
        public event Action<Collider,GameObject> OnSelectionEndAction;

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
            //throw new NotImplementedException();
            if (RectTransformUtility.RectangleContainsScreenPoint(movePanRectParentPanel, eventData.position,ToolCamera))
            {
                if(StartTool())
                {
                    //raycast time
                    Plane fPlane = new Plane(ToolCamera.transform.forward,ForwardPlaneLocation.position);
                    var PointData = FP_UtilityData.GetMouseWorldPositionOnPlane(ToolCamera,eventData.position,fPlane);
                    RaycastHit potentialHit;
                    //var worldPoint = ToolCamera.ScreenToWorldPoint(eventData.position);
                    var direction = (PointData.Item2 - ToolCamera.transform.position).normalized;
                    UnityEngine.Ray ray = new Ray(ToolCamera.transform.position, direction);
                    Debug.LogWarning($"Ray: {ray.origin} | {ray.direction}");
                    Debug.DrawRay(ray.origin, ray.direction * RaycastMaxDistance, FP_UtilityData.ReturnColorByStatus(SequenceStatus.Unlocked), 5f);
                    Physics.Raycast(ray, out potentialHit, RaycastMaxDistance);
                    if(potentialHit.collider!=null)
                    {
                        Debug.Log($"Hit: {potentialHit.collider.name}");
                        var FPMRItem = potentialHit.collider.gameObject.GetComponent<FP_CollideItem>();
                        if(FPMRItem!=null)
                        {
                            selectedItemDetails=FPMRItem.MoveRotateItem;
                            Debug.Log($"Hit: {selectedItemDetails.name}");
                            SelectionStart(selectedItemDetails.gameObject);
                        }
                    }
                }
                
            }
        }

        public void PointerUp(PointerEventData eventData)
        {
            //throw new NotImplementedException();
            SelectionEnd();
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
                UpdateMouseScreenPosition(eventData.position);
            }
        }
        #endregion
        /// <summary>
        /// Assuming something else is handling mouse information - we just need that :)
        /// </summary>
        /// <param name="passedMouseCor"></param>
        public void UpdateMouseScreenPosition(Vector2 passedMouseCor)
        {
            //screen starts 0,0 bottom left
            //screen ends Screen.width, Screen.height top right
            if(!isMoving)
            {
                return;
            }
            mouseScreenPos = passedMouseCor;
            //move item
            if (selectedItem == null) return;

            Vector3 calculatedPos = ToolCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, originalDistanceFromCam+ zOffsetDistance));
            var returnLocation = calculatedPos;
            //get items localized location based on items information and restrictions
            //we are then going to override that if we can or have the authority to do so
            
            //can do some override here and/or lock by our manager
            //this will ignore the local items restrictions entirely for movement
            //Apply axis lock if we have authority
            if (ManagerOverride) 
            {
                //manager has authority 
                //just need to check on our locked global axis.
                if (IsXLocked) returnLocation.x = selectedItem.transform.position.x;
                if (IsYLocked) returnLocation.y = selectedItem.transform.position.y;
                if (IsZLocked) returnLocation.z = selectedItem.transform.position.z;
            }
            else
            {
                //individual item has authority
                //use the on item restrictions to move the item
                returnLocation=selectedItemDetails.MoveItem(calculatedPos);
            }

            selectedItem.transform.position = returnLocation;
        }
        
        
        /// <summary>
        /// tweak our z offset from another user input like forward/backward key
        /// </summary>
        /// <param name="passedZOffset"></param>
        public void UpdateZOffset(float passedZOffset)
        {
            if (UseOffsetGridSnapping)
            {
                if (passedZOffset > 0)
                {
                    zOffsetDistance += OffsetGridSize;
                    
                }
                else
                {
                    zOffsetDistance -= OffsetGridSize;
                }
                return;
            }
            //normal non grid snapping
            
            zOffsetDistance += passedZOffset;
            
        }
        #region Action Based Functions
        
        /// <summary>
        /// public access for selection start based events
        /// Coming in from the CC_Pickable Events
        /// </summary>
        /// <param name="item"></param>
        public void SelectionStart(GameObject item)
        {
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
            //set up our distance from camera
            originalDistanceFromCam = Vector3.Distance(item.transform.position, ToolCamera.transform.position);
            //fin
            isMoving = true;
            selectedItemDetails.MoveStarted();
            //OnSelectionStartAction.Invoke(selectedItem);
        }
        /// <summary>
        /// public access for selection ending based events
        /// </summary>
        public void SelectionEnd()
        {
            Debug.Log($"On Pointer up");
            if(!ToolIsCurrent)
            {
                return;
            }
            if (EndTool())
            {
                DeactivateTool();
            }else
            {
                 DeactivateTool();
            }
            return;
            var nearPoint = FindNearestSnapPosition(selectedItem.transform.position);
            Debug.Log($"Return Location:{nearPoint.Item1}, from {nearPoint.Item2.gameObject.name}");
            selectedItem.transform.position = nearPoint.Item1;
            Debug.Log($"SelectedItem.transform.position { selectedItem.gameObject.name}");
            lastHitSnapPoint=nearPoint.Item2;
            isMoving = false;
            //snap to nearest snap point?
            if(OnSelectionEndAction==null)
            {
                Debug.LogError($"Our OnSelectionEndAction apparently is null!?");
            }else
            {
                if(lastHitSnapPoint!=null)
                {
                    OnSelectionEndAction.Invoke(lastHitSnapPoint,selectedItem);
                }
                else
                {
                    Debug.LogWarning($"no lastHitSnapPoint");
                }
            }
           
            if (selectedItemDetails != null)
            {
                selectedItemDetails.MoveEnd();
            }
            selectedItemDetails = null;
            selectedItem = null;
        }
        #endregion
        /// <summary>
        /// This is coming in from Unlock sequences
        /// </summary>
        /// <param name="snapPoint"></param>
        public void AddSnapPoint(Collider snapPoint)
        {
            if(!SnapPoints.Contains(snapPoint) && !UsedSnapPoints.Contains(snapPoint))
            {
                SnapPoints.Add(snapPoint);
            }
            
        }
        public void RemoveSnapPoint(Collider snapPointMatch)
        {
            if (SnapPoints.Contains(snapPointMatch))
            {
                SnapPoints.Remove(snapPointMatch);
                UsedSnapPoints.Add(snapPointMatch);
            }
        }
        /// <summary>
        /// quick distance formula based on location of item in world space
        /// </summary>
        /// <param name="currentPosition"></param>
        /// <returns></returns>
        private (Vector3,Collider) FindNearestSnapPosition(Vector3 currentPosition)
        {
            //lets try seeing if we have a direct hit first
            Collider potentialHit;
            UnityEngine.Ray ray = ToolCamera.ScreenPointToRay(mouseScreenPos);
            var foundMatch = TryGetSnapPointFromRaycast(ray, out potentialHit);
            if (foundMatch)
            {
                if (Vector3.Distance(currentPosition, potentialHit.bounds.center) >= MaxDistanceBoundsCatch)
                {
                    /*
                    if (UseGameObjectPositionNotCollider)
                    {
                        Debug.LogWarning($"Potential Hit Item Location:{potentialHit.gameObject.name}| {potentialHit.gameObject.transform.position}");
                        return (potentialHit.gameObject.transform.position, potentialHit);
                    }
                    */
                    Debug.LogWarning($"We have exceeded the MaxDistanceBoundsCatch! - return {originalLocationOnActive} via the potential hit {potentialHit}");
                    return (originalLocationOnActive, potentialHit);
                }
                else
                {
                    if (UseGameObjectPositionNotCollider)
                    {
                        return (potentialHit.gameObject.transform.position, potentialHit);
                    }
                    else
                    {
                        return (potentialHit.bounds.center, potentialHit);
                    }
                }
                
               

            }
            
            float minDistance = float.MaxValue;
            Vector3 nearestSnapPosition = currentPosition;
            Collider nearestSnapCollider = null;
            for(int i = 0; i < SnapPoints.Count; i++)
            {
                var snapPoint = SnapPoints[i];
                if (snapPoint != null)
                {
                    Vector3 snapCenter = snapPoint.bounds.center;
                    float distance = Vector3.Distance(currentPosition, snapCenter);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        if (UseGameObjectPositionNotCollider)
                        {
                            Debug.LogWarning($"SnapPoint Hit Location: {snapPoint.gameObject.name}| {snapPoint.gameObject.transform.position}");
                            nearestSnapPosition = snapPoint.gameObject.transform.position;
                        }
                        else
                        {
                            nearestSnapPosition = snapCenter;
                        }

                        nearestSnapCollider = snapPoint;
                    }
                }
            }
            if (Vector3.Distance(currentPosition, nearestSnapPosition) >= MaxDistanceBoundsCatch)
            {
                return (originalLocationOnActive,nearestSnapCollider);
            }
            return (nearestSnapPosition,nearestSnapCollider);
        }
        /// <summary>
        /// See if we find a snap point from a raycast thru the mouse position
        /// </summary>
        /// <param name="mousePosition"></param>
        /// <param name="snapObject"></param>
        /// <returns></returns>
        private bool TryGetSnapPointFromRaycast(Ray ray, out Collider snapObject)
        {
            Debug.LogWarning($"Try to get snap point from raycast");
            var hitPts=Physics.RaycastAll(ray);
            /*
            foreach(var pt in hitPts)
            {
                Debug.Log($"Item: {pt.collider.name}");
            }
            */
            if (hitPts.Length > 0)
            {
                foreach(var hitPt in hitPts)
                {
                    if (SnapPoints.Contains(hitPt.collider))
                    {
                        snapObject = hitPt.collider;
                        
                        return true;
                    }
                }
            }

            snapObject = null;
            return false;
        }

        
    }
}
