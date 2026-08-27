using UnityEngine;

public static class InteractionInput
{
    private static int consumedFrame = -1;

    public static bool TryConsumeInteract()
    {
        if (!Input.GetKeyDown(KeyCode.E) || consumedFrame == Time.frameCount)
        {
            return false;
        }

        consumedFrame = Time.frameCount;
        return true;
    }
}
