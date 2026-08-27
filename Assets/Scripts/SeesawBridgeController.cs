using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(HingeJoint2D), typeof(BoxCollider2D))]
public sealed class SeesawBridgeController : MonoBehaviour
{
    [Header("Bridge Mode")]
    [SerializeField] private bool continuousRotation;
    [SerializeField] private bool autoOscillation;
    [Tooltip("Left-first means the left end dips before the right end.")]
    [SerializeField] private bool startLeftFirst = true;
    [SerializeField] private float motorSpeed = 45f;
    [SerializeField, Min(1f)] private float motorTorque = 1500f;
    [SerializeField, Range(5f, 80f)] private float autoTurnAngle = 24f;

    [Header("Seesaw Stability")]
    [SerializeField, Min(0f)] private float returnStrength = 0.12f;
    [SerializeField, Min(0f)] private float rotationDamping = 0.45f;
    [SerializeField, Min(5f)] private float maxAngularSpeed = 85f;

    [Header("Bridge Shape")]
    [SerializeField, Min(2f)] private float bridgeLength = 5f;
    [SerializeField, Min(0.1f)] private float colliderHeight = 0.32f;
    [SerializeField] private SpriteRenderer bridgeVisual;

    private Rigidbody2D body;
    private HingeJoint2D hinge;
    private BoxCollider2D surfaceCollider;
    private float restingRotation;
    private int autoDirection = 1;

    public bool IsContinuousRotation => continuousRotation;
    public bool IsAutoOscillating => autoOscillation;
    public bool StartsLeftFirst => startLeftFirst;
    public float MotorSpeed => motorSpeed;
    public float AutoTurnAngle => autoTurnAngle;
    public float BridgeLength => bridgeLength;

    private void Awake()
    {
        CacheComponents();
        restingRotation = body.rotation;
        ResetAutoDirection();
        ApplyConfiguration();
    }

    private void FixedUpdate()
    {
        if (body == null)
        {
            return;
        }

        body.angularVelocity = Mathf.Clamp(
            body.angularVelocity,
            -maxAngularSpeed,
            maxAngularSpeed);

        if (continuousRotation)
        {
            return;
        }

        if (autoOscillation)
        {
            UpdateAutoOscillation();
            return;
        }

        float angleFromRest = Mathf.DeltaAngle(restingRotation, body.rotation);
        float stabilizingTorque = -angleFromRest * returnStrength
            - body.angularVelocity * rotationDamping;
        body.AddTorque(stabilizingTorque);
    }

    public void Configure(
        float length,
        bool rotateContinuously,
        float rotationSpeed,
        float maximumTilt = 28f)
    {
        bridgeLength = Mathf.Max(2f, length);
        continuousRotation = rotateContinuously;
        autoOscillation = false;
        motorSpeed = rotationSpeed;

        CacheComponents();
        ApplyConfiguration();
    }

    public void ConfigureAutoOscillation(
        float length,
        bool leftSideFirst,
        float rotationSpeed = 28f,
        float turnAngle = 24f)
    {
        bridgeLength = Mathf.Max(2f, length);
        continuousRotation = false;
        autoOscillation = true;
        startLeftFirst = leftSideFirst;
        motorSpeed = Mathf.Max(5f, Mathf.Abs(rotationSpeed));
        autoTurnAngle = Mathf.Clamp(turnAngle, 5f, 80f);
        ResetAutoDirection();

        CacheComponents();
        restingRotation = body != null ? body.rotation : transform.eulerAngles.z;
        ApplyConfiguration();
    }

    private void CacheComponents()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        if (hinge == null)
        {
            hinge = GetComponent<HingeJoint2D>();
        }

        if (surfaceCollider == null)
        {
            surfaceCollider = GetComponent<BoxCollider2D>();
        }

        if (bridgeVisual == null)
        {
            bridgeVisual = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void ApplyConfiguration()
    {
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Dynamic;
            body.freezeRotation = false;
            body.simulated = true;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            body.WakeUp();
        }

        if (surfaceCollider != null)
        {
            surfaceCollider.size = new Vector2(bridgeLength, colliderHeight);
            surfaceCollider.offset = Vector2.zero;
        }

        if (bridgeVisual != null && bridgeVisual.sprite != null)
        {
            Bounds spriteBounds = bridgeVisual.sprite.bounds;
            float width = Mathf.Max(0.01f, spriteBounds.size.x);
            Vector3 scale = bridgeVisual.transform.localScale;
            scale.x = bridgeLength / width;
            bridgeVisual.transform.localScale = scale;

            Vector3 scaledCenter = Vector3.Scale(spriteBounds.center, scale);
            bridgeVisual.transform.localPosition = -scaledCenter;
        }

        if (hinge == null)
        {
            return;
        }

        // A zero-speed HingeJoint motor actively holds the plank at its current
        // angle. The old prefab saved Use Motor=true with speed 0, which made a
        // normal bridge look completely static even while Player stood off-centre.
        // Free seesaws must have neither a motor nor angle limits. Only the final
        // rotators use a motor, and they also remain free to rotate through 360 degrees.
        hinge.useMotor = false;
        hinge.useLimits = false;

        if (continuousRotation || autoOscillation)
        {
            JointMotor2D motor = hinge.motor;
            motor.motorSpeed = autoOscillation
                ? -Mathf.Abs(motorSpeed) * autoDirection
                : motorSpeed;
            motor.maxMotorTorque = motorTorque;
            hinge.motor = motor;
            hinge.useMotor = true;
        }
    }

    private void UpdateAutoOscillation()
    {
        if (hinge == null)
        {
            return;
        }

        float angleFromRest = Mathf.DeltaAngle(restingRotation, body.rotation);
        if (autoDirection > 0 && angleFromRest >= autoTurnAngle)
        {
            autoDirection = -1;
            ApplyAutoMotorDirection();
        }
        else if (autoDirection < 0 && angleFromRest <= -autoTurnAngle)
        {
            autoDirection = 1;
            ApplyAutoMotorDirection();
        }
    }

    private void ApplyAutoMotorDirection()
    {
        JointMotor2D motor = hinge.motor;
        // HingeJoint2D positive motor speed produces clockwise (negative Transform
        // angle) rotation, so the motor sign is opposite the desired plank angle.
        motor.motorSpeed = -Mathf.Abs(motorSpeed) * autoDirection;
        motor.maxMotorTorque = motorTorque;
        hinge.motor = motor;
        hinge.useMotor = true;
        body.WakeUp();
    }

    private void ResetAutoDirection()
    {
        // Positive 2D rotation raises the right end and lowers the left end.
        autoDirection = startLeftFirst ? 1 : -1;
    }

    private void OnValidate()
    {
        bridgeLength = Mathf.Max(2f, bridgeLength);
        colliderHeight = Mathf.Max(0.1f, colliderHeight);
        maxAngularSpeed = Mathf.Max(5f, maxAngularSpeed);
        autoTurnAngle = Mathf.Clamp(autoTurnAngle, 5f, 80f);
        ResetAutoDirection();
        CacheComponents();
        ApplyConfiguration();
    }

#if UNITY_EDITOR
    public void SimulateFixedStepForEditorTest()
    {
        FixedUpdate();
    }
#endif
}
