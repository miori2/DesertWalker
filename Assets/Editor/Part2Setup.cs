using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Run once with the Part2 scene open: Tools > Build Part 2 Scene
// Builds the sand, ocean, four robots and camera from the video.
// It only creates objects, so you can change anything afterwards in the Inspector.
public static class Part2Setup
{
    const float Speed = 6.2f;       // Every robot walks at this speed, so the group stays together
    const float BaseLeg = 4.4f;     // Leg length of a robot with scale 1

    [MenuItem("Tools/Build Part 2 Scene")]
    static void Build()
    {
        if (SceneManager.GetActiveScene().name != "Part2")
        {
            Debug.LogWarning("Open the Part2 scene first. Nothing was changed.");
            return;
        }
        if (GameObject.Find("GreenRobot") != null)
        {
            Debug.LogWarning("The robots already exist. Nothing was changed.");
            return;
        }

        // Colors, measured from the video.
        Material red = Mat("Part2Red", new Color32(215, 45, 30, 255));
        Material blue = Mat("Part2Blue", new Color32(40, 50, 230, 255));
        Material cream = Mat("Part2Limb", new Color32(235, 225, 195, 255));
        Material head = Mat("Part2Head", new Color32(55, 75, 80, 255));
        Material green = Mat("Part2Green", new Color32(30, 240, 90, 255));
        Material yellow = Mat("Part2Yellow", new Color32(225, 180, 70, 255));
        Material ocean = Mat("Part2Ocean", new Color32(15, 35, 230, 255));

        MakeSand();

        // Scale sets the robot's size. Swing is how far its hips and shoulders move, in degrees.
        // Green is the robot the camera follows, so it sits at the middle of the world.
        Transform greenRobot = Robot("GreenRobot", new Vector3(0f, 0f, 0f), 1f, 25f, green, red, blue, cream, head);
        Robot("TallYellowRobot", new Vector3(-28f, 0f, 25f), 1.2f, 30f, yellow, red, blue, cream, head);
        Robot("SmallYellowRobot", new Vector3(-16.7f, 0f, 17.7f), 0.73f, 25f, yellow, red, blue, cream, head);
        Robot("BlueRobot", new Vector3(18.2f, 0f, 23.3f), 1.02f, 25f, blue, red, blue, cream, head);

        // The sea in the video never gets closer, so the ocean travels with the green robot.
        GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "OceanPlane";
        Object.DestroyImmediate(sea.GetComponent<Collider>());
        sea.GetComponent<Renderer>().sharedMaterial = ocean;
        sea.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        sea.transform.SetParent(greenRobot, false);
        sea.transform.localPosition = new Vector3(0f, 0.3f, 1640f);   // Shore starts 140 ahead of the robot
        sea.transform.localScale = new Vector3(600f, 1f, 300f);       // A plane is 10 units wide before scaling

        // Sun behind the camera and to the right, so shadows fall to the left and away.
        Light sun = Object.FindFirstObjectByType<Light>();
        sun.transform.rotation = Quaternion.Euler(45f, -45f, 0f);
        sun.intensity = 1.4f;

        // Low camera behind the green robot, aimed slightly downward.
        Camera cam = Camera.main;
        cam.farClipPlane = 4000f;
        CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = greenRobot;
        follow.offset = new Vector3(0f, 12f, -32.4f);
        follow.lookHeight = 9.7f;
        cam.transform.position = greenRobot.TransformPoint(follow.offset);
        cam.transform.LookAt(greenRobot.position + Vector3.up * follow.lookHeight);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Part 2 scene built. Press Play.");
    }

    // Applies the look that was tuned against the video, to a scene that's already built.
    [MenuItem("Tools/Adjust Part 2 Look")]
    static void Adjust()
    {
        Camera.main.GetComponent<CameraFollow>().lookHeight = 9.7f;
        Transform robot = GameObject.Find("GreenRobot").transform;
        Camera.main.transform.LookAt(robot.position + Vector3.up * 9.7f);
        robot.Find("OceanPlane").localPosition = new Vector3(0f, 0.3f, 1640f);
        Object.FindFirstObjectByType<Light>().intensity = 1.4f;

        // Far enough for the shadows of the robots at the back of the group.
        UniversalRenderPipelineAsset pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        pipeline.shadowDistance = 120f;
        EditorUtility.SetDirty(pipeline);
        AssetDatabase.SaveAssets();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Part 2 look adjusted.");
    }

    static Transform Robot(string name, Vector3 position, float scale, float swing,
        Material body, Material red, Material blue, Material limb, Material head)
    {
        GameObject robot = new GameObject(name);
        Transform root = robot.transform;

        RoboController controller = robot.AddComponent<RoboController>();
        controller.Speed = Speed;

        // Stride time that keeps this robot's feet in step with its walking speed.
        // Bigger robots take longer strides, so they swing more slowly.
        float strideTime = 4f * BaseLeg * scale * Mathf.Sin(swing * Mathf.Deg2Rad) / Speed;

        Cube("Body", root, new Vector3(0f, 6.63f, 0f), new Vector3(2f, 4.45f, 1f), body);
        Part(PrimitiveType.Sphere, "Head", root, new Vector3(0f, 9.35f, 0f), Vector3.one * 1.05f, head);

        // Blue joints are on the robot's left and red joints are on its right.
        // Each arm swings opposite the leg on the same side.
        Arm("Left", root, -1.3f, -1f, swing, strideTime, blue, limb);
        Arm("Right", root, 1.3f, 1f, swing, strideTime, red, limb);
        Leg("Left", root, -0.6f, 1f, swing, strideTime, blue, limb);
        Leg("Right", root, 0.6f, -1f, swing, strideTime, red, limb);

        root.localScale = Vector3.one * scale;
        // Slightly lowered, because a swinging leg lifts the foot off the ground.
        root.position = position + Vector3.down * 0.25f * scale;
        return root;
    }

