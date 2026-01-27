namespace FuzzPhyte.Tools
{
    using UnityEngine;
    using FuzzPhyte.Utility;
    [CreateAssetMenu(fileName = "FP Equipment Data", menuName = "FuzzPhyte/Tools/Equipment", order = 20)]
    public class FP_EquipmentData:FP_Data
    {
        public string DisplayName;

        [Header("Capabilities")]
        public bool SupportsTimer;
        public bool SupportsFill;     // sink/kettle/blender
        public bool SupportsHeat;     // burner/oven/toaster
        public int SubUnitCount;      // stove burners count, etc.

        [Header("Fill/Capacity")]
        public float MaxVolumeLiters = 1f;

        [Header("Heat")]
        public float MaxHeatLevel = 1f; // normalized scale mapping
    }
}
