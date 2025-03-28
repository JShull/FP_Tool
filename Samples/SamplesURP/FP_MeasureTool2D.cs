namespace FuzzPhyte.Tools
{
    using FuzzPhyte.Tools.Samples;
    //JOHN NEED TO TRANSITION FROM THIS LINE RENDERER TO THE FP_MEASURELINE
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    public class FPMeasureTool2D : FP_Tool<FP_MeasureToolData>, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        
        [SerializeField] protected RectTransform canvasRect;
        [Tooltip("Where do we want our measurements to be saved under")]
        [SerializeField] protected RectTransform measurementParentSpace;
        //[SerializeField] protected LineRenderer lineRenderer;
        //[SerializeField] protected Text measurementText;
        [Header("Unity Events")]
        public UnityEvent OnMeasureToolActivated;
        public UnityEvent OnMeasureToolStarting;
        public UnityEvent OnMeasureToolEnding;
        public UnityEvent OnMeasureToolDeactivated;
        [Header("Internal parameters")]
        protected Vector2 startPosition;
        protected Vector2 endPosition;

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
            return false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if(!ToolIsCurrent)
            {
                return;
            }
            if (RectTransformUtility.RectangleContainsScreenPoint(measurementParentSpace, eventData.position))
            {
                if (StartTool())
                {
                    Vector2 screenPosition = eventData.position;
                    startPosition = ScreenToCanvasPosition(screenPosition);
                    OnMeasureToolStarting?.Invoke();
                    var spawnedItem = Instantiate(toolData.MeasurementPointPrefab, measurementParentSpace);
                    if (spawnedItem.GetComponent<FP_MeasureLine>() != null)
                    {
                        currentActiveLine = spawnedItem.GetComponent<FP_MeasureLine>();
                        currentActiveLine.SetupLine(this);
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
                Vector2 screenPosition = eventData.position;
                endPosition = ScreenToCanvasPosition(screenPosition);
                if (currentActiveLine != null) 
                {
                    currentActiveLine.DropSecondPoint(endPosition);
                }
                UpdateMeasurementText();
            } 
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if(!ToolIsCurrent)
            {
                return;
            }
            //are we in the position?
            if (RectTransformUtility.RectangleContainsScreenPoint(measurementParentSpace, eventData.position))
            {
                if (EndTool())
                {
                    Vector2 screenPosition = eventData.position;
                    endPosition = ScreenToCanvasPosition(screenPosition);
                    if (currentActiveLine != null)
                    {
                        currentActiveLine.DropSecondPoint(endPosition);
                    }
                    UpdateMeasurementText();
                    OnMeasureToolEnding?.Invoke();
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

        protected Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 canvasPos);
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
