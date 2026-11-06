using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Run once from the menu: Tools > Update Robot
// Makes the legs and hips bigger and puts the camera behind the robot.
// You can delete this file afterwards.
public static class WalkerSetup
{
    [MenuItem("Tools/Update Robot")]
    static void UpdateRobot()
    {
        Transform robot = GameObject.Find("Robot").transform;

        // Space the hips evenly under the body, with room for the wider legs.
        ResizeLeg("LeftHipPivot", "LeftHip", "LeftShin", 128.85f);
        ResizeLeg("RightHipPivot", "RightHip", "RightShin", 130.25f);

        // The legs are longer now, so lift the robot to keep its feet on the ground.
        Undo.RecordObject(robot, "Raise robot");
        robot.position += new Vector3(0f, 0.69f, 0f);

        // Longer legs take longer steps, so the robot walks a little faster.
        RoboController controller = robot.GetComponent<RoboController>();
        Undo.RecordObject(controller, "Set speed");
        controller.Speed = 5.2f;

        // Aim the camera at the Robot parent, because the Body cube is tilted and flipped.
        Camera cam = Camera.main;
        CameraFollow follow = cam.GetComponent<CameraFollow>();
        Undo.RecordObject(follow, "Set camera");
        follow.target = robot;
        follow.offset = new Vector3(0f, 11f, -20f);
        follow.lookHeight = 7f;
        Undo.RecordObject(cam.transform, "Move camera");
        cam.transform.position = robot.TransformPoint(follow.offset);
        cam.transform.LookAt(robot.position + Vector3.up * follow.lookHeight);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Robot updated. Press Play, and save the scene when you are happy.");
    }

    static void ResizeLeg(string pivotName, string hipName, string shinName, float x)
    {
        Transform pivot = GameObject.Find(pivotName).transform;
        Transform hip = GameObject.Find(hipName).transform;
        Transform shin = GameObject.Find(shinName).transform;
        float z = 208.6f;

        Undo.RecordObject(pivot, "Resize leg");
        Undo.RecordObject(hip, "Resize leg");
        Undo.RecordObject(shin, "Resize leg");

        pivot.position = new Vector3(x, 4.74f, z);

        // Hip: 1.31 tall, hanging from the pivot.
        hip.position = new Vector3(x, 4.085f, z);
        hip.localScale = new Vector3(1.2f, 1.31f, 0.9f);

        // Shin: 3.75 tall, hanging from the bottom of the hip.
        shin.position = new Vector3(x, 1.555f, z);
        shin.localScale = new Vector3(1.2f, 3.75f, 0.9f);
    }
}
