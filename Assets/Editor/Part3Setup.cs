using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Run once with the Part3 scene open: Tools > Build Part 3 Scene
// Builds the checkered ground, three robots with hands and fingers, and the camera from the video.
// It only creates objects, so you can change anything afterwards in the Inspector.
public static class Part3Setup
{
    const float Speed = 4.5f;        // Every robot walks at this speed
    const float LegLength = 5.2f;    // Hip pivot to the ground, for a robot with scale 1
    const float Swing = 22f;         // How far hips and shoulders swing, in degrees

    static Material red, yellow, maroon, hand, black;

    [MenuItem("Tools/Build Part 3 Scene")]
    static void Build()
    {
        if (SceneManager.GetActiveScene().name != "Part3")
        {
            Debug.LogWarning("Open the Part3 scene first. Nothing was changed.");
            return;
        }
        // Running it again replaces the robots and ground it made before.
        foreach (string old in new[] { "MaroonRobot", "GreenRobot", "WhiteRobot", "Terrain" })
        {
            GameObject existing = GameObject.Find(old);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        // Colors, matched by eye to the video.
        red = Mat("Part3Red", new Color32(225, 35, 40, 255));
        yellow = Mat("Part3Yellow", new Color32(230, 225, 45, 255));
        maroon = Mat("Part3Maroon", new Color32(110, 22, 22, 255));
        hand = Mat("Part3Hand", new Color32(160, 150, 55, 255));
        black = Mat("Part3Black", new Color32(20, 20, 20, 255));
        Material green = Mat("Part3Green", new Color32(45, 215, 70, 255));
        Material white = Mat("Part3White", new Color32(235, 235, 235, 255));
        Material olive = Mat("Part3Olive", new Color32(135, 130, 40, 255));

        MakeGround();

        // AutoTurn makes each robot walk a big circle around the camera.
        // Different turn rates give different circles, so the robots spread out and meet again.
        Robot("MaroonRobot", new Vector3(-6f, 0f, 16f), 2.6f, maroon, maroon, maroon, 16f);
        Robot("GreenRobot", new Vector3(0f, 0f, 16f), 2.2f, green, black, green, 18f);
        Robot("WhiteRobot", new Vector3(6f, 0f, 16f), 2.2f, white, olive, green, 20f);

        // Sun from behind and to the right, so shadows fall to the left.
        Light sun = Object.FindFirstObjectByType<Light>();
        sun.transform.rotation = Quaternion.Euler(40f, -70f, 0f);
        sun.shadows = LightShadows.Soft;

        // The camera stays still and the robots walk around it.
        Camera cam = Camera.main;
        cam.transform.position = new Vector3(0f, 9f, -6f);
        cam.transform.rotation = Quaternion.Euler(12.5f, 0f, 0f);
        cam.farClipPlane = 2000f;
        cam.gameObject.AddComponent<PoseHotkeys>();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Part 3 scene built. Press Play.");
    }

    // Adds the finger and arm hotkeys to a Part3 scene that's already built.
    [MenuItem("Tools/Add Part 3 Hotkeys")]
    static void AddHotkeys()
    {
        Camera cam = Camera.main;
        if (cam.GetComponent<PoseHotkeys>() == null)
            Undo.AddComponent<PoseHotkeys>(cam.gameObject);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Hotkeys added to Main Camera: F, B, N for fingers and U, D for arms.");
    }

    static void Robot(string name, Vector3 position, float bodyWidth,
        Material body, Material head, Material legs, float turn)
    {
        Transform root = new GameObject(name).transform;
        RoboController controller = root.gameObject.AddComponent<RoboController>();
        controller.Speed = Speed;
        controller.AutoTurn = -turn;   // Negative turns left

        // Stride time that keeps the feet in step with the walking speed.
        float strideTime = 4f * LegLength * Mathf.Sin(Swing * Mathf.Deg2Rad) / Speed;

        Cube("Body", root, new Vector3(0f, 7.6f, 0f), new Vector3(bodyWidth, 4.8f, 1.2f), body);
        Part(PrimitiveType.Sphere, "Head", root, new Vector3(0f, 10.6f, 0f), Vector3.one * 1.3f, head);

        // Left arm: red with yellow bands. Right arm: yellow with maroon bands.
        // Each arm swings opposite the leg on the same side.
        float shoulderX = bodyWidth / 2f + 0.35f;
        Arm("Left", root, -shoulderX, -1f, strideTime, red, yellow);
        Arm("Right", root, shoulderX, 1f, strideTime, yellow, maroon);

        // Legs are the robot's leg color, with red bands on the left and yellow on the right.
        Leg("Left", root, -0.6f, 1f, strideTime, legs, red);
        Leg("Right", root, 0.6f, -1f, strideTime, legs, yellow);

        // Slightly lowered, because a swinging leg lifts the foot off the ground.
        root.position = position + Vector3.down * 0.2f;
    }

    static void Arm(string side, Transform parent, float x, float direction, float strideTime, Material limb, Material band)
    {
        Transform shoulder = Pivot(side + "ShoulderPivot", parent, new Vector3(x, 9.7f, 0f));
        Oscillate(shoulder, Swing, direction, 0f, strideTime);
        Cube(side + "ShoulderBand", shoulder, new Vector3(0f, -0.2f, 0f), new Vector3(0.72f, 0.4f, 0.72f), band);
        Cube(side + "UpperArm", shoulder, new Vector3(0f, -1.1f, 0f), new Vector3(0.7f, 1.4f, 0.7f), limb);

        // The elbow bends forward, most of all when the arm is at the front of its swing.
        float elbowSwing = 12f;
        Transform elbow = Pivot(side + "ElbowPivot", shoulder, new Vector3(0f, -1.8f, 0f));
        elbow.localRotation = Quaternion.Euler(-elbowSwing, 0f, 0f);
        Oscillate(elbow, elbowSwing, direction, 0f, strideTime);
        Cube(side + "ElbowBand", elbow, new Vector3(0f, -0.15f, 0f), new Vector3(0.72f, 0.3f, 0.72f), band);
        Cube(side + "LowerArm", elbow, new Vector3(0f, -1.05f, 0f), new Vector3(0.7f, 1.5f, 0.7f), limb);

        Hand(side, elbow, new Vector3(0f, -1.8f, 0f), direction, strideTime);
    }

    // A palm with four fingers of three pieces each, and a thumb of two pieces: 14 joints, like the video.
    static void Hand(string side, Transform parent, Vector3 wrist, float direction, float strideTime)
    {
        Transform palm = Pivot(side + "Palm", parent, wrist);
        Cube("PalmBlock", palm, new Vector3(0f, -0.3f, 0f), new Vector3(0.7f, 0.6f, 0.3f), hand);

        float[] fingerX = { -0.26f, -0.09f, 0.09f, 0.26f };
        for (int i = 0; i < fingerX.Length; i++)
            Finger(side + "Finger" + (i + 1), palm, new Vector3(fingerX[i], -0.6f, 0f), 3, direction, strideTime);

        // The thumb sits on the inner edge of the palm, angled outward.
        float inner = side == "Left" ? 1f : -1f;
        Transform thumbBase = Pivot(side + "ThumbBase", palm, new Vector3(0.35f * inner, -0.25f, 0f));
        thumbBase.localRotation = Quaternion.Euler(0f, 0f, 35f * inner);
        Finger(side + "Thumb", thumbBase, Vector3.zero, 2, direction, strideTime);
    }

    // Each finger piece curls toward the palm and back, from straight to twice "curl" degrees.
    static void Finger(string name, Transform parent, Vector3 position, int pieces, float direction, float strideTime)
    {
        float curl = 15f;
        float startAngle = 0.5f * curl * direction;
        Transform current = parent;
        for (int i = 0; i < pieces; i++)
        {
            Transform joint = Pivot(name + "Piece" + (i + 1), current, i == 0 ? position : new Vector3(0f, -0.26f, 0f));
            joint.localRotation = Quaternion.Euler(-curl + startAngle, 0f, 0f);
            Oscillate(joint, curl, direction, startAngle, strideTime);
            Cube("Joint", joint, new Vector3(0f, -0.03f, 0f), new Vector3(0.15f, 0.06f, 0.15f), red);
            Cube("Bone", joint, new Vector3(0f, -0.15f, 0f), new Vector3(0.13f, 0.2f, 0.13f), hand);
            current = joint;
        }
    }

    static void Leg(string side, Transform parent, float x, float direction, float strideTime, Material limb, Material band)
    {
        Transform hip = Pivot(side + "HipPivot", parent, new Vector3(x, LegLength, 0f));
        Oscillate(hip, Swing, direction, 0f, strideTime);
        Cube(side + "HipBand", hip, new Vector3(0f, -0.25f, 0f), new Vector3(0.82f, 0.5f, 0.82f), band);
        Cube(side + "Thigh", hip, new Vector3(0f, -1.5f, 0f), new Vector3(0.8f, 2f, 0.8f), limb);

        // The knee bends backward only, as the foot leaves the ground.
        float kneeSwing = 22f;
        float startAngle = -0.5f * kneeSwing * direction;
        Transform knee = Pivot(side + "KneePivot", hip, new Vector3(0f, -2.5f, 0f));
        knee.localRotation = Quaternion.Euler(kneeSwing + startAngle, 0f, 0f);
        Oscillate(knee, kneeSwing, direction, startAngle, strideTime);
        Cube(side + "KneeBand", knee, new Vector3(0f, -0.22f, 0f), new Vector3(0.82f, 0.45f, 0.82f), band);
        Cube(side + "Shin", knee, new Vector3(0f, -1.4f, 0f), new Vector3(0.8f, 1.9f, 0.8f), limb);
        Part(PrimitiveType.Sphere, side + "Foot", knee, new Vector3(0f, -2.35f, 0.1f), new Vector3(0.9f, 0.7f, 1.1f), black);
    }

    // Adds your OscillationScript. A full swing is 4 * Max degrees, so this Stp
    // makes every joint on a robot take the same time, whatever its Max is.
    static void Oscillate(Transform pivot, float max, float direction, float startAngle, float strideTime)
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
        material.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void MakeGround()
    {
        // A terrain with no layers shows Unity's grey checkerboard, like the video.
        // It's flat where the robots walk, with low sand hills far away.
        TerrainData data = new TerrainData();
        data.heightmapResolution = 257;
        data.size = new Vector3(2000f, 20f, 2000f);
        int n = data.heightmapResolution;
        float[,] heights = new float[n, n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x / (float)(n - 1) - 0.5f) * 2000f;
                float dz = (y / (float)(n - 1) - 0.5f) * 2000f;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                float hills = Mathf.Clamp01((distance - 250f) / 300f);
                heights[y, x] = hills * Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.6f;
            }
        }
        data.SetHeights(0, 0, heights);
        AssetDatabase.CreateAsset(data, "Assets/Material/Part3Terrain.asset");

        GameObject terrain = Terrain.CreateTerrainGameObject(data);
        terrain.name = "Terrain";
        terrain.transform.position = new Vector3(-1000f, 0f, -1000f);
        terrain.GetComponent<Terrain>().materialTemplate = GraphicsSettings.currentRenderPipeline.defaultTerrainMaterial;
    }
}
