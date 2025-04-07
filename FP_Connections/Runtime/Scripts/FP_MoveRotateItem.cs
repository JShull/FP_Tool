namespace FuzzPhyte.Tools.Connections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    /*
     * private void AlignmentCheck()
        {
            if (potentialMate != null && correctPart &&!finalPlacement)
            {
                //check if we are aligned
                //use cross product to check if we are aligned for euler rotation value
                var potentialMateValue = Quaternion.Euler(potentialMate.gameObject.transform.rotation.eulerAngles);
                Debug.LogWarning($"{this.gameObject.name}| potentialMate: {potentialMate.gameObject.name}, mate rotation = ({potentialMateValue.x},{potentialMateValue.y},{potentialMateValue.z},{potentialMateValue.w})");
                var curRotation = Quaternion.Euler(this.GameData.EulerOrientation);
                float angleDifference = Quaternion.Angle(potentialMateValue, curRotation);
                //var dot = Vector3.Dot(potentialMate.gameObject.transform.rotation.eulerAngles, this.GameData.EulerOrientation);
                var lowerBound = 0 - (potentialMate.GameData.AlignmentVariation*50);
                var upperBound = 0 + (potentialMate.GameData.AlignmentVariation*50);
                Debug.LogWarning($"{this.gameObject.name}| angleDiff: {angleDifference} lowerBound {lowerBound} upperBound {upperBound}");
                
                if (angleDifference >= lowerBound && angleDifference <= upperBound)
                {
                    correctOrientation = true;
                    //force it to the right alignment
                    potentialMate.gameObject.transform.rotation = Quaternion.Euler(this.GameData.EulerOrientation);
                }
                
            }
        }
     */
    public class FP_MoveRotateItem : MonoBehaviour
    {
        //this class is designed to manage item related restrictions like rotation and movement by type.
        [Header("Requirements for Motion and Rotation")]
        public Vector3 RotationSnap = new Vector3(15f,15f,15f);
        public Vector3 MovementSnap = new Vector3(0.025f, 0.025f, 0.025f);
        [Space]
        public bool UseRotationSnap=false;
        public bool XRotationLocked;
        public bool YRotationLocked;
        public bool ZRotationLocked;
        [Space]
        public bool UseMovementSnap=false;
        public bool XMovementLocked;
        public bool YMovementLocked;
        public bool ZMovementLocked;

        private Vector3 accumulatedDelta = Vector3.zero;
        private Quaternion accumulatedRotation = Quaternion.identity;
        public event Action<GameObject> OnMoveStartedEvent;
        public event Action<GameObject> OnMoveEndEvent;
        public event Action<GameObject> OnRotationEndEvent;

        public void OnEnable()
        {
            OnMoveEndEvent = new Action<GameObject>((gameObject) => { });
            OnMoveStartedEvent = new Action<GameObject>((gameObject) => { });
            OnRotationEndEvent = new Action<GameObject>((gameObject) => { });
        }
        public void MoveStarted()
        {
            OnMoveStartedEvent.Invoke(this.gameObject);
        }
        public void MoveEnd()
        {
            OnMoveEndEvent.Invoke(this.gameObject);
        }
        #region Public Accessors
        /// <summary>
        /// The user is going to pass a next location in world coordinates
        /// and we will move the item to that location
        /// based on the restrictions set above.
        /// Returns a world coordinate location
        /// </summary>
        /// <param name="passedLocation">world coordinates</param>
        public Vector3 MoveItem(Vector3 passedLocation)
        {
            if (!UseMovementSnap)
            {
                //we aren't using our movement snap just return the location we were passed
                return passedLocation;
            }
            Vector3 currentPosition = this.transform.position;
            Vector3 deltaPosition = passedLocation - currentPosition;

            // Accumulate delta movements
            accumulatedDelta += deltaPosition;

            // Snap movement
            accumulatedDelta.x = TrySnapMovement(accumulatedDelta.x, MovementSnap.x);
            accumulatedDelta.y = TrySnapMovement(accumulatedDelta.y, MovementSnap.y);
            accumulatedDelta.z = TrySnapMovement(accumulatedDelta.z, MovementSnap.z);

            // Apply movement restrictions
            if (XMovementLocked) accumulatedDelta.x = currentPosition.x;
            if (YMovementLocked) accumulatedDelta.y = currentPosition.y;
            if (ZMovementLocked) accumulatedDelta.z = currentPosition.z;

            // Calculate new location
            Vector3 returnLocation = currentPosition + accumulatedDelta;
            accumulatedDelta = Vector3.zero; // Reset accumulated delta

            return returnLocation;
        }

        /// <summary>
        /// I need to make sure that this is delta rotation and not just a full rotation
        /// </summary>
        /// <param name="passedEulerRotation"></param>
        /// <returns></returns>
        public Quaternion RotateItem(Vector3 passedEulerRotation)
        {
            Quaternion targetRotation = Quaternion.Euler(passedEulerRotation);
            Quaternion currentRotation = transform.rotation;
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(currentRotation);

            // Accumulate delta rotations
            accumulatedRotation *= deltaRotation;

            // Check if the accumulated rotation meets the snapping criteria
            Vector3 accumulatedEuler = accumulatedRotation.eulerAngles;
            bool shouldSnap = Mathf.Abs(accumulatedEuler.x) >= RotationSnap.x ||
                              Mathf.Abs(accumulatedEuler.y) >= RotationSnap.y ||
                              Mathf.Abs(accumulatedEuler.z) >= RotationSnap.z;

            if (shouldSnap)
            {
                // Apply snapping and restrictions
                Debug.Log($"Rotation Snap");
                Quaternion snappedAndRestrictedRotation = ApplyRotationSnappingAndRestrictions(accumulatedRotation, currentRotation);
                accumulatedRotation = Quaternion.identity; // Reset only after snapping
                return snappedAndRestrictedRotation;
            }

            // If not yet meeting the snap criteria, return the current rotation
            OnRotationEndEvent.Invoke(this.gameObject);
            Debug.Log($"{gameObject.name}: Rotating!");
            return currentRotation;
        }
        /// <summary>
        /// public accessor to just rotate the item by the local values
        /// </summary>
        public void RotateItemByLocalValues()
        {
            if (UseRotationSnap)
            {
                if(XRotationLocked)
                {
                    RotationSnap.x = 0;
                }
                if (YRotationLocked)
                {
                    RotationSnap.y = 0;
                }
                if (ZRotationLocked)
                {
                    RotationSnap.z = 0;
                }
                transform.Rotate(RotationSnap);
                OnRotationEndEvent.Invoke(this.gameObject);
                Debug.Log($"{gameObject.name}: Rotating!");
            }
            //var rotationReturn = RotateItem(RotationSnap);
            //transform.rotation = rotationReturn;
        }
        public void RotateItemByLocalValuesInverse()
        {
            if (UseRotationSnap)
            {
                if (XRotationLocked)
                {
                    RotationSnap.x = 0;
                }
                if (YRotationLocked)
                {
                    RotationSnap.y = 0;
                }
                if (ZRotationLocked)
                {
                    RotationSnap.z = 0;
                }
                transform.Rotate(RotationSnap*-1f);
            }
        }
        public void RotateItemByLocalValuesAndCamera(Camera raycastCam)
        {
            //raycast from the camera to this transform center point
            //get the direction of the raycast
            Debug.DrawRay(transform.position, Vector3.up, Color.yellow, 10f);
            //UnityEngine.Ray throwRay = new UnityEngine.Ray(raycastCam.transform.position,Vector3.Normalize(transform.position - raycastCam.transform.position));
            //UnityEngine.Ray forwardRay = new UnityEngine.Ray(raycastCam.transform.position, raycastCam.transform.forward);
            //var angleOff = Vector3.SignedAngle(forwardRay.direction, throwRay.direction, Vector3.up);
            //Debug.Log($"Forward Angle Signed: {angleOff}");
            //Debug.DrawRay(throwRay.origin, throwRay.direction * 10, Color.red, 10f);
            //Debug.DrawRay(forwardRay.origin,forwardRay.direction * 10, Color.blue, 10f);
            /*
            if (angleOff < 0)
            {
                RotateItemByLocalValuesInverse();
            }
            else
            {
                RotateItemByLocalValues();
            }
            */
        }

        #endregion
        protected float TrySnapMovement(float delta, float snapValue)
        {
            if (Mathf.Abs(delta) >= snapValue)
            {
                return Mathf.Round(delta / snapValue) * snapValue;
            }
            return 0;
        }
        

        protected Quaternion ApplyRotationSnappingAndRestrictions(Quaternion deltaRotation, Quaternion baseRotation)
        {
            Vector3 deltaEuler = deltaRotation.eulerAngles;

            // Snap rotation
            deltaEuler.x = TrySnapRotation(deltaEuler.x, RotationSnap.x);
            deltaEuler.y = TrySnapRotation(deltaEuler.y, RotationSnap.y);
            deltaEuler.z = TrySnapRotation(deltaEuler.z, RotationSnap.z);

            // Apply rotation restrictions
            Vector3 baseEuler = baseRotation.eulerAngles;
            if (XRotationLocked) deltaEuler.x = baseEuler.x;
            if (YRotationLocked) deltaEuler.y = baseEuler.y;
            if (ZRotationLocked) deltaEuler.z = baseEuler.z;

            // Combine base rotation and delta rotation
            return Quaternion.Euler(baseEuler + deltaEuler);
        }

        protected float TrySnapRotation(float delta, float snapValue)
        {
            // Ensure delta is within 0-360 range
            delta = NormalizeAngle(delta);

            if (Mathf.Abs(delta) >= snapValue)
            {
                return Mathf.Round(delta / snapValue) * snapValue;
            }
            return 0;
        }

        protected float NormalizeAngle(float angle)
        {
            while (angle > 360) angle -= 360;
            while (angle < 0) angle += 360;
            return angle;
        }

    }
}
