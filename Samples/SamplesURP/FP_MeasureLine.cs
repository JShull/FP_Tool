namespace FuzzPhyte.Tools.Samples
{
    using UnityEngine;
    using FuzzPhyte.Tools;
    using TMPro;
    using System.Collections.Generic;
    using FuzzPhyte.Utility;

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
        protected RectTransform workSpace;
        protected Camera myCamera;
        protected Canvas canvas;
        /// <summary>
        /// Setup the prefab, we need the data and the work space our 2D tool will be in
        /// </summary>
        /// <param name="theTool">The data</param>
        /// <param name="rectPanel">Work space for the 2D tool</param>
        public void SetupLine(FP_Tool<FP_MeasureToolData> theTool, RectTransform rectPanel, Canvas mainCanvas,Camera mainCamera,FontSetting fontDetails,int lineRenderOrder=10)
        {
            workSpace = rectPanel;
            myCamera = mainCamera;
            canvas = mainCanvas;
            myTool = theTool;
            LineRenderer.positionCount = 2;
            //testing local space
            LineRenderer.useWorldSpace = false;
            LineRenderer.sortingOrder = lineRenderOrder;
            Material cachedMat = theTool.ToolData.LineMat;
            cachedMat.color = theTool.ToolData.lineColor;
            
            List<Material> materials = new List<Material>();
            materials.Add(cachedMat);
            LineRenderer.SetMaterials(materials);
            LineRenderer.startColor = theTool.ToolData.lineColor;
            LineRenderer.endColor = theTool.ToolData.lineColor;
            LineRenderer.startWidth = theTool.ToolData.lineWidth;
            FP_UtilityData.ApplyFontSetting(MeasurementText,fontDetails);
            //now listen in
            myTool.OnActivated += OnToolActivated;
            myTool.OnStarting += OnToolStarting;
            myTool.OnActiveUse += OnToolInActiveUse;
            myTool.OnEnding += OnToolEnding;
            myTool.OnDeactivated += OnToolDeactivated;
        }

        public virtual void DropFirstPoint(Vector2 screenPosition)
        {
            //convert the pixel space to world space
            /*
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            Input.mousePosition, parentCanvas.worldCamera,
            out movePos);
        Vector3 positionToReturn = parentCanvas.transform.TransformPoint(movePos);
        positionToReturn.z = parentCanvas.transform.position.z - 0.01f;
            */
            //Vector2 aPosition = RectTransformUtility.ScreenPointToLocalPointInRectangle(workSpace, screenPosition, myCamera, out Vector2 somePos) ? somePos : Vector2.zero;
            FirstPoint.localPosition = screenPosition;
            FirstPoint.gameObject.SetActive(true);
            SecondPoint.gameObject.SetActive(true);
            var depthAdj = new Vector3(screenPosition.x, screenPosition.y,-1f);
            //Vector3 worldPos = workSpace.transform.TransformPoint(aPosition);
            LineRenderer.SetPosition(0, depthAdj);
            LineRenderer.SetPosition(1, depthAdj);
        }
        public virtual void DropSecondPoint(Vector2 screenPosition)
        {
            SecondPoint.localPosition = screenPosition;
            var depthAdj = new Vector3(screenPosition.x, screenPosition.y,-1f);
            //Vector2 aPosition = RectTransformUtility.ScreenPointToLocalPointInRectangle(workSpace, screenPosition, myCamera, out Vector2 somePos) ? somePos : Vector2.zero;
            //Vector3 worldPos = workSpace.transform.TransformPoint(aPosition);
            LineRenderer.SetPosition(1, depthAdj);
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
