namespace FuzzPhyte.Tools.Connections
{
    using System.Collections.Generic;
    using UnityEngine;
    using FuzzPhyte.Utility;

    public enum ConnectionPointStatus
    {
        None,
        Aligned,
        Connected,
    }
    public class ConnectionPointUnity : MonoBehaviour
    {
        protected ConnectableItem theConnectItem;
        public ConnectableItem TheConnectItem { get { return theConnectItem; } }
        protected ConnectionPointData connectionPointData;
        public ConnectionPointData ConnectionPointData { get { return connectionPointData; } }
        public ConnectionToolTrigger MyToolTriggerRef;
        public BoxCollider CPUTriggerCollider;
        public ConnectionPointStatus ConnectionPointStatusPt = ConnectionPointStatus.None;
        [Space]
        [SerializeField]
        [Tooltip("This is the alignment point we are on")]
        private ConnectionPointUnity otherAlignedPoint;
        [SerializeField]
        private ConnectionPointUnity otherConnection;
        [SerializeField]
        private bool isConnected;
        public ConnectionPointUnity OtherConnection => otherConnection;
        [SerializeField]
        List<Quaternion> validRotations = new List<Quaternion>();
        [SerializeField]
        List<Vector3> connectorLocations = new List<Vector3>();
        public float AngleTolerance = 30f;
        public float InitialAlignmentTolerance = 20f;
        [Header("Gizmo Related")]
        [Range(0.01f, 1f)]
        public float PercentageOfMeasure = 0.2f;
        [SerializeField]protected bool setupFinished;

        /// <summary>
        /// We call this after we've made sure to have the ConnectionPointDataFile setup
        /// </summary>
        public void SetupDataFromDataFile(ConnectableItem theItem, ConnectionPointData theData)
        {
            theConnectItem = theItem;
            connectionPointData = theData;
            
            if (connectionPointData != null)
            {
                //move to my position
                this.transform.localPosition = connectionPointData.LocalRelativePivotPosition;
                validRotations = new List<Quaternion>();
                foreach (var angle in connectionPointData.localRotationAngles)
                {
                    Quaternion rotation = Quaternion.Euler(angle);
                    validRotations.Add(rotation);
                }
                connectorLocations = new List<Vector3>();
                foreach (var connector in connectionPointData.localConnectors)
                {
                    connectorLocations.Add(connector);
                }
                AngleTolerance = connectionPointData.AngleTolerance;
                InitialAlignmentTolerance = connectionPointData.InitAlignmentTolerance;
                PercentageOfMeasure = connectionPointData.PercentOfMeasure;
                //setup box trigger
                if (CPUTriggerCollider)
                {
                    CPUTriggerCollider.center = connectionPointData.TriggerCenter;
                    CPUTriggerCollider.size = connectionPointData.TriggerSize;
                    CPUTriggerCollider.isTrigger = true;
                    CPUTriggerCollider.enabled = true;
                }
                if (MyToolTriggerRef)
                {
                    MyToolTriggerRef.TagToCompare = connectionPointData.TriggerTagCompare;
                    MyToolTriggerRef.MyPoint = this;
                }
                SetupCPUData();
            }
        }
        protected void SetupCPUData()
        {
            setupFinished = true;
        }
        public void UpdateConnectableItem(ConnectableItem theItem)
        {
            theConnectItem = theItem;
        }
        protected void OnDrawGizmos()
        {
            if (connectionPointData != null&& setupFinished)
            {
                var items = FP_UtilityData.ReturnValueInMeters(connectionPointData.width, connectionPointData.ConnectionUnitOfMeasure);
                // Draw the local forward direction
                Gizmos.color = Color.magenta;
                if (items.Item1)
                {
                    Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(connectionPointData.localForward) * items.Item2*3f);
                }
                else 
                {
                    Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(connectionPointData.localForward) * 0.25f);
                }
                   
                
                //Gizmos.DrawRay(transform.position,ConnectionPointData.localForward.normalized * 0.25f);
                // Draw a small sphere at the connection point
                Gizmos.color = Color.red;
                if(ConnectionPointStatusPt == ConnectionPointStatus.Aligned)
                {
                    Gizmos.color = Color.cyan;
                }
                else if(ConnectionPointStatusPt == ConnectionPointStatus.Connected)
                {
                    Gizmos.color = Color.green;
                }

                
                if (items.Item1)
                {
                    //convert to meter & adjust/scale
                    Gizmos.DrawSphere(transform.position, items.Item2 * PercentageOfMeasure);
                }
                else
                {
                    Gizmos.DrawSphere(transform.position, connectionPointData.width * 0.01f);
                }

