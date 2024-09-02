using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FishSchool1_2 : MonoBehaviour
{
    [SerializeField]
    Mesh mesh;
    [SerializeField]
    Material material;
    [SerializeField]
    int maxPopulation;
    [SerializeField]
    float moveSpeed;
    [SerializeField]
    float turnSpeed;
    [SerializeField, Min(0.0001f)]
    float tickDelay;

    List<Matrix4x4> fishTRS;
    NativeList<Matrix4x4> fishContainer;
    JobHandle handle;

    private void Awake()
    {
        fishTRS = new List<Matrix4x4>();
        for(int i = 0; i < maxPopulation; i++)
        {
            AddFish(Vector3.zero, Quaternion.identity, 1);
        }
        fishContainer = new NativeList<Matrix4x4>(1, Allocator.Persistent);
        StartCoroutine(Tick());
    }

    private void OnDestroy()
    {
        handle.Complete();
        if (fishContainer.IsCreated) fishContainer.Dispose();
    }

    private void Update()
    {
        if (fishTRS.Count > 0)
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, fishTRS);
        }
    }

    IEnumerator Tick()
    {
        float lastTime = Time.time;
        while (true)
        {
            #region Update Fish Container
            {
                fishContainer.SetCapacity(fishTRS.Count);
                NativeArray<Matrix4x4> temp = new NativeArray<Matrix4x4>(fishTRS.ToArray(), Allocator.TempJob);
                fishContainer.CopyFrom(temp);
                temp.Dispose();
                yield return new WaitForFixedUpdate();
            }
            #endregion

            #region Update Job
            UpdateJob job = new UpdateJob()
            {
                deltaTime = Time.time - lastTime,
                moveSpeed = moveSpeed,
                fishContainer = fishContainer,
            };

            lastTime = Time.time;

            handle = job.Schedule(fishTRS.Count, 8);
            yield return new WaitUntil(() => handle.IsCompleted);
            handle.Complete();
            #endregion

            #region Update Render List
            Parallel.For(0, fishTRS.Count, i =>
            {
                fishTRS[i] = fishContainer[i];
            });
            #endregion

            yield return new WaitForSeconds(tickDelay);
        }
    }

    void AddFish(Vector3 pos, Quaternion rot, float scale)
    {
        fishTRS.Add(Matrix4x4.TRS(pos, rot, scale * Vector3.one));
    }

    [BurstCompile]
    struct UpdateJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeList<Matrix4x4> fishContainer;
        public float moveSpeed;
        public float deltaTime;
        public void Execute(int index)
        {
            Matrix4x4 fishTRS = fishContainer[index];

            float3 pos = fishTRS.GetPosition();
            Quaternion rot = fishTRS.rotation;
            float3 size = fishTRS.lossyScale;

            float3 fishForward = math.mul(rot, new float3(0, 0, 1));
            pos += fishForward * moveSpeed * deltaTime;

            fishContainer[index] = Matrix4x4.TRS(pos, rot, size);
        }
    }
}
