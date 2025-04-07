namespace FuzzPhyte.Tools.Connections
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class ConnectableItem : MonoBehaviour
    {
        #region Delegate and Actions
        public delegate void ConnectionAction(ConnectableItem item, ConnectionPointUnity connectionPoint, ConnectableItem targetItem, ConnectionPointUnity targetPoint);
        public ConnectionAction OnAlignmentFail;
        public ConnectionAction OnAlignmentSuccess;
        public ConnectionAction OnConnectionSuccess;
        public ConnectionAction OnConnectionFail;
        public ConnectionAction OnConnectionRemoved;
        #endregion
        [Header("Parameters")]
        public float ConnectionDistanceMax = 1.5f;
        public Transform FakePivot;
        //public Rigidbody MyRigidBody;
        [Tooltip("Are we locked into a static situation")]
        public bool ConnectedLockedInPlace { get { return connectedLockedInPlace; } }
        [Tooltip("Are we locked into a static situation")]
        [SerializeField]
        protected bool connectedLockedInPlace;
        [Space]
        [Header("VIP Partially Connected Status")]
        public bool IsPartiallyConnected = false;
        [Space]
        [SerializeField]
        public Dictionary<ConnectionPointUnity, ConnectableItem> ConnectionPairs = new Dictionary<ConnectionPointUnity, ConnectableItem>();
        public Dictionary<ConnectionPointUnity, Joint> ConnectionJoints = new Dictionary<ConnectionPointUnity, Joint>();

        
        [Tooltip("All/any actual points open for a connection")]
        public List<ConnectionPointUnity> InternalAvailableConnections = new List<ConnectionPointUnity>();
        [Tooltip("The points that are directly attached to this item")]
        [SerializeField]
        protected List<ConnectionPointUnity> myConnectionPoints = new List<ConnectionPointUnity>();
        public List<ConnectionPointUnity> MyConnectionPoints { get { return myConnectionPoints; } }
        #region New Progress
        [Space]
        [Header("Connectable Item Data")]
        [Tooltip("This should be equal to myself")]
        [SerializeField]protected GameObject RootConnectableItemRef;

        [Space]
        public Transform ColliderParent;
        public Transform ConnectionPointParent;
        public List<GameObject> MyBodyColliders = new List<GameObject>();

        [Tooltip("Visual Item")]
        public GameObject MyVisualItem;
        [SerializeField]
        protected int UniqueIndexPos;
        public int GetUniqueIndexPos { get { return UniqueIndexPos; } }
        public Dictionary<int,ConnectableData> LookupPreviousParts = new Dictionary<int, ConnectableData>();
        public Dictionary<int,List<GameObject>> LookupPreviousColliders = new Dictionary<int, List<GameObject>>();
        
        public void SetupConnectionPart(int ID,List<ConnectionPointUnity> connectionPoints)
        {
            myConnectionPoints.Clear();
            InternalAvailableConnections.Clear();
            MyConnectionPoints.AddRange(connectionPoints);
            InternalAvailableConnections.AddRange(myConnectionPoints);
            SetupData(ID);
        }
        /// <summary>
        /// We are setting up the internal data ConnectableData from the Editor inside Unity
        /// </summary>
        /// <param name="uniqueID"></param>
        protected void SetupData(int uniqueID)
        {
            UniqueIndexPos = uniqueID;
            List<Vector3> localConnectionPoints = new List<Vector3>();
            for(int i=0;i < myConnectionPoints.Count; i++)
            {
                localConnectionPoints.Add(myConnectionPoints[i].transform.localPosition);
            }
            LookupPreviousParts.Add(
                UniqueIndexPos,
                new ConnectableData(
                    RootConnectableItemRef, 
                    UniqueIndexPos, 
                    Vector3.zero,
                    localConnectionPoints,
                    ConnectionDistanceMax
                    )
                );
        }
        public void CopyIncomingData(ConnectableItem incomingItem, ConnectionPointUnity incomingConnectionPt, ConnectionPointUnity myConnectionPoint)
        {
            // move the visuals to under my visual item
            incomingItem.MyVisualItem.transform.SetParent(MyVisualItem.transform);

            // move colliders to under my parent collider
            var runningColliderList = new List<GameObject>();
            for (int i=0;i<incomingItem.MyBodyColliders.Count; i++)
            {
                var curCollider = incomingItem.MyBodyColliders[i];
                curCollider.transform.SetParent(ColliderParent);
                runningColliderList.Add(incomingItem.MyBodyColliders[i]);
            }
            LookupPreviousColliders.Add(incomingItem.GetUniqueIndexPos, runningColliderList);
            // new index value is take my nextIndexPos and add it to the incomingData index
            for (int i=0;i< incomingItem.LookupPreviousParts.Keys.Count; i++)
            {
                // next key 
                var curKey = incomingItem.LookupPreviousParts.Keys.ElementAt(i);
                var curItemValue = incomingItem.LookupPreviousParts[curKey];
                ConnectableData newData = new ConnectableData(
                    curItemValue.Prefab,
                    curItemValue.StoredUniqueIndex,
                    curItemValue.LocalPivotPosition,
                    curItemValue.LocalConnectionPointLocations,
                    curItemValue.ConnectionDistance);
                LookupPreviousParts.Add(curKey, newData);
            }
            // move connection points over to us that aren't connected
            for(int i=0; i < incomingItem.InternalAvailableConnections.Count; i++)
            {
                var curPoint = incomingItem.InternalAvailableConnections[i];
                if(curPoint.ConnectionPointStatusPt != ConnectionPointStatus.Connected)
                {
                    curPoint.transform.SetParent(ConnectionPointParent);
                    myConnectionPoints.Add(curPoint);
                }
            }
            // remove my other triggers
            myConnectionPoints.Remove(incomingConnectionPt);
            myConnectionPoints.Remove(myConnectionPoint);
            //
            // Debug all the data to confirm we are tracking what we need to track
            for(int i = 0; i < LookupPreviousParts.Keys.Count; i++)
            {
                var curKey = LookupPreviousParts.Keys.ElementAt(i);
                var curData = LookupPreviousParts[curKey];
                Debug.Log($"Key: {curKey} => {curData.Prefab.gameObject}");
            }
            // Debug connectionpoints
            for (int i = 0; i < myConnectionPoints.Count; i++)
            {
                Debug.Log($"Connection Point: {myConnectionPoints[i].name}");
            }
            // reset internal connections based on open points
            
            InternalAvailableConnections.Clear();
            for(int i = 0; i < myConnectionPoints.Count; i++)
            {
                var curPoint = myConnectionPoints[i];
                if(curPoint.ConnectionPointStatusPt != ConnectionPointStatus.Connected)
                {
                    InternalAvailableConnections.Add(curPoint);
                }
            }
        }
        #endregion
       
        
        public bool TryToMakeAconnection(ConnectableItem otherItem,ConnectionPointUnity otherPoint,ConnectionPointUnity myPoint)
        {
            //make connection
            var successConnection = MakeConnection(otherItem, otherPoint, myPoint);
            if (successConnection)
            {
                OnConnectionSuccess?.Invoke(this, myPoint, otherItem, otherPoint);
            }
            else
            {
                OnConnectionFail?.Invoke(this, myPoint, otherItem, otherPoint);
            }
            return successConnection;
        }
        /// <summary>
        /// Called from OVRPipePart after Unity Wrapper is invoked
        /// </summary>
        /// <param name="otherItem"></param>
        /// <returns></returns>
        public bool TryAlignmentOnRelease(ConnectableItem otherItem)
        {
            return TryAlignmentOnRelease(otherItem, InternalAvailableConnections);
        }
        /// <summary>
        /// For Alignment purposes
        /// </summary>
        /// <param name="otherItem"></param>
        /// <param name="availableConnections"></param>
        /// <returns></returns>
        public bool TryAlignmentOnRelease(ConnectableItem otherItem, List<ConnectionPointUnity> availableConnections)
        {
            //compare my connection points to other item connections points closest distance wins
            if (otherItem != null)
            {
                ConnectionPointUnity myClosestPoint = null;
                ConnectionPointUnity otherClosestPoint = null;
                float closestDistance = ConnectionDistanceMax;
                for (int i = 0; i < availableConnections.Count; i++)
                {
                    var curPoint = availableConnections[i];
                    for (int j = 0; j < otherItem.InternalAvailableConnections.Count; j++)
                    {
                        var otherPoint = otherItem.InternalAvailableConnections[j];
                        var distCheck = Vector3.Distance(curPoint.transform.position, otherPoint.transform.position);
                        if (distCheck < closestDistance)
                        {
                            closestDistance = distCheck;
                            myClosestPoint = curPoint;
                            otherClosestPoint = otherPoint;
                        }
                    }
                }
                if (myClosestPoint != null && otherClosestPoint != null)
                {
                    var outcome = myClosestPoint.IsCompatibleWith(otherClosestPoint, out Quaternion bestRotation);
                    Debug.Log($"Between point:{myClosestPoint.name} and other point: {otherClosestPoint.name} => Return Best Rotation {bestRotation.eulerAngles}");
                    if (outcome.Item1)
                    {
                        //align the two items
                        AlignTo(otherItem, otherClosestPoint, myClosestPoint, outcome.Item2, outcome.Item3);
                        OnAlignmentSuccess?.Invoke(this, myClosestPoint, otherItem, otherClosestPoint);
                        return true;
                    }
                    else
                    {
                        OnAlignmentFail?.Invoke(this, myClosestPoint, otherItem, otherClosestPoint);
                        return false;
                    }
                }
            }

            return false;
        }

        protected void AlignTo(ConnectableItem targetItem, ConnectionPointUnity targetPoint, ConnectionPointUnity myPoint, Vector3 dataMyVector, Vector3 dataTargetVector)
        {
            // Step 1: Calculate the forward vectors from the connection points
            Vector3 myForward = myPoint.transform.TransformDirection(myPoint.ConnectionPointData.localForward);
            // Invert targetForward to handle 180-degree misalignment
            Vector3 targetForward = targetPoint.transform.TransformDirection(-targetPoint.ConnectionPointData.localForward);
            //Vector3 targetForward = targetPoint.transform.TransformDirection(targetPoint.connectionPointData.localForward);

            // Convert the local data vectors to world space
            Vector3 myDataWorld = myPoint.transform.TransformDirection(dataMyVector);
            Vector3 targetDataWorld = targetPoint.transform.TransformDirection(dataTargetVector);

            // Step 2: Compute the cross products to determine the normal vectors
            Vector3 myCross = Vector3.Cross(myDataWorld, myForward).normalized;
            Vector3 targetCross = Vector3.Cross(targetDataWorld, targetForward).normalized;

            // Step 3: Calculate the cross product of the two normal vectors
            Vector3 rotationAxis = Vector3.Cross(myCross, targetCross).normalized;

            // Step 4: Compute the angle required to align these normal vectors
            float angle = Vector3.SignedAngle(myCross, targetCross, rotationAxis);

            // Step 5: Create a rotation that aligns myPoint with targetPoint on the first axis
            Quaternion alignmentRotation1 = Quaternion.AngleAxis(angle, rotationAxis);

            // Step 6: Reparent the object to the Fake Pivot
            FakePivot.transform.position = myPoint.transform.position;
            FakePivot.transform.rotation = myPoint.transform.rotation;
            FakePivot.SetParent(null); // Ensure the pivot is in world space
            this.transform.SetParent(FakePivot.transform);

            // Step 7: Apply the first alignment rotation to the Fake Pivot
            FakePivot.transform.rotation = alignmentRotation1 * FakePivot.transform.rotation;

            // Step 8: Recalculate the forward vectors after the first rotation
            myForward = myPoint.transform.TransformDirection(myPoint.ConnectionPointData.localForward);
            targetForward = targetPoint.transform.TransformDirection(-targetPoint.ConnectionPointData.localForward); // Keep it inverted
            myDataWorld = myPoint.transform.TransformDirection(dataMyVector);
            targetDataWorld = targetPoint.transform.TransformDirection(dataTargetVector);

            // Step 9: Calculate the second rotation required to align the remaining axis
            Vector3 newRotationAxis = Vector3.Cross(myForward, targetForward).normalized;
            float newAngle = Vector3.SignedAngle(myForward, targetForward, newRotationAxis);

            Quaternion alignmentRotation2 = Quaternion.AngleAxis(newAngle, newRotationAxis);
            
            // Apply the second rotation
            FakePivot.transform.rotation = alignmentRotation2 * FakePivot.transform.rotation;

            // Step 10: Adjust the position of the Fake Pivot to align myPoint with targetPoint
            Vector3 positionOffset = targetPoint.transform.position - myPoint.transform.position;
            FakePivot.transform.position += positionOffset;
            // Step 11: flip 180 around forward axis due to inverse conditions of targetForward
            Vector3 pointForward = myPoint.transform.TransformDirection(myPoint.ConnectionPointData.localForward);
            FakePivot.transform.rotation = Quaternion.AngleAxis(180f, pointForward) * FakePivot.transform.rotation;
            
            // Step 12: Unparent the object from the Fake Pivot
            this.transform.SetParent(null);
            //reparent
            FakePivot.SetParent(this.transform);

            // Step 13: Update the connection point status for both
            myPoint.AddAlignmentPoint(targetPoint);
            targetPoint.AddAlignmentPoint(myPoint);
        }
        
        /// <summary>
        /// I am becoming part of the targetItem
        /// </summary>
        /// <param name="targetItem"></param>
        /// <param name="targetPoint"></param>
        /// <param name="myPoint"></param>
        /// <param name="updatePair"></param>
        public bool MakeConnection(ConnectableItem targetItem, ConnectionPointUnity targetPoint, ConnectionPointUnity myPoint)
        {
            // make the connection permanent
            // make sure to close the connection points
            // reparent the item
            if (!ConnectionPairs.ContainsKey(myPoint))
            {
                // double check we are able to do this
                if(!targetItem.RequestedConnection(targetPoint))
                {
                    Debug.LogError($"Connection was not made between {myPoint.name} and {targetPoint.name}");
                    return false;
                }
                // remove my point from the list of connection points that I have left open
                InternalAvailableConnections.Remove(myPoint);
                // update the connection points
                myPoint.AddConnectionPoint(targetPoint);
                targetPoint.AddConnectionPoint(myPoint);
               
                ConnectionPairs.Add(myPoint, targetItem);

                //are we statically locked?
                //check our parent or targetItem in this case
                if (targetItem.ConnectedLockedInPlace)
                {
                    //then we are
                    connectedLockedInPlace = true;
                }
                //OnConnectionMade?.Invoke(this, myPoint, targetItem, targetPoint);
                return true;
            }
            else
            {
                Debug.LogError($"My connection point is already in the dictionary... {myPoint.name}");
                return false;
            }
        }
        /// <summary>
        /// called from another ConnectableItem to update/notify that a connection has been requested/made
        /// </summary>
        /// <param name="myPoint"></param>
        public bool RequestedConnection(ConnectionPointUnity myPoint)
        {
            //check this point and remove it from my AvailableConnections
            if (InternalAvailableConnections.Contains(myPoint))
            {
                InternalAvailableConnections.Remove(myPoint);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Not finished
        /// </summary>
        /// <param name="pointToFree"></param>
        public void RemoveConnection(ConnectionPointUnity pointToFree)
        {
            
            //John still need code here

            if (ConnectionPairs.ContainsKey(pointToFree))
            {
                var otherItem = ConnectionPairs[pointToFree];
               
                
                // need to check static conditions now?

                // remove a joint?

                // dictionary clean up
                ConnectionJoints.Remove(pointToFree);
                ConnectionPairs.Remove(pointToFree);

                OnConnectionRemoved?.Invoke(this, pointToFree, otherItem, pointToFree.OtherConnection);
            }
            
        }
        
        public void RequestedDisconnect(ConnectableItem theItem)
        {
            
        }

        /// <summary>
        /// Returns a List of Open or !IsConnected Connection Points
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// Recursively retrieves a list of open ConnectionPointUnity objects, including those from connected items.
        /// </summary>
        public List<ConnectionPointUnity> GetOpenConnectionPoints()
        {
            // Get the open connection points from the current item
            List<ConnectionPointUnity> openConnectionPoints = myConnectionPoints
                .Where(point => point.ConnectionPointStatusPt != ConnectionPointStatus.Connected)
                .ToList();

            // Recursively get open connection points from connected items
            foreach (var connectedPoint in myConnectionPoints.Where(point => point.ConnectionPointStatusPt == ConnectionPointStatus.Connected))
            {
                if (ConnectionPairs.TryGetValue(connectedPoint, out ConnectableItem connectedItem))
                {
                    // Recursively get open connection points from the connected item
                    openConnectionPoints.AddRange(connectedItem.GetOpenConnectionPoints());
                }
            }

            return openConnectionPoints;
        }
    }
}
