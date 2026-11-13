using UnityEngine;

// Hotkeys that change how the fingers and arms move, for every robot in the scene.
// Put this on any one object in the scene. In Part3 it's on Main Camera.
//
//   F = make fists          B = bend fingers backward    N = fingers back to normal
//   U = arms swing up to 90 degrees in front    D = arms swing normally again
public class PoseHotkeys : MonoBehaviour
{
    public float FistBend = -65f;   // Each finger piece curls this much more toward the palm
    public float BackBend = 45f;    // Each finger piece bends this much backward
    public float ArmSwingUp = 90f;  // With U, each arm swings forward this far, in degrees

    OscillationScript[] joints;     // Every swinging joint in the scene

    void Start()
    {
        joints = FindObjectsByType<OscillationScript>(FindObjectsSortMode.None);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) SetFingers(FistBend, true);
        if (Input.GetKeyDown(KeyCode.B)) SetFingers(BackBend, true);
        if (Input.GetKeyDown(KeyCode.N)) SetFingers(0f, false);

        if (Input.GetKeyDown(KeyCode.U)) SetArms(true);
        if (Input.GetKeyDown(KeyCode.D)) SetArms(false);
    }

    // Finds the finger joints by name: "Finger" or "Thumb".
    void SetFingers(float bend, bool hold)
    {
        foreach (OscillationScript joint in joints)
        {
            if (joint.name.Contains("Finger") || joint.name.Contains("Thumb"))
            {
                joint.Pose = bend;
                joint.Hold = hold;
            }
        }
    }

    // Makes the shoulder swing bigger or back to normal. The arm still swings back
    // to its usual spot behind the robot. Only the forward part of the swing grows.
    void SetArms(bool up)
    {
        foreach (OscillationScript joint in joints)
        {
            if (!joint.name.Contains("ShoulderPivot")) continue;

            float normal = joint.NormalMax;
            if (up)
            {
                // The swing now covers from "normal" behind to ArmSwingUp in front:
                // half of that range each way, centered partway forward.
                joint.TargetMax = (ArmSwingUp + normal) / 2f;
                joint.Pose = -(ArmSwingUp - normal) / 2f;   // Negative turns the arm forward
            }
            else
            {
                joint.TargetMax = normal;
                joint.Pose = 0f;
            }
        }
    }
}