                    // Optional: Label the connection point in the scene view
                    // UnityEditor.Handles.Label(transform.position, connectionPointData.name);
                    // Draw valid rotations relative to localForward
                    foreach (var angle in connectionPointData.localRotationAngles)
                    {
                        Quaternion rotation = Quaternion.Euler(angle);
                        Vector3 direction = rotation * connectionPointData.localForward;
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(transform.position, transform.position + transform.TransformDirection(direction) * 0.1f);
                        //Gizmos.DrawRay(transform.position, direction.normalized * 0.1f);
                    }
                // Draw Connector locations
                Gizmos.color = Color.yellow;
                foreach (var connector in connectorLocations)
                {
                    var connectorConversion = FP_UtilityData.ReturnVector3InMeters(connector, connectionPointData.ConnectionUnitOfMeasure);
                    if (items.Item1)
                    {
                        //convert connector into meters from units

                        Gizmos.DrawWireSphere(transform.TransformPoint(connectorConversion), items.Item2* PercentageOfMeasure*2);
                    }
                    else
                    {
                        Gizmos.DrawWireSphere(transform.TransformPoint(connector), 0.02f);
                    }
                   
                }
            }
        }
        /// <summary>
        /// Check if we can align/connect by state
        /// Check then our position and rotation within tolerances
        /// Find the best match based on my orientation and the other connection orientation
        /// Return that data
        /// </summary>
        /// <param name="other"></param>
        /// <param name="bestRotation"></param>
        /// <returns></returns>
        public (bool,Vector3,Vector3) IsCompatibleWith(ConnectionPointUnity other, out Quaternion bestRotation)
        {
            //check if the other connection point is already in an aligned state
            bestRotation = Quaternion.identity;
            
            switch (other.ConnectionPointStatusPt)
            {
                case ConnectionPointStatus.Aligned:
                    //check if my otherAlignedPoint is the same - if it is just continue - if it's not then we have to return a false/failure
                    if(otherAlignedPoint !=other)
                    {
                        Debug.LogWarning($"Already in an alignment status with {otherAlignedPoint.gameObject.name} and cannot align/connect with a new passed point like {other.gameObject.name} right now.");
                        return (false, Vector3.zero, Vector3.zero);
                    }
                    break;
                case ConnectionPointStatus.Connected:
                    Debug.LogWarning($"Already in a connected status with {other.otherConnection.gameObject.name} and cannot align/connect with anyone else right now.");
                    return (false,Vector3.zero,Vector3.zero);
                case ConnectionPointStatus.None:
                    break;
            }
            
            float minAngleDifference = float.MaxValue;
            float minSumAngleMovement = float.MaxValue;
            Vector3 bestAxis = Vector3.up;
            Vector3 myDataRotation = Vector3.zero;
            Vector3 theOtherDataRotation = Vector3.zero;
            // Calculate the angle difference between the two forward vectors

            Vector3 myActualForward = this.transform.TransformDirection(connectionPointData.localForward);
            Vector3 theOtherForward = other.transform.TransformDirection(-other.connectionPointData.localForward); //inverse this forward to compare it to mine or minus 180 from answer
            float angleDifference = Vector3.Angle(myActualForward, theOtherForward);
            //Debug.Log($"Angle Difference between {this.name} and {other.name} = {angleDifference}");
            if(angleDifference< InitialAlignmentTolerance)
            {
                for(int i = 0; i < connectionPointData.localRotationAngles.Count; i++)
                {
                    var myRotation = connectionPointData.localRotationAngles[i];
                    Quaternion myTargetRotation = Quaternion.Euler(myRotation) * this.transform.rotation;
                    Vector3 myForward = myTargetRotation * connectionPointData.localForward;
                    
                    for(int j = 0; j < other.connectionPointData.localRotationAngles.Count; j++)
                    {
                        var otherRotation = other.connectionPointData.localRotationAngles[j];
                        //JOHN Cross of my Cross
                        var curForwardWorld = this.transform.TransformDirection(connectionPointData.localForward);
                        var curDirRotAngle = this.transform.TransformDirection(myRotation);
                        var curCross = Vector3.Cross(curForwardWorld, curDirRotAngle);
                        //
                        var otherForwardWorld = other.transform.TransformDirection(other.connectionPointData.localForward);
                        var otherDirRotAngle = other.transform.TransformDirection(otherRotation);
                        var otherCross = Vector3.Cross(otherForwardWorld, otherDirRotAngle);
                        //compare the cross of the cross
                        var techAngle = Vector3.Angle(curCross, otherCross);

                        Debug.Log($"Other local= {otherRotation}, my local {myRotation}, cross angle {techAngle} and the angle difference between the two forward vectors = {angleDifference}");

                        //first filter on tolerance angle
                        if (techAngle < AngleTolerance)
                        {
                            //we are parallel enough to consider a match
                            Vector3 otherForward = (Quaternion.Euler(otherRotation) * other.transform.rotation) * other.connectionPointData.localForward;
                            // Calculate the cross product for the rotation axis
                            Vector3 axis = Vector3.Cross(myForward, otherForward).normalized;
                            
                            var possibleBestMatch = Quaternion.AngleAxis(techAngle, axis).eulerAngles;
                            var testDotProduct = Vector3.Dot(possibleBestMatch.normalized, myRotation.normalized);
                            Debug.Log($"Cross Product Vector: {axis}, test dot product{testDotProduct} with angle difference {angleDifference} with my rotation {myRotation} and other rotation {otherRotation}");
                           
                            
                            var sumAngleTech = possibleBestMatch.x + possibleBestMatch.y + possibleBestMatch.z;// + otherRotation.x + otherRotation.y + otherRotation.z;
                            //store smallest one
                            if (techAngle < minAngleDifference && sumAngleTech < minSumAngleMovement)
                            {
                                minSumAngleMovement = sumAngleTech;
                                minAngleDifference = techAngle;
                                bestRotation = Quaternion.AngleAxis(minAngleDifference, axis); // Rotate around the chosen axis
                                bestAxis = axis;
                                Debug.Log($"Found a best match: {bestRotation.eulerAngles} with axis {bestAxis}, and a dot product of {testDotProduct}, a min angle diff of {minAngleDifference}, and best sum angle {sumAngleTech}");
                                myDataRotation = myRotation;
                                theOtherDataRotation = otherRotation;
                            }
                        }
                    }
                }
                
                return (minAngleDifference <= AngleTolerance, myDataRotation, theOtherDataRotation);
            }
            return (false,Vector3.zero,Vector3.zero);
        }

        public void AddAlignmentPoint(ConnectionPointUnity connectionPointUnity)
        {
            if(otherAlignedPoint != null)
            {
                RemoveAlignmentPoint(connectionPointUnity, true);
            }
            otherAlignedPoint = connectionPointUnity;
            switch(ConnectionPointStatusPt)
            {
                case ConnectionPointStatus.None:
                    ConnectionPointStatusPt = ConnectionPointStatus.Aligned;
                    break;
            }
        }
        public void RemoveAlignmentPoint(ConnectionPointUnity connectionPointUnity, bool updatePair)
        {
            if (otherAlignedPoint == connectionPointUnity)
            {
                if (updatePair)
                {
                    otherConnection.RemoveAlignmentPoint(this, false);
                }
                otherAlignedPoint = null;
            }
            ConnectionPointStatusPt = ConnectionPointStatus.None;
        }      
        public void AddConnectionPoint(ConnectionPointUnity connectionPointUnity)
        {
            if (otherConnection != null)
            {
                RemoveConnectionPoint(otherConnection, true);
            }
            //JOHN need to introduce alignment confirmation here
            otherConnection = connectionPointUnity;
            switch (ConnectionPointStatusPt)
            {
                case ConnectionPointStatus.None:
                case ConnectionPointStatus.Aligned:
                    ConnectionPointStatusPt = ConnectionPointStatus.Connected;
                    break;
            }
        }
        public void RemoveConnectionPoint(ConnectionPointUnity connectionPointUnity, bool updatePair)
        {
            if (otherConnection == connectionPointUnity)
            {
                if (updatePair)
                {
                    otherConnection.RemoveConnectionPoint(this,false);
                }
                otherConnection = null;
            }
            ConnectionPointStatusPt = ConnectionPointStatus.None;
        }
        public void ForceOtherClearConnection()
        {
            otherConnection = null;
            ConnectionPointStatusPt = ConnectionPointStatus.None;
        }
    }
}
