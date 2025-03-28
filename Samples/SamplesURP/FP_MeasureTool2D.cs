namespace FuzzPhyte.Tools
{
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
        [SerializeField] protected LineRenderer lineRenderer;
        [SerializeField] protected Text measurementText;
        [Header("Unity Events")]
        public UnityEvent OnMeasureToolActivated;
        public UnityEvent OnMeasureToolStarting;
        public UnityEvent OnMeasureToolEnding;
        public UnityEvent OnMeasureToolDeactivated;
        [Header("Internal parameters")]
        protected Vector2 startPosition;
        protected Vector2 endPosition;

        [Tooltip("This is a list of all the measurement points we have created.")]
        [SerializeField]protected List<GameObject> measurementPoints = new List<GameObject>();
        
        private void Awake()
        {
            if(measurementText == null)
            {
                Debug.LogError($"{gameObject.name} does not have a measurement text assigned. Please assign a Text component.");
                return;
            }
            measurementText.gameObject.SetActive(false);
            if(lineRenderer == null)
            {
                Debug.LogError($"{gameObject.name} does not have a line renderer assigned. Please assign a LineRenderer component.");
                return;
            }
        }
        public override void Initialize(FP_MeasureToolData data)
        {
            base.Initialize(data);
            lineRenderer.positionCount = 2;
            lineRenderer.material = toolData.LineMat;
            lineRenderer.startColor = lineRenderer.endColor = toolData.lineColor;
            lineRenderer.startWidth = lineRenderer.endWidth = toolData.lineWidth;
            UpdateTextFormat(0f);;
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
            if(StartTool())
            {
                lineRenderer.gameObject.SetActive(true);
                measurementText.gameObject.SetActive(true);
                Vector2 screenPosition = eventData.position;
                startPosition = ScreenToCanvasPosition(screenPosition);
                lineRenderer.SetPosition(0, startPosition);
                lineRenderer.SetPosition(1, startPosition);
                //use our point prefab to drop this into the scene at hte start position
                GameObject point = Instantiate(toolData.MeasurementPointPrefab);
                point.transform.position = startPosition;
                point.transform.SetParent(measurementParentSpace, false);
                measurementPoints.Add(point);
                OnMeasureToolStarting?.Invoke();
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
                lineRenderer.SetPosition(1, endPosition);
                UpdateMeasurementText();
            } 
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            if(!ToolIsCurrent)
            {
                return;
            }
            if(EndTool())
            {
                Vector2 screenPosition = eventData.position;
                endPosition = ScreenToCanvasPosition(screenPosition);
                lineRenderer.SetPosition(1, endPosition);
                UpdateMeasurementText();
                OnMeasureToolEnding?.Invoke();
                DeactivateTool();
            }
        }
        public override bool DeactivateTool()
        {
            if(base.DeactivateTool())
            {
                ToolIsCurrent = false;
                //lineRenderer.gameObject.SetActive(false);
                //measurementText.gameObject.SetActive(false);
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
            UpdateTextFormat(distance);
            measurementText.rectTransform.anchoredPosition = (startPosition + endPosition) / 2;
        }
        protected void UpdateTextFormat(float distance)
        {
            measurementText.text = $"{toolData.measurementPrefix}: {distance}:{toolData.measurementPrecision} {toolData.measurementUnits}";
        }

        
    }
}
