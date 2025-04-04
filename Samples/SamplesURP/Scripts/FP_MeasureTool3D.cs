namespace FuzzPhyte.Tools.Samples
{

    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using FuzzPhyte.Utility;
    using UnityEngine.UI;
    using System.Collections;

    /// <summary>
    /// This class is using the Unity UI Interfaces like IDrag/IPoint/etc. which means we need a canvas/RectTransform to work with
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FP_MeasureTool3D : FP_Tool<FP_MeasureToolData>, IFPUIEventListener<FP_Tool<FP_MeasureToolData>>
    {
        public Transform ParentDecals;
        public GameObject FirstPointObjectTest;
        public GameObject SecondPointObjectTest;

        [Header("Internal parameters")]
        protected Vector3 startPosition = Vector3.zero;
        protected Vector3 endPosition = Vector3.zero;
        [SerializeField] FP_MeasureLine3D currentActiveLine;
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
        /// <summary>
        /// Process Event Data and pass it to the tool
        /// </summary>
        /// <param name="eventData"></param>
        public void OnUIEvent(FP_UIEventData<FP_Tool<FP_MeasureToolData>> eventData)
        {

        }
        public void PointerDrag(PointerEventData eventData)
        {
           
        }

        public void PointerDown(PointerEventData eventData)
        {
           
        }

        public void PointerUp(PointerEventData eventData)
        {
           
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

                Vector3 midPoint = (startPosition + endPosition) * 0.5f;
                Vector3 direction = (endPosition - startPosition).normalized;
                Vector3 upRef = Vector3.up;
                if (Vector3.Dot(direction, upRef) > 0.99f) // Avoid parallel up vector
                    upRef = Vector3.right; // Switch to right axis

                Vector3 perpendicular = Vector3.Cross(direction, upRef).normalized;
              
                // Offset
                Vector3 offset = perpendicular * toolData.measurementLabelOffsetPixels.y +
                                 direction * toolData.measurementLabelOffsetPixels.x;

                // Final position
                Vector3 labelPosition = midPoint + offset;
                // Apply offset relative to perpendicular direction
               
                currentActiveLine.UpdateTextLocation(toolData.measurementLabelOffsetPixels);
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                //currentActiveLine.SetTextRotation(direction);
            }
        }
    }
}
