using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] float startCamera;
    [SerializeField] float endCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is create

    // Update is called once per frame
    void Update()
    {
        if (player == null)
        return;
        float playerX = player.transform.position.x;
        float cameraX = transform.position.x;
        float cameraY = transform.position.y;
        float cameraZ = transform.position.z;
        if (playerX > startCamera && playerX < endCamera)
        {
            cameraX = playerX;
        }
        else if (playerX <= startCamera)
        {
            cameraX = startCamera;
        }
        else if (playerX >= endCamera)
        {
            cameraX = endCamera;
        }
        transform.position = new Vector3(cameraX, cameraY, cameraZ);
    }

    public void SnapToPlayer()
    {
        if (player == null)
        {
            return;
        }

        float snappedX = Mathf.Clamp(player.transform.position.x, startCamera, endCamera);
        transform.position = new Vector3(snappedX, transform.position.y, transform.position.z);
    }
}
