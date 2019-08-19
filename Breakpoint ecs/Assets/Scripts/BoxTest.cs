using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;


//public struct Collision : IComponentData
//{
//    public bool collided;
//}

//public struct AABB : IComponentData
//{
//    public float w, h;
//    public bool collided;
//    public int id;
//    public int bucketId1;
//    public int bucketId2;
//    public int bucketId3;
//    public int bucketId4;


//    public float2 GetMin(float2 xy)
//    {
//        return new float2(xy.x - w, xy.y - h);
//    }
//    public float2 GetMax(float2 xy)
//    {
//        return new float2(xy.x + w, xy.y + h);
//    }
//}

public struct AABB : IComponentData
{
    public float w, h;
    public bool collided;
}

public class BroadPhaseSystem : JobComponentSystem
{
    EntityQuery query;

    protected override void OnCreate()
    {
        query = GetEntityQuery(ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<AABB>());
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var aabbs = query.ToComponentDataArray<AABB>(Allocator.TempJob);
        var translations = query.ToComponentDataArray<Translation>(Allocator.TempJob);
        BoxTest boxTestJob = new BoxTest() { aabbs = aabbs, translations = translations };

        return boxTestJob.Schedule(this, inputDeps);
    }

    public struct BoxTest : IJobForEach<AABB, Translation>
    {
        [ReadOnly] public NativeArray<AABB> aabbs;
        [ReadOnly] public NativeArray<Translation> translations;

        public void Execute(ref AABB c0, ref Translation c1)
        {
            float4 thisAABB = new float4(c1.Value.x, c1.Value.y, c0.w, c0.h);
            for (int i = 0; i < aabbs.Length; i++)
            {
                float4 thatAABB = new float4(translations[i].Value.x, translations[i].Value.y, aabbs[i].w, aabbs[i].h);
                if (AABBTest(thisAABB, thatAABB))
                {
                    c0.collided = true;
                }
            }
        }

        bool AABBTest(float4 a, float4 b)
        {
            return (a.x + a.z > b.x &&
                    b.x + b.z > a.x) &&
                   (a.y + a.w > b.y &&
                    b.y + b.w > a.y);
        }
    }
}



//public class BroadPhaseSystem : JobComponentSystem
//{

//    public struct BucketObject{
//        public float x, y, w, h;
//        public Entity e;


//    }
//    Rectangle WORLDSIZE = new Rectangle(0, 0, 100, 100);

//    const int cellsize = 25;
//    const int worldSize = 1001;
//    const int EMPTY = -worldSize * 2;
//    EntityQuery sizeQueryEntities;

//    protected override void OnCreate()
//    {
//        sizeQueryEntities = GetEntityQuery(ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<AABB>());
//    }

//    protected override void OnDestroy()
//    {

//    }

//    public struct BoxTestJob : IJob
//    {
//        [ReadOnly]public NativeSlice<BucketObject> bucketObjects;
//        public EntityCommandBuffer commandBuffer;
//        public void Execute()
//        {
//            for (int i = 0; i < bucketObjects.Length - 1; i++)
//            {
//                var a = bucketObjects[i];

//                for (int j = i + 1; j < bucketObjects.Length; j++)
//                {
//                    var b = bucketObjects[j];

//                    if(AABBTest(new float4(a.x, a.y, a.w, a.h), new float4(b.x, b.y, b.w, b.h)))
//                    {
//                        commandBuffer.SetComponent(a.e, new Collision() { collided = true });
//                        commandBuffer.SetComponent(b.e, new Collision() { collided = true });
//                    }
//                }
//            }
//            bool AABBTest(float4 a, float4 b)
//            {
//                return (a.x + a.z > b.x &&
//                        b.x + b.z > a.x) &&
//                    (a.y + a.w > b.y &&
//                        b.y + b.w > a.y);
//            }
//        }
//    }

//    public struct CreateBucketsJob : IJob
//    {
//        [ReadOnly]public int EMPTY;
//        [ReadOnly]public NativeArray<int4> indexes;
//        [ReadOnly]public NativeArray<int> start;
//        public NativeArray<BucketObject> bucketsArray;

//        [ReadOnly]public NativeArray<Entity> entities;
//        [ReadOnly]public NativeArray<Translation> translations;
//        [ReadOnly]public NativeArray<AABB> aABBs;

