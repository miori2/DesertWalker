using UnityEngine;

// Swings a limb back and forth, like a pendulum.
// Put this on a pivot (the empty object at the shoulder or hip), not on the cube.
// The limb then turns around the joint, because the cubes are children of the pivot.
public class OscillationScript : MonoBehaviour
{
    public float Max = 20f;   // How far the limb swings each way, in degrees
    public float Stp = 60f;   // Swing speed in degrees per second. Negative starts the opposite way.
    public float Ang = 0f;    // The limb's current angle. The script updates this for you.

    RoboController robo;      // The controller tells us whether the robot is walking

    void Start()
    {
        // Look up through the parents to find the RoboController on Robot.
        robo = GetComponentInParent<RoboController>();
    }

    void Update()
    {
        // Robot standing still? Straighten the limb and skip the swing.
        if (!robo.IsWalking)
        {
            transform.Rotate(-Ang, 0, 0);
            Ang = 0f;
            return;
        }

        // How far to move this frame. Time.deltaTime keeps the speed
        // the same on fast and slow computers.
        float move = Stp * Time.deltaTime;

        // Turn the limb, and remember its new angle.
        transform.Rotate(move, 0, 0);
        Ang += move;

        // Reached the limit? Reverse direction.
        if ((Ang >= Max && Stp > 0) || (Ang <= -Max && Stp < 0))
        {
            Stp = -Stp;
        }
    }
}
