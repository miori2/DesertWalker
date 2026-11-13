using UnityEngine;

// Swings a limb back and forth, like a pendulum.
// Put this on a pivot (the empty object at the shoulder or hip), not on the cube.
// The limb then turns around the joint, because the cubes are children of the pivot.
public class OscillationScript : MonoBehaviour
{
    public float Max = 20f;     // How far the limb swings each way, in degrees
    public float Stp = 60f;     // Swing speed in degrees per second. Negative starts the opposite way.
    public float Ang = 0f;      // Where the swing is right now. The script updates this for you.
    public float Pose = 0f;     // Extra bend added on top of the swing, in degrees. PoseHotkeys changes this.
    public bool Hold = false;   // True: fade the swing out and stay still at Pose
    public float TargetMax;     // Max eases toward this. PoseHotkeys changes it to make the swing bigger.
    public float NormalMax { get; private set; }  // The Max the joint started with

    RoboController robo;        // The controller tells us whether the robot is walking
    float shown;                // The angle the joint is actually turned to right now
    float poseNow = 0f;         // Pose, eased in a little each frame so the joint doesn't jump
    float swingAmount = 1f;     // 1 = full swing, 0 = no swing

    void Start()
    {
        // Look up through the parents to find the RoboController on Robot.
        robo = GetComponentInParent<RoboController>();

        // The scene was saved with the joint turned to Ang.
        shown = Ang;

        NormalMax = Max;
        TargetMax = Max;
    }

    void Update()
    {
        // Ease the swing size toward TargetMax. Ang and Stp grow by the same amount,
        // so the joint stays at the same point in its swing and keeps the same timing.
        if (Max != TargetMax)
        {
            float newMax = Mathf.MoveTowards(Max, TargetMax, 90f * Time.deltaTime);
            float grow = newMax / Max;
            Ang *= grow;
            Stp *= grow;
            Max = newMax;
        }

        // While the robot walks, keep the swing counting even if Hold is on.
        // That way a held arm comes back in step with the legs.
        if (robo.IsWalking)
        {
            // Time.deltaTime keeps the speed the same on fast and slow computers.
            Ang += Stp * Time.deltaTime;

            // Reached the limit? Reverse direction.
            if ((Ang >= Max && Stp > 0) || (Ang <= -Max && Stp < 0))
            {
                Stp = -Stp;
            }
        }

        // Swing only while walking and not held. Otherwise fade the swing out.
        float wantedSwing = (robo.IsWalking && !Hold) ? 1f : 0f;
        swingAmount = Mathf.MoveTowards(swingAmount, wantedSwing, 3f * Time.deltaTime);

        // Ease toward the pose at 180 degrees per second.
        poseNow = Mathf.MoveTowards(poseNow, Pose, 180f * Time.deltaTime);

        // Turn the joint by the difference between where it should be and where it is.
        float target = Ang * swingAmount + poseNow;
        transform.Rotate(target - shown, 0, 0);
        shown = target;
    }
}
