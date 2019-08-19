using UnityEngine;
using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;

public class Bootstrap : MonoBehaviour
{
    public float3 spawnPosition;
    public quaternion spawnRotation;

    public Mesh playerMesh;
    public Material playerMat;
    public MeshCollider playerCollider;

    public MeshCollider zoneCollider;
    public Mesh zoneMesh;
    public Material zoneMaterial;

    EntityManager em;

    void Start()
    {
        em = World.Active.GetOrCreateManager<EntityManager>();
        var playerColliderIndex = AddShapeData(playerCollider);
        var zoneColliderIndex = AddShapeData(zoneCollider);

        var staticArchetype = em.CreateArchetype(
            typeof(EntityTag),
            typeof(Position),
            typeof(Rotation),
            typeof(Scale),
            typeof(Health),
            typeof(HealthEffect),
            typeof(Stats),
            typeof(MeshInstanceRenderer),
            typeof(MovementSpeed),
            typeof(BuffEffect),
            typeof(NavAgent),
            typeof(NavAgentPath),
            typeof(Shape),
            typeof(CollisionEvent),
            typeof(DeathReward),
            typeof(HealthBar)
            );

                var blizzardArchetype = em.CreateArchetype(typeof(EntityTag),
            typeof(Position),
            typeof(Rotation),
            typeof(Scale),
            typeof(CollisionEvent),
            typeof(Shape),
            typeof(SpellEffectZone),
            typeof(MeshInstanceRenderer)
            );


        var playerArchetype = em.CreateArchetype(
            typeof(EntityTag),
            typeof(MeshInstanceRenderer),
            typeof(ThirdPersonCamera),
            typeof(Position),
            typeof(Rotation),
            typeof(Scale),
            typeof(Player),
            typeof(Health),
            typeof(HealthEffect),
            typeof(MovementSpeed),
            typeof(Velocity),
            typeof(Gravity),
            typeof(SleepingStatus),
            typeof(Stats),
            typeof(BuffEffect),
            typeof(Level),
            typeof(Experience),
            typeof(Shape),
            typeof(CollisionEvent),
            typeof(HealthBar)

            );
    
        var collisionShapeDataLookupArchetype = em.CreateArchetype(typeof(ShapeData));

        for(int x = 0; x < 30; x++)
        {
            var staticEntity = em.CreateEntity(staticArchetype);
            SetPosition(staticEntity, new float3(10 + (x * 3), 0, 15) );
            SetScale(staticEntity, new float3(2, 2, 2));
            SetRotation(staticEntity, spawnRotation);
            SetEntityTag(staticEntity, 1);
            SetMeshInstanceRenderer(staticEntity, playerMesh, playerMat);
            SetShapeReference(staticEntity, playerColliderIndex);

            em.SetComponentData<Health>(staticEntity, new Health { maxHP = 100, currentHealth = 100 });
            em.SetComponentData<MovementSpeed>(staticEntity, new MovementSpeed { value = 450 });
            em.SetComponentData<NavAgent>(staticEntity, new NavAgent { destination = float3.zero, areaMask = -1, newDestination = 0 });
        }

        var playerEntity = em.CreateEntity(playerArchetype);
        SetPosition(playerEntity, spawnPosition);
        SetScale(playerEntity, new float3(1, 1, 1));
        SetRotation(playerEntity, spawnRotation);
        SetEntityTag(playerEntity, 0);
        SetMeshInstanceRenderer(playerEntity, playerMesh, playerMat);
        SetShapeReference(playerEntity, playerColliderIndex);

        em.SetComponentData<Level>(playerEntity, new Level { currentLevel = 1, experienceToNextLevel = 100, maxLevel = 30 });
        em.SetComponentData<Health>(playerEntity, new Health { maxHP = 100, currentHealth = 100 });
        em.SetComponentData<MovementSpeed>(playerEntity, new MovementSpeed { value = 300 });
        //em.SetComponentData<NavAgent>(playerEntity, new NavAgent { destination = float3.zero, areaMask = -1, newDestination = 0 });


        SpawnBlizzardEntity(blizzardArchetype, new float3(10, 0, 15), 5, 15, 1, 30, playerEntity, zoneColliderIndex);
    }


    void SpawnBlizzardEntity(EntityArchetype ea, float3 position, float radius, int damage, float interval, int slowPercent, Entity casterEntity, int zoneColliderIndex)
    {
        var blizzardEntity = em.CreateEntity(ea);
        SetPosition(blizzardEntity,position);
        SetScale(blizzardEntity, new float3(radius, 1, radius));
        SetRotation(blizzardEntity, spawnRotation);
        SetEntityTag(blizzardEntity, 2);
        SetSpellEffectZone(blizzardEntity, damage, interval, slowPercent, casterEntity.Index);
        SetShapeReference(blizzardEntity, zoneColliderIndex);
        SetMeshInstanceRenderer(blizzardEntity, zoneMesh, zoneMaterial);
    }

    void SetPosition(Entity e, float3 position)
    {
        em.SetComponentData<Position>(e, new Position { Value = position});
    }

    void SetRotation(Entity e, quaternion q)
    {
        em.SetComponentData<Rotation>(e, new Rotation { Value = q });
    }

    void SetScale(Entity e, float3 scale)
    {
        em.SetComponentData<Scale>(e, new Scale { Value = scale});
    }

    void SetEntityTag(Entity e, uint layer)
    {
        em.SetComponentData<EntityTag>(e, new EntityTag { id = e.Index, layer = layer });
    }

    int AddShapeData(MeshCollider col)
    {
        var e = em.CreateEntity(typeof(ShapeData));
        var zoneBuffer = em.GetBuffer<ShapeData>(e);
        for(int i = 0; i < col.sharedMesh.vertexCount; i++)
        {
            zoneBuffer.Add(new ShapeData { vertice = col.sharedMesh.vertices[i] });
        }
        return e.Index;
    }

    void SetSpellEffectZone(Entity e, int damage, float interval, int slowPercent, int fromEntity)
    {
        em.SetComponentData<SpellEffectZone>(e, new SpellEffectZone { damage = damage, interval = interval, slowPercent = slowPercent, fromEntity = fromEntity });
    }

    void SetShapeReference(Entity entity, int colliderIndex)
    {
        em.SetComponentData<Shape>(entity, new Shape { id = colliderIndex });
    }

    void SetMeshInstanceRenderer( Entity entity, Mesh mesh, Material material)
    {
        em.SetSharedComponentData<MeshInstanceRenderer>(entity, new MeshInstanceRenderer
        {
            mesh = mesh,
            material = material,
            receiveShadows = true,
             castShadows =  UnityEngine.Rendering.ShadowCastingMode.On
        });
    }

}
