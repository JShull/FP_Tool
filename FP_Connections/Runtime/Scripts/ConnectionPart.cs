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
        [Header("Pipe Related")]
        public ConnectableItem TheItem;
        public GameObject FixedAttachedPrefab;
        public Transform FixedAttachedParent;
        public Transform ConnectionPtParent;
        [Space]
        [Header("Unity Events")]
        public UnityEvent OnAlignmentSuccessEvent;
        public UnityEvent OnTriggerEnterUnityEvent;
        public UnityEvent OnTriggerExitUnityEvent;
        
        //data
        public List<ConnectionToolTrigger> ConnectionPointTriggersListeners = new List<ConnectionToolTrigger>();
        public Dictionary<ConnectionPointUnity, ConnectionToolTrigger> ConnectionPointsTriggersLookUp = new Dictionary<ConnectionPointUnity, ConnectionToolTrigger>();
        [SerializeField]
        [Tooltip("Hold a list of bolts by ConnectionPointUnity")]
        protected Dictionary<ConnectionPointUnity, List<ConnectionFixed>> WeldsByPoint = new Dictionary<ConnectionPointUnity, List<ConnectionFixed>>();
        [Tooltip("Hold a Connectable Item by a Point")]
        protected Dictionary<ConnectionPointUnity, ConnectableItem> PossibleTargetByPoint = new Dictionary<ConnectionPointUnity, ConnectableItem>();
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
                GameObject prefabSpawned = GameObject.Instantiate(toolData.ConnectionEndPrefab, ConnectionPtParent);
                
                //get component
                var cpu = prefabSpawned.GetComponent<ConnectionPointUnity>();
                if(cpu != null)
                {
                    prefabSpawned.gameObject.name = curPointData.connectionType.ToString()+"_"+i;
                    cpu.SetupDataFromDataFile(TheItem, curPointData);
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

            }
            
            TheItem.SetupConnectionPart(toolData.UniquePartID, cachedPoints);
        }
        
        public virtual void OnEnable()
        {
            TheItem.OnAlignmentFail += OnPartAlignmentFail;
            TheItem.OnAlignmentSuccess += OnPartAlignmentMade;
            TheItem.OnConnectionSuccess += OnPartConnectionMade;
            TheItem.OnConnectionRemoved += OnPartDisconnect;
            TheItem.OnConnectionFail += OnPartConnectionFail;

            foreach (var cp in ConnectionPointTriggersListeners)
            {
                cp.OnPartTriggerEnterAction += OnConnectionPointTriggerEnter;
                cp.OnPartTriggerExitAction += OnConnectionPointTriggerExit;
                cp.gameObject.name = $"{TheItem.GetUniqueIndexPos}_{cp.gameObject.name}";
            }
        }
        public virtual void OnDisable()
        {
            TheItem.OnAlignmentFail -= OnPartAlignmentFail;
            TheItem.OnAlignmentSuccess -= OnPartAlignmentMade;
            TheItem.OnConnectionSuccess -= OnPartConnectionMade;
            TheItem.OnConnectionRemoved -= OnPartDisconnect;
            TheItem.OnConnectionFail -= OnPartConnectionFail;
            foreach (var cp in ConnectionPointTriggersListeners)
            {
                cp.OnPartTriggerEnterAction -= OnConnectionPointTriggerEnter;
                cp.OnPartTriggerExitAction -= OnConnectionPointTriggerExit;
            }
        }
        #region Listeners for External Pointers/Events




        #endregion
        #region Interface Requirements for Input Information
        /// <summary>
        /// This just sets our state up for being ready to use the tool
        /// </summary>
        public override bool ActivateTool()
        {
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
            ActivateTool();
            if (StartTool())
            {
                Debug.Log($"{this.gameObject.name}: ConnectionPart.cs Pointer DOWN");
            }
            
        }
        /// <summary>
        /// Unity Event Wrapper tied to the PointerEventData class
        /// E.g. when we 'drop' our part we want to check for a connection
        /// </summary>
        /// <param name="pointerEvent"></param>
        public void PointerUp(PointerEventData eventData)
        {
            Debug.Log($"{this.gameObject.name}: Pointer UP");
            // see if we had a possible pipe target
            // possible target comes in from the trigger events
            // loop through our dictionary of possible targets
            Debug.Log($"{this.gameObject.name}: ConnectionPart.cs Pointer UP");
            var possibleTargets = PossibleTargetByPoint.Keys.ToList();
            List<ConnectableItem> allPossibleTargets = new List<ConnectableItem>();
            for (int i = 0; i < possibleTargets.Count; i++)
            {
                var aConnectionPoint = possibleTargets[i];
                var aConnectableItem = PossibleTargetByPoint[aConnectionPoint];
                if (aConnectableItem != null)
                {
                    allPossibleTargets.Add(aConnectableItem);
                }
            }
            if (allPossibleTargets.Count > 1)
            {
                //special case
                Debug.LogError($"Special case in which possible targets exceed 1");
            }
            else
            {
                if (allPossibleTargets.Count == 1)
                {
                    //standard singular case
                    var theTarget = allPossibleTargets[0];
                    if (theTarget != null)
                    {
                        //build up all possible points based on current connection points and connected items
                        //var allOpenPossiblePoints = ThePipe.GetOpenConnectionPoints();

                        var greatSuccess = TheItem.TryAlignmentOnRelease(theTarget);
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
                }
                else
                {
                    Debug.LogWarning($"{this.gameObject.name}, has zero connections, need to clean up! I currently have: {AlignmentConnectionPointPair.Count} Alignment(s)");
                }
            }
            DeactivateTool();
        }

        public void PointerDrag(PointerEventData eventData)
        {
            if (UseTool())
            {
                //Debug.Log($"{this.gameObject.name}: ConnectionPart.cs Pointer DRAG");
            }
        }
        #endregion
        #region Delegate stuff
        #region Callbacks for ConnectableItem
        private void OnPartConnectionMade(ConnectableItem item, ConnectionPointUnity connectionPoint, ConnectableItem targetItem, ConnectionPointUnity targetPoint)
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
        private void OnPartConnectionFail(ConnectableItem item, ConnectionPointUnity connetionPoint, ConnectableItem targetItem, ConnectionPointUnity targetPoint)
        {
            Debug.LogWarning($"{this.gameObject.name} got the call back for connection failed=> {item.gameObject.name} with {targetItem.gameObject.name} was not connected!");
        }
        private void OnPartDisconnect(ConnectableItem item, ConnectionPointUnity connectionPoint, ConnectableItem targetItem, ConnectionPointUnity targetPoint)
        {
            //turn on the associated triggers for those endpoints
        }
        private void OnPartAlignmentFail(ConnectableItem item, ConnectionPointUnity connectionPoint, ConnectableItem targetItem, ConnectionPointUnity targetPoint)
        {
            Debug.LogError($"Alignment failed... {item.gameObject.name} and {targetItem.gameObject.name}");
        }
        private void OnPartAlignmentMade(ConnectableItem item, ConnectionPointUnity connectionPoint, ConnectableItem targetItem, ConnectionPointUnity targetPoint)
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
        
        //OnWrenchBoltToolEnd
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
                        TheItem.TryToMakeAconnection(thePossibleTarget, thePossibleConnection, ptData);
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

        /// <summary>
        /// Called from the other end - syncing the trigger states for turning them off/on
        /// </summary>
        /// <param name="thePointToCheck"></param>
        public void OnPipeConnectionRequest(ConnectionPart otherPart, ConnectableItem theItemToCheck, ConnectionPointUnity thePointToCheck, ConnectionPointUnity myPointTrigger)
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
            TheItem.CopyIncomingData(theItemToCheck, thePointToCheck, myPointTrigger);

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
            if (TheItem.InternalAvailableConnections.Contains(myPointTrigger))
            {
                TheItem.InternalAvailableConnections.Remove(myPointTrigger);
                myPointTrigger.GetComponent<ConnectionToolTrigger>().SetActiveTrigger(false);
            }
            if (TheItem.InternalAvailableConnections.Contains(thePointToCheck))
            {
                TheItem.InternalAvailableConnections.Remove(thePointToCheck);
                thePointToCheck.GetComponent<ConnectionToolTrigger>().SetActiveTrigger(false);
            }
            // re-add our triggers based on the updated data from ThePipe
            for (int i = 0; i < TheItem.MyConnectionPoints.Count; i++)
            {
                var aPoint = TheItem.MyConnectionPoints[i];
                Debug.LogWarning($"Readding Triggers: {aPoint.gameObject.name}");
                if (aPoint.gameObject.GetComponent<ConnectionToolTrigger>() != null)
                {
                    var cpTrigger = aPoint.gameObject.GetComponent<ConnectionToolTrigger>();
                    ConnectionPointsTriggersLookUp.Add(aPoint, cpTrigger);
                    ConnectionPointTriggersListeners.Add(cpTrigger);
                    // add listener functions to the triggers

                    cpTrigger.OnPartTriggerEnterAction += OnConnectionPointTriggerEnter;
                    cpTrigger.OnPartTriggerExitAction += OnConnectionPointTriggerExit;

                    aPoint.UpdateConnectableItem(TheItem);
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
            Debug.LogWarning($"Callback--> Connection Point Trigger Enter: {item.gameObject.name} with {myPoint.gameObject.name}");
            if (otherPoint.TheConnectItem != null)
            {
                if (PossibleTargetByPoint.ContainsKey(myPoint))
                {
                    PossibleTargetByPoint[myPoint] = otherPoint.TheConnectItem;
                }
                else
                {
                    PossibleTargetByPoint.Add(myPoint, otherPoint.TheConnectItem);
                    OnTriggerEnterUnityEvent.Invoke();
                    Debug.LogWarning($"This {this.gameObject.name} is adding Dictionary target point: {myPoint.gameObject.name} with {otherPoint.TheConnectItem.gameObject.name}");
                }
                //sync with Alignment AlignmentConnectionPointPair
                if (AlignmentConnectionPointPair.ContainsKey(myPoint))
                {
                    AlignmentConnectionPointPair[myPoint] = otherPoint;
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
            Debug.LogWarning($"Callback--> Connection Point Trigger Exit: {item.gameObject.name} with {myPoint.gameObject.name}");
            if (item.gameObject.GetComponent<ConnectionPointUnity>() != null)
            {
               
                //var cpu = item.gameObject.GetComponent<ConnectionPointUnity>();
                if (PossibleTargetByPoint.ContainsKey(myPoint))
                {
                    PossibleTargetByPoint.Remove(myPoint);
                    Debug.LogWarning($"This {this.gameObject.name} is removing a Target Point from the Dictionary: {myPoint.gameObject.name}");
                    if (AlignmentConnectionPointPair.ContainsKey(myPoint))
                    {
                        AlignmentConnectionPointPair.Remove(myPoint);
                        OnTriggerExitUnityEvent.Invoke();
                        Debug.LogWarning($"This {this.gameObject.name} is removing an alignment connection point {myPoint.gameObject.name}");
                        myPoint.RemoveAlignmentPoint(otherPoint,false);
                        otherPoint.RemoveAlignmentPoint(myPoint, false);
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
        #endregion
    }
}
