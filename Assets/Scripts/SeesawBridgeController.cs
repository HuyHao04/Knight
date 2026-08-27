using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(HingeJoint2D), typeof(BoxCollider2D))]
public sealed class SeesawBridgeController : MonoBehaviour
{
    [Header("Bridge Mode")]
    [SerializeField] private bool continuousRotation;
    [SerializeField, Range(5f, 80f)] private float tiltLimit = 28f;
    [SerializeField] private float motorSpeed = 45f;
    [SerializeField, Min(1f)] private float motorTorque = 1500f;

    [Header("Seesaw Stability")]
    [SerializeField, Min(0f)] private float returnStrength = 0.35f;
    [SerializeField, Min(0f)] private float rotationDamping = 1.4f;
    [SerializeField, Min(5f)] private float maxAngularSpeed = 85f;

    [Header("Bridge Shape")]
    [SerializeField, Min(2f)] private float bridgeLength = 5f;
    [SerializeField, Min(0.1f)] private float colliderHeight = 0.32f;
    [SerializeField] private SpriteRenderer bridgeVisual;

    private Rigidbody2D body;
    private HingeJoint2D hinge;
    private BoxCollider2D surfaceCollider;
    private float restingRotation;

    public bool IsContinuousRotation => continuousRotation;
    public float BridgeLength => bridgeLength;

    private void Awake()
    {
        CacheComponents();
        restingRotation = body.rotation;
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
        motorSpeed = rotationSpeed;
        tiltLimit = Mathf.Clamp(maximumTilt, 5f, 80f);

        CacheComponents();
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

        hinge.useLimits = !continuousRotation;
        hinge.useMotor = continuousRotation;

        JointAngleLimits2D limits = hinge.limits;
        limits.min = -tiltLimit;
        limits.max = tiltLimit;
        hinge.limits = limits;

        JointMotor2D motor = hinge.motor;
        motor.motorSpeed = motorSpeed;
        motor.maxMotorTorque = motorTorque;
        hinge.motor = motor;
    }

    private void OnValidate()
    {
        bridgeLength = Mathf.Max(2f, bridgeLength);
        colliderHeight = Mathf.Max(0.1f, colliderHeight);
        maxAngularSpeed = Mathf.Max(5f, maxAngularSpeed);
        CacheComponents();
        ApplyConfiguration();
    }
}
