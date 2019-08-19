using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

public class FlippersToEntity : MonoBehaviour
{
    EntityManager em;
    EntityArchetype archetype;

    public Mesh mesh;
    public Material mat;
    void Start()
    {
        em = World.Active.EntityManager;
        archetype = em.CreateArchetype(typeof(RenderMesh), typeof(Translation), typeof(Flipper), typeof(Rotation), typeof(LocalToWorld));

        var left = em.CreateEntity(archetype);
        var right = em.CreateEntity(archetype);

        float flip = math.radians(180);
        

        quaternion flipQuat = quaternion.AxisAngle(new float3(0, 1, 0), flip);
        em.SetSharedComponentData<RenderMesh>(left, new RenderMesh { mesh = mesh, material = mat });
        em.SetSharedComponentData<RenderMesh>(right, new RenderMesh { mesh = mesh, material = mat });

        em.SetComponentData<Translation>(left, new Translation { Value = new float3(-1.25f, 0, -1) });
        em.SetComponentData<Translation>(right, new Translation { Value = new float3(1.25f, 0, -1) });

        em.SetComponentData<Flipper>(left, new Flipper { extendedRotation = quaternion.Euler(0, 0, math.radians(90)), oppositeRotation = quaternion.Euler(0, 0, -math.radians(90)), originRot = quaternion.identity, speed = 1.0f, time = 0.0f });
        em.SetComponentData<Flipper>(right, new Flipper { extendedRotation = quaternion.Euler(0, math.radians(180), math.radians(90)), oppositeRotation = math.inverse(quaternion.Euler(0, math.radians(180), math.radians(90))),
            originRot = flipQuat, speed = 1.0f, time = 0.0f });

        em.SetComponentData<Rotation>(left, new Rotation {Value = quaternion.identity });
        em.SetComponentData<Rotation>(right, new Rotation { Value = flipQuat });

        em.SetComponentData<LocalToWorld>(left, new LocalToWorld {});
        em.SetComponentData<LocalToWorld>(right, new LocalToWorld {});  
    }
}
