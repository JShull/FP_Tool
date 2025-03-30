namespace FuzzPhyte.Tools.Samples
{

    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    /// <summary>
    /// This class is using the Unity UI Interfaces like IDrag/IPoint/etc. which means we need a canvas/RectTransform to work with
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FPMeasureTool2D : FP_Tool<FP_MeasureToolData>, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] protected Canvas canvasRect;
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

        [SerializeField]protected FP_MeasureLine currentActiveLine;

        [Tooltip("This is a list of all the measurement points we have created.")]
        [SerializeField]protected List<FP_MeasureLine> allMeasuredLines = new List<FP_MeasureLine>();

        /// <summary>
        /// Public accessor from UI to start the "tool"
        /// </summary>
        public void ButtonUI()
        {
            Initialize(toolData);
            ActivateTool();
        }
        public override void Initialize(FP_MeasureToolData data)
        {
            base.Initialize(data);
        }
       
        /// <summary>
        /// This just sets our state up for being ready to use the tool
        /// </summary>
        public override bool ActivateTool()
        {
            if(base.ActivateTool())
            {
                ToolIsCurrent = true;
                OnMeasureToolActivated?.Invoke();
                return true;
            }
            Debug.LogWarning($"Didn't activate the tool?");
            return false;
        }

        public void OnPointerDown(PointerEventData eventData)
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
                    
                    OnMeasureToolStarting?.Invoke();
                    var spawnedItem = Instantiate(toolData.MeasurementPointPrefab, measurementParentSpace);
                    if (spawnedItem.GetComponent<FP_MeasureLine>() != null)
                    {
                        currentActiveLine = spawnedItem.GetComponent<FP_MeasureLine>();
                        currentActiveLine.SetupLine(this,measurementParentSpace,canvasRect,ToolCamera,lineSortOrderCounter);
                        currentActiveLine.DropFirstPoint(startPosition);
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

        public void OnDrag(PointerEventData eventData)
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
        public void OnPointerUp(PointerEventData eventData)
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
                    OnMeasureToolEnding?.Invoke();
                    DeactivateTool();
                    lineSortOrderCounter++;
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
        public override bool DeactivateTool()
        {
            if(base.DeactivateTool())
            {
                ToolIsCurrent = false;
                OnMeasureToolDeactivated?.Invoke();
                return true;
            }
            return false;
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
        protected void UpdateTextFormat(float distance)
        {
            if (currentActiveLine != null)
            {
                currentActiveLine.UpdateTextInformation($"{toolData.measurementPrefix}: {distance}:{toolData.measurementPrecision} {toolData.measurementUnits}");
                currentActiveLine.UpdateTextLocation((startPosition + endPosition) / 2);
            }
        }

        
    }
}
