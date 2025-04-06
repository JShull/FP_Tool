namespace FuzzPhyte.Tools.Samples
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using FuzzPhyte.Utility;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine.Events;

    /// <summary>
    /// This class is using the Unity UI Interfaces like IDrag/IPoint/etc. which means we need a canvas/RectTransform to work with
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FP_MeasureTool3D : FP_Tool<FP_MeasureToolData>, IFPUIEventListener<FP_Tool<FP_MeasureToolData>>
    {
        public Transform ParentDecals;
        [Tooltip("We are 2D casting into 3D Space - this RectTransform is our boundary")]
        [SerializeField] protected RectTransform measurementParentSpace;
        [Header("Unity Events")]
        public UnityEvent OnMeasureToolActivated;
        public UnityEvent OnMeasureToolStarting;
        public UnityEvent OnMeasureToolEnding;
        [Header("Internal parameters")]
        protected Vector3 startPosition = Vector3.zero;
        protected Vector3 endPosition = Vector3.zero;
        [SerializeField] FP_MeasureLine3D currentActiveLine;
        [SerializeField]protected List<FP_MeasureLine3D> allMeasuredLines = new List<FP_MeasureLine3D>();
        /*
        [ContextMenu("Spawn Chalk Line")]
        
        public void SpawnChalkLineTest()
        {
            startPosition = FirstPointObjectTest.transform.position;
            endPosition = SecondPointObjectTest.transform.position;
            StartCoroutine(DelayLineGeneration());
        }
        protected IEnumerator DelayLineGeneration()
        {
            var spawnLinePrefab = GameObject.Instantiate(toolData.MeasurementPointPrefab);
            currentActiveLine = spawnLinePrefab.GetComponent<FP_MeasureLine3D>(); 
            yield return new WaitForSeconds(0.5f);
            if (currentActiveLine!=null)
            {
                currentActiveLine.Setup(this);
                currentActiveLine.DropFirstPoint(startPosition);
                yield return new WaitForSeconds(2f);
                currentActiveLine.DropSecondPoint(endPosition);
                UpdateMeasurementText();
                yield return new WaitForSeconds(1f);
                currentActiveLine.transform.SetParent(ParentDecals);
            }
        }
        */
        public void DeactivateResetLinesUI()
        {
            DeactivateTool();
            //blast all the lines
            foreach (var line in allMeasuredLines)
            {
                Destroy(line.gameObject);
            }
        }
        public void DeactivateToolFromUI()
        {
            DeactivateTool();
        }
        /// <summary>
        /// This just sets our state up for being ready to use the tool
        /// </summary>
        public override bool ActivateTool()
        {
            if(base.ActivateTool())
            {
                ToolIsCurrent = true;
                OnMeasureToolActivated.Invoke();
                return true;
            }
            Debug.LogWarning($"Didn't activate the tool?");
            return false;
        }
        /// <summary>
        /// Process Event Data and pass it to the tool
        /// </summary>
        /// <param name="eventData"></param>
        public void OnUIEvent(FP_UIEventData<FP_Tool<FP_MeasureToolData>> eventData)
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
                    case FP_UIEventType.PointerUp:
                        PointerUp(eventData.UnityPointerEventData);
                        break;
                    case FP_UIEventType.Drag:
                        PointerDrag(eventData.UnityPointerEventData);
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
            if(!ToolIsCurrent)
            {
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(measurementParentSpace, eventData.position,ToolCamera))
            {
                if(StartTool())
                {
                    //if we do we then want to cast into 3D space
                    //activate the ray - fire it once
                    //move transform to the world space position based on the mouse position relative to rect
                    Plane fPlane = new Plane(ToolCamera.transform.forward,ForwardPlaneLocation.position);
                    var PointData = FP_UtilityData.GetMouseWorldPositionOnPlane(ToolCamera,eventData.position,fPlane);
                    if(PointData.Item1)
                    {
                        startPosition = PointData.Item2;
                        endPosition = PointData.Item2;
                        var spawnLinePrefab = GameObject.Instantiate(toolData.MeasurementPointPrefab);
                        currentActiveLine = spawnLinePrefab.GetComponent<FP_MeasureLine3D>(); 
                        if (currentActiveLine!=null)
                        {
                            currentActiveLine.Setup(this);
                            currentActiveLine.CameraPass(ToolCamera);
                            currentActiveLine.DropFirstPoint(startPosition);
                            currentActiveLine.DropSecondPoint(endPosition);
                            UpdateMeasurementText();
                            allMeasuredLines.Add(currentActiveLine);
                            OnMeasureToolStarting.Invoke();
                        }
                    }
                }  
            }
        }
        public void PointerDrag(PointerEventData eventData)
        {
            if(!ToolIsCurrent)
            {
                return;
            }

            if(UseTool())
            {
                Plane fPlane = new Plane(ToolCamera.transform.forward,ForwardPlaneLocation.position);
                var PointData = FP_UtilityData.GetMouseWorldPositionOnPlane(ToolCamera,eventData.position,fPlane);
                if(PointData.Item1)
                {
                    endPosition = PointData.Item2;
                    if (currentActiveLine != null)
                    {
                        currentActiveLine.DropSecondPoint(endPosition);
                        UpdateMeasurementText();
                    }
                }
                //currentActiveLine.DropSecondPoint(endPosition);
            }
        }
        public void PointerUp(PointerEventData eventData)
        {
            Debug.Log($"On Pointer up");
            if(!ToolIsCurrent)
            {
                return;
            }
            //are we in the position?
            if (RectTransformUtility.RectangleContainsScreenPoint(measurementParentSpace, eventData.position,ToolCamera))
            {
                if (EndTool())
                {
                    //update location
                    Plane fPlane = new Plane(ToolCamera.transform.forward,ForwardPlaneLocation.position);
                    var PointData = FP_UtilityData.GetMouseWorldPositionOnPlane(ToolCamera,eventData.position,fPlane);
                    if(PointData.Item1)
                    {
                        endPosition = PointData.Item2;
                        if (currentActiveLine != null)
                        {
                            currentActiveLine.DropSecondPoint(endPosition);
                            UpdateMeasurementText();
                            currentActiveLine.transform.SetParent(ParentDecals);
                        }
                    }
                    OnMeasureToolEnding.Invoke();
                    DeactivateTool();
                }
            }
            else
            {
                //destroy it
                DeactivateTool();
                if (currentActiveLine != null) 
                {
                    allMeasuredLines.Remove(currentActiveLine);
                    Destroy(currentActiveLine.gameObject);
                }
            }
        }
        protected void UpdateMeasurementText()
        {
            float distance = Vector3.Distance(startPosition, endPosition);
            //format text
            if (currentActiveLine != null)
            {
                UpdateTextFormat(distance);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="distance">incoming meter distance coordinate value</param>
        protected void UpdateTextFormat(float distance)
        {
            if (currentActiveLine != null)
            {
                //convert to the correct measurement system - our distance coming in is going to be in world meters
                var unitReturn = FP_UtilityData.ReturnUnitByPixels(1, distance, toolData.measurementUnits);
                if (unitReturn.Item1)
                {
                    var formattedDistance = unitReturn.Item2.ToString($"F{toolData.measurementPrecision}");
                    currentActiveLine.UpdateText($"{toolData.measurementPrefix} {formattedDistance} {toolData.measurementUnits}");
                }
                else
                {
                    Debug.LogWarning($"Failed to convert the distance {distance} to the correct measurement system {toolData.measurementUnits}");
                    currentActiveLine.UpdateText($"{toolData.measurementPrefix} {distance} pixels");
                }
                currentActiveLine.UpdateTextLocation(ToolData.measurementLabelOffsetPixels);
                
            }
        }
        [Header("Additional Details")]
        public Transform ForwardPlaneLocation;
        /*
        
        [Header("Raycaster Related Parameters")]
        public SO_FPRaycaster RayData;
        public Transform RaycastEndDir;
        public Transform RaycastOrigin;
        public float3 RayDirection
        {
            get { 
                if(RaycastEndDir==null||RaycastOrigin==null)
                {
                    return Vector3.zero;
                }
                return Vector3.Normalize(RaycastEndDir.position - RaycastOrigin.position); }
            set { RayDirection = value; }
        }
        protected FP_RayArgumentHit _rayHit;
        public FP_Raycaster Raycaster { get; set; }
        public SO_FPRaycaster FPRayInformation
        {
            get { return RayData; }
            set { RayData = value; }
        }
        public Transform RayOrigin {
            get { return RaycastOrigin; }
            set { RaycastOrigin = value; }
        }
        public virtual void SetupRaycaster()
        {
            Raycaster = new FP_Raycaster(this);
        }
        protected virtual void Awake()
        {
            SetupRaycaster();
        }
        public virtual void OnEnable()
        {
            Raycaster.OnFPRayFireHit += OnRayStay;
            Raycaster.OnFPRayEnterHit += OnRayEnter;
            Raycaster.OnFPRayExit += OnRayExit;
            Raycaster.ActivateRaycaster();
        }
        public virtual void OnDisable()
        {
            Raycaster.OnFPRayFireHit -= OnRayStay;
            Raycaster.OnFPRayEnterHit -= OnRayEnter;
            Raycaster.OnFPRayExit -= OnRayExit;
            Raycaster.DeactivateRaycaster();
        }
        public virtual void OnRayEnter(object sender, FP_RayArgumentHit arg)
        {
            if (arg.HitObject != null)
            {
                Debug.LogWarning($"RAY Enter: {arg.HitObject.name}");
            }
            //check if our rayhit was valid
            //update first position with the information
            _rayHit = arg;
        }
        public virtual void OnRayStay(object sender, FP_RayArgumentHit arg)
        {
            if (arg.HitObject != null)
            {
                Debug.LogWarning($"RAY Stay: {arg.HitObject.name}");
            }
            
            _rayHit = arg;
        }
        public virtual void OnRayExit(object sender, FP_RayArgumentHit arg)
        {
            if (arg.HitObject != null)
            {
                Debug.LogWarning($"RAY Exit: {arg.HitObject.name}");
            }
            
            _rayHit = arg;
        }
        
        */
    }
}
