namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using UnityEngine.UI;
    public class FPMeasureTool2D : FP_Tool<FP_MeasureToolData>
    {
        [SerializeField] protected RectTransform canvasRect;
        [SerializeField] protected LineRenderer lineRenderer;
        [SerializeField] protected Text measurementText;
        private Vector2 startPosition;
        private Vector2 endPosition;
        private void Awake()
        {
            if(measurementText == null)
            {
                Debug.LogError($"{gameObject.name} does not have a measurement text assigned. Please assign a Text component.");
                return;
            }
            measurementText.gameObject.SetActive(false);
        }
        public override void Initialize(FP_MeasureToolData data)
        {
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.positionCount = 2;
            base.Initialize(data);
            lineRenderer.material = toolData.LineMat;
            lineRenderer.startColor = lineRenderer.endColor = toolData.lineColor;
            lineRenderer.startWidth = lineRenderer.endWidth = toolData.lineWidth;
            measurementText.text = "";
        }

        public override void ActivateTool()
        {
            lineRenderer.enabled = true;
            measurementText.gameObject.SetActive(true);
            base.ActivateTool();
        }

        public void OnPointerDown(Vector2 screenPosition)
        {
            startPosition = ScreenToCanvasPosition(screenPosition);
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.SetPosition(1, startPosition);
            StartTool();
        }

        public void OnDrag(Vector2 screenPosition)
        {
            UseTool();
            endPosition = ScreenToCanvasPosition(screenPosition);
            lineRenderer.SetPosition(1, endPosition);
            UpdateMeasurementText();
        }

        public void OnPointerUp(Vector2 screenPosition)
        {
            EndTool();
            endPosition = ScreenToCanvasPosition(screenPosition);
            lineRenderer.SetPosition(1, endPosition);
            UpdateMeasurementText();
            DeactivateTool();
        }

        private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 canvasPos);
            return canvasPos;
        }

        private void UpdateMeasurementText()
        {
            float distance = Vector2.Distance(startPosition, endPosition);
            measurementText.text = $"{distance}:{toolData.measurementPrecision} units";
            measurementText.rectTransform.anchoredPosition = (startPosition + endPosition) / 2;
        }
    }
}
