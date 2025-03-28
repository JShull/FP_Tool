namespace FuzzPhyte.Tools.Samples
{
    using UnityEngine;
    using FuzzPhyte.Tools;
    using FuzzPhyte.Utility;
    using TMPro;

    /// <summary>
    /// Used to store information on deployed 'measure line'
    /// Should be part of a root transform prefab
    /// </summary>
    public class FP_MeasureLine : MonoBehaviour, IFPToolListener<FP_MeasureToolData>
    {
        protected FP_Tool<FP_MeasureToolData> myTool;
        public RectTransform FirstPoint;
        public RectTransform SecondPoint;
        public TextMeshProUGUI MeasurementText;
        public LineRenderer LineRenderer;
        public void SetupLine(FP_Tool<FP_MeasureToolData> theTool)
        {
            myTool = theTool;
            LineRenderer.positionCount = 2;
            LineRenderer.material = theTool.ToolData.LineMat;
            LineRenderer.startColor = theTool.ToolData.lineColor;
            LineRenderer.startWidth = theTool.ToolData.lineWidth;
            
            //now listen in
            myTool.OnActivated += OnToolActivated;
            myTool.OnStarting += OnToolStarting;
            myTool.OnActiveUse += OnToolInActiveUse;
            myTool.OnEnding += OnToolEnding;
            myTool.OnDeactivated += OnToolDeactivated;
        }

        public virtual void DropFirstPoint(Vector2 position)
        {
            FirstPoint.localPosition = position;
            FirstPoint.gameObject.SetActive(true);
            SecondPoint.gameObject.SetActive(true);
            LineRenderer.SetPosition(0, position);
            LineRenderer.SetPosition(1, position);
        }
        public virtual void DropSecondPoint(Vector2 position)
        {
            SecondPoint.localPosition = position;
            LineRenderer.SetPosition(1, position);
        }
        public void OnDestroy()
        {
            if(myTool != null)
            {
                myTool.OnActivated -= OnToolActivated;
                myTool.OnStarting -= OnToolStarting;
                myTool.OnActiveUse -= OnToolInActiveUse;
                myTool.OnEnding -= OnToolEnding;
                myTool.OnDeactivated -= OnToolDeactivated;
            }
        }
        public void OnToolActivated(FP_Tool<FP_MeasureToolData> tool)
        {
            if(tool != myTool) return;
        }
        public void OnToolStarting(FP_Tool<FP_MeasureToolData> tool)
        {
            if(tool != myTool) return;
        }
        public void OnToolInActiveUse(FP_Tool<FP_MeasureToolData> tool)
        {
            if(tool != myTool) return;
        }
        public void OnToolEnding(FP_Tool<FP_MeasureToolData> tool)
        {
            if(tool != myTool) return;
        }
        public void OnToolDeactivated(FP_Tool<FP_MeasureToolData> tool)
        {
            if(tool != myTool) return;
        }
        public void UpdateTextInformation(string text)
        {
            MeasurementText.text = text;
        }
        public void UpdateTextLocation(Vector2 coordinate)
        {
            MeasurementText.rectTransform.anchoredPosition = coordinate;
        }
    }
}