    static void Arm(string side, Transform parent, float x, float direction, float swing, float strideTime, Material joint, Material limb)
    {
        Transform shoulder = Pivot(side + "ShoulderPivot", parent, new Vector3(x, 8.4f, 0f));
        Swing(shoulder, swing, direction, 0f, strideTime);
        Cube(side + "ShoulderJoint", shoulder, Vector3.zero, Vector3.one * 0.72f, joint);
        Cube(side + "UpperArm", shoulder, new Vector3(0f, -1f, 0f), new Vector3(0.6f, 2f, 0.6f), limb);

        // The elbow bends forward, most of all when the arm is at the front of its swing.
        float elbowSwing = 15f;
        Transform elbow = Pivot(side + "ElbowPivot", shoulder, new Vector3(0f, -2f, 0f));
        elbow.localRotation = Quaternion.Euler(-elbowSwing, 0f, 0f);
        Swing(elbow, elbowSwing, direction, 0f, strideTime);
        Cube(side + "ElbowJoint", elbow, Vector3.zero, Vector3.one * 0.72f, joint);
        Cube(side + "LowerArm", elbow, new Vector3(0f, -1f, 0f), new Vector3(0.6f, 2f, 0.6f), limb);
    }

    static void Leg(string side, Transform parent, float x, float direction, float swing, float strideTime, Material joint, Material limb)
    {
        Transform hip = Pivot(side + "HipPivot", parent, new Vector3(x, 4.4f, 0f));
        Swing(hip, swing, direction, 0f, strideTime);
        Cube(side + "HipJoint", hip, Vector3.zero, Vector3.one * 0.72f, joint);
        Cube(side + "Thigh", hip, new Vector3(0f, -1.1f, 0f), new Vector3(0.6f, 2.2f, 0.6f), limb);

        // The knee bends backward only: its angle runs from 0 to twice kneeSwing.
        // It starts part-way through its swing, so it bends as the foot leaves the ground.
        float kneeSwing = 25f;
        float startAngle = -0.5f * kneeSwing * direction;
        Transform knee = Pivot(side + "KneePivot", hip, new Vector3(0f, -2.2f, 0f));
        knee.localRotation = Quaternion.Euler(kneeSwing + startAngle, 0f, 0f);
        Swing(knee, kneeSwing, direction, startAngle, strideTime);
        Cube(side + "KneeJoint", knee, Vector3.zero, Vector3.one * 0.72f, joint);
        Cube(side + "Shin", knee, new Vector3(0f, -1.1f, 0f), new Vector3(0.6f, 2.2f, 0.6f), limb);
    }

    // Adds your OscillationScript. A full swing is 4 * Max degrees, so this Stp
    // makes every joint on a robot take the same time, whatever its Max is.
    static void Swing(Transform pivot, float max, float direction, float startAngle, float strideTime)
    {
        OscillationScript joint = pivot.gameObject.AddComponent<OscillationScript>();
        joint.Max = max;
        joint.Stp = direction * 4f * max / strideTime;
        joint.Ang = startAngle;
    }

    static Transform Pivot(string name, Transform parent, Vector3 localPosition)
    {
        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = localPosition;
        return pivot;
    }

    static void Cube(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
    {
        Part(PrimitiveType.Cube, name, parent, localPosition, size, material);
    }

    static void Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    static Material Mat(string name, Color color)
    {
        string path = "Assets/Material/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void MakeSand()
    {
        // Draw a two-color checker texture and save it as a PNG.
        string texturePath = "Assets/Material/Part2SandChecker.png";
        Color32 light = new Color32(232, 195, 135, 255);
        Color32 dark = new Color32(198, 150, 98, 255);
        Texture2D checker = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                checker.SetPixel(x, y, ((x / 32 + y / 32) % 2 == 0) ? light : dark);
        File.WriteAllBytes(texturePath, checker.EncodeToPNG());
        Object.DestroyImmediate(checker);
        AssetDatabase.ImportAsset(texturePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 8;
        importer.SaveAndReimport();

        TerrainLayer layer = new TerrainLayer();
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        layer.tileSize = new Vector2(1.2f, 1.2f);
        AssetDatabase.CreateAsset(layer, "Assets/Material/Part2Sand.terrainlayer");

        // A long, flat strip of sand. At walking speed it lasts about 15 minutes.
        TerrainData data = new TerrainData();
        data.heightmapResolution = 33;
        data.size = new Vector3(6000f, 10f, 6000f);
        data.alphamapResolution = 16;
        data.terrainLayers = new TerrainLayer[] { layer };
        float[,,] paint = new float[16, 16, 1];
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                paint[y, x, 0] = 1f;
        data.SetAlphamaps(0, 0, paint);
        AssetDatabase.CreateAsset(data, "Assets/Material/Part2Terrain.asset");

        GameObject terrain = Terrain.CreateTerrainGameObject(data);
        terrain.name = "Terrain2";
        terrain.transform.position = new Vector3(-3000f, 0f, -500f);
        terrain.GetComponent<Terrain>().materialTemplate = GraphicsSettings.currentRenderPipeline.defaultTerrainMaterial;
    }
}
