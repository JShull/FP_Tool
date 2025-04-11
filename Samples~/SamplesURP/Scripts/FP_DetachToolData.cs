namespace FuzzPhyte.Tools.Samples
{
    using UnityEngine;
    using FuzzPhyte.Utility;
    [CreateAssetMenu(fileName = "FP_DetachToolData", menuName = "FuzzPhyte/Tools/FPDetachData")]
    public class FP_DetachToolData : FP_Data
    {
        public float RaycastMax = 15;
    }
}