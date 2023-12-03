using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FishSchoolTutorial : MonoBehaviour
{
    [SerializeField]
    Mesh mesh;
    [SerializeField]
    Material material;
    [SerializeField]
    int maxPopulation;
    [SerializeField]
    float moveSpeed;
    [SerializeField, Range(0f,1f)]
    float agility;
    [SerializeField]
    float2 repelAngle;
    [SerializeField, Min(0.0001f)]
    float tickDelay;
    [SerializeField, Min(0)]
    int numNeighbors;
    [SerializeField, Min(0.001f)]
    float schoolRadius;
    [SerializeField]
    float3 oscilateMag;
    [SerializeField]
    float3 oscilateFreq;

    List<Matrix4x4> fishTRS;
    NativeList<Matrix4x4> fish_container;
    Unity.Mathematics.Random random;
    JobHandle handle;

    void Awake()
    {
        fishTRS = new List<Matrix4x4>();

        for(int i = 0; i < maxPopulation; i++)
        {
            AddFish(Vector3.zero, Quaternion.identity, 1);
        }
        random = new Unity.Mathematics.Random(1122);
        fish_container = new NativeList<Matrix4x4>(1, Allocator.Persistent);
        StartCoroutine(Tick());
    }

    void OnDestroy()
    {
        handle.Complete();
        if (fish_container.IsCreated)
        {
            fish_container.Dispose();
        }
    }

    void Update()
    {
        if (fishTRS.Count > 0)
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, fishTRS);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, schoolRadius);
    }

    void AddFish(Vector3 pos, Quaternion rot, float fishSize)
    {
        fishTRS.Add(Matrix4x4.TRS(pos, rot, Vector3.one * fishSize));
    }

    IEnumerator Tick()
    {
        float lastTime = Time.time;
        while(true)
        {
            #region Update Fish Container
            {
                fish_container.SetCapacity(fishTRS.Count);
                NativeArray<Matrix4x4> temp = new NativeArray<Matrix4x4>(fishTRS.ToArray(), Allocator.TempJob);
                fish_container.CopyFrom(temp);
                temp.Dispose();
                yield return new WaitForFixedUpdate();
            }
            #endregion
            
            transform.position = (Vector3)(math.sin(oscilateFreq * Time.time ) * oscilateMag * Time.deltaTime);
            yield return null;

            #region  Update Job
            UpdateJob job = new UpdateJob()
            {
                deltaTime = Time.time - lastTime,
                moveSpeed = moveSpeed,
                fish_container = fish_container,
                numNeighbors = numNeighbors,
                random = random,
                repelAngle = repelAngle,
                schoolUp = transform.up,
                schoolCenter = transform.position,
                agility = agility,
                schoolRadius =schoolRadius,

            };

            lastTime = Time.time;

            handle = job.Schedule(fishTRS.Count, 8);
            yield return new WaitUntil(() => handle.IsCompleted); // Maintains High FPS
            handle.Complete(); 
            #endregion

            #region Update Render List
            Parallel.For(0, fishTRS.Count, (i) => {
                fishTRS[i] = fish_container[i];
            });
            #endregion
            yield return new WaitForSeconds(tickDelay);
        }
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeList<Matrix4x4> fish_container;
        [ReadOnly] public float moveSpeed;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public int numNeighbors;
        [ReadOnly] public Unity.Mathematics.Random random;
        [ReadOnly] public float2 repelAngle;
        [ReadOnly] public float agility;
        [ReadOnly] public float3 schoolCenter;
        [ReadOnly] public float3 schoolUp;
        [ReadOnly] public float schoolRadius;

        public void Execute(int index)
        {
            Matrix4x4 fishTRS = fish_container[index];

            float3 pos = fishTRS.GetPosition();
            Quaternion rot = fishTRS.rotation;
            float3 size = fishTRS.lossyScale;

            float3 fishForward = math.mul(rot, new float3(0, 0, 1));
            float3 fishUp = math.mul(rot, new float3(0, 1, 0));
            float3 fishRight = math.mul(rot, new float3(1, 0, 0));
            // position update
            pos += fishForward * moveSpeed * deltaTime;

            #region rotation update
            #region neighbor search
            int startIndex = random.NextInt(0, fish_container.Length);
            int endIndex = random.NextInt(startIndex+1, startIndex+1+numNeighbors);
            endIndex = Mathf.Clamp(endIndex, 0, fish_container.Length);
            float totalRepelAngleX = 0;
            float totalRepelAngleY = 0;
            float3 repelDir = 0;
            for(int i = startIndex; i < endIndex; i++)
            {
                Matrix4x4 ntrs = fish_container[i];
                float3 neighborPos = ntrs.GetPosition();
    
                float distance = math.distance(neighborPos, pos);
                if (distance < 1e-3f)
                {
                    // push away
                    totalRepelAngleX += repelAngle.x * random.NextFloat(-2, 2);
                    // random angle causes fish to turn left and right
                    totalRepelAngleY += repelAngle.y * random.NextFloat(-2, 2);
                    // random angle causes fish to turn up and down
                }
                else
                {
                    // casually swim away
                    float3 fromNeighborToMeVec = pos - neighborPos;
                    float d = math.length(fromNeighborToMeVec);
                    float multiplier = SpecialFunc(d);
                    repelDir += (fromNeighborToMeVec / d) * multiplier;
                }
            }

            float3 meToSchoolCenter = schoolCenter - pos;
            float disFromSchoolCenter = math.length(meToSchoolCenter);
            float3 meToSchoolCenterDir = meToSchoolCenter / disFromSchoolCenter;

            float goHomeT = disFromSchoolCenter / schoolRadius;

            #endregion
            #region move away from neighbors
            Quaternion targetRot = Quaternion.LookRotation(repelDir + meToSchoolCenterDir, schoolUp)* Quaternion.AngleAxis(totalRepelAngleX, fishUp) * Quaternion.AngleAxis(totalRepelAngleY, fishRight);
            
            targetRot = Quaternion.Lerp(targetRot, Quaternion.LookRotation(meToSchoolCenterDir, fishUp), 0.95f * goHomeT);    
            
            rot = Quaternion.Lerp(rot, targetRot, agility);
            //rot = rot 
            #endregion
            #endregion
            fish_container[index] = Matrix4x4.TRS(pos, rot, size);
        }

        float SpecialFunc(float x)
        {
            return 1 / (1+x);
        }
    }
}
