using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.VisualScripting;

public class FishSchool : MonoBehaviour
{
    [SerializeField]
    Mesh fishMesh;
    [SerializeField]
    Material fishMaterial;

    [SerializeField]
    float tickRate;
    [SerializeField]
    SphereCollider collider;
    [SerializeField]
    Rigidbody rb;
    [SerializeField]
    FishSchoolData schoolData;

    [System.Serializable]
    struct FishSchoolData
    {
        public int maxHerrings;
        [HideInInspector]
        public float3 center;
        public float radius;
        public float moveSpeed;
        public float agilityDamping;
        public int neighborRandomSearch;
        [Tooltip("How much one turns away when repelled")]
        public float2 repelAngle;

        public float3 oscillationMag;
        public float3 oscillationFreq;
        [Min(0.01f)]
        public float cellSize;
    }

    List<Matrix4x4> fishTRS;

    NativeList<Matrix4x4> fish_container;
    NativeParallelMultiHashMap<int3, Matrix4x4> grid;
    Unity.Mathematics.Random random;
    JobHandle handle;
    
    void Awake()
    {
        random = new Unity.Mathematics.Random(123);
        collider.isTrigger = false;
        collider.radius = schoolData.radius;
        //gameObject.layer = LayerMask.NameToLayer("MobSchoolingOcean");
        rb.isKinematic =false;
        rb.useGravity = false;
        rb.freezeRotation = true;
        fishTRS = new List<Matrix4x4>();
        fish_container = new NativeList<Matrix4x4>(1, Allocator.Persistent);
        grid = new NativeParallelMultiHashMap<int3, Matrix4x4>(100000, Allocator.Persistent);
    }

    
    void Start()
    {
        for(int i = 0; i < schoolData.maxHerrings; i++)
        {
            AddFish(transform.position, Quaternion.identity, 1);
        }
        StartCoroutine(HerringSchoolTickJob());
    }

    void OnDestroy()
    {
        handle.Complete();
        if (fish_container.IsCreated) fish_container.Dispose();
        if (grid.IsCreated) grid.Dispose();
    }
    // Update is called once per frame
    void Update()
    {
        if (fishTRS.Count > 0)
            Graphics.DrawMeshInstanced(fishMesh, 0, fishMaterial, fishTRS);
    }

    void AddFish(float3 position, quaternion rot, float3 size)
    {
        fishTRS.Add(Matrix4x4.TRS(position, rot, size));
    }

    IEnumerator HerringSchoolTickJob()
    {
        float lastTime = Time.time;
        while(true)
        {
            schoolData.center = transform.position + (Vector3)(math.sin(schoolData.oscillationFreq * Time.time) * schoolData.oscillationMag);
            collider.radius = schoolData.radius;

            yield return new WaitForFixedUpdate();
            Vector3 schoolUp = transform.up;
            Vector3 schoolRight = transform.right;
            Vector3 schoolForward = transform.forward;
            Quaternion schoolRot = transform.rotation;

            {
                fish_container.SetCapacity(fishTRS.Count);
                NativeArray<Matrix4x4> temp = new NativeArray<Matrix4x4>(fishTRS.ToArray(), Allocator.TempJob);
                fish_container.CopyFrom(temp);
                temp.Dispose();
                yield return new WaitForFixedUpdate();
            }

            grid.Clear();

            // hash grid
            HashGrid gridJOb = new HashGrid()
            {
                grid = grid.AsParallelWriter(),
                fish_container = fish_container,
                cellSize = schoolData.cellSize,
            };

            handle = gridJOb.Schedule(fish_container.Length, 8);
            yield return new WaitUntil(() => handle.IsCompleted);
            handle.Complete();


            UpdateJob job = new UpdateJob()
            {
                fish_container = fish_container,
                schoolData = schoolData,
                deltaTime = Time.time - lastTime,
                random = random,
                schoolForward = schoolForward,
                schoolRight = schoolRight,
                schoolUp = schoolUp,
                schoolRot = schoolRot,
                grid = grid,
                cellSize = schoolData.cellSize,
            };

            lastTime = Time.time;
            handle = job.Schedule(fishTRS.Count, 8);
            yield return new WaitUntil(() => handle.IsCompleted);
            handle.Complete();

            Parallel.For(0, fishTRS.Count, (i) => {
                fishTRS[i] = fish_container[i];
            });

            yield return new WaitForSeconds(tickRate);
        }
    }

    [BurstCompile]
    struct HashGrid : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeParallelMultiHashMap<int3, Matrix4x4>.ParallelWriter grid;
        [ReadOnly] public NativeList<Matrix4x4> fish_container;
        [ReadOnly] public float cellSize;

