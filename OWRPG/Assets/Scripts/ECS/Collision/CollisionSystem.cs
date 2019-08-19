using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CollisionEventSystem : JobComponentSystem
{
    public struct CEData
    {
        public readonly int Length;
        public BufferArray<CollisionEvent> Events;
    }
    [Inject] CEData data;


    public struct CEClearEventsJob : IJobParallelFor
    {
        public BufferArray<CollisionEvent> Events;
        public void Execute(int i)
        {
            if(Events[i].Length > 0)
            {
                Events[i].RemoveRange(0, Events[i].Length);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new CEClearEventsJob { Events = data.Events}.Schedule(data.Length, 32, inputDeps);
    }
}

public enum CollisionState
{
    Enter,
    Exit, 
    Stay
}

public class CollisionDetectionSystem : JobComponentSystem
{
    public struct CollisionDetectionData
    {
        public readonly int Length;

        public ComponentDataArray<EntityTag> EntityTag;
        public ComponentDataArray<Position> Position;
        public ComponentDataArray<Rotation> Rotation;
        public ComponentDataArray<Scale> Scale;
        public ComponentDataArray<Shape> Shape;
        public BufferArray<CollisionEvent> Events;
    }
    public struct EntitiesData
    {
        [ReadOnly] public EntityArray Entities;
        [ReadOnly] public BufferArray<ShapeData> ShapeData;
    }

    [Inject] CollisionDetectionData data;
    [Inject] EntitiesData ShapeEntity;
    [Inject] public BufferFromEntity<ShapeData> ShapeData;

    [BurstCompile]
    public struct NarrowPhase : IJobParallelFor
    {
        const int MAX_ITERATIONS = 30;
        public struct GJKStruct
        {
            public float3 v;
            public float3 b, c, d;
            public uint n; 
        }

        [ReadOnly] public int Length;


        public ComponentDataArray<EntityTag> EntityTag;
        public ComponentDataArray<Position> Position;
        public ComponentDataArray<Rotation> Rotation;
        public ComponentDataArray<Scale> Scale;
        public ComponentDataArray<Shape> ShapeReference;
        public BufferArray<CollisionEvent> Events;

        [NativeDisableParallelForRestriction]
        public BufferFromEntity<ShapeData> ShapeData;
        [ReadOnly] public EntityArray Entities;

        public void Execute(int i)
        {
            int eA = 0;
            for(; eA < Entities.Length; eA++)
            {
                if(Entities[eA].Index == ShapeReference[i].id)
                {
                    break;
                }
            }
            var aShapeDataEntity = Entities[eA];

            if(ShapeData.Exists(aShapeDataEntity))
            {
                if(ShapeData[aShapeDataEntity].Length > 0 && Length > 1)
                {
                    for(int x = 0; x < Length; x++)
                    {
                        if(x == i)
                        {
                            continue;
                        }

                        int eB = 0;
                        for(; eB < Entities.Length; eB++)
                        {
                            if(Entities[eB].Index == ShapeReference[x].id)
                            {
                                break;
                            }
                        }
                        var bShapeDataEntity = Entities[eB];
                        if(ShapeData.Exists(bShapeDataEntity))
                        {
                            if(ShapeData[bShapeDataEntity].Length > 0)
                            {
                                GJKStruct gjkstruct = new GJKStruct();
                                gjkstruct.v = float3.zero;
                                gjkstruct.b = float3.zero;
                                gjkstruct.c = float3.zero;
                                gjkstruct.d = float3.zero;
                                gjkstruct.n = 0;

                                var modelMatrix1 = new float4x4(Rotation[i].Value, Position[i].Value);
                                modelMatrix1.c0.x = Scale[i].Value.x;
                                modelMatrix1.c1.y = Scale[i].Value.y;
                                modelMatrix1.c2.z = Scale[i].Value.z;

                                var modelMatrix2 = new float4x4(Rotation[x].Value, Position[x].Value);
                                modelMatrix2.c0.x = Scale[x].Value.x;
                                modelMatrix2.c1.y = Scale[x].Value.y;
                                modelMatrix2.c2.z = Scale[x].Value.z;

                                if(Intersect(ShapeData[aShapeDataEntity], modelMatrix1, ShapeData[bShapeDataEntity], modelMatrix2, ref gjkstruct))
                                {
                                    CollisionEvent e = new CollisionEvent();
                                    e.entityID = EntityTag[x].id;
                                    e.otherLayer = EntityTag[x].layer;
                                    e.point = gjkstruct.b;
                                    Events[i].Add(e);
                                }
                            }
                        }
                    }
                }
            }
        }

        bool Intersect(DynamicBuffer<ShapeData> mesh1, float4x4 modelMatrix1, DynamicBuffer<ShapeData> mesh2, float4x4 modelMatrix2, ref GJKStruct GJKData)
        {
            GJKData.v = new float3(1, 0, 0);//initial vector
            GJKData.n = 0;//set simplex size 0

            GJKData.c = support(mesh1, modelMatrix1, mesh2, modelMatrix2, GJKData.v);

            if(math.dot(GJKData.c, GJKData.v) < 0)
            {
                return false;
            }
            GJKData.v = -GJKData.c;
            GJKData.b = support(mesh1, modelMatrix1, mesh2, modelMatrix2, GJKData.v);

            if(math.dot(GJKData.b, GJKData.v) < 0)
            {
                return false;
            }
            GJKData.v = tripleProduct(GJKData.c - GJKData.b, -GJKData.b);
            GJKData.n = 2;

            for(int i = 0; i < MAX_ITERATIONS; ++i)
            {
                float3 a = support(mesh1, modelMatrix1, mesh2, modelMatrix2, GJKData.v);
                if(math.dot(a, GJKData.v) < 0)
                {
                    // no intersection
                    return false;
                }

                if(update(a, ref GJKData))
                {
                    return true;
                }
            }
            return true;
        }

        float3 GetFarthestPointInDirection(float3 v, float4x4 modelMatrix, DynamicBuffer<ShapeData> mesh1)
        {

            float3 vert = mesh1[0].vertice;

            float4 p = math.mul(modelMatrix, new float4(vert.x, vert.y, vert.z, 1));
            float3 worldVert = p.xyz;
            float3 best = worldVert;
            double farthest = math.dot(worldVert, v);

            for(int i = 1; i < mesh1.Length; i++)
            {
                vert = mesh1[i].vertice;
                p = math.mul(modelMatrix, new float4(vert.x, vert.y, vert.z, 1));
                worldVert.x = p[0];
                worldVert.y = p[1];
                worldVert.z = p[2];
                double d = math.dot(worldVert, v);
                if(farthest < d)
                {
                    best = worldVert;
                    farthest = d;
                }
            }

            return best;
        }
        float3 support(DynamicBuffer<ShapeData> mesh1, float4x4 modelMatrix1, DynamicBuffer<ShapeData> mesh2, float4x4 modelMatrix2, float3 v)
        {
            float3 p1 = GetFarthestPointInDirection(v, modelMatrix1, mesh1);
            float3 p2 = GetFarthestPointInDirection(-v, modelMatrix2, mesh2);
            float3 p3 = p1 - p2;
            return p3;
        }
        float3 tripleProduct(float3 ab, float3 c)
        {
            return math.cross(math.cross(ab, c), ab);
        }

        bool update(float3 a, ref GJKStruct GJKData)
        {
            if(GJKData.n == 2)
            {
                //handling triangle
                float3 ao = -a;
                float3 ab = GJKData.b - a;
                float3 ac = GJKData.c - a;

                float3 abc = math.cross(ab, ac);//normal of triangle abc

                // plane test on edge ab
                float3 abp = math.cross(ab, abc);//direction vector pointing inside triangle abc from ab
                if(math.dot(abp, ao) > 0)
                {
                    //origin lies outside the triangle abc, near the edge ab
                    GJKData.c = GJKData.b;
                    GJKData.b = a;
                    GJKData.v = tripleProduct(ab, ao);
                    return false;
                }

                //plane test on edge ac

                //direction vector pointing inside triangle abc from ac
                //note that different than abp, the result of acp is abc cross ac, while abp is ab cross abc.
                //The order does matter. Based on the right-handed rule, we want the vector pointing inside the triangle.
                float3 acp = math.cross(abc, ac);
                if(math.dot(acp, ao) > 0)
                {
                    //origin lies outside the triangle abc, near the edge ac
                    GJKData.b = a;
                    GJKData.v = tripleProduct(ac, ao);
                    return false;
                }

                // Now the origin is within the triangle abc, either above or below it.
                if(math.dot(abc, ao) > 0)
                {
                    //origin is above the triangle
                    GJKData.d = GJKData.c;
                    GJKData.c = GJKData.b;
                    GJKData.b = a;
                    GJKData.v = abc;
                }
                else
                {
                    //origin is below the triangle
                    GJKData.d = GJKData.b;
                    GJKData.b = a;
                    GJKData.v = -abc;
                }

                GJKData.n = 3;

                return false;

            }

            if(GJKData.n == 3)
            {
                float3 ao = -a;
                float3 ab = GJKData.b - a;
                float3 ac = GJKData.c - a;
                float3 ad = GJKData.d - a;

                float3 abc = math.cross(ab, ac);
                float3 acd = math.cross(ac, ad);
                float3 adb = math.cross(ad, ab);

                float3 tmp;

                const int over_abc = 0x1;
                const int over_acd = 0x2;
                const int over_adb = 0x4;

                int plane_tests =
                    (math.dot(abc, ao) > 0 ? over_abc : 0) |
                    (math.dot(acd, ao) > 0 ? over_acd : 0) |
                    (math.dot(adb, ao) > 0 ? over_adb : 0);

                switch(plane_tests)
                {
                    case 0:
                    {
                        //inside the tetrahedron
                        return true;
                    }
                    case over_abc:
                    {
                        if(!checkOneFaceAC(abc, ac, ao, ref GJKData))
                        {
                            //in the region of AC
                            return false;
                        }
                        if(!checkOneFaceAB(abc, ab, ao, ref GJKData))
                        {
                            //in the region of AB
                            return false;
                        }

                        //otherwise, in the region of ABC
                        GJKData.d = GJKData.c;
                        GJKData.c = GJKData.b;
                        GJKData.b = a;
                        GJKData.v = abc;
                        GJKData.n = 3;
                        return false;
                    }
                    case over_acd:
                    {
                        //rotate acd to abc, and perform the same procedure
                        GJKData.b = GJKData.c;
                        GJKData.c = GJKData.d;

                        ab = ac;
                        ac = ad;

                        abc = acd;

                        if(!checkOneFaceAC(abc, ac, ao, ref GJKData))
                        {
                            //in the region of AC (actually is ad)
                            return false;
                        }
                        if(!checkOneFaceAB(abc, ab, ao, ref GJKData))
                        {
                            //in the region of AB (actually is ac)
                            return false;
                        }

                        //otherwise, in the region of "ABC" (which is actually acd)
                        GJKData.d = GJKData.c;
                        GJKData.c = GJKData.b;
                        GJKData.b = a;
                        GJKData.v = abc;
                        GJKData.n = 3;
                        return false;

                    }
                    case over_adb:
                    {
                        //rotate adb to abc, and perform the same procedure
                        GJKData.c = GJKData.b;
                        GJKData.b = GJKData.d;

                        ac = ab;
                        ab = ad;

                        abc = adb;
                        if(!checkOneFaceAC(abc, ac, ao, ref GJKData))
                        {
                            //in the region of "AC" (actually is AB)
                            return false;
                        }
                        if(!checkOneFaceAB(abc, ab, ao, ref GJKData))
                        {
                            //in the region of AB (actually is AD)
                            return false;
                        }

                        //otherwise, in the region of "ABC" (which is actually acd)
                        GJKData.d = GJKData.c;
                        GJKData.c = GJKData.b;
                        GJKData.b = a;
                        GJKData.v = abc;
                        GJKData.n = 3;
                        return false;
                    }
                    case over_abc | over_acd:
                    {
                        if(!checkTwoFaces(abc, acd, ac, ab, ad, ao, ref GJKData))
                        {
                            if(!checkOneFaceAC(abc, ac, ao, ref GJKData))
                            {
                                //in the region of "AC" (actually is AB)
                                return false;
                            }
                            if(!checkOneFaceAB(abc, ab, ao, ref GJKData))
                            {
                                //in the region of AB (actually is AD)
                                return false;
                            }
                            //otherwise, in the region of "ABC" (which is actually acd)
                            GJKData.d = GJKData.c;
                            GJKData.c = GJKData.b;
                            GJKData.b = a;
                            GJKData.v = abc;
                            GJKData.n = 3;
                            return false;
                        }
                        else
                        {
                            if(!checkOneFaceAB(abc, ab, ao, ref GJKData))
                            {
                                return false;
                            }
                            GJKData.d = GJKData.c;
                            GJKData.c = GJKData.b;
                            GJKData.b = a;
                            GJKData.v = abc;
                            GJKData.n = 3;
                            return false;
                        }
                    }
                    case over_acd | over_adb:
                    {
                        //rotate ACD, ADB into ABC, ACD
                        tmp = GJKData.b;
                        GJKData.b = GJKData.c;
                        GJKData.c = GJKData.d;
                        GJKData.d = tmp;

                        tmp = ab;
                        ab = ac;
                        ac = ad;
                        ad = tmp;

                        abc = acd;
                        acd = adb;
                        if(!checkTwoFaces(abc, acd, ac, ab, ad, ao, ref GJKData))
                        {
                            if(!checkOneFaceAC(abc, ac, ao, ref GJKData))
                            {
                                //in the region of "AC" (actually is AB)
                                return false;
                            }
                            if(!checkOneFaceAB(abc, ab, ao, ref GJKData))
                            {
                                //in the region of AB (actually is AD)
                                return false;
                            }
                            //otherwise, in the region of "ABC" (which is actually acd)
                            GJKData.d = GJKData.c;
                            GJKData.c = GJKData.b;
                            GJKData.b = a;
                            GJKData.v = abc;
                            GJKData.n = 3;
                            return false;
                        }
                        else
                        {
                            if(!checkOneFaceAB(abc, ab, ao, ref GJKData))
                            {
                                return false;
                            }
                            GJKData.d = GJKData.c;
                            GJKData.c = GJKData.b;
                            GJKData.b = a;
                            GJKData.v = abc;
                            GJKData.n = 3;
                            return false;
                        }
                    }
                    case over_adb | over_abc:
                    {
                        //rotate ADB, ABC into ABC, ACD
                        tmp = GJKData.c;
                        GJKData.c = GJKData.b;
                        GJKData.b = GJKData.d;
                        GJKData.d = tmp;

                        tmp = ac;
                        ac = ab;
                        ab = ad;
                        ad = tmp;

                        acd = abc;
                        abc = adb;

                        if(!checkTwoFaces(abc, acd, ac, ab, ad, ao, ref GJKData))
                        {
                            if(!checkOneFaceAC(abc, ac, ao, ref GJKData))
                            {
                                //in the region of "AC" (actually is AB)
                                return false;
                            }
                            if(!checkOneFaceAB(abc, ab, ao, ref GJKData))
                            {
                                //in the region of AB (actually is AD)
                                return false;
                            }
                            //otherwise, in the region of "ABC" (which is actually acd)
                            GJKData.d = GJKData.c;
                            GJKData.c = GJKData.b;
                            GJKData.b = a;
                            GJKData.v = abc;
                            GJKData.n = 3;
                            return false;
                        }
                        else
                        {
                            if(!checkOneFaceAB(abc, ab, ao, ref GJKData))
                            {
                                return false;
                            }
                            GJKData.d = GJKData.c;
                            GJKData.c = GJKData.b;
                            GJKData.b = a;
                            GJKData.v = abc;
                            GJKData.n = 3;
                            return false;
                        }
                    }
                    default:
                    return true;
                }
            }
            return true;
        }
        bool checkOneFaceAC(float3 abc, float3 ac, float3 ao, ref GJKStruct GJKData)
        {
            if(math.dot(math.cross(abc, ac), ao) > 0)
            {
                //origin is in the region of edge ac
                GJKData.b = -ao;//b=a
                GJKData.v = tripleProduct(ac, ao);
                GJKData.n = 2;

                return false;
            }
            return true;
        }
        bool checkOneFaceAB(float3 abc, float3 ab, float3 ao, ref GJKStruct GJKData)
        {
            if(math.dot(math.cross(ab, abc), ao) > 0)
            {
                //origin in the region of edge ab
                GJKData.c = GJKData.b;
                GJKData.b = -ao;//b=a
                GJKData.v = tripleProduct(ab, ao);
                GJKData.n = 2;

                return false;
            }
            return true;
        }
        bool checkTwoFaces(float3 abc, float3 acd, float3 ac, float3 ab, float3 ad, float3 ao, ref GJKStruct GJKData)
        {
            if(math.dot(math.cross(abc, ac), ao) > 0)
            {
                GJKData.b = GJKData.c;
                GJKData.c = GJKData.d;
                ab = ac;
                ac = ad;

                abc = acd;
                return false;
            }
            return true;
        }
         
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new NarrowPhase
        {
            Length = data.Length,
            ShapeReference = data.Shape,
            Entities = ShapeEntity.Entities,
            ShapeData = ShapeData,
            EntityTag = data.EntityTag,
            Events = data.Events,
            Position = data.Position,
            Rotation = data.Rotation,
            Scale = data.Scale
        }.Schedule(data.Length, 64, inputDeps);
    }

}
