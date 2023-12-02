using System.Collections;
using System.Collections.Generic;
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
    [SerializeField]
    float turnSpeed;

    List<Matrix4x4> fishTRS;
    void Awake()
    {
        fishTRS = new List<Matrix4x4>();

        for(int i = 0; i < maxPopulation; i++)
        {
            AddFish(Vector3.zero, Quaternion.identity, 1);
        }
    }

    void Update()
    {
        if (fishTRS.Count > 0)
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, fishTRS);
        }

        Tick();
    }

    void Tick()
    {
        for(int i = 0; i < fishTRS.Count; i++)
        {
            Matrix4x4 trs = fishTRS[i];
            Vector3 position = trs.GetPosition();
            Quaternion rotation = trs.rotation;
            Vector3 scale = trs.lossyScale;

            Vector3 fishForward = rotation * Vector3.forward;
            position += fishForward * moveSpeed * Time.deltaTime;

            rotation = rotation * Quaternion.AngleAxis(turnSpeed * Time.deltaTime, Vector3.up);

            fishTRS[i] = Matrix4x4.TRS(position, rotation, scale);
        }
    }

    void AddFish(Vector3 pos, Quaternion rot, float fishSize)
    {
        fishTRS.Add(Matrix4x4.TRS(pos, rot, Vector3.one * fishSize));
    }
}
