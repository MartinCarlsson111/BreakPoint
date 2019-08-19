using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
public class BricksToEntities : MonoBehaviour
{
    EntityManager em;
    EntityArchetype archetype;

    public Mesh mesh;
    public Material mat;

    public float2 extents;
    void Start()
    {
        em = World.Active.EntityManager;
        archetype = em.CreateArchetype(typeof(Translation), typeof(RenderMesh), typeof(Bricks), typeof(LocalToWorld), typeof(AABB));

        for (int i = 3; i < extents.x + 3; i++)
        {
            for (int j = 0; j < extents.y; j++)
            {
                var entity = em.CreateEntity(archetype);
                em.SetComponentData<Bricks>(entity, new Bricks { state = 0, value = 0 });
                em.SetComponentData<Translation>(entity, new Translation { Value = new float3(i, j, 0) });
                em.SetSharedComponentData<RenderMesh>(entity, new RenderMesh { mesh = mesh, material = mat });
                em.SetComponentData<LocalToWorld>(entity, new LocalToWorld { });
            }
        }

        var e = em.CreateEntity(archetype);
        em.SetComponentData<Bricks>(e, new Bricks { state = 0, value = 0 });
        em.SetComponentData<Translation>(e, new Translation { Value = new float3(1, 1, 0) });
        em.SetSharedComponentData<RenderMesh>(e, new RenderMesh { mesh = mesh, material = mat });
        em.SetComponentData<LocalToWorld>(e, new LocalToWorld { });
    }

}