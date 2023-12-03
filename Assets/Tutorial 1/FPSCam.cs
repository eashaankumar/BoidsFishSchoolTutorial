using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FPSCam : MonoBehaviour
{
    [SerializeField]
    float m_xSensitivity;
    [SerializeField]
    float m_ySensitivity;
    [SerializeField]
    float speed;
    [SerializeField]
    Camera m_camera;
    [SerializeField]
    UnityEvent m_cursorLockedEvent;
    [SerializeField]
    Rigidbody m_rb;

    Vector2 delta;
    Vector2 moveInput;
    float pitch;

    public static FPSCam Instance;
    State state;
    public enum State
    {
        Locked, unlocked
    }

    public Ray _Ray
    {
        get
        {
            return new Ray(transform.position, transform.forward);
        }
    }

    public Vector2 LookInput
    {
        get
        {
            return delta;
        }
    }

    public State _State
    {
        get { return state; }
    }

    private void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_rb.useGravity = false;
        m_rb.freezeRotation = true;
    }

    Queue<State> statesQueue = new Queue<State>();

    // Update is called once per frame
    void Update()
    {
        delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleLock();
        }
        if (statesQueue.Count > 0)
        {
            state = statesQueue.Dequeue();
        }
    }

    private void OnDisable()
    {
        print("Disabled");
    }

    private void OnEnable()
    {
        print("Enabled");
    }

    public void ToggleLock()
    {
        ReleaseLock(state == State.Locked);
    }


    public void ReleaseLock(bool release)
    {
        if (release)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            statesQueue.Enqueue(State.unlocked);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            statesQueue.Enqueue(State.unlocked); // Delay locking by one tick to help tools not act instantly
            statesQueue.Enqueue(State.Locked);
            m_cursorLockedEvent?.Invoke();
        }
    }

    void FixedUpdate()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            pitch += m_ySensitivity * delta.y * Time.fixedDeltaTime;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.Rotate((Vector3.up * delta.x * m_xSensitivity) * Time.fixedDeltaTime);
            transform.eulerAngles = new Vector3(-pitch, transform.eulerAngles.y, 0);
        }
        moveInput.Normalize();
        m_rb.velocity = (transform.forward * moveInput.y + transform.right * moveInput.x) * Time.fixedDeltaTime * speed;
    }
}