namespace FuzzPhyte.Tools.Samples
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using FuzzPhyte.Utility;
    using UnityEngine.UI;
    using System.Xml;

    /// <summary>
    /// This class is using the Unity UI Interfaces like IDrag/IPoint/etc. which means we need a canvas/RectTransform to work with
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FPMeasureTool2D : FP_Tool<FP_MeasureToolData>, IFPUIEventListener<FP_Tool<FP_MeasureToolData>>
    {
        [Space]
        //[Header("Keep Using the Line Tool?")]
        //public bool ConstantLineMeasurement = false;
        [Space]
        [SerializeField] protected Canvas canvasRect;
        protected CanvasScaler canvasScaler;
        [Tooltip("Where do we want our measurements to be saved under")]
        [SerializeField] protected RectTransform measurementParentSpace;
        [Header("Unity Events")]
        public UnityEvent OnMeasureToolActivated;
        public UnityEvent OnMeasureToolStarting;
        public UnityEvent OnMeasureToolEnding;
        public UnityEvent OnMeasureToolDeactivated;
        [Header("Internal parameters")]
        protected Vector2 startPosition = Vector2.zero;
        protected Vector2 endPosition = Vector2.zero;
        protected int lineSortOrderCounter =10;

        [SerializeField]protected FP_MeasureLine2D currentActiveLine;

        [Tooltip("This is a list of all the measurement points we have created.")]
        [SerializeField]protected List<FP_MeasureLine2D> allMeasuredLines = new List<FP_MeasureLine2D>();

        
        /// <summary>
        /// Public accessor from UI to 'stop' the tool
        /// Called at the end of the 'game' example 
        /// </summary>
        public void DeactivateResetLinesUI()
        {
            ForceDeactivateTool();
            //blast all the lines
            foreach (var line in allMeasuredLines)
            {
                Destroy(line.gameObject);
            }
            allMeasuredLines.Clear();
        }
        public void DeactivateToolFromUI()
        {
            ForceDeactivateTool();
        }
        
        public override void Initialize(FP_MeasureToolData data)
        {
            base.Initialize(data);
            if(canvasRect!=null)
            {
                canvasScaler = canvasRect.GetComponent<CanvasScaler>();
            }
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
        public override bool DeactivateTool()
        {
            if(base.DeactivateTool())
            {
                if(!LoopTool)
                {
                    //we do want to turn off ToolIsCurrent
                    ToolIsCurrent = false;
                }
                OnMeasureToolDeactivated.Invoke();
                return true;
            }
            return false;
        }
        public override bool ForceDeactivateTool()
        {
            if(base.ForceDeactivateTool())
            {
                ToolIsCurrent = false;
                OnMeasureToolDeactivated.Invoke();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Interface method for the FP UI Event Listener system to process the event data
        /// </summary>
        /// <param name="eventData"></param>
        public void OnUIEvent(FP_UIEventData<FP_Tool<FP_MeasureToolData>> eventData)
        {
            Debug.LogWarning($"OnUIEvent was processed {eventData.EventType} {eventData.AdditionalData} {this} {ToolIsCurrent}");
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
            //Debug.LogWarning($"Passed Tool is Current");
            //need to convert the eventData.Position to the RectTransform space of the canvas
            
            //RectTransformUtility.ScreenPointToLocalPointInRectangle(measurementParentSpace,eventData.position,ToolCamera, out Vector2 localPoint);
            //Debug.LogWarning($"Event Data position: {eventData.position}, converted point {localPoint}");
            //bool inRectangle = RectTransformUtility.RectangleContainsScreenPoint(measurementParentSpace, eventData.position, ToolCamera);
            if (RectTransformUtility.RectangleContainsScreenPoint(measurementParentSpace, eventData.position,ToolCamera))
            {
                if (StartTool())
                {
                    Vector2 screenPosition = eventData.position;
                    startPosition = ScreenToRelativeRectPosition(screenPosition, measurementParentSpace);
                    
                    OnMeasureToolStarting.Invoke();
                    var spawnedItem = Instantiate(toolData.MeasurementPointPrefab, measurementParentSpace);
                    if (spawnedItem.GetComponent<FP_MeasureLine2D>() != null)
                    {
                        currentActiveLine = spawnedItem.GetComponent<FP_MeasureLine2D>();
                        currentActiveLine.Setup(this);
                        currentActiveLine.SetupLine(measurementParentSpace,canvasRect,ToolCamera,lineSortOrderCounter);
                        currentActiveLine.DropFirstPoint(startPosition);
                        currentActiveLine.DropSecondPoint(startPosition);
                        UpdateTextFormat(0);
                        currentActiveLine.UpdateTextLocation(startPosition);
                        allMeasuredLines.Add(currentActiveLine);
                    }
                    else
                    {
                        Destroy(spawnedItem);
                        Debug.LogError($"missing FP_measure line component");
                        DeactivateTool();
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
                endPosition = ScreenToRelativeRectPosition(eventData.position, measurementParentSpace);
                if (currentActiveLine != null) 
                {
                    currentActiveLine.DropSecondPoint(endPosition);
                }
                UpdateMeasurementText();
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
                    
                    endPosition = ScreenToRelativeRectPosition(eventData.position,measurementParentSpace);
                    if (currentActiveLine != null)
                    {
                        currentActiveLine.DropSecondPoint(endPosition);
                    }
                    UpdateMeasurementText();
                    OnMeasureToolEnding.Invoke();
                    DeactivateTool();
                    lineSortOrderCounter++;
                    /*
                    if(ConstantLineMeasurement)
                    {
                        //reset the line to start a new one without having to push the button
                        Initialize(toolData);
                        ActivateTool();
                    }
                    */
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
                /*
                if(ConstantLineMeasurement)
                {
                    //reset the line to start a new one without having to push the button
                    Initialize(toolData);
                    ActivateTool();
                }
                */
            }
        }
        
        /// <summary>
        /// Reset whatever we need on this tool
        /// </summary>
        public void ResetVisuals()
        {
            DeactivateResetLinesUI();
        }
        /// <summary>
        /// Returns a screen position coordinate based on the canvas already assigned in the inspector
        /// </summary>
        /// <param name="screenPosition"></param>
        /// <returns></returns>
        protected Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect.GetComponent<RectTransform>(), screenPosition, ToolCamera, out Vector2 canvasPos);
            return canvasPos;
        }
        /// <summary>
        /// Returns a Screen Position Coordinate based on the rect transform passed in
        /// </summary>
        /// <param name="screenPosition"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        protected Vector2 ScreenToRelativeRectPosition(Vector2 screenPosition, RectTransform rect)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, ToolCamera, out Vector2 canvasPos);
            return canvasPos;
        }

        protected void UpdateMeasurementText()
        {
            float distance = Vector2.Distance(startPosition, endPosition);
            //format text
            if (currentActiveLine != null) 
            {
                UpdateTextFormat(distance);
            }     
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="distance">incoming pixel coordinate value</param>
        protected void UpdateTextFormat(float distance)
        {
            if (currentActiveLine != null)
            {
                //convert to the correct measurement system
                var unitReturn = FP_UtilityData.ReturnUnitByPixels(canvasScaler.referencePixelsPerUnit,distance,toolData.measurementUnits);
                if(unitReturn.Item1)
                {
                    var formattedDistance = unitReturn.Item2.ToString($"F{toolData.measurementPrecision}");
                    currentActiveLine.UpdateText($"{toolData.measurementPrefix} {formattedDistance} {toolData.measurementUnits}");
                }else
                {
                    Debug.LogWarning($"Failed to convert the distance {distance} to the correct measurement system {toolData.measurementUnits}");
                    currentActiveLine.UpdateText($"{toolData.measurementPrefix} {distance} pixels");
                    
                }

                Vector2 midPoint = (startPosition + endPosition) * 0.5f;
                Vector2 direction = (endPosition - startPosition).normalized;
                // Perpendicular vector (rotate 90 degrees CCW)
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                // Apply offset relative to perpendicular direction
                Vector2 offset = perpendicular * toolData.measurementLabelOffsetPixels.y + direction * toolData.measurementLabelOffsetPixels.x;
                Vector2 labelPosition = midPoint + offset;
                currentActiveLine.UpdateTextLocation(labelPosition);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                currentActiveLine.SetTextRotation(angle);
            }
        }

       
    }
}
