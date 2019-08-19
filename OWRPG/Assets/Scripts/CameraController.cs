using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct TopDownCamera : IComponentData
{

}


public struct ThirdPersonCamera : IComponentData
{

}


//Smooth Camera Controls
//Camera Rotation
//Camera Follow
//Camera Shake
public class CameraController : ComponentSystem
{
    public float targetHeight = 1.7f;
	public float distance = 5.0f;
	public float offsetFromWall = 0.1f;

	public float maxDistance = 20;
	public float minDistance = .6f;
	public float speedDistance = 5;

	public float xSpeed = 10.0f;
	public float ySpeed = 10.0f;

	public int yMinLimit = -40;
	public int yMaxLimit = 80;

	public int zoomRate = 40;

	public float rotationDampening = 3.0f;
	public float zoomDampening = 5.0f;

	public LayerMask collisionLayers = -1;

    float3 target = float3.zero;
    quaternion targetRotation = quaternion.identity;
        
    private float xDeg = 0.0f;
	private float yDeg = 0.0f;
	private float currentDistance;
	private float desiredDistance;
	private float correctedDistance;

    struct PlayerData
    {
        public readonly int Length;
        public ComponentDataArray<Player> PlayerComponent;
        public ComponentDataArray<Position> Position;
        public ComponentDataArray<Rotation> Rotation;
    }
    [Inject] private PlayerData playerData;

    struct TopDownCameraData
    {
        public readonly int Length;
        public ComponentDataArray<TopDownCamera> PlayerComponent;
    }

    struct ThirdPersonCameraData
    {
        public readonly int Length;
        public ComponentDataArray<ThirdPersonCamera> Rotation;
    }


    [Inject] private TopDownCameraData topDownData;
    [Inject] private ThirdPersonCameraData thirdPersonData;

    float3 offset = new float3(0, 28, -5.4f);


    protected override void OnCreateManager()
    {
        float3 angles = new float3();

        float sinr_cosp = +2.0f * (targetRotation.value.w * targetRotation.value.x + targetRotation.value.y * targetRotation.value.z);
        float cosr_cosp = +1.0f - 2.0f * (targetRotation.value.x * targetRotation.value.x + targetRotation.value.y * targetRotation.value.y);
        angles.x = math.atan2(sinr_cosp, cosr_cosp);

        // pitch (y-axis rotation)
        float sinp = +2.0f * (targetRotation.value.w *targetRotation.value.y -targetRotation.value.z * targetRotation.value.x);
        if(math.abs(sinp) >= 1)
        {
            float e = 0.0f;
            if (sinp < 0.0)e  = ((float)math.PI / 2.0f) * -1.0f;

            angles.y = e;// use 90 degrees if out of range
        }

        else
            angles.y = math.asin(sinp);
        
        // yaw (z-axis rotation)
        float siny_cosp = +2.0f * (targetRotation.value.w * targetRotation.value.z + targetRotation.value.x * targetRotation.value.y);
        float cosy_cosp = +1.0f - 2.0f * (targetRotation.value.y * targetRotation.value.y + targetRotation.value.z * targetRotation.value.z);
        angles.z = math.atan2(siny_cosp, cosy_cosp);

        xDeg = angles.x;
        yDeg = angles.y;

        currentDistance = distance;
        desiredDistance = distance;
        correctedDistance = distance;
    }

    protected override void OnUpdate()
    {
        float3 vTargetOffset;
        if(playerData.Length != 1)
        {
            return;
        }

        if(topDownData.Length == 1)
        {
            //topdown camera

        }
        else if(thirdPersonData.Length == 1)
        {
            // If either mouse buttons are down, let the mouse govern camera position
            if(GUIUtility.hotControl == 0)
            {
                if(Input.GetMouseButton(0) || Input.GetMouseButton(1))
                {
                    xDeg += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                    yDeg -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                    if(yDeg > math.radians(89.0f))
                    {
                        yDeg = math.radians(89.0f);
                    }

                    if(yDeg < math.radians(-89.0f))
                    {
                        yDeg = math.radians(-89.0f);
                    }
                }

                //Autofollow BLEEEEEH EWW NOOO
                /*
                // otherwise, ease behind the target if any of the directional keys are pressed
                else if(Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0)
                {
                  //  float targetRotationAngle = target.eulerAngles.y;
                  // float currentRotationAngle = transform.eulerAngles.y;
                 //   xDeg = Mathf.LerpAngle(currentRotationAngle, targetRotationAngle, rotationDampening * Time.deltaTime);
                }*/
            }


            // calculate the desired distance
            desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance) * speedDistance;
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);

            yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);

            // set camera rotation
            quaternion rotation = quaternion.Euler(yDeg, xDeg, 0);
            correctedDistance = desiredDistance;

            // calculate desired camera position
            vTargetOffset = new Vector3(0, -targetHeight, 0);
            float3 position = playerData.Position[0].Value - (math.mul(rotation, new float3(0, 0, 1)) * desiredDistance + vTargetOffset);

            // check for collision using the true target's desired registration point as set by user using height
            RaycastHit collisionHit;
            float3 trueTargetPosition = playerData.Position[0].Value - vTargetOffset;

            // if there was a collision, correct the camera position and calculate the corrected distance
            bool isCorrected = false;
            if(Physics.Linecast(trueTargetPosition, position, out collisionHit, collisionLayers.value))
            {
                // calculate the distance from the original estimated position to the collision location,
                // subtracting out a safety "offset" distance from the object we hit.  The offset will help
                // keep the camera from being right on top of the surface we hit, which usually shows up as
                // the surface geometry getting partially clipped by the camera's front clipping plane.
                correctedDistance = Vector3.Distance(trueTargetPosition, collisionHit.point) - offsetFromWall;
                isCorrected = true;
            }

            // For smoothing, lerp distance only if either distance wasn't corrected, or correctedDistance is more than currentDistance
            currentDistance = !isCorrected || correctedDistance > currentDistance ? Mathf.Lerp(currentDistance, correctedDistance, Time.deltaTime * zoomDampening) : correctedDistance;

            // keep within legal limits
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

            // recalculate position based on the new currentDistance
            position = playerData.Position[0].Value - (math.mul(rotation, new float3(0, 0, 1)) * currentDistance + vTargetOffset);

            Camera.main.transform.position = position;
            Camera.main.transform.rotation = rotation;

            if(Input.GetMouseButton(1))
            {
                 float3 forward = Camera.main.transform.forward;
                forward.y = 0;

                var newQuat = quaternion.LookRotation(forward, new float3(0, 1, 0));


                if(float.IsNaN(newQuat.value.x) || float.IsNaN(newQuat.value.y) || float.IsNaN(newQuat.value.z) || float.IsNaN(newQuat.value.w))
                {

                }
                else
                {
                    var newRot = new Unity.Transforms.Rotation { Value = newQuat };
                    playerData.Rotation[0] = newRot;
                }
            }
        }
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if(angle < -360)
            angle += 360;
        if(angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }
}


     /*
     protected override void OnUpdate()
    {

    }
    */