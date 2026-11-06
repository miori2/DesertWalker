using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    // Where the camera sits, measured from the target: Z is behind it, Y is above it
    public Vector3 offset = new Vector3(0, 11, -20);

    public float followSpeed = 5f;

    // How far above the target's feet the camera aims
    public float lookHeight = 7f;

    void LateUpdate()
    {
        if (target == null)
            return;

        // TransformPoint measures the offset from the target's point of view,
        // so "behind" stays behind the robot when it turns.
        Vector3 desiredPosition = target.TransformPoint(offset);

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        transform.LookAt(target.position + Vector3.up * lookHeight);
    }

    // if (input.GetKey(KyCode.O))
//      {
//        this.transform.Rotate(new Vector3(Stp, 0f, 0f)); anX += Stp;
//          if (Mathf.Abs(anX) >= Max) { anX -= 2 * Stp; Stp = -Stp; )
//      }

                            // Max = 10f, Stp= 1f, Ang
}