//        public void Execute()
//        {
//            for (int i = 0; i < entities.Length; i++)
//            {
//                BucketObject bucketObject = new BucketObject
//                {
//                    e = entities[i],
//                    x = translations[i].Value.x,
//                    y = translations[i].Value.y,
//                    w = aABBs[i].w,
//                    h = aABBs[i].h
//                };

//                for (int j = 0; j < 4; j++)
//                {
//                    if (indexes[i][j] != EMPTY)
//                    {
//                        if (indexes[i][j] > bucketsArray.Length || indexes[i][j] < 0) continue;
//                        int bucketStart = start[indexes[i][j]];
//                        int bucketEnd = indexes[i][j] + 1 < start.Length ? start[indexes[i][j] + 1] : start.Length-1;
//                        for (int k = bucketStart; k < bucketEnd; k++)
//                        {
//                            if (bucketsArray[k].e != null)
//                            {
//                                bucketsArray[k] = bucketObject;
//                                break;
//                            }
//                        }
//                    }
//                }
//            }
//        }
//    }

//    [BurstCompile]
//    public struct SizeQueryJob : IJob
//    {
//        [ReadOnly]public int cellSize;
//        [ReadOnly]public int width;
//        [ReadOnly]public int EMPTY;
//        [ReadOnly]public NativeArray<Entity> entities;
//        [ReadOnly]public NativeArray<AABB> aabbs;

//        [ReadOnly]public NativeArray<Translation> translations;
//        public NativeHashMap<int, int> bucketSizes;
//        public NativeArray<int4> index;
//        public void Execute()
//        {
//            for (int i = 0; i < entities.Length; i++)
//            {
//                float2 max = aabbs[i].GetMax(translations[i].Value.xy);
//                float2 min = aabbs[i].GetMin(translations[i].Value.xy);
//                float2 leftTop = min;
//                float2 rightTop = new float2(max.x, min.y);
//                float2 leftBottom = new float2(min.x, max.y);
//                float2 rightBottom = max;

//                float2x4 corners = new float2x4(leftTop, rightTop, leftBottom, rightBottom);

//                int4 buckets = new int4(EMPTY);

//                TryAdd(leftTop,      ref   buckets, 0);
//                TryAdd(rightTop,     ref   buckets, 1);
//                TryAdd(leftBottom,   ref   buckets, 2);
//                TryAdd(rightBottom,  ref   buckets, 3);

//                index[i] = buckets;
//            }
//        }

//        public void TryAdd(float2 c, ref int4 bucket, int index)
//        {
//            bucket[index] = AddBucket(c);
//            for (int i = 0; i < index; i++)
//            {
//                if (bucket[i] == bucket[index])
//                {
//                    bucket[index] = EMPTY;
//                    return;
//                }

//            }
//            if (!bucketSizes.TryAdd(bucket[index], 1))
//            {
//                bucketSizes[bucket[index]]++;
//            }
//        }

//        public int AddBucket(float2 pos)
//        {
//            return (int)(math.floor(pos.x / cellSize) +
//                math.floor(pos.y / cellSize) * width);
//        }
//    }

//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        NativeHashMap<int, int> bucketSizes = new NativeHashMap<int, int>(worldSize / cellsize, Allocator.TempJob);
//        var aabbs = sizeQueryEntities.ToComponentDataArray<AABB>(Allocator.TempJob);
//        var translations = sizeQueryEntities.ToComponentDataArray<Translation>(Allocator.TempJob);
//        var entities = sizeQueryEntities.ToEntityArray(Allocator.TempJob);
//        NativeArray<int4> indexes = new NativeArray<int4>(entities.Length, Allocator.TempJob);
//        var sizeQueryJob = new SizeQueryJob()
//        {
//            cellSize = cellsize,
//            width = worldSize,
//            entities = entities,
//            aabbs = aabbs,
//            translations = translations,
//            bucketSizes = bucketSizes,
//            index = indexes,
//            EMPTY = EMPTY
//        };

//        var sizeQueryJobHandle = sizeQueryJob.Schedule(inputDeps);
//        sizeQueryJobHandle.Complete();
//        var valueArray = bucketSizes.GetValueArray(Allocator.TempJob);
//        int totalSize = 0;
//        NativeArray<int> indices = new NativeArray<int>(valueArray.Length, Allocator.TempJob);
//        int i = 0;
//        foreach (var vArray in valueArray)
//        {
//            indices[i] = totalSize;
//            totalSize += vArray;
//            i++;
//        }

//        NativeArray<BucketObject> buckets = new NativeArray<BucketObject>(totalSize, Allocator.TempJob);

