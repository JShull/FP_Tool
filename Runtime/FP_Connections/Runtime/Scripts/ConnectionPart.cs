namespace FuzzPhyte.Tools.Connections
{
    using FuzzPhyte.Utility;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    /// <summary>
    /// Responsible to manage the Pointer Event Coming in
    /// OVRPipePart
    /// </summary>
    public class ConnectionPart : FP_Tool<PartData>, IFPUIEventListener<FP_Tool<PartData>>
    {
        [Header("Part Related")]
        public bool UseSimpleAlign=true;
        //public GameObject FixedAttachedPrefab;
        [Tooltip("The reference transform we might attach another connected part to later for attachment/removal")]
        public Transform FixedAttachedParent;
        [Tooltip("The reference transform we are going to nest our connection points under")]
        public Transform ConnectionPtParent;
        [SerializeField]
        protected int UniqueIndexPos;
        public int GetUniqueIndexPos { get { return UniqueIndexPos; } }
        [Space]
        [Header("Unity Events")]
        public UnityEvent OnAlignmentSuccessEvent;
        public UnityEvent OnTriggerEnterUnityEvent;
        public UnityEvent OnTriggerExitUnityEvent;
        public UnityEvent OnPartLockedEvent;
        public UnityEvent OnPartUnlockedEvent;
        [Tooltip("List of ConnectionToolTrigger listeners that we're monitoring/responsible for")]
        protected List<ConnectionToolTrigger> ConnectionPointTriggersListeners = new List<ConnectionToolTrigger>();
        public Dictionary<ConnectionPointUnity, ConnectionToolTrigger> ConnectionPointsTriggersLookUp = new Dictionary<ConnectionPointUnity, ConnectionToolTrigger>();
        [SerializeField]
        [Tooltip("Hold a list of bolts by ConnectionPointUnity")]
        protected Dictionary<ConnectionPointUnity, List<ConnectionFixed>> WeldsByPoint = new Dictionary<ConnectionPointUnity, List<ConnectionFixed>>();
        [Tooltip("Hold a Connectable Item by a Point")]
        protected Dictionary<ConnectionPointUnity, ConnectionPart> PossibleTargetByPoint = new Dictionary<ConnectionPointUnity, ConnectionPart>();
        [Tooltip("Match the ConnectionPointUnity with the Target ConnectionPointUnity")]
        protected Dictionary<ConnectionPointUnity, ConnectionPointUnity> AlignmentConnectionPointPair = new Dictionary<ConnectionPointUnity, ConnectionPointUnity>();
        public virtual void Awake()
        {
            //build out the connectionPointData from our PartData
            ConnectionPointTriggersListeners.Clear();
            List<ConnectionPointUnity> cachedPoints = new List<ConnectionPointUnity>();
            for (int i = 0; i < toolData.AllConnectionPointsForPart.Count; i++)
            {
                var curPointData = toolData.AllConnectionPointsForPart[i];
              
                GameObject prefabSpawned = null;
                //we use the ConnectionPointData prefab if we have one or we use one that is defined in the toolData for all
                if (curPointData.ConnectionEndPrefab != null)
                {
                    prefabSpawned = GameObject.Instantiate(curPointData.ConnectionEndPrefab, ConnectionPtParent);
                }
                else
                {
                    prefabSpawned = GameObject.Instantiate(toolData.ConnectionEndPrefab, ConnectionPtParent);
                }

                //get component
                var cpu = prefabSpawned.GetComponent<ConnectionPointUnity>();
                var fpCollide = prefabSpawned.GetComponent<FP_CollideItem>();
                var myFPMoveRotate = this.GetComponent<FP_MoveRotateItem>();
                if (cpu != null)
                {
                    prefabSpawned.gameObject.name = curPointData.connectionType.ToString()+"_"+i;
                    cpu.SetupDataFromDataFile(this, curPointData);
                    cpu.ConnectionPointStatusPt = ConnectionPointStatus.None;
                    if (cpu.MyToolTriggerRef != null)
                    {
                        ConnectionPointTriggersListeners.Add(cpu.MyToolTriggerRef);
                        ConnectionPointsTriggersLookUp.Add(cpu, cpu.MyToolTriggerRef);
                    }
                    else
                    {
                        Debug.LogError($"CPU Tool trigger ref is missing");
                    }
                    cachedPoints.Add(cpu);
                }
                else
                {
                    Debug.LogError($"This needs a ConnectionPointUnity script");
                }
                if (fpCollide != null && myFPMoveRotate!=null)
                {
                    fpCollide.MoveRotateItem = myFPMoveRotate;
                }
            }
            
            myConnectionPoints.Clear();
            InternalAvailableConnections.Clear();
            MyConnectionPoints.AddRange(cachedPoints);
            InternalAvailableConnections.AddRange(myConnectionPoints);
            //SetupData(toolData.UniquePartID);
            UniqueIndexPos = toolData.UniquePartID;
            List<Vector3> localConnectionPoints = new List<Vector3>();
            for(int i=0;i < myConnectionPoints.Count; i++)
            {
                localConnectionPoints.Add(myConnectionPoints[i].transform.localPosition);
            }
            LookupPreviousParts.Add
            (
                UniqueIndexPos,
                new ConnectableData(
                    RootConnectableItemRef, 
                    UniqueIndexPos, 
                    Vector3.zero,
                    localConnectionPoints,
                    ConnectionDistanceMax
                    )
            );
            //do we have a fake pivot for alignment needs?
            if(FakePivot == null)
            {
                Debug.LogError($"FakePivot is null, creating a new one for you.");
                FakePivot = new GameObject("FakePivot").transform;
                FakePivot.transform.SetParent(this.transform);
                FakePivot.transform.localPosition = Vector3.zero;
                FakePivot.gameObject.layer = this.gameObject.layer; // Set the layer of the Fake Pivot to match this object
            }
        }
        public virtual void OnEnable()
        {
            foreach (var cp in ConnectionPointTriggersListeners)
            {
                cp.OnPartTriggerEnterAction += OnConnectionPointTriggerEnter;
                cp.OnPartTriggerExitAction += OnConnectionPointTriggerExit;
                cp.gameObject.name = $"{GetUniqueIndexPos}_{cp.gameObject.name}";
            }
        }
        public virtual void OnDisable()
        {
            foreach (var cp in ConnectionPointTriggersListeners)
            {
                cp.OnPartTriggerEnterAction -= OnConnectionPointTriggerEnter;
                cp.OnPartTriggerExitAction -= OnConnectionPointTriggerExit;
            }
        }
        
        #region State Actions & OnUIEvent
        /// <summary>
        /// This just sets our state up for being ready to use the tool
        /// </summary>
        public override bool ActivateTool()
        {
            //check lock state before we go any further
            if (CurrentState== FPToolState.Locked)
            {
                return false;
            }
            if (base.ActivateTool())
            {
                ToolIsCurrent = true;
                return true;
            }
            return false;
        }
        public override bool DeactivateTool()
        {
            if (base.DeactivateTool())
            {
                ToolIsCurrent = false;
                return true;
            }
            return false;
        }
        public override bool LockTool()
        {
            if(base.LockTool())
            {
                ToolIsCurrent = false;
                OnPartLockedEvent.Invoke();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Dont call this function from external sources
        /// </summary>
        /// <returns></returns>
        public override bool UnlockTool()
        {
            //check our conditions
            if(base.UnlockTool())
            {
                OnPartUnlockedEvent.Invoke();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Called from the actual moving 'Tool'
        /// </summary>
        /// <param name="eventData"></param>
        public void OnUIEvent(FP_UIEventData<FP_Tool<PartData>> eventData)
        {
            switch (eventData.EventType)
            {
                case FP_UIEventType.PointerDown:
                    PointerDown(eventData.UnityPointerEventData);
                    break;
                case FP_UIEventType.PointerUp:
                    PointerUp(eventData.UnityPointerEventData);
                    break;
                case FP_UIEventType.Drag:
                    break;
            }
        }
        /// <summary>
        /// Unity Event Wrapper 
        /// whatever is managing our input (mouse down?) will need to call this and pass
        /// the pointer data
        /// Haptic call is made here
        /// </summary>
        /// <param name="pointerEvent"></param>
        public void PointerDown(PointerEventData eventData)
        {
            //throw new System.NotImplementedException();
            if (ActivateTool())
            {
                if (StartTool())
                {
                    Debug.Log($"{this.gameObject.name}: ConnectionPart.cs Pointer DOWN");
                }
            }
        }
        /// <summary>
        /// Unity Event Wrapper tied to the PointerEventData class
        /// E.g. when we 'drop' our part we want to check for a connection
        /// </summary>
        /// <param name="pointerEvent"></param>
        public void PointerUp(PointerEventData eventData)
        {
            if(!ToolIsCurrent)
            {
                return;
            }
            Debug.Log($"{this.gameObject.name}: Pointer UP");
            // see if we had a possible pipe target
            // possible target comes in from the trigger events
            // loop through our dictionary of possible targets
            Debug.Log($"{this.gameObject.name}: ConnectionPart.cs Pointer UP");
            var possibleTargets = PossibleTargetByPoint.Keys.ToList();
            List<ConnectionPart> allPossibleTargets = new List<ConnectionPart>();
            Debug.LogWarning($"Possible cached Targets Count: {possibleTargets.Count} for {this.gameObject.name} with {PossibleTargetByPoint.Count} items in the dictionary");
            for (int i = 0; i < possibleTargets.Count; i++)
            {
                var aConnectionPoint = possibleTargets[i];
                Debug.LogWarning($"A possible connection point: {aConnectionPoint.gameObject.name}");
                var aConnectableItem = PossibleTargetByPoint[aConnectionPoint];
                if (aConnectableItem != null)
                {
                    Debug.LogWarning($"A possible connectable item: {aConnectableItem.gameObject.name}, going to add this to my cached possible target list");
                    allPossibleTargets.Add(aConnectableItem);
                }
            }
            //possible targets always just grab the first

            //standard singular case
            if(allPossibleTargets.Count>0)
            {
                var theTarget = allPossibleTargets[0];
                if (theTarget != null)
                {
                    //build up all possible points based on current connection points and connected items
                    //var allOpenPossiblePoints = ThePipe.GetOpenConnectionPoints();
                        
                    var greatSuccess = TryAlignmentOnRelease(theTarget, InternalAvailableConnections);
                    if (greatSuccess)
                    {
                        Debug.LogWarning($"Pipe Grab Alignment worked!");
                        // change state
                    }
                    else
                    {
                        Debug.LogWarning($"Pipe Grab Alignment Failed!");
                    }
                }
                else
                {
                    Debug.LogError($"The target is null, we are not able to connect to anything");
                }
            }
            
            DeactivateTool();
        }
        public void PointerDrag(PointerEventData eventData)
        {
            if (!ToolIsCurrent)
            {
                return;
            }
            if (UseTool())
            {
                //Debug.Log($"{this.gameObject.name}: ConnectionPart.cs Pointer DRAG");
            }
        }
        public void ResetVisuals()
        {
            //do nothing
        }
        #endregion
        
        #region Callbacks
    
        private void ResetDestroyBoltsAfterMoving(ConnectionPointUnity connectionPt)
        {
            if (WeldsByPoint.ContainsKey(connectionPt))
            {
                var boltsByConnectionPoint = WeldsByPoint[connectionPt];
                List<GameObject> boltsToDestroy = new List<GameObject>();
                for (int i = 0; i < boltsByConnectionPoint.Count; i++)
                {
                    var aBolt = boltsByConnectionPoint[i];
                    aBolt.OnPartConnectionFixedToolFinished -= OnFixedWeldedToolEnd;
                    boltsToDestroy.Add(aBolt.gameObject);
                }
                WeldsByPoint.Remove(connectionPt);
                foreach (var deadBolt in boltsToDestroy)
                {
                    Destroy(deadBolt);
                }
            }
            else
            {
                Debug.LogError($"Missing the Connection Pt {connectionPt.gameObject.name} in our Bolt Dictionary");
            }
        }
        private void OnFixedWeldedToolEnd(ConnectionPointUnity ptData)
        {
            //check number of bolts being finished or locked in?
            //if just one bolt is finished then we activate the connection in
            int fixedWelds = 0;

            Debug.LogWarning($"Wrench Bolt Tool Ended Callback by {gameObject.name}: with the passed CPUnity {ptData.gameObject.name}, does my dictionary exist? Dictionary has how many? {WeldsByPoint.Count}");
            if (WeldsByPoint.ContainsKey(ptData))
            {
                Debug.LogWarning($"Dictionary has the key!");
                var boltsByConnectionPoint = WeldsByPoint[ptData];
                Debug.LogWarning($"Dictionary has '{boltsByConnectionPoint.Count}' bolts for the {ptData.gameObject.name} connection point");
                for (int i = 0; i < boltsByConnectionPoint.Count; i++)
                {
                    var aBolt = boltsByConnectionPoint[i];
                    if (aBolt.IsFixedDown)
                    {
                        fixedWelds++;
                    }
                }
                Debug.LogWarning($"Number of bolts down: {fixedWelds} for {this.gameObject.name}");
                //PossibleTargetByPoint have the SpawnedBoltsByPoint Key?
                var possibleTargets = PossibleTargetByPoint.Keys.ToList();
                for (int i = 0; i < possibleTargets.Count; i++)
                {
                    var curKey = possibleTargets[i];
                    Debug.LogWarning($"Target Possible with key {curKey}? ==> |Value: {PossibleTargetByPoint[curKey].gameObject.name}");
                }
                if (PossibleTargetByPoint.ContainsKey(ptData))
                {
                    var thePossibleConnection = AlignmentConnectionPointPair[ptData];
                    var thePossibleTarget = PossibleTargetByPoint[ptData];
                    if (fixedWelds >= 1 && !thePossibleTarget.IsPartiallyConnected)
                    {
                        //lock it down via Activate and become one with the other item and deactivate the bolt triggers
                        //ThePipe.IsPartiallyConnected = true;
                        thePossibleTarget.IsPartiallyConnected = true;
                        // very important IsPartiallyConnected will make sure we only run this code once
                        TryToMakeAconnection(thePossibleTarget, thePossibleConnection, ptData);
                    }
                    if (fixedWelds >= boltsByConnectionPoint.Count)
                    {
                        foreach (var aBolt in boltsByConnectionPoint)
                        {
                            aBolt.LockedDownConnection();
                        }
                    }
                }
            }
        }
        #endregion
        IEnumerator DestroyOneFrameLater(GameObject theOBJToDestroy)
        {
            yield return new WaitForFixedUpdate();
            theOBJToDestroy.SetActive(false);
            yield return new WaitForEndOfFrame();
            Destroy(theOBJToDestroy, Time.fixedDeltaTime);
        }
        #region Callbacks for Trigger Events Tied to ConnectionPointUnity
        protected void OnConnectionPointTriggerEnter(Collider item, ConnectionPointUnity myPoint, ConnectionPointUnity otherPoint)
        {
            Debug.LogWarning($"This {this.gameObject.name} is performing a Callback--> Connection Point Trigger Enter: {item.gameObject.name} via myPoint {myPoint.gameObject.name} with other point named: {otherPoint.gameObject.name}");
            if (otherPoint.TheConnectionPart != null)
            {
                if (PossibleTargetByPoint.ContainsKey(myPoint))
                {
                    //do nothing because we already have something as a possible target by my CPU point
                    //PossibleTargetByPoint[myPoint] = otherPoint.TheConnectionPart;
                }
                else
                {
                    PossibleTargetByPoint.Add(myPoint, otherPoint.TheConnectionPart);
                    OnTriggerEnterUnityEvent.Invoke();
                    Debug.LogWarning($"This {this.gameObject.name} is adding Dictionary target point: {myPoint.gameObject.name} with {otherPoint.TheConnectionPart.gameObject.name}");
                }
                //sync with Alignment AlignmentConnectionPointPair
                if (AlignmentConnectionPointPair.ContainsKey(myPoint))
                {
                    //if I already have a pair then I want to do nothing else.
                    //AlignmentConnectionPointPair[myPoint] = otherPoint;
                }
                else
                {
                    AlignmentConnectionPointPair.Add(myPoint, otherPoint);
                    Debug.LogWarning($"This {this.gameObject.name} is adding Alignment Connection Point Pair: {myPoint.gameObject.name} with {otherPoint.gameObject.name}");
                }
            }
        }
        protected void OnConnectionPointTriggerExit(Collider item, ConnectionPointUnity myPoint, ConnectionPointUnity otherPoint)
        {
            Debug.LogWarning($"This {this.gameObject.name} is performing a Callback--> Connection Point Trigger Exit: {item.gameObject.name} via myPoint {myPoint.gameObject.name} with other point named: {otherPoint.gameObject.name}");
            if (item.gameObject.GetComponent<ConnectionPointUnity>() != null)
            {
               
                //var cpu = item.gameObject.GetComponent<ConnectionPointUnity>();
                if (PossibleTargetByPoint.ContainsKey(myPoint))
                {
                    //I do have a match but does it actually align to the value of the otherPoint
                    //check the stored value
                    var valueStored = PossibleTargetByPoint[myPoint];
                    //does this match?
                    if(valueStored== otherPoint.TheConnectionPart)
                    {
                        //we do match, this means our possible target by point from our dictionary has left us
                        PossibleTargetByPoint.Remove(myPoint);
                        Debug.LogWarning($"This {this.gameObject.name} is removing a Target Point from the Dictionary: {myPoint.gameObject.name}");
                        //sync with alignment as we have already confirmed we care about this information
                        if (AlignmentConnectionPointPair.ContainsKey(myPoint))
                        {
                            AlignmentConnectionPointPair.Remove(myPoint);
                            OnTriggerExitUnityEvent.Invoke();
                            Debug.LogWarning($"This {this.gameObject.name} is removing an alignment connection point {myPoint.gameObject.name}");
                            myPoint.RemoveAlignmentPoint(otherPoint, false);
                            otherPoint.RemoveAlignmentPoint(myPoint, false);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"This {this.gameObject.name} had a trigger exit callback, but it was some other part that I don't care about, part passing by was: {otherPoint.gameObject.name}");
                    }
                    
                }
                else
                {
                    Debug.LogWarning($"This {this.gameObject.name} is missing a target point in the dictionary: {myPoint.gameObject.name}");
                    return;
                }
            }
        }
        #endregion
        
        #region Connectable Item 
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
        public bool IsPartiallyConnected = false;
        [Space]
        [SerializeField]
        [Tooltip("Hold the connection pairs")]
        public Dictionary<ConnectionPointUnity, ConnectionPart> ConnectionPairs = new Dictionary<ConnectionPointUnity, ConnectionPart>();
        [Tooltip("If we end up connecting larger pieces")]
        public Dictionary<ConnectionPointUnity, Joint> ConnectionJoints = new Dictionary<ConnectionPointUnity, Joint>();
        
        [Tooltip("All/any actual points open for a connection")]
        public List<ConnectionPointUnity> InternalAvailableConnections = new List<ConnectionPointUnity>();
        [Tooltip("The points that are directly attached to this item")]
        [SerializeField]
        protected List<ConnectionPointUnity> myConnectionPoints = new List<ConnectionPointUnity>();
        public List<ConnectionPointUnity> MyConnectionPoints { get { return myConnectionPoints; } }
        #region New Progress
        [Space]
        [Header("MOVED DATA")]
        [Tooltip("This should be equal to myself")]
        [SerializeField]protected GameObject RootConnectableItemRef;

        [Space]
        public Transform ColliderParent;
        public Transform ConnectionPointParent;
        public List<GameObject> MyBodyColliders = new List<GameObject>();

        [Tooltip("Visual Item")]
        public GameObject MyVisualItem;
        
        public Dictionary<int,ConnectableData> LookupPreviousParts = new Dictionary<int, ConnectableData>();
        public Dictionary<int,List<GameObject>> LookupPreviousColliders = new Dictionary<int, List<GameObject>>();
        public void CopyIncomingData(ConnectionPart incomingItem, ConnectionPointUnity incomingConnectionPt, ConnectionPointUnity myConnectionPoint)
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
        /// <summary>
        /// Called for updating connection point data and shifting the visuals
        /// </summary>
        /// <param name="newConnectionData"></param>
        /// <param name="localOffsetForVisuals"></param>
        /// <returns></returns>
        public bool UpdateConnectionPointLocations(List<ConnectionPointData>newConnectionData,Vector3 localOffsetForVisuals) 
        {
            //move the new connection data points based on the data
            MyVisualItem.transform.localPosition = localOffsetForVisuals;
            if (newConnectionData.Count != MyConnectionPoints.Count)
            {
                Debug.LogError($"New Connection Data does not match the number of connection points");
                return false;
            }
            for (int i=0;i< MyConnectionPoints.Count; i++)
            {
                var cpuExisting = MyConnectionPoints[i];
                cpuExisting.SetupDataFromDataFile(this, newConnectionData[i]);
            }
            return true;
        }
        #endregion
        #region Alignment Related
        /// <summary>
        /// For Alignment purposes
        /// </summary>
        /// <param name="otherItem"></param>
        /// <param name="availableConnections"></param>
        /// <returns></returns>
        public bool TryAlignmentOnRelease(ConnectionPart otherItem, List<ConnectionPointUnity> availableConnections)
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
                        //AlignTo(otherItem, otherClosestPoint, myClosestPoint, outcome.Item2, outcome.Item3);
                        if(UseSimpleAlign)
                        {
                            AlignToSimple(otherClosestPoint, myClosestPoint);
                        }
                        else
                        {
                            AlignTo(otherItem, otherClosestPoint, myClosestPoint, outcome.Item2, outcome.Item3);
                        }
                        OnPartAlignmentMade(this, myClosestPoint, otherItem, otherClosestPoint);
                        return true;
                    }
                    else
                    {
                        OnPartAlignmentFail(this, myClosestPoint, otherItem, otherClosestPoint);
                        return false;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Will hook this in soon
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <param name="myPoint"></param>
        /// <returns></returns>
        public bool RemoveAlignmentOnRelease(ConnectionPointUnity targetPoint, ConnectionPointUnity myPoint)
        {
            targetPoint.RemoveAlignmentPoint(myPoint, false);
            myPoint.RemoveAlignmentPoint(targetPoint, false);
            return true;
        }
        protected void AlignToSimple(ConnectionPointUnity targetPoint,ConnectionPointUnity myPoint)
        {
            // Step 1: Calculate the forward vectors from the connection points
            Vector3 myForward = myPoint.transform.TransformDirection(myPoint.ConnectionPointData.localForward);
            Vector3 targetForward = targetPoint.transform.TransformDirection(-targetPoint.ConnectionPointData.localForward);

            // Step 2: Compute the cross product to determine the normal vector
            Vector3 rotationAxis = Vector3.Cross(myForward, targetForward).normalized;

            // Step 3: Compute the angle required to align these normal vectors
            float angle = Vector3.SignedAngle(myForward, targetForward, rotationAxis);

            // Step 4: Create a rotation that aligns myPoint with targetPoint on the first axis
            Quaternion alignmentRotation = Quaternion.AngleAxis(angle, rotationAxis);

            // Step 5: Apply the alignment rotation to parent transform
            this.transform.rotation = alignmentRotation * this.transform.rotation;
            
            //move the vector direction and distance between targetPoint and myPoint but apply it to my root
            Vector3 positionOffset = targetPoint.transform.position - myPoint.transform.position;
            // Step 6: Adjust the position of myPoint to align with targetPoint
            this.transform.position += positionOffset;
            // Step 6: Update the connection point status for both
            myPoint.AddAlignmentPoint(targetPoint);
            targetPoint.AddAlignmentPoint(myPoint);
        }
        protected void AlignTo(ConnectionPart targetItem, ConnectionPointUnity targetPoint, ConnectionPointUnity myPoint, Vector3 dataMyVector, Vector3 dataTargetVector)
        {
            // Step 1: Calculate the forward vectors from the connection points
            Vector3 myForward = myPoint.transform.TransformDirection(myPoint.ConnectionPointData.localForward);
            // Invert targetForward to handle 180-degree misalignment
            Vector3 targetForward = targetPoint.transform.TransformDirection(-targetPoint.ConnectionPointData.localForward);
            //Vector3 targetForward = targetPoint.transform.TransformDirection(targetPoint.ConnectionPointData.localForward);

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
        private void OnPartAlignmentFail(ConnectionPart item, ConnectionPointUnity connectionPoint, ConnectionPart targetItem, ConnectionPointUnity targetPoint)
        {
            Debug.LogError($"Alignment failed... {item.gameObject.name} and {targetItem.gameObject.name}");
        }
        private void OnPartAlignmentMade(ConnectionPart item, ConnectionPointUnity connectionPoint, ConnectionPart targetItem, ConnectionPointUnity targetPoint)
        {
            Debug.LogWarning($"Alignment Made: {item.gameObject.name} and {targetItem.gameObject.name}");

            //spawn my bolts based on the number of ConnectionPointData within - this will eventually be "Welds"
            /*
            if (FixedAttachedPrefab != null)
            {
                List<ConnectionFixed> bolts = new List<ConnectionFixed>();
                for (int i = 0; i < connectionPoint.ConnectionPointData.localConnectors.Count; i++)
                {
                    var connectorBoltlocalPos = connectionPoint.ConnectionPointData.localConnectors[i];
                    var pos = connectionPoint.transform.TransformPoint(connectorBoltlocalPos);
                    var fixedPT = Instantiate(FixedAttachedPrefab, Vector3.zero, Quaternion.identity);
                    //bolt.transform.SetParent(BoltParent);
                    fixedPT.transform.position = pos;
                    var boltForwardShouldBe = connectionPoint.transform.TransformDirection(connectionPoint.ConnectionPointData.localForward);
                    // make the bolt face that forward vector
                    fixedPT.transform.rotation = Quaternion.LookRotation(boltForwardShouldBe);
                    fixedPT.transform.SetParent(FixedAttachedParent);
                    fixedPT.name = $"{i.ToString()}_{connectionPoint.gameObject.name}_bolt";
                    fixedPT.GetComponent<ConnectionFixed>().FixedAssociatedConnectionPoint = connectionPoint;
                    fixedPT.GetComponent<ConnectionFixed>().OnPartConnectionFixedToolFinished += OnFixedWeldedToolEnd;

                    bolts.Add(fixedPT.GetComponent<ConnectionFixed>());
                    //register for bolted events
                }
                //setup and confirm dictionary
                if (WeldsByPoint.ContainsKey(connectionPoint))
                {
                    WeldsByPoint[connectionPoint] = bolts;
                }
                else
                {
                    WeldsByPoint.Add(connectionPoint, bolts);
                }
                // setup AlignmentConnectionPointPair
                if (AlignmentConnectionPointPair.ContainsKey(connectionPoint))
                {
                    AlignmentConnectionPointPair[connectionPoint] = targetPoint;
                }
                else
                {
                    AlignmentConnectionPointPair.Add(connectionPoint, targetPoint);
                }
            }
            */
            if (AlignmentConnectionPointPair.ContainsKey(connectionPoint))
            {
                AlignmentConnectionPointPair[connectionPoint] = targetPoint;
            }
            else
            {
                AlignmentConnectionPointPair.Add(connectionPoint, targetPoint);
            }
            OnAlignmentSuccessEvent.Invoke();
        }
        #endregion
        #region Final Connection / Weld
        
        private void OnPartConnectionMade(ConnectionPart item, ConnectionPointUnity connectionPoint, ConnectionPart targetItem, ConnectionPointUnity targetPoint)
        {
            //turn off the associated triggers for those endpoints
            Debug.LogWarning($"{this.gameObject.name} from OVRPipePart.cs got the call back for connection made=> {item.gameObject.name} with {targetItem.gameObject.name} was connected!");
            //turn off 
            if (ConnectionPointsTriggersLookUp.ContainsKey(connectionPoint))
            {
                ConnectionPointsTriggersLookUp[connectionPoint].SetActiveTrigger(false);
                // reset the possible target since we are now connected //remove from dictionary
                if (PossibleTargetByPoint.ContainsKey(connectionPoint))
                {
                    PossibleTargetByPoint.Remove(connectionPoint);
                }
                if (AlignmentConnectionPointPair.ContainsKey(connectionPoint))
                {
                    AlignmentConnectionPointPair.Remove(connectionPoint);
                }
                //possibleTarget = null;
            }
            if (targetItem.GetComponent<ConnectionPart>() != null)
            {
                var targetPipePart = targetItem.GetComponent<ConnectionPart>();
                // deactivate target point trigger object
                targetPipePart.OnPipeConnectionRequest(this, item, connectionPoint, targetPoint);
            }
        }
        public bool TryToMakeAconnection(ConnectionPart otherItem,ConnectionPointUnity otherPoint,ConnectionPointUnity myPoint)
        {
            //make connection
            var successConnection = MakeConnection(otherItem, otherPoint, myPoint);
            if (successConnection)
            {
                OnPartConnectionMade(this, myPoint, otherItem, otherPoint);
            }
            else
            {
                OnPartConnectionFail(this, myPoint, otherItem, otherPoint);
            }
            return successConnection;
        }
        /// <summary>
        /// I am becoming part of the targetItem
        /// </summary>
        /// <param name="targetItem"></param>
        /// <param name="targetPoint"></param>
        /// <param name="myPoint"></param>
        /// <param name="updatePair"></param>
        protected bool MakeConnection(ConnectionPart targetItem, ConnectionPointUnity targetPoint, ConnectionPointUnity myPoint)
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
        private void OnPartConnectionFail(ConnectionPart item, ConnectionPointUnity connetionPoint, ConnectionPart targetItem, ConnectionPointUnity targetPoint)
        {
            Debug.LogWarning($"{this.gameObject.name} got the call back for connection failed=> {item.gameObject.name} with {targetItem.gameObject.name} was not connected!");
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
                OnPartDisconnect(this, pointToFree, otherItem, pointToFree.OtherConnection);
            }
        }
        
        private void OnPartDisconnect(ConnectionPart item, ConnectionPointUnity connectionPoint, ConnectionPart targetItem, ConnectionPointUnity targetPoint)
        {
            //turn on the associated triggers for those endpoints
        }
        /// <summary>
        /// Called from the other end - syncing the trigger states for turning them off/on
        /// </summary>
        /// <param name="thePointToCheck"></param>
        public void OnPipeConnectionRequest(ConnectionPart otherPart, ConnectionPart theItemToCheck, ConnectionPointUnity thePointToCheck, ConnectionPointUnity myPointTrigger)
        {
            GameObject cPGameObject = null;
            var nameOfOtherPart = otherPart.gameObject.name;
            var nameOfMyConnection = myPointTrigger.gameObject.name;
            var nameOfOtherConnection = thePointToCheck.gameObject.name;
            Debug.Log($"On Pipe Connection Request running on {this.gameObject.name}, Name of The Other Part: {nameOfOtherPart} | Name of Other Connection = {nameOfOtherConnection} | Name of my Connection = {nameOfMyConnection}");
            if (ConnectionPointsTriggersLookUp.ContainsKey(thePointToCheck))
            {
                ConnectionPointsTriggersLookUp[thePointToCheck].SetActiveTrigger(false);
                cPGameObject = ConnectionPointsTriggersLookUp[thePointToCheck].gameObject;
                thePointToCheck.ForceOtherClearConnection();
                //possibleTarget = null;
            }
            if (cPGameObject != null)
            {
                Debug.LogWarning($"Turned off the trigger on {cPGameObject.name}!");
            }
            // copy data first
            CopyIncomingData(theItemToCheck, thePointToCheck, myPointTrigger);

            // clean up
            // remove all listeners?
            foreach (var cp in ConnectionPointTriggersListeners)
            {
                cp.OnPartTriggerEnterAction -= OnConnectionPointTriggerEnter;
                cp.OnPartTriggerExitAction -= OnConnectionPointTriggerExit;
            }
            ConnectionPointTriggersListeners.Clear();
            ConnectionPointsTriggersLookUp.Clear();

            // move all bolts
            
            List<ConnectionFixed> theBoltList = new List<ConnectionFixed>();
            int childCount = otherPart.FixedAttachedParent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var otherBolt = otherPart.FixedAttachedParent.GetChild(i);
                //clear the other listeners
                otherBolt.GetComponent<ConnectionFixed>().OnPartConnectionFixedToolFinished -= otherPart.OnFixedWeldedToolEnd;
                //add my listener to it
                otherBolt.GetComponent<ConnectionFixed>().OnPartConnectionFixedToolFinished += OnFixedWeldedToolEnd;
                theBoltList.Add(otherBolt.GetComponent<ConnectionFixed>());
                otherBolt.GetComponent<ConnectionFixed>().FixedAssociatedConnectionPoint = myPointTrigger;
            }
            foreach (var aBolt in theBoltList)
            {
                aBolt.transform.SetParent(FixedAttachedParent);
            }

            //clear /replace bolt dictionary

            if (WeldsByPoint.ContainsKey(myPointTrigger))
            {
                WeldsByPoint[myPointTrigger] = theBoltList;
            }
            else
            {
                WeldsByPoint.Add(myPointTrigger, theBoltList);
            }
            
            // clean up internal connection list InternalAvailableConnections and drop myPointTrigger
            if (InternalAvailableConnections.Contains(myPointTrigger))
            {
                InternalAvailableConnections.Remove(myPointTrigger);
                myPointTrigger.GetComponent<ConnectionToolTrigger>().SetActiveTrigger(false);
            }
            if (InternalAvailableConnections.Contains(thePointToCheck))
            {
                InternalAvailableConnections.Remove(thePointToCheck);
                thePointToCheck.GetComponent<ConnectionToolTrigger>().SetActiveTrigger(false);
            }
            // re-add our triggers based on the updated data from ThePipe
            for (int i = 0; i < MyConnectionPoints.Count; i++)
            {
                var aPoint = MyConnectionPoints[i];
                Debug.LogWarning($"Readding Triggers: {aPoint.gameObject.name}");
                if (aPoint.gameObject.GetComponent<ConnectionToolTrigger>() != null)
                {
                    var cpTrigger = aPoint.gameObject.GetComponent<ConnectionToolTrigger>();
                    ConnectionPointsTriggersLookUp.Add(aPoint, cpTrigger);
                    ConnectionPointTriggersListeners.Add(cpTrigger);
                    // add listener functions to the triggers

                    cpTrigger.OnPartTriggerEnterAction += OnConnectionPointTriggerEnter;
                    cpTrigger.OnPartTriggerExitAction += OnConnectionPointTriggerExit;

                    aPoint.UpdateConnectableItem(this);
                    Debug.LogWarning($"Setting the connect item to {theItemToCheck.gameObject.name} on {aPoint.gameObject.name} and registered the trigger: {cpTrigger.gameObject.name}!");
                }
                else
                {
                    Debug.LogError($"I am missing an OVRToolTrigger on {aPoint.gameObject.name}!");
                }
            }
            // lets now blast the other item and my Trigger

            StartCoroutine(DestroyOneFrameLater(myPointTrigger.gameObject));
            StartCoroutine(DestroyOneFrameLater(thePointToCheck.gameObject));
            StartCoroutine(DestroyOneFrameLater(otherPart.gameObject));
            //this.possibleTarget = null;
            Debug.LogWarning($"Destroyed {nameOfOtherPart}, is this other ConnectableItem still around? {theItemToCheck.gameObject.name}| I Also got rid of {nameOfMyConnection}");
        }
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
                if (ConnectionPairs.TryGetValue(connectedPoint, out ConnectionPart connectedItem))
                {
                    // Recursively get open connection points from the connected item
                    openConnectionPoints.AddRange(connectedItem.GetOpenConnectionPoints());
                }
            }

            return openConnectionPoints;
        }
        #endregion
        #endregion
        #region Quick Connection Mode
        public bool UILockItem(ConnectionPointUnity incomingPoint)
        {
            // now we need to make sure that our connection points that are "aligned" with incomingPoint is in a state of off as well as it's state of motion
            var keysToJointPairs = AlignmentConnectionPointPair.Keys.ToList();
            for (int i = 0; i < keysToJointPairs.Count; i++)
            {
                var curKey = keysToJointPairs[i];
                var curValue = AlignmentConnectionPointPair[curKey];
                if (curValue == incomingPoint)
                {
                    //lock the joint back to it's normal state which will immediately realign on next frame
                    //lock both ConnectionToolTriggers and break out
                    incomingPoint.MyToolTriggerRef.SetActiveTrigger(false);
                    incomingPoint.AddConnectionPoint(curKey);
                    //my point
                    curKey.MyToolTriggerRef.SetActiveTrigger(false);
                    curKey.AddConnectionPoint(incomingPoint);
                    //request my other connected part to lock as well - we always lock our other part
                    incomingPoint.TheConnectionPart.LockTool();
                    // lockmyself
                    return LockTool();
                }
            }
            return false;
        }
        /// <summary>
        /// This should be the main way to request an unlock via a connectionPointUnity ref
        /// </summary>
        /// <param name="incomingPoint"></param>
        public bool UIAttemptUnlockItem(ConnectionPointUnity incomingPoint)
        {
            if(CurrentState == FPToolState.Locked)
            {
                // now we need to make sure that our connection points that are "aligned" with incomingPoint is in a state of on
                // we were successful in locking
                // now we need to make sure that our connection points that are "aligned" with incomingPoint is in a state of off
                var keysToJointPairs = AlignmentConnectionPointPair.Keys.ToList();
                for (int i = 0; i < keysToJointPairs.Count; i++)
                {
                    var curKey = keysToJointPairs[i];
                    var curValue = AlignmentConnectionPointPair[curKey];
                    if (curValue == incomingPoint)
                    {
                        //lock the joint back to it's normal state which will immediately realign on next frame
                        //lock both ConnectionToolTriggers and break out
                        incomingPoint.RemoveConnectionPoint(curKey, false);
                        incomingPoint.MyToolTriggerRef.SetActiveTrigger(true);
                        //my point
                        curKey.RemoveConnectionPoint(incomingPoint, false);
                        curKey.MyToolTriggerRef.SetActiveTrigger(true);
                        //request an unlock now for both my incoming and myself
                        incomingPoint.TheConnectionPart.IncomingUnlockRequest();
                        return IncomingUnlockRequest();
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Unlocking by external request gets funny because we need to check all of our coneciton points not just the one
        /// </summary>
        public bool IncomingUnlockRequest() 
        {
            // we need to check our ConnectionPointUnity and specifically look for ConnectionPointStatusPt
            var myConnectionPoints = ConnectionPointsTriggersLookUp.Keys.ToList();
            for(int i =0; i < myConnectionPoints.Count; i++)
            {
                var curConnectionPoint = myConnectionPoints[i];
                if(curConnectionPoint.ConnectionPointStatusPt == ConnectionPointStatus.Connected)
                {
                    // we need to unlock the trigger
                    // if just one point is stil on a connected state we need to not unlock
                    return false;
                }
            }
            //none of our connection points are in a connected state so we can actually unlock
            if (UnlockTool())
            {
                Debug.LogWarning($"We were able to unlock:{this.gameObject.name}");
                return true;
            }
            return false;
        }
        // just need to have a way to lock the item
        // turn off all high level moving/rotating
        // maintain other items
        // needs to relay information to the ConnectionPointUnity to let it know that it's not looking for connections

        #endregion
        #region New Connecting System From data
        //much more exhaustive approach - going to leave this alone for now 4-10
        //we have visuals
        //we have colliders
        //on the collider is the FP_CollideItem = which needs to be updated via MoveRotateItem
        //we have connection points
        //listeners
        /*
        ConnectionPointTriggersListeners
        var cpu = prefabSpawned.GetComponent<ConnectionPointUnity>();
        foreach (var cp in ConnectionPointTriggersListeners)
            {
                cp.OnPartTriggerEnterAction += OnConnectionPointTriggerEnter;
                cp.OnPartTriggerExitAction += OnConnectionPointTriggerExit;
                cp.gameObject.name = $"{GetUniqueIndexPos}_{cp.gameObject.name}";
            }
        */
        //we have aligned AlignmentConnectionPointPair & PossibleTargetByPoint
        //order of operations
        // 1. ID who is the parent, deactivate all major systems on other (child)
        // 1.1 turn off Connection Part, FP_MoveRotateItem, go to the ConnectionPtParent - disable it
        // 2. move other (child) to ConnectionFixedParent

        #endregion
    }
}
