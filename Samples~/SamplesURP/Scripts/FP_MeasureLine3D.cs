namespace FuzzPhyte.Tools.Samples
{
    using FuzzPhyte.Utility;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    public class FP_MeasureLine3D: FPTwoPointLine<FP_MeasureToolData>
    {
        [Header("3D World Points")]
        public GameObject StartPointDecal;
        public GameObject EndPointDecal;
        public GameObject LineParent;
        public DecalProjector LineDecal;
        [Header("Measurement")]
        public TextMeshPro MeasurementText;
        [SerializeField]protected Camera toolCam;

        public override void Setup(FP_Tool<FP_MeasureToolData> tool)
        {
            myTool = tool;
            FP_MeasureToolData data = tool.ToolData;
            LineDecal.material = data.LineMat;
            LineDecal.size = new Vector3( data.lineWidth,0,data.lineDepth);
            FP_UtilityData.ApplyFontSetting(MeasurementText, data.MeasurementFontSetting);

            // Apply Decal color if they have a Renderer
            if (StartPointDecal.TryGetComponent(out Renderer r1))
                r1.material.color = data.lineColor;

            if (EndPointDecal.TryGetComponent(out Renderer r2))
                r2.material.color = data.lineColor;

            // Register listeners
            myTool.OnActivated += OnToolActivated;
            myTool.OnStarting += OnToolStarting;
            myTool.OnActiveUse += OnToolInActiveUse;
            myTool.OnEnding += OnToolEnding;
            myTool.OnDeactivated += OnToolDeactivated;
        }
        /// <summary>
        /// Pass in the camera relative our system
        /// </summary>
        /// <param name="theToolCam"></param>
        public void CameraPass(Camera theToolCam)
        {
            toolCam = theToolCam;
        }
        public override void DropFirstPoint(Vector3 worldPos)
        {
            StartPointDecal.transform.position = worldPos;
            StartPointDecal.SetActive(true);
            EndPointDecal.SetActive(true);
            LineParent.gameObject.transform.position = worldPos;
            LineDecal.size = new Vector3(myTool.ToolData.lineWidth,0,myTool.ToolData.lineDepth);
        }

        public override void DropSecondPoint(Vector3 worldPos)
        {
            EndPointDecal.transform.position = worldPos;
            //need to update our URP decal rotation relative the camera right/left and the points
            //UpdateDecal(StartPointDecal.transform.position, worldPos);
            var p1 = StartPointDecal.transform.position;
            var p2 = worldPos;
            Vector3 direction = (p2 - p1);
            float distance = direction.magnitude;

            // Midpoint
            Vector3 midPoint = (p1 + p2) * 0.5f;
            LineParent.transform.position = midPoint;
            Vector3 dirNormalized = direction.normalized;
            LineDecal.size = new Vector3(
               myTool.ToolData.lineWidth,      // Width stays fixed (in decal's local X)
               distance,            // Height adapts (in decal's local Y)
               myTool.ToolData.lineDepth // Keep your projection depth
            );

            Vector3 camRight = toolCam.transform.right;
            float camRightness = Vector3.Dot(direction, camRight);
            LineParent.transform.rotation = Quaternion.LookRotation(dirNormalized, Vector3.up);
            if (camRightness < 0)
            {
                LineParent.transform.localRotation *= Quaternion.Euler(0f, 180f, 0f);
            }
        }

        public override void UpdateText(string text)
        {
            MeasurementText.text = text;
        }
        public void UpdateTextLocation(Vector3 offsetValue)
        {
            MeasurementText.transform.localPosition = offsetValue;
        }
        public override void OnToolDeactivated(FP_Tool<FP_MeasureToolData> tool)
        {
            if (tool != myTool) return;
        }
    }
}