//        var createBucketsJob = new CreateBucketsJob()
//        {
//            translations = translations,
//            aABBs = aabbs,
//            entities = entities,
//            indexes = indexes,
//            start = indices,
//            bucketsArray = buckets,
//            EMPTY = EMPTY
//        };
//        var createBucketsJobHandle = createBucketsJob.Schedule(sizeQueryJobHandle);

//        NativeArray<JobHandle> jobHandles = new NativeArray<JobHandle>(indices.Length, Allocator.TempJob);

//        List<EntityCommandBuffer> cmds = new List<EntityCommandBuffer>();
//        //create job and array for each bucket
//        createBucketsJobHandle.Complete();
//        int start = 0;
//        for (int j = 1; j < indices.Length; j++)
//        {
//            var cmd = new EntityCommandBuffer(Allocator.TempJob);
//            cmds.Add(cmd);
//            var slice = buckets.Slice(start, indices[j] - start);
//            start = indices[j];
//            var job = new BoxTestJob()
//            {
//                bucketObjects = slice,
//                commandBuffer = cmd
//            };

//            jobHandles[j] = job.Schedule(createBucketsJobHandle);
//        }

//        for (int v = 0; v < jobHandles.Length; v++)
//        {
//            jobHandles[v].Complete();
//        }

//        for (int v = 0; v < cmds.Count; v++)
//        {
//            cmds[v].Playback(World.Active.EntityManager);
//            cmds[v].Dispose();
//        }

//        jobHandles.Dispose();

//        buckets.Dispose();
//        indexes.Dispose();
//        valueArray.Dispose();
//        indices.Dispose();
//        aabbs.Dispose();
//        translations.Dispose();
//        entities.Dispose();
//        bucketSizes.Dispose();

//        return inputDeps;
//    }
//}


//experiment with a tree structure



// public struct AABB : IComponentData
// {
//     public int id;
//     public bool collided;
//     public float w;
//     public float h;

//     public float2 GetMin(float2 xy)
//     {
//         return new float2(xy.x - w, xy.y - h);
//     }

//     public float2 GetMax(float2 xy)
//     {
//         return new float2(xy.x + w, xy.y + h);
//     }

//     public int id1;
//     public int id2;
//     public int id3;
//     public int id4;
// }

// [InternalBufferCapacity(4)]
// public struct BucketIDBufferData : IBufferElementData
// {
//     public static implicit operator int(BucketIDBufferData e) { return e.id; }
//     public static implicit operator BucketIDBufferData(int e) { return new BucketIDBufferData { id = e }; }

//     public int id;
// }

// public struct AABBResult : IComponentData
// {
//     bool collided;
// }

// public class BoxTestSystem : JobComponentSystem
// {
//     const int MAX_ENTITIES = 2001;
//     const float SCENEWIDTH = 10000.0f;
//     const float CELLSIZE = 10.0f;
//     const float WIDTH = SCENEWIDTH / CELLSIZE;
//     public struct BucketObject
//     {
//         public Entity entityID;
//         public int bucketID;
//         public float4 aabb;
//     }

//     NativeArray<BucketObject> buckets;

//     //job four
//     //do narrowphase
//     //fill narrowphase component with relevant data for the user
//     //narrowphase result probably is bufferelementdata for each contact


//     EntityQuery updateIDsGroup;
//     EntityQuery bucketUpdateGroup;
//     EntityQuery broadPhaseGroup;
//     EntityQuery narrowPhaseGroup;

//     protected override void OnCreate()
//     {
//         updateIDsGroup = GetEntityQuery(ComponentType.ReadOnly<AABB>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadWrite<BucketIDBufferData>());
//         updateIDsGroup.SetFilterChanged(typeof(AABB));
//         updateIDsGroup.SetFilterChanged(typeof(Translation));

//         broadPhaseGroup = GetEntityQuery(ComponentType.ReadOnly<AABB>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadWrite<AABBResult>());

//         bucketUpdateGroup = GetEntityQuery(ComponentType.ReadOnly<BucketIDBufferData>());
//         bucketUpdateGroup.SetFilterChanged(typeof(BucketIDBufferData));

//         //need shape component, need narrowphase result component
//         narrowPhaseGroup = GetEntityQuery(ComponentType.ReadOnly<AABBResult>(), ComponentType.ReadOnly<Translation>());
//     }

//     protected override void OnDestroy()
//     {

//     }


