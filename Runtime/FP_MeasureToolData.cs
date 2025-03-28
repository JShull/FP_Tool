namespace FuzzPhyte.Tools
{
    using FuzzPhyte.Utility;
    using UnityEngine;

    [CreateAssetMenu(fileName = "FP_MeasureToolData", menuName = "FuzzPhyte/Tools/FPMeasureData")]
    public class FP_MeasureToolData : FP_Data
    {
        public Color lineColor = Color.green;
        public Material LineMat;
        public float lineWidth = 0.02f;
        public float measurementPrecision = 2f; // Decimal places
    }
}
