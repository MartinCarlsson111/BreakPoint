using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

//Handle keybinding
//Gather inputs each frame and send to ECS

public struct Player : IComponentData
{
    public float3 move;
    public int jump;
    //actions
}


public struct Actions
{
    //spell id
    //cooldown
    //
}




public class PlayerInputs : ComponentSystem
{
    struct PlayerData
    {
        public readonly int Length;
        public ComponentDataArray<Player> Input;
    }

    struct TopDownCameraData
    {
        public readonly int Length;
        public ComponentDataArray<TopDownCamera> Camera;
    }

    struct ThirdPersonCameraData
    {
        public readonly int Length;
        public ComponentDataArray<ThirdPersonCamera> Camera;
    }


    [Inject] private PlayerData playerData;
    [Inject] private ThirdPersonCameraData thirdPersonCameraData;
    [Inject] private TopDownCameraData topDownCameraData;
    protected override void OnUpdate()
    {
        for(int i = 0; i < playerData.Length; ++i)
        {
            UpdatePlayerInput(i);
        }
    }

    private void UpdatePlayerInput(int i)
    {
        if(Camera.main != null)
        {
            if(thirdPersonCameraData.Length == 1)
            {
                KeyCode wKey = KeyCode.W;
                KeyCode sKey = KeyCode.S;
                KeyCode dKey = KeyCode.D;
                KeyCode aKey = KeyCode.A;
                KeyCode qKey = KeyCode.Q;
                KeyCode eKey = KeyCode.E;
                KeyCode jumpKey = KeyCode.Space;

                var newMove = new Player();
                newMove = playerData.Input[i];
                newMove.move = float3.zero;

                if(Input.GetKey(wKey))
                {
                    newMove.move.z += 1;
                }
                if(Input.GetKey(sKey))
                {
                    newMove.move.z -= 1;
                }
                if(Input.GetKey(dKey))
                {
                    newMove.move.x += 1;
                }
                if(Input.GetKey(aKey))
                {
                    newMove.move.x -= 1;
                }
                if(Input.GetKey(qKey))
                {
                    newMove.move.z += 1;
                    newMove.move.x -= 1;
                }
                if(Input.GetKey(eKey))
                {
                    newMove.move.z += 1;
                    newMove.move.x += 1;
                }
                if(newMove.move.x == 0 && newMove.move.y == 0 && newMove.move.z == 0)
                {

                }
                else
                {
                    newMove.move = math.normalize(newMove.move);
                }


                if(Input.GetKeyDown(jumpKey))
                {
                    newMove.jump = 1;
                    //Jump
                }
                else
                {
                    newMove.jump = 0;
                }

                playerData.Input[i] = newMove;
            }
            else if(thirdPersonCameraData.Length > 1)
            {
                Debug.Log("Too many ThirdPersonCamera components have been added");
            }

            else if(topDownCameraData.Length == 1)
            {
                if(Input.GetMouseButtonDown(1))
                {
                    Player pi = new Player();
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    RaycastHit hit = new RaycastHit();
                    Physics.Raycast(ray.origin, ray.direction, out hit);

                    pi.move = hit.point;
                    playerData.Input[i] = pi;
                }
            }
            else if(topDownCameraData.Length > 1)
            {
                Debug.Log("Too many TopDownCamera components have been added");
            }


            else
            {
                Debug.Log("There are no camera components attached to an entity");
            }

            /*
            pi.Move.x = Input.GetAxis("Horizontal");
            pi.Move.y = 0.0f;
            pi.Move.z = Input.GetAxis("Vertical");
            pi.Shoot.x = Input.GetAxis("ShootX");
            pi.Shoot.y = 0.0f;
            pi.Shoot.z = Input.GetAxis("ShootY");
            pi.FireCooldown = Mathf.Max(0.0f, m_Players.Input[i].FireCooldown - dt);

            m_Players.Input[i] = pi;*/
        }
        else
        {
            Debug.Log("Scene contains no camera");
        }
    }


}
