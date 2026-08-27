using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnergyBeamController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer beamRenderer;
    [SerializeField] private Animator animator;

    [Header("Beam Geometry")]
    [SerializeField, Min(0.01f)] private float nativeLength = 1.92f;
    [SerializeField, Min(0.01f)] private float thicknessScale = 0.55f;
    // The imported LVGames beam points toward local -X (left).
    [SerializeField] private float spriteAngleOffset = 180f;

    private Transform origin;
    private Transform target;
    private bool isPlaying;

    private void Awake()
    {
        if (beamRenderer == null)
        {
            beamRenderer = GetComponent<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        beamRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (isPlaying)
        {
            AlignBeam();
        }
    }

    public void PlayBeam(Transform beamOrigin, Transform beamTarget)
    {
        origin = beamOrigin;
        target = beamTarget;

        if (origin == null || target == null)
        {
            Debug.LogWarning("EnergyBeamController requires both Origin and Target.", this);
            return;
        }

        isPlaying = true;
        beamRenderer.enabled = true;

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.enabled = true;
            animator.Play(0, 0, 0f);
        }

        AlignBeam();
    }

    public void StopBeam()
    {
        isPlaying = false;

        if (beamRenderer != null)
        {
            beamRenderer.enabled = false;
        }

        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    private void AlignBeam()
    {
        if (origin == null || target == null)
        {
            StopBeam();
            return;
        }

        Vector2 start = origin.position;
        Vector2 end = target.position;
        Vector2 direction = end - start;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            beamRenderer.enabled = false;
            return;
        }

        beamRenderer.enabled = true;
        transform.position = (start + end) * 0.5f;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);
        transform.localScale = new Vector3(distance / nativeLength, thicknessScale, 1f);
    }
}
