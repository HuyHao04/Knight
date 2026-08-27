using System.Collections;
using UnityEngine;

public class ObeliskManager : MonoBehaviour
{
    [Header("Encounter References")]
    [SerializeField] private ObeliskController leftObelisk;
    [SerializeField] private ObeliskController rightObelisk;
    [SerializeField] private NecromancerBoss boss;
    [SerializeField] private EnergyBeamController beamPrefab;
    [SerializeField] private PortalPromptUI interactionPrompt;

    [Header("Counter Balance")]
    [SerializeField, Min(1)] private int obeliskDamage = 25;
    [SerializeField, Min(0f)] private float chargeDelay = 0.3f;
    [SerializeField, Min(0.05f)] private float beamHitDelay = 0.2f;
    [SerializeField, Min(0.1f)] private float beamVisibleDuration = 0.75f;

    [Header("Optional Audio")]
    [SerializeField] private AudioClip counterChargeSfx;
    [SerializeField] private AudioClip beamFireSfx;
    [SerializeField] private AudioClip bossImpactSfx;

    private bool leftActivated;
    private bool rightActivated;
    private bool windowOpen;
    private bool counterTriggered;
    private bool damageApplied;
    private Coroutine counterRoutine;
    private EnergyBeamController leftBeam;
    private EnergyBeamController rightBeam;

    public bool WindowOpen => windowOpen;
    public bool CounterSequenceRunning => counterRoutine != null;

    private void Awake()
    {
        ResolveInteractionPrompt();
        leftObelisk?.Initialize(this, interactionPrompt);
        rightObelisk?.Initialize(this, interactionPrompt);
        ResetState(false);
    }

    public void OpenWindow()
    {
        if (windowOpen || counterTriggered || boss == null || boss.IsDefeated)
        {
            return;
        }

        ResolveInteractionPrompt();
        leftObelisk?.Initialize(this, interactionPrompt);
        rightObelisk?.Initialize(this, interactionPrompt);

        CleanupBeams();
        leftActivated = false;
        rightActivated = false;
        damageApplied = false;
        counterTriggered = false;
        windowOpen = true;

        leftObelisk?.SetState(ObeliskController.ObeliskState.Ready);
        rightObelisk?.SetState(ObeliskController.ObeliskState.Ready);

        Debug.Log("OBELISK WINDOW OPEN", this);
    }

    private void ResolveInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            return;
        }

        interactionPrompt = FindAnyObjectByType<PortalPromptUI>(
            FindObjectsInactive.Include
        );

        if (interactionPrompt == null)
        {
            Debug.LogError(
                "ObeliskManager: missing PortalPromptUI, so [E] Activate cannot be displayed.",
                this
            );
        }
    }

    public void NotifyActivated(ObeliskController obelisk)
    {
        if (!windowOpen || counterTriggered || obelisk == null)
        {
            return;
        }

        if (obelisk == leftObelisk)
        {
            leftActivated = true;
            Debug.Log("LEFT OBELISK ACTIVATED", this);
        }
        else if (obelisk == rightObelisk)
        {
            rightActivated = true;
            Debug.Log("RIGHT OBELISK ACTIVATED", this);
        }
        else
        {
            return;
        }

        if (leftActivated && rightActivated)
        {
            TriggerObeliskCounter();
        }
    }

    public void CloseFailedWindow()
    {
        if (counterTriggered)
        {
            return;
        }

        if (windowOpen)
        {
            Debug.Log("OBELISK WINDOW FAILED", this);
        }

        ResetState(true);
    }

    public void ResetEncounter()
    {
        if (counterRoutine != null)
        {
            StopCoroutine(counterRoutine);
            counterRoutine = null;
        }

        CleanupBeams();
        ResetState(false);
    }

    public void AbortEncounter()
    {
        ResetEncounter();
    }

    private void TriggerObeliskCounter()
    {
        if (counterTriggered || !leftActivated || !rightActivated)
        {
            return;
        }

        counterTriggered = true;
        windowOpen = false;

        Debug.Log("BOTH OBELISKS ACTIVATED", this);

        if (boss == null || !boss.RequestObeliskCounterInterrupt())
        {
            ResetState(true);
            return;
        }

        counterRoutine = StartCoroutine(ObeliskCounterSequence());
    }

    private IEnumerator ObeliskCounterSequence()
    {
        Debug.Log("OBELISK COUNTER START", this);
        PlayOptionalSfx(counterChargeSfx);

        yield return new WaitForSeconds(chargeDelay);

        if (boss == null || boss.IsDefeated || beamPrefab == null || boss.ObeliskBeamTarget == null)
        {
            if (boss != null && !boss.IsDefeated)
            {
                boss.CompleteObeliskCounter();
            }

            ResetState(true);
            counterRoutine = null;
            yield break;
        }

        leftObelisk?.SetState(ObeliskController.ObeliskState.Firing);
        rightObelisk?.SetState(ObeliskController.ObeliskState.Firing);

        leftBeam = Instantiate(beamPrefab, transform);
        rightBeam = Instantiate(beamPrefab, transform);
        leftBeam.PlayBeam(leftObelisk.BeamOrigin, boss.ObeliskBeamTarget);
        rightBeam.PlayBeam(rightObelisk.BeamOrigin, boss.ObeliskBeamTarget);

        Debug.Log("LEFT BEAM FIRED", this);
        Debug.Log("RIGHT BEAM FIRED", this);
        PlayOptionalSfx(beamFireSfx);

        float hitDelay = Mathf.Min(beamHitDelay, beamVisibleDuration);
        yield return new WaitForSeconds(hitDelay);

        if (!damageApplied && boss != null && !boss.IsDefeated)
        {
            damageApplied = boss.TakeObeliskDamage(obeliskDamage);
            if (damageApplied)
            {
                Debug.Log("MALAKOR HIT BY OBELISK", this);
                PlayOptionalSfx(bossImpactSfx);
            }
        }

        float remainingBeamTime = Mathf.Max(0f, beamVisibleDuration - hitDelay);
        if (remainingBeamTime > 0f)
        {
            yield return new WaitForSeconds(remainingBeamTime);
        }

        CleanupBeams();

        if (boss != null && !boss.IsDefeated)
        {
            boss.CompleteObeliskCounter();
        }

        Debug.Log("OBELISK COUNTER SUCCESS", this);
        ResetState(true);
        counterRoutine = null;
    }

    private void ResetState(bool logReset)
    {
        windowOpen = false;
        counterTriggered = false;
        leftActivated = false;
        rightActivated = false;
        damageApplied = false;

        leftObelisk?.SetState(ObeliskController.ObeliskState.Inactive);
        rightObelisk?.SetState(ObeliskController.ObeliskState.Inactive);

        if (logReset)
        {
            Debug.Log("OBELISKS RESET", this);
        }
    }

    private void CleanupBeams()
    {
        if (leftBeam != null)
        {
            leftBeam.StopBeam();
            Destroy(leftBeam.gameObject);
            leftBeam = null;
        }

        if (rightBeam != null)
        {
            rightBeam.StopBeam();
            Destroy(rightBeam.gameObject);
            rightBeam = null;
        }
    }

    private static void PlayOptionalSfx(AudioClip clip)
    {
        if (clip != null && AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX(clip);
        }
    }
}
