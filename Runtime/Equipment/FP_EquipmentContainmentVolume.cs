namespace FuzzPhyte.Tools
{
    using UnityEngine;
    [RequireComponent(typeof(Collider))]
    public class FP_EquipmentContainmentVolume : MonoBehaviour
    {
        [SerializeField] private FP_EquipmentBase equipment;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<IFPContainedItem>(out var item))
                equipment.AddItemToEquipment(item);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<IFPContainedItem>(out var item))
                equipment.RemoveItemFromEquipment(item);
        }
    }
}
