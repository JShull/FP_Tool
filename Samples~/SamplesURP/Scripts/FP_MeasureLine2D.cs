namespace FuzzPhyte.Tools.Samples
{
    using UnityEngine;
    using FuzzPhyte.Tools;
    using TMPro;
    using System.Collections.Generic;
    using FuzzPhyte.Utility;
    using UnityEngine.UI;
    using UnityEngine.Rendering;

    /// <summary>
    /// Used to store information on deployed 'measure line'
    /// Should be part of a root transform prefab
    /// </summary>
    public class FP_MeasureLine2D : FPTwoPointLine<FP_MeasureToolData>
    {
        public RectTransform FirstPoint;
        public RectTransform SecondPoint;
        public TextMeshProUGUI MeasurementText;
        public LineRenderer LineRenderer;
        protected RectTransform workSpace;
        protected Camera myCamera;
        protected Canvas canvas;
        public override void Setup(FP_Tool<FP_MeasureToolData> tool)
        {
            myTool = tool;
            Material cachedMat = myTool.ToolData.LineMat;
            cachedMat.color = myTool.ToolData.lineColor;
            List<Material> materials = new List<Material>();
            materials.Add(cachedMat);
            LineRenderer.SetMaterials(materials);
            LineRenderer.startColor = myTool.ToolData.lineColor;
            LineRenderer.endColor = myTool.ToolData.lineColor;
            LineRenderer.startWidth = myTool.ToolData.lineWidth;
            //myTool = theTool;
            LineRenderer.positionCount = 2;
            //testing local space
            LineRenderer.useWorldSpace = false;
            var pointOneImage = FirstPoint.gameObject.GetComponent<Image>();
            var pointTwoImage = SecondPoint.gameObject.GetComponent<Image>();
            if (pointOneImage)
            {
                pointOneImage.color = myTool.ToolData.lineColor;
            }
            if (pointTwoImage)
            {
                pointTwoImage.color = myTool.ToolData.lineColor;
            }
            FP_UtilityData.ApplyFontSetting(MeasurementText, tool.ToolData.MeasurementFontSetting);
            //now listen in
            myTool.OnActivated += OnToolActivated;
            myTool.OnStarting += OnToolStarting;
            myTool.OnActiveUse += OnToolInActiveUse;
            myTool.OnEnding += OnToolEnding;
            myTool.OnDeactivated += OnToolDeactivated;
        }
        /// <summary>
        /// Setup the additional components for our 2D line
        /// </summary>
        /// <param name="rectPanel">The panel to keep tabs on</param>
        /// <param name="mainCanvas">Our canvas we are operating in</param>
        /// <param name="mainCamera">The camera needed for projection/calculations</param>
        /// <param name="lineRenderOrder">depth starting order for our first line</param>
        public void SetupLine(RectTransform rectPanel, Canvas mainCanvas,Camera mainCamera,int lineRenderOrder=10)
        {
            workSpace = rectPanel;
            myCamera = mainCamera;
            canvas = mainCanvas;
            LineRenderer.sortingOrder = lineRenderOrder;
            if(FirstPoint!=null && SecondPoint != null)
            {
                var pointOneSort = FirstPoint.gameObject.GetComponent<SortingGroup>();
                var pointTwoSort = SecondPoint.gameObject.GetComponent<SortingGroup>();
                if (pointOneSort)
                {
                    pointOneSort.sortingOrder = lineRenderOrder + 1;
                }
                if (pointTwoSort)
                {
                    pointTwoSort.sortingOrder = lineRenderOrder + 1;
                }
            }
        }

        public override void DropFirstPoint(Vector3 screenPosition)
        {
            FirstPoint.localPosition = screenPosition;
            
            FirstPoint.gameObject.SetActive(true);
            SecondPoint.gameObject.SetActive(true);
            var depthAdj = new Vector3(screenPosition.x, screenPosition.y,-1f);
            //Vector3 worldPos = workSpace.transform.TransformPoint(aPosition);
            LineRenderer.SetPosition(0, depthAdj);
            LineRenderer.SetPosition(1, depthAdj);
        }
        public override void DropSecondPoint(Vector3 screenPosition)
        {
            SecondPoint.localPosition = screenPosition;
            var depthAdj = new Vector3(screenPosition.x, screenPosition.y,-1f);
            //Vector2 aPosition = RectTransformUtility.ScreenPointToLocalPointInRectangle(workSpace, screenPosition, myCamera, out Vector2 somePos) ? somePos : Vector2.zero;
            //Vector3 worldPos = workSpace.transform.TransformPoint(aPosition);
            LineRenderer.SetPosition(1, depthAdj);
        }
        
        public override void OnToolActivated(FP_Tool<FP_MeasureToolData> tool)
        {
            if(tool != myTool) return;
        }
        public override void OnToolStarting(FP_Tool<FP_MeasureToolData> tool)
        {
            if(tool != myTool) return;
        }
        public override void OnToolInActiveUse(FP_Tool<FP_MeasureToolData> tool)
        {
            if(tool != myTool) return;
        }
        public override void OnToolEnding(FP_Tool<FP_MeasureToolData> tool)
        {
            if(tool != myTool) return;
        }
        public override void OnToolDeactivated(FP_Tool<FP_MeasureToolData> tool)
        {
            if(tool != myTool) return;
        }
        public override void UpdateText(string text)
        {
            MeasurementText.text = text;
        }
        public void UpdateTextLocation(Vector2 coordinate)
        {
            MeasurementText.rectTransform.anchoredPosition = coordinate;
        }
        public void SetTextRotation(float angle)
        {
            if (angle > 90f || angle < -90f)
            {
                angle += 180f;
            }
            MeasurementText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
