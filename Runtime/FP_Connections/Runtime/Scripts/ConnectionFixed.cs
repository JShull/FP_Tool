// Copyright (c) 2026 John B. Shull
// FuzzPhyte LLC is a company associated with John B. Shull
// This file is part of FP_Tool Package.
//
// Public license: GNU GPLv3-or-later.
// Commercial/proprietary use requires a separate license from John B. Shull.
//
// See LICENSE.md COMMERCIAL-LICENSE.md, and NOTICE.md.

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
