using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class BallToEntity : MonoBehaviour
{
    EntityManager em;
    EntityArchetype archetype;

    public Mesh mesh;
    public Material mat;
    void Start()
    {
        em = World.Active.EntityManager;
        archetype = em.CreateArchetype(typeof(RenderMesh), typeof(Translation), typeof(BallComponent), typeof(LocalToWorld));
        var entity = em.CreateEntity(archetype);

        em.SetSharedComponentData<RenderMesh>(entity, new RenderMesh { mesh = mesh, material = mat });

        em.SetComponentData<Translation>(entity, new Translation { Value = new float3(0, -2, 0) });
        em.SetComponentData<LocalToWorld>(entity, new LocalToWorld { });
        em.SetComponentData<BallComponent>(entity, new BallComponent { dir = new float2(1, 1), speed = 1.0f });



    }

}