        public void Execute(int index)
        {
            Matrix4x4 trs = fish_container[index];
            float3 pos = trs.GetPosition();
            int3 c = new int3( (int)(pos.x / cellSize), (int)(pos.y / cellSize), (int)(pos.z / cellSize));
            grid.Add(c, trs);
        }
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeList<Matrix4x4> fish_container;
        [ReadOnly] public NativeParallelMultiHashMap<int3, Matrix4x4> grid;
        [ReadOnly] public float cellSize;
        [ReadOnly] public FishSchoolData schoolData;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public Unity.Mathematics.Random random;
        [ReadOnly] public float3 schoolUp;
        [ReadOnly] public float3 schoolRight;
        [ReadOnly] public float3 schoolForward;
        [ReadOnly] public Quaternion schoolRot;

        public void Execute(int index)
        {
            Matrix4x4 trs = fish_container[index];

            float3 pos = trs.GetPosition();
            Quaternion rot = trs.rotation;
            float3 scale = trs.lossyScale;
            float3 forward = math.mul(rot, new float3(0,0,1));
            float3 right = math.mul(rot, new float3(1,0,0));
            float3 up = math.mul(rot, new float3(0,1,0));

            // update pos
            pos = pos + forward * schoolData.moveSpeed * deltaTime;
            // rotate towards target
            float3 schoolCenter = schoolData.center;
            float3 meToSchoolCenterVec = (schoolCenter - pos) + 1e-3f;
            float disFromSchoolCenter = math.length(meToSchoolCenterVec);
            float3 meToSchoolCenterDir = math.normalize(meToSchoolCenterVec);

            float3 repelDir = float3.zero;
            
            NativeArray<float3> vecToNeighbors = VecToNeighbors(pos);

            float rotAwayXAngle = 0;
            float rotAwayYAngle = 0;
            float goHomeT = math.clamp(disFromSchoolCenter / schoolData.radius, 0f, 1f);
            float n = noise.snoise(pos / schoolData.radius);

            int3 c = new int3( (int)(pos.x / cellSize), (int)(pos.y / cellSize), (int)(pos.z / cellSize));
            //if (grid.ContainsKey(c))
            {
                foreach(var otherPos in vecToNeighbors)
                {
                    //Matrix4x4 trsOther = v;
                    //float3 otherPos = trsOther.GetPosition();
                    float d = math.length(otherPos);
                    if (d <= 1e-3f)
                    {
                        //repelDir += up * random.NextFloat() + right * random.NextFloat();
                        
                        rotAwayXAngle += schoolData.repelAngle.x * random.NextFloat(-2,2) + n;
                        rotAwayYAngle += schoolData.repelAngle.y * random.NextFloat(-2,2) + n;
                    }
                    else
                    {
                        float3 dir = otherPos/d;
                        repelDir -= dir * SpecialFunc(d); // this scaling graph causes hyperbolic intersection with y=0 (limits repel mag for close fish)
                    }
                }
            }
            vecToNeighbors.Dispose();

            // farher away = care less about repel, more about home
            float m = SpecialFunc(1-goHomeT);
            rotAwayXAngle *= m;
            rotAwayYAngle *= m;

            Quaternion towardsTarget = Quaternion.LookRotation(meToSchoolCenterDir + repelDir, schoolUp) * Quaternion.AngleAxis(rotAwayXAngle, up) * Quaternion.AngleAxis(rotAwayYAngle, right);

            towardsTarget = Quaternion.Lerp(towardsTarget, Quaternion.LookRotation(meToSchoolCenterDir, up), 0.95f * goHomeT);    
        
            rot = Quaternion.Lerp(rot, towardsTarget, schoolData.agilityDamping);

            fish_container[index] = Matrix4x4.TRS(pos, rot, scale);
        }

        float SpecialFunc(float x)
        {
            return 1 / (x+1);
        }

        NativeArray<float3> VecToNeighbors(float3 pos)
        {
            int startIndex = random.NextInt(0, fish_container.Length);
            int endIndex = random.NextInt(startIndex+1, startIndex+1+schoolData.neighborRandomSearch);
            endIndex = Mathf.Clamp(endIndex, 0, fish_container.Length);

            NativeArray<float3> dirs = new NativeArray<float3>(endIndex - startIndex + 2, Allocator.Temp);

            int a = 0;
            for(int i = startIndex; i < endIndex; i++)
            {
                Matrix4x4 trs = fish_container[i];
                float3 neighPos = trs.GetPosition();
                float3 meToNeigh = (neighPos - pos) + + 1e-3f;
                dirs[a] = (meToNeigh);
                a++;
            }

            return dirs;
        }
    
    }
}
