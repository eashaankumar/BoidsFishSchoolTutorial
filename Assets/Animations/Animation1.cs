using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Animation1 : MonoBehaviour
{
    [SerializeField]
    Camera cam;
    [SerializeField]
    AnimatedArrow forward;
    [SerializeField]
    AnimatedArrow up;
    [SerializeField]
    AnimatedArrow right;
    [SerializeField]
    Transform fish;
    [SerializeField]
    MeshRenderer plane;
    [SerializeField]
    Material transparentMat;
    [SerializeField]
    Material opaqueMat;
    [SerializeField]
    AnimationDetails animationDetails;

    [System.Serializable]
    struct AnimationDetails
    {
        public Color forwardColor, upColor, rightColor;
        public EnterAnimationDetails enterAnimation;
    }   

    [System.Serializable]
    struct EnterAnimationDetails
    {
        public float startingDis;
        public float focusDis;
        public float waitAfterMovingToFish;
        public Vector3 focusViewOffset;
        public Vector3 planeViewOffset;
        public float revolveDuration;
        public float arrowLength;
        public float fontSize;
        public Color planeColor;
        public TMPro.TMP_Text quaternionText;
        public TMPro.TMP_Text vectorIn;
        public TMPro.TMP_Text vectorOut;
        public Canvas canvas;

        public Vector3 quaternionTextPlacement;
    }

    IEnumerator Start()
    {
        yield return Animation();
    }

    IEnumerator Animation()
    {
        // scene 1: Show fish and its local axis
        yield return Scene1();
    }

    IEnumerator Scene1()
    {
        Hide(forward);
        Hide(up);
        Hide(right);
        cam.transform.position = Vector3.zero;
        cam.transform.rotation = Quaternion.identity;
        fish.gameObject.SetActive(false);
        fish.transform.position = cam.transform.position + cam.transform.forward * animationDetails.enterAnimation.startingDis;
        fish.gameObject.SetActive(true);
        animationDetails.enterAnimation.canvas.enabled= false;
        // present fish
        Timer timer = new Timer();
        yield return ParallelAnimations(new [] 
        {
            TimerRoutine(timer),
            MoveToTarget(cam.transform.position,
                                fish.transform.position - cam.transform.forward * animationDetails.enterAnimation.focusDis,
                                cam.transform, timer),
        });

        yield return new WaitForSeconds(animationDetails.enterAnimation.waitAfterMovingToFish);
        plane.transform.position = fish.transform.position + animationDetails.enterAnimation.planeViewOffset;
        yield return ParallelAnimations(new [] 
        {
            TimerRoutine(timer),
            MoveToTarget(cam.transform.position, animationDetails.enterAnimation.focusViewOffset, cam.transform, timer),
            LookAt(cam.transform, fish.transform, timer),
            SetMeshRendererAlpha(0, 1, plane, animationDetails.enterAnimation.planeColor, timer)
        });

        // reveal arrows
        forward.Position = up.Position = right.Position = fish.transform.position;
        forward.Direction = fish.transform.forward;
        up.Direction = fish.transform.up;
        right.Direction = fish.transform.right;
        forward.SetColorBody(animationDetails.forwardColor);
        up.SetColorBody(animationDetails.upColor);
        right.SetColorBody(animationDetails.rightColor);

        yield return ParallelAnimations(new [] 
        {
            TimerRoutine(timer),
            SetArrowBodyAlpha(0,1,right, timer),
            SetArrowBodyAlpha(0,1,up, timer),
            SetArrowBodyAlpha(0,1,forward, timer),
            SetArrowTotalLengthAlpha(0,animationDetails.enterAnimation.arrowLength, right, timer),
            SetArrowTotalLengthAlpha(0,animationDetails.enterAnimation.arrowLength, up, timer),
            SetArrowTotalLengthAlpha(0,animationDetails.enterAnimation.arrowLength, forward, timer),
        });

        // rovolve
        timer.SetDuration = animationDetails.enterAnimation.revolveDuration;
        right.SetFontSize(animationDetails.enterAnimation.fontSize);
        forward.SetFontSize(animationDetails.enterAnimation.fontSize);
        up.SetFontSize(animationDetails.enterAnimation.fontSize);
        right.SetText("<1,0,0>");
        up.SetText("<0,1,0>");
        forward.SetText("<0,0,1>");
        right.SetTextColor(animationDetails.rightColor);
        forward.SetTextColor(animationDetails.forwardColor);
        up.SetTextColor(animationDetails.upColor);

        yield return ParallelAnimations(new [] 
        {
            TimerRoutine(timer),
            Revolve(Vector3.up, fish.transform.position, cam.transform, 360, timer),
            LookAtCanvas(right.Canvas, cam.transform, timer),
            LookAtCanvas(up.Canvas, cam.transform, timer),
            LookAtCanvas(forward.Canvas, cam.transform, timer),

            SetArrowTextAlpha(0,1,right, timer),
            SetArrowTextAlpha(0,1,up, timer),
            SetArrowTextAlpha(0,1,forward, timer),
        });

        #region quaternion canvas
        animationDetails.enterAnimation.canvas.enabled = true;

        Debug.Assert(animationDetails.enterAnimation.canvas != null);
        animationDetails.enterAnimation.canvas.transform.position = cam.transform.position + 
            cam.transform.forward * animationDetails.enterAnimation.quaternionTextPlacement.z + 
            cam.transform.up * animationDetails.enterAnimation.quaternionTextPlacement.y + 
            cam.transform.right * animationDetails.enterAnimation.quaternionTextPlacement.x;
        
        animationDetails.enterAnimation.vectorIn.enabled = false;
        animationDetails.enterAnimation.vectorIn.color = animationDetails.forwardColor;
        animationDetails.enterAnimation.vectorOut.enabled = false;
        animationDetails.enterAnimation.quaternionText.enabled = true;

        yield return ParallelAnimations(new [] 
        {
            TimerRoutine(timer),
            LookAtCanvas(animationDetails.enterAnimation.canvas, cam.transform, timer),
            TextAlpha(animationDetails.enterAnimation.quaternionText, 0, 1, timer),
            SetLocalQuaternionText(animationDetails.enterAnimation.quaternionText, fish.transform, timer),
        });

        animationDetails.enterAnimation.vectorIn.enabled = true;
        yield return ParallelAnimations(new [] 
        {
            TimerRoutine(timer),
            LookAtCanvas(animationDetails.enterAnimation.quaternionText.canvas, cam.transform, timer),
            TextAlpha(animationDetails.enterAnimation.vectorIn, 0, 1, timer)
        });

        animationDetails.enterAnimation.vectorOut.enabled = true;
        animationDetails.enterAnimation.vectorOut.color = animationDetails.forwardColor;

        yield return ParallelAnimations(new [] 
        {
            TimerRoutine(timer),
            LookAtCanvas(animationDetails.enterAnimation.quaternionText.canvas, cam.transform, timer),
            TextAlpha(animationDetails.enterAnimation.vectorOut, 0, 1, timer)
        });
        #endregion

        forward.transform.SetParent(fish.transform);
        right.transform.SetParent(fish.transform);
        up.transform.SetParent(fish.transform);

        // rotate
        yield return ParallelAnimations(new [] 
        {
            TimerRoutine(timer),
            RotateQuaternion(fish.transform, Vector3.right, -30, timer),
            ArrowRotationText(forward, timer),
            ArrowRotationText(up, timer),
            ArrowRotationText(right, timer),

            LookAtCanvas(right.Canvas, cam.transform, timer),
            LookAtCanvas(up.Canvas, cam.transform, timer),
            LookAtCanvas(forward.Canvas, cam.transform, timer),

            CopyText(forward.Text, animationDetails.enterAnimation.vectorOut, timer, "= "),
            SetLocalQuaternionText(animationDetails.enterAnimation.quaternionText, fish.transform, timer),

        });

        yield return ParallelAnimations(new [] 
        {
            TimerRoutine(timer),
            RotateQuaternion(fish.transform, Vector3.right, 15 + 30, timer),
            RotateQuaternion(fish.transform, Vector3.up, 180, timer),
            RotateQuaternion(fish.transform, Vector3.forward, 30, timer),
            ArrowRotationText(forward, timer),
            ArrowRotationText(up, timer),
            ArrowRotationText(right, timer),

            LookAtCanvas(right.Canvas, cam.transform, timer),
            LookAtCanvas(up.Canvas, cam.transform, timer),
            LookAtCanvas(forward.Canvas, cam.transform, timer),

            CopyText(forward.Text, animationDetails.enterAnimation.vectorOut, timer, "= "),
            SetLocalQuaternionText(animationDetails.enterAnimation.quaternionText, fish.transform, timer),
        });

        yield return ParallelAnimations(new [] 
        {
            TimerRoutine(timer),
            SlerpQuaternion(fish.transform, fish.transform.localRotation, Quaternion.identity, timer),
            ArrowRotationText(forward, timer),
            ArrowRotationText(up, timer),
            ArrowRotationText(right, timer),

            LookAtCanvas(right.Canvas, cam.transform, timer),
            LookAtCanvas(up.Canvas, cam.transform, timer),
            LookAtCanvas(forward.Canvas, cam.transform, timer),

            CopyText(forward.Text, animationDetails.enterAnimation.vectorOut, timer, "= "),
            SetLocalQuaternionText(animationDetails.enterAnimation.quaternionText, fish.transform, timer),
        });
    }

    IEnumerator SetLocalQuaternionText(TMPro.TMP_Text text, Transform target, Timer timer)
    {
        while(timer.GetTimer <= 1)
        {
            var q = target.localRotation;
            string x = q.x.ToString("0.#");
            string y = q.y.ToString("0.#");
            string z = q.z.ToString("0.#");
            string w = q.w.ToString("0.#");

            text.text = $"({x}, {y}, {z}, {w})";
            yield return null;
        }
    }

    IEnumerator CopyText(TMPro.TMP_Text from, TMPro.TMP_Text to, Timer timer, string prefix)
    {
        while(timer.GetTimer <= 1)
        {
            to.text = prefix + from.text;
            yield return null;
        }
    }

    IEnumerator TextAlpha(TMPro.TMP_Text text, float startA, float endA, Timer timer)
    {
        Color color = text.color;
        color.a = startA;
        text.color = color;
        while(timer.GetTimer <= 1)
        {
            color.a = Mathf.Lerp(startA, endA, timer.GetTimer);
            text.color = color;
            yield return null;
        }
    }

    IEnumerator SlerpQuaternion(Transform transform, Quaternion start, Quaternion end, Timer timer)
    {
        while(timer.GetTimer <= 1)
        {
            transform.localRotation = Quaternion.Slerp(start, end, timer.GetTimer);
            yield return null;
        }
    }

    IEnumerator ArrowRotationText(AnimatedArrow arrow, Timer timer)
    {
        while(timer.GetTimer <= 1)
        {
            Vector3 dir = arrow.Direction.normalized;
            string x = dir.x.ToString("0.#");
            string y = dir.y.ToString("0.#");
            string z = dir.z.ToString("0.#");

            arrow.SetText($"<{x}, {y}, {z}>");
            yield return null;
        }
    }

    IEnumerator RotateQuaternion(Transform t, Vector3 axis, float delta, Timer timer)
    {
        while(timer.GetTimer <= 1)
        {
            t.transform.localRotation *= Quaternion.AngleAxis(delta * timer.GetDelta, axis);
            yield return null;
        }
    }

    IEnumerator SetMeshRendererAlpha(float start, float end, MeshRenderer renderer, Color color, Timer timer)
    {
        renderer.material = transparentMat;
        renderer.material.color = new Color(color.r, color.g, color.b, start);
        while(timer.GetTimer <= 1)
        {
            Color c = color;
            c.a = Mathf.Lerp(start, end, timer.GetTimer);
            renderer.material.color = c;
            yield return null;
        }
        renderer.material = opaqueMat;
        renderer.material.color = new Color(color.r, color.g, color.b, end);
    }

    IEnumerator SetArrowBodyAlpha(float start, float end, AnimatedArrow arrow, Timer timer)
    {
        while(timer.GetTimer <= 1)
        {
            float a = Mathf.Lerp(start, end, timer.GetTimer);
            arrow.SetAlphaBody(a);
            yield return null;
        }
    }

    IEnumerator SetArrowTotalLengthAlpha(float start, float end, AnimatedArrow arrow, Timer timer)
    {
        while(timer.GetTimer <= 1)
        {
            float a = Mathf.Lerp(start, end, timer.GetTimer);
            arrow.SetTotalLength(a);
            yield return null;
        }
    }

    IEnumerator SetArrowTextAlpha(float start, float end, AnimatedArrow arrow, Timer timer)
    {
        while(timer.GetTimer <= 1)
        {
            float a = Mathf.Lerp(start, end, timer.GetTimer);
            arrow.SetTextAlpha(a);
            yield return null;
        }
    }

    IEnumerator TimerRoutine(Timer timer)
    {
        timer.SetTimer = 0;
        while(timer.GetTimer <= 1)
        {
            timer.Update(Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator MoveToTarget(Vector3 startPos, Vector3 endPos, Transform obj, Timer timer)
    {  
        while(timer.GetTimer <= 1)
        {
            obj.position = Vector3.Lerp(startPos, endPos, timer.GetTimer);
            yield return null;
        }
    }

    IEnumerator LookAt(Transform obj, Transform lookAt, Timer timer)
    {
        while(timer.GetTimer <= 1)
        {
            obj.LookAt(lookAt);
            yield return null;
        }
    }

    IEnumerator LookAtCanvas(Canvas obj, Transform lookAt, Timer timer)
    {
        while(timer.GetTimer <= 1)
        {
            obj.transform.LookAt(lookAt);
            obj.transform.Rotate(Vector3.up, 180);
            yield return null;
        }
    }
    
    IEnumerator Revolve(Vector3 aroundAxis, Vector3 center, Transform target, float deltaAngle, Timer timer)
    {
        while(timer.GetTimer <= 1)
        {
            target.RotateAround(center, aroundAxis, deltaAngle * timer.GetDelta);
            
            yield return null;
        }
    }

    IEnumerator ParallelAnimations(IEnumerator[] anims)
    {
        var parallel = new Coroutine[anims.Length];
        for(int i = 0; i < anims.Length; i++)
        {
            parallel[i] = StartCoroutine(anims[i]);
        }
        foreach (var routine in parallel) yield return routine;
    }

    void Hide(AnimatedArrow a)
    {
        a.SetTextAlpha(0);
     
        a.SetAlphaBody(0);
    }

    class Timer
    {
        float timer;
        float speed = 0;
        float delta;

        public Timer(){
            timer = 0;
            speed = 1;
        }

        public void Update(float dt)
        {
            delta = dt * speed;
            timer += delta;
        }

        public float GetDelta
        {
            get
            {
                return delta;
            }
        }

        public float SetDuration
        {
            set{
                speed = 1/value;
            }
        }

        public float GetTimer
        {
            get
            {
                return timer;
            }
        }

        public float SetTimer
        {
            set
            {
                timer = value;
            }
        }

    }
}
