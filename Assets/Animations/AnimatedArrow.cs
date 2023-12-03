using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedArrow : MonoBehaviour
{
    [SerializeField]
    Arrow3D arrow;
    [SerializeField]
    Canvas canvas;
    [SerializeField]
    TMPro.TMP_Text text;

    public TMPro.TMP_Text Text
    {
        get
        {
            return text;
        }
    }

    public Vector3 Direction
    {
        set
        {
            arrow.transform.forward = value;
        }
        get
        {
            return arrow.transform.forward;
        }
    }

    public Vector3 Position
    {
        set
        {
            arrow.transform.position = value;
        }
        get
        {
            return arrow.transform.position;
        }
    }

    public Canvas Canvas
    {
        get
        {
            return canvas;
        }
    }
    
    public void SetTotalLength(float length)
    {
        var data = arrow.Data;
        data.tailLength = length - data.headLength;
        arrow.Data = data;
        arrow.GenerateArrow();
        canvas.transform.position = arrow.transform.position + arrow.transform.forward * length;
    }

    public void SetAlphaBody(float a)
    {
        a = Mathf.Clamp01(a);
        var data = arrow.Data;
        data.color.a = a;
        arrow.Data = data;
        arrow.GenerateArrow();
    }

    public void SetColorBody(Color c)
    {
        var data = arrow.Data;
        c.a = data.color.a;
        data.color = c;
        arrow.Data = data;
        arrow.GenerateArrow();
    }

    public void SetText(string t)
    {
        text.text = t;
    }

    public void SetTextAlpha(float a)
    {
        Color c = text.color;
        c.a = a;
        text.color = c;
    }

    public void SetTextColor(Color c)
    {
        text.color = c;
    }

    public void SetCanvasSize(Vector2 size)
    {
        (canvas.transform as RectTransform).sizeDelta = size;
    }

    public void SetFontSize(float size)
    {
        text.fontSize = size;
    }

}