//     [BurstCompile]
//     public struct UpdateIdsJob : IJobParallelFor
//     {
//         public float cellSize;
//         public float width;
//         const int nIds = 4;

//         [ReadOnly] public NativeArray<AABB> aABBs;
//         [ReadOnly] public NativeArray<Translation> translations;
//         [ReadOnly] public NativeArray<Entity> entities;
//         [NativeDisableParallelForRestriction] public BufferFromEntity<BucketIDBufferData> bufferIds;

//         public void Execute(int index)
//         {
//             float2 max = aABBs[index].GetMax(translations[index].Value.xy);
//             float2 min = aABBs[index].GetMin(translations[index].Value.xy);
//             float2 leftTop = min;
//             float2 rightTop = new float2(max.x, min.y);
//             float2 leftBottom = new float2(min.x, max.y);
//             float2 rightBottom = max;
//             var thisBufferID = bufferIds[entities[index]];
//             if (thisBufferID.Length != 0)
//             {
//                 thisBufferID.RemoveRange(0, 3);
//             }


//             thisBufferID.Add(AddBucket(leftTop));
//             thisBufferID.Add(AddBucket(rightTop));
//             thisBufferID.Add(AddBucket(leftBottom));
//             thisBufferID.Add(AddBucket(rightBottom));
//         }

//         public int AddBucket(float2 pos)
//         {
//             return (int)(math.floor(pos.x / cellSize) +
//                 math.floor(pos.y / cellSize) * width);
//         }
//     }

//     [BurstCompile]
//     public struct UpdateBucketsJob : IJob
//     {
//         [ReadOnly] public BufferFromEntity<BucketIDBufferData> bufferIds;
//         [ReadOnly] public NativeArray<Entity> entities;
//         [ReadOnly] public NativeArray<Translation> translations;
//         [ReadOnly] public NativeArray<AABB> aABBs;
//         public NativeArray<BucketObject> buckets;


//         public void Execute()
//         {
//             int bucketIndex = 0;
//             for (int i = 0; i < entities.Length; i++)
//             {
//                 int lastValue = int.MinValue;
//                 var buffer = bufferIds[entities[i]];
//                 for (int j = 0; j < 4; j++)
//                 {
//                     if (buffer[j].id == lastValue)
//                     {
//                         continue;
//                     }
//                     buckets[bucketIndex] = new BucketObject() { entityID = entities[i], bucketID = buffer[j].id, aabb = new float4(translations[i].Value.xy, aABBs[i].w, aABBs[i].h) };
//                     lastValue = buffer[j].id;

//                 }
//             }
//         }
//     }

//     [BurstCompile]
//     public struct BroadPhaseIntersect : IJob
//     {
//         public NativeArray<BucketObject> objectsToTest;

//         public void Execute()
//         {
//             for (int i = 0; i < objectsToTest.Length - 1; i++)
//             {
//                 var a = objectsToTest[i];

//                 for (int j = i + 1; j < objectsToTest.Length; j++)
//                 {
//                     var b = objectsToTest[j];

//                     if (a.entityID == b.entityID) continue;
//                     //if (a.dynamic == b.dynamic) continue;


//                     if (AABBTest(a.aabb, b.aabb))
//                     {
//                         var x = 0;
//                         x++;

//                         //TODO: Figure out how to message entity for collision events
//                         //a.entity.OnCollision (b.entity);
//                         //b.entity.OnCollision (a.entity);
//                     }
//                 }
//             }
//         }

//         bool AABBTest(float4 a, float4 b)
//         {
//             return (a.x + a.z > b.x &&
//                     b.x + b.z > a.x) &&
//                 (a.y + a.w > b.y &&
//                     b.y + b.w > a.y);
//         }
//     }

//     protected override JobHandle OnUpdate(JobHandle inputDeps)
//     {
//         buckets = new NativeArray<BucketObject>(MAX_ENTITIES, Allocator.TempJob);
//         var bucketIdsArray = GetBufferFromEntity<BucketIDBufferData>();
//         JobHandle updateIdsHandle;
//         {
//             JobHandle aabbsHandle;
//             JobHandle translationHandle;
//             JobHandle entitiesHandle;
//             var aabbs = updateIDsGroup.ToComponentDataArray<AABB>(Allocator.TempJob, out aabbsHandle);
//             var translation = updateIDsGroup.ToComponentDataArray<Translation>(Allocator.TempJob, out translationHandle);
//             var entities = updateIDsGroup.ToEntityArray(Allocator.TempJob, out entitiesHandle);

