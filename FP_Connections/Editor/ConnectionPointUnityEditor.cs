using UnityEditor;
using UnityEngine;

namespace FuzzPhyte.Connections
{
    [CustomEditor(typeof(ConnectionPointUnity))]
    public class ConnectionPointUnityEditor : Editor
    {
        private void OnSceneGUI()
        {
            // Get the target object
            ConnectionPointUnity connectionPoint = (ConnectionPointUnity)target;

            if (connectionPoint.ConnectionPointData != null)
            {
                // Draw the local forward direction using Handles
                Handles.color = Color.magenta;
                Vector3 forwardDirection = connectionPoint.transform.TransformDirection(connectionPoint.ConnectionPointData.localForward);
                Handles.ArrowHandleCap(0, connectionPoint.transform.position, Quaternion.LookRotation(forwardDirection), 0.25f, EventType.Repaint);

                // Draw valid rotations relative to localForward
                Handles.color = Color.yellow;
                foreach (var angle in connectionPoint.ConnectionPointData.localRotationAngles)
                {
                    Quaternion rotation = Quaternion.Euler(angle);
                    Vector3 direction = connectionPoint.transform.TransformDirection(rotation * connectionPoint.ConnectionPointData.localForward);
                    Handles.ArrowHandleCap(0, connectionPoint.transform.position, Quaternion.LookRotation(direction), 0.1f, EventType.Repaint);
                }

                // Draw a sphere at the connection point
                switch (connectionPoint.ConnectionPointStatusPt)
                {
                    case ConnectionPointStatus.None:
                        Handles.color = Color.red;
                        break;
                    case ConnectionPointStatus.Aligned:
                        Handles.color = Color.cyan;
                        break;
                    case ConnectionPointStatus.Connected:
                        Handles.color = Color.green;
                        break;
                }
                //Handles.color = connectionPoint.IsConnected ? Color.red : Color.cyan;
                Handles.SphereHandleCap(0, connectionPoint.transform.position, Quaternion.identity, connectionPoint.ConnectionPointData.width * 0.01f, EventType.Repaint);
            }
        }
    }
}
