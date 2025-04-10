using FuzzPhyte.Utility;
using UnityEngine;

namespace FuzzPhyte.Tools.Samples
{
    [CreateAssetMenu(fileName = "FP_AttachToolData", menuName = "FuzzPhyte/Tools/FPAttachData")]
    public class FP_AttachToolData : FP_Data
    {
        public GameObject AttachVisual;
        public float RaycastMax = 15;
    }
}