//             var updateIdsJob = new UpdateIdsJob()
//             {
//                 cellSize = CELLSIZE,
//                 width = WIDTH,
//                 aABBs = aabbs,
//                 translations = translation,
//                 entities = entities,
//                 bufferIds = bucketIdsArray
//             };

//             aabbsHandle.Complete();
//             translationHandle.Complete();
//             entitiesHandle.Complete();
//             updateIdsHandle = updateIdsJob.Schedule(entities.Length, 64, inputDeps);
//         }

//         JobHandle updateBucketsHandle;
//         {
//             JobHandle entityArrayGet;
//             JobHandle translationArrayGet;
//             JobHandle aabbsArrayGet;
//             updateIdsHandle.Complete();
//             var entities = bucketUpdateGroup.ToEntityArray(Allocator.TempJob, out entityArrayGet);
//             var translations = bucketUpdateGroup.ToComponentDataArray<Translation>(Allocator.TempJob, out translationArrayGet);
//             var aabbs = bucketUpdateGroup.ToComponentDataArray<AABB>(Allocator.TempJob, out aabbsArrayGet);
//             var updateBucketsJob = new UpdateBucketsJob()
//             {
//                 bufferIds = bucketIdsArray,
//                 entities = entities,
//                 buckets = buckets,
//                 aABBs = aabbs,
//                 translations = translations
//             };
//             translationArrayGet.Complete();
//             aabbsArrayGet.Complete();
//             updateBucketsHandle = updateBucketsJob.Schedule(entityArrayGet);
//         }

//         updateBucketsHandle.Complete();

//         var bucketsArray = MergeSort(buckets.ToArray(), 0, buckets.Length);

//         NativeList<int> bucketIndexes = new NativeList<int>(Allocator.Temp);

//         int currentBucket = -1;
//         for (int i = 0; i < bucketsArray.Length; i++)
//         {
//             if (bucketsArray[i].bucketID != currentBucket)
//             {
//                 bucketIndexes.Add(i);
//             }
//         }

//         NativeList<JobHandle> jobHandles = new NativeList<JobHandle>();

//         NativeMultiHashMap<int, NativeArray<BucketObject>> disposer = new NativeMultiHashMap<int, NativeArray<BucketObject>>();
//         for (int i = 0; i < bucketIndexes.Length - 1; i++)
//         {
//             NativeArray<BucketObject> objs = new NativeArray<BucketObject>(bucketIndexes[i], Allocator.TempJob);

//             for (int j = 0; j < bucketIndexes[i]; j++)
//             {
//                 objs[j] = bucketsArray[j];
//             }

//             var entities = broadPhaseGroup.ToEntityArray(Allocator.TempJob);
//             var translations = broadPhaseGroup.ToComponentDataArray<Translation>(Allocator.TempJob);


//             jobHandles.Add(new BroadPhaseIntersect() { }.Schedule(updateBucketsHandle));
//             disposer.Add(i, objs);
//         }

//         for (int i = 0; i < jobHandles.Length; i++)
//         {
//             jobHandles[i].Complete();
//         }

//         var array = disposer.GetValueArray(Allocator.Temp);
//         for (int i = 0; i < array.Length; i++)
//         {
//             array[i].Dispose();
//         }
//         array.Dispose();
//         jobHandles.Dispose();
//         disposer.Dispose();
//         bucketIndexes.Dispose();

//         //do narrowphase

//         buckets.Dispose();

//         return updateBucketsHandle;
//     }

//     private static BucketObject[] MergeSort(BucketObject[] array, int start, int end)
//     {
//         if (start < end)
//         {
//             int middle = (end + start) / 2;
//             BucketObject[] leftArr = MergeSort(array, start, middle);
//             BucketObject[] rightArr = MergeSort(array, middle + 1, end);
//             BucketObject[] mergedArr = MergeArray(leftArr, rightArr);
//             return mergedArr;
//         }
//         return new BucketObject[] { array[start] };
//     }

//     private static BucketObject[] MergeArray(BucketObject[] leftArr, BucketObject[] rightArr)
//     {
//         BucketObject[] mergedArr = new BucketObject[leftArr.Length + rightArr.Length];

//         int leftIndex = 0;
//         int rightIndex = 0;
//         int mergedIndex = 0;

