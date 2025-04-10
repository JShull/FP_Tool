namespace FuzzPhyte.Tools.Connections
{
    using UnityEngine;

    /// <summary>
    /// placeholder script for the equivalent of a "weld" or fixed lock
    /// </summary>
    public class ConnectionFixed : MonoBehaviour
    {
        [Space]
        public ConnectionPointUnity FixedAssociatedConnectionPoint;
        //will probably need an FP_Tool passed here
        public delegate void FixedInteractionDelegate(ConnectionPointUnity ptData);
        public FixedInteractionDelegate OnPartConnectionFixedToolFinished;
        public bool IsFixedDown;
        public void LockedDownConnection()
        {

        }
    }
}
