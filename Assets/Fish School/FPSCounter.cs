using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField]
    TMPro.TMP_Text fps;

    int frames;
    float timeElapsed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timeElapsed += Time.deltaTime;
        frames++;
        if (timeElapsed >= 1)
        {
            fps.text = $"FPS: {frames}";
            timeElapsed = 0;
            frames = 0;
        }
    }
}