//         // Traverse both arrays simultaneously and store the smallest element of both to mergedArr
//         while (leftIndex < leftArr.Length && rightIndex < rightArr.Length)
//         {
//             if (leftArr[leftIndex].bucketID < rightArr[rightIndex].bucketID)
//             {
//                 mergedArr[mergedIndex++] = leftArr[leftIndex++];
//             }
//             else
//             {
//                 mergedArr[mergedIndex++] = rightArr[rightIndex++];
//             }
//         }

//         // If any elements remain in the left array, append them to mergedArr
//         while (leftIndex < leftArr.Length)
//         {
//             mergedArr[mergedIndex++] = leftArr[leftIndex++];
//         }

//         // If any elements remain in the right array, append them to mergedArr
//         while (rightIndex < rightArr.Length)
//         {
//             mergedArr[mergedIndex++] = rightArr[rightIndex++];
//         }

//         return mergedArr;
//     }
// }

/*

[BurstCompile]
struct UpdateBucketsJob : IJobForEach<AABB, Translation>
{
    [ReadOnly] public float width;
    [ReadOnly] public float cellSize;

    public NativeList<SpatialObject> buckets;

    public void Execute(ref AABB c1, ref Translation c2)
    {
        float2 max = c1.GetMax(c2.Value.xy);
        float2 min = c1.GetMin(c2.Value.xy);
        float2 leftTop = min;
        float2 rightTop = new float2(max.x, min.y);
        float2 leftBottom = new float2(min.x, max.y);
        float2 rightBottom = max;

        var id1 = AddBucket(leftTop);
        var id2 = AddBucket(rightTop);
        var id3 = AddBucket(leftBottom);
        var id4 = AddBucket(rightBottom);

    }

    public int AddBucket(float2 pos)
    {
        return (int)(math.floor(pos.x / cellSize) +
            math.floor(pos.y / cellSize) * width);
    }
}


/*


            SpatialObject o = new SpatialObject();
            o.x = c2.Value.x;
            o.y = c2.Value.y;
            o.w = c1.w;
            o.h = c1.h;
            o.bucket = id1;
            o.id = c1.id;

            buckets.Add(o);

            if (id2 != id1)
            {
                o.bucket = id2;
                buckets.Add(o);
            }
            if (id3 != id1 && id3 != id2)
            {
                o.bucket = id3;
                buckets.Add(o);
            }
            if (id4 != id1 && id4 != id2 && id4 != id3)
            {
                o.bucket = id4;
                buckets.Add(o);
            }

 */

//sort array

/*
    [BurstCompile]
    unsafe struct GenerateBucketsJob : IJobForEach<SpatialHashingComponent, AABB, Translation>
    {

        public NativeMultiHashMap<int, NativeList<SpatialObject>> buckets;

        public void Execute(ref SpatialHashingComponent c0, ref AABB c1, ref Translation c2)
        {

            SpatialObject spatialObject = new SpatialObject();

            spatialObject.x = c2.Value.x;
            spatialObject.y = c2.Value.y;
            spatialObject.w = c1.w;
            spatialObject.h = c1.h;

            for (int i = 0; i < c0.nBuckets; i++)
            {
                buckets.GetValuesForKey(c0.buckets[i]).Current.Add(spatialObject);
            }
        }
    } */

/*    [BurstCompile]
    struct BoxTestJob : IJob {
        [ReadOnly] public NativeArray<Translation> translations;
        [ReadOnly] public NativeArray<AABB> aabbs;
        public AABB c0;


    }
 */

/*
//[BurstCompile]
public struct BoxTestJob2 : IJob
{
   public NativeList<SpatialObject> objectsToTest;

   public void Execute()
   {
       for (int i = 0; i < objectsToTest.Length - 1; i++)
       {
           var a = objectsToTest[i];

           for (int j = i + 1; j < objectsToTest.Length; j++)
           {


               var b = objectsToTest[j];

               if (b.bucket != a.bucket) continue; //this means that all objects of bucket n have been tested

               if (a.id == b.id) continue;
               //if (a.dynamic == b.dynamic) continue;


               if (AABBTest(a, b))
               {
                   var x = 0;
                   x++;

                   //TODO: Figure out how to message entity for collision events
                   //a.entity.OnCollision (b.entity);
                   //b.entity.OnCollision (a.entity);
               }
           }
       }
   }

   bool AABBTest(SpatialObject a, SpatialObject b)
   {
       return (a.x + a.w > b.x &&
               b.x + b.w > a.x) &&
           (a.y + a.h > b.y &&
               b.y + b.h > a.y);
   }
} */
