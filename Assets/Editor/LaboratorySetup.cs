using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Genera el laboratorio y guarda la escena automaticamente.
/// Menu: Tools > Laboratory > Setup Full Lab
/// </summary>
public class LaboratorySetup
{
    // Superficie del suelo segun la geometria real de la escena
    const float FLOOR   = 0.25f;
    // Altura real de la superficie de la mesa (FLOOR + 0.50 tabletop + 0.025 semiespesor = 0.775)
    // Escala real: mesa a 0.75 m del suelo (~estandar)
    const float TABLE_S = 0.775f;

    // GUIDs del SciFi Office Lite pack
    const string G_TABLE_METAL  = "d50a3e9b50b5f0f4b9c69fe84036a6ed";
    const string G_STOOL        = "30971ea7d19a057458bf89e85d9d1f6c";
    const string G_PC           = "f9a1a5c1a73edba4e8a191866f65123e";
    const string G_TV           = "fe19f183652f0434dba515f0a67f32fe";
    const string G_DRAWER       = "c492efe3e037d4447923edc38633bd5b";
    const string G_SHELF        = "07ed13550a76cd2429958ba8f43d7c0e";
    const string G_SERVER_RACK  = "ea0853a3b2545524481b0c984f27afd1";
    const string G_CHAIR        = "dc35c61ecac9f3e4ea7e96c2ce7f1218";
    const string G_CEIL_LIGHT   = "ddfc1292959bce64f8c5c758157712f1";

    // -----------------------------------------------------------------------
    [MenuItem("Tools/Laboratory/Setup Full Lab")]
    static void SetupLab()
    {
        ClearLab();
        CreateMaterials();
        CreateWorkstations();
        CreateCentralArea();
        CreateStorageWall();
        CreateServerCorner();
        CreateLights();
        CreateDecorations();

        // Guardar escena automaticamente → objetos permanentes
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Lab] Laboratorio creado y guardado. Los objetos son permanentes.");
    }

    [MenuItem("Tools/Laboratory/Clear Lab Objects")]
    static void ClearLab()
    {
        foreach (var n in new[]{ "Lab_Workstations","Lab_Central","Lab_Storage",
                                  "Lab_Servers","Lab_Lights","Lab_Decor",
                                  "Lab_Tables","Lab_Equipment","Lab_Structure","Lab_Lights_Old" })
        {
            var go = GameObject.Find(n);
            if (go) Undo.DestroyObjectImmediate(go);
        }
    }

    // -----------------------------------------------------------------------
    // MATERIALES
    // -----------------------------------------------------------------------
    static Material matTop, matLeg, matFPanel, matFEmit,
                    matMetal, matRubber, matScreen, matRed;

    static void CreateMaterials()
    {
        const string path = "Assets/Materials/Lab";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets/Materials", "Lab");

        matTop    = Mat(path,"Mat_TableTop",   new Color(0.88f,0.90f,0.88f), 0.05f, 0.4f);
        matLeg    = Mat(path,"Mat_TableLeg",   new Color(0.22f,0.22f,0.24f), 0.85f, 0.3f);
        matFPanel = Mat(path,"Mat_FluorPanel", new Color(0.82f,0.82f,0.82f), 0.1f,  0.5f);
        matFEmit  = MatEmit(path,"Mat_FluorEmit",
                       new Color(0.97f,0.99f,1f), new Color(0.97f,0.99f,1f)*4f);
        matMetal  = Mat(path,"Mat_Metal",      new Color(0.55f,0.58f,0.62f), 0.9f,  0.25f);
        matRubber = Mat(path,"Mat_Rubber",     new Color(0.08f,0.08f,0.08f), 0f,    0.1f);
        matScreen = MatEmit(path,"Mat_Screen",
                       new Color(0.1f,0.6f,0.9f), new Color(0.1f,0.6f,0.9f)*1.5f);
        matRed    = Mat(path,"Mat_Red",        Color.red,                    0.1f,  0.2f);

        AssetDatabase.SaveAssets();
    }

    // -----------------------------------------------------------------------
    // ESTACIONES DE TRABAJO — lados izquierdo y derecho
    // -----------------------------------------------------------------------
    static void CreateWorkstations()
    {
        var root = Root("Lab_Workstations");
        float[] zs = { -5f, 0f, 5f };

        foreach (var z in zs)
        {
            // LADO IZQUIERDO
            Prefab(G_TABLE_METAL, root, $"Table_L{z:0}",
                new Vector3(-7.5f, FLOOR, z), new Vector3(0, 90, 0));
            Prefab(G_STOOL, root, $"Stool_L{z:0}",
                new Vector3(-5.8f, FLOOR, z), new Vector3(0, 90, 0));
            Prefab(G_PC, root, $"PC_L{z:0}",
                new Vector3(-7.5f, FLOOR + 0.75f, z + 0.2f), new Vector3(0, 90, 0));
            Prefab(G_DRAWER, root, $"Drawer_L{z:0}",
                new Vector3(-8.3f, FLOOR, z + 0.5f), new Vector3(0, 90, 0));

            // LADO DERECHO
            Prefab(G_TABLE_METAL, root, $"Table_R{z:0}",
                new Vector3(7.5f, FLOOR, z), new Vector3(0, -90, 0));
            Prefab(G_STOOL, root, $"Stool_R{z:0}",
                new Vector3(5.8f, FLOOR, z), new Vector3(0, -90, 0));
            Prefab(G_PC, root, $"PC_R{z:0}",
                new Vector3(7.5f, FLOOR + 0.75f, z - 0.2f), new Vector3(0, -90, 0));
            Prefab(G_DRAWER, root, $"Drawer_R{z:0}",
                new Vector3(8.3f, FLOOR, z - 0.5f), new Vector3(0, -90, 0));
        }

        // Monitores en pared sur — rotY=180 para que la pantalla mire al interior
        Prefab(G_TV, root, "Monitor_A", new Vector3(-5f, 3.5f, -9.4f), new Vector3(0, 180, 0));
        Prefab(G_TV, root, "Monitor_B", new Vector3( 0f, 3.5f, -9.4f), new Vector3(0, 180, 0));
        Prefab(G_TV, root, "Monitor_C", new Vector3( 5f, 3.5f, -9.4f), new Vector3(0, 180, 0));
    }

    // -----------------------------------------------------------------------
    // AREA CENTRAL
    // -----------------------------------------------------------------------
    static void CreateCentralArea()
    {
        var root = Root("Lab_Central");

        Table(root, "MesaCentral", new Vector3(0, FLOOR, 0), 4f, 1.1f);

        for (int i = -1; i <= 1; i++)
        {
            Prefab(G_CHAIR, root, $"Chair_S{i}",
                new Vector3(i * 1.2f, FLOOR, -1.0f), new Vector3(0,   0, 0));
            Prefab(G_CHAIR, root, $"Chair_N{i}",
                new Vector3(i * 1.2f, FLOOR,  1.0f), new Vector3(0, 180, 0));
        }

        Flask(root,"Flask_G",new Vector3(-1.2f,TABLE_S, 0.1f),new Color(0.2f,0.85f,0.3f,0.6f));
        Flask(root,"Flask_R",new Vector3(-0.6f,TABLE_S, 0f),  new Color(0.9f,0.2f,0.2f,0.6f));
        Flask(root,"Flask_B",new Vector3( 0f,  TABLE_S, 0.1f),new Color(0.2f,0.45f,0.95f,0.6f));
        Flask(root,"Flask_Y",new Vector3( 0.6f,TABLE_S, 0f),  new Color(0.95f,0.88f,0.1f,0.6f));
        Flask(root,"Flask_P",new Vector3( 1.2f,TABLE_S, 0.1f),new Color(0.7f,0.2f,0.9f,0.6f));
        TubeRack(root, "Rack_A",  new Vector3(-0.3f, TABLE_S, 0.4f));
        TubeRack(root, "Rack_B",  new Vector3( 0.6f, TABLE_S, 0.4f));
        Microscope(root,"Micro_C",new Vector3( 1.6f, TABLE_S, 0.35f));
        Centrifuge(root,"Centri", new Vector3(-1.6f, TABLE_S, 0.35f));
    }

    // -----------------------------------------------------------------------
    // PARED NORTE — almacenamiento
    // -----------------------------------------------------------------------
    static void CreateStorageWall()
    {
        var root = Root("Lab_Storage");

        foreach (var x in new float[]{ -6f, -2f, 2f, 6f })
            Prefab(G_SHELF, root, $"Shelf_{x:0}",
                new Vector3(x, FLOOR, 8.7f), new Vector3(0, 180, 0));

        Prefab(G_DRAWER, root, "Drawer_NA", new Vector3(-4.5f, FLOOR, 8.3f), new Vector3(0,180,0));
        Prefab(G_DRAWER, root, "Drawer_NB", new Vector3( 4.5f, FLOOR, 8.3f), new Vector3(0,180,0));

        Table(root,"Bench_NA",new Vector3(-5f, FLOOR, 8.0f), 2.5f, 0.65f);
        Table(root,"Bench_NB",new Vector3( 5f, FLOOR, 8.0f), 2.5f, 0.65f);

        Microscope(root,"Micro_NA",new Vector3(-5.5f, TABLE_S, 8.0f));
        Microscope(root,"Micro_NB",new Vector3(-4.5f, TABLE_S, 8.0f));
        Flask(root,"Flask_NA",new Vector3(4.8f,TABLE_S,8.0f),new Color(0.2f,0.85f,0.3f,0.6f));
        Flask(root,"Flask_NB",new Vector3(5.2f,TABLE_S,8.0f),new Color(0.9f,0.5f,0.1f,0.6f));
        TubeRack(root,"Rack_N",new Vector3(5.0f, TABLE_S, 8.25f));
    }

    // -----------------------------------------------------------------------
    // ESQUINA SERVIDORES
    // -----------------------------------------------------------------------
    static void CreateServerCorner()
    {
        var root = Root("Lab_Servers");

        Prefab(G_SERVER_RACK,root,"Rack_A",new Vector3(8.5f,FLOOR,-6.5f),new Vector3(0,-90,0));
        Prefab(G_SERVER_RACK,root,"Rack_B",new Vector3(8.5f,FLOOR,-5.0f),new Vector3(0,-90,0));
        Prefab(G_SERVER_RACK,root,"Rack_C",new Vector3(8.5f,FLOOR,-3.5f),new Vector3(0,-90,0));

        for (int i = 0; i < 3; i++)
            StatusLED(root,$"LED_{i}",new Vector3(7.5f,1.5f,-6.5f + i*1.5f));

        Table(root,"ControlDesk",new Vector3(6.5f,FLOOR,-5.5f),1.5f,0.65f);
        Prefab(G_PC,root,"ControlPC",new Vector3(6.5f,FLOOR+0.85f,-5.5f),new Vector3(0,-90,0));
    }

    // -----------------------------------------------------------------------
    // LUCES DE TECHO
    // -----------------------------------------------------------------------
    static void CreateLights()
    {
        var root = Root("Lab_Lights");
        int idx = 0;
        foreach (var z in new float[]{ -4f, 0f, 4f })
        {
            foreach (var x in new float[]{ -5f, 0f, 5f })
            {
                string ppath = AssetDatabase.GUIDToAssetPath(G_CEIL_LIGHT);
                if (!string.IsNullOrEmpty(ppath))
                    Prefab(G_CEIL_LIGHT, root, $"CeilLight_{idx}",
                        new Vector3(x, 9.7f, z), Vector3.zero);
                else
                    FluorPanel(root, $"Panel_{idx}", new Vector3(x, 9.58f, z));

                PointLight(root,$"PLight_{idx}",new Vector3(x,9.3f,z),
                    new Color(0.97f,0.99f,1f),2.5f,9f);
                idx++;
            }
        }

        EmergencyLight(root,"Emerg_A",new Vector3(-9f,3f,-9f));
        EmergencyLight(root,"Emerg_B",new Vector3( 9f,3f,-9f));
    }

    // -----------------------------------------------------------------------
    // DECORACION
    // -----------------------------------------------------------------------
    static void CreateDecorations()
    {
        var root = Root("Lab_Decor");

        Extinguisher(root,"Ext_A",new Vector3(-9.3f,FLOOR, 9f));
        Extinguisher(root,"Ext_B",new Vector3( 9.3f,FLOOR, 9f));
        Extinguisher(root,"Ext_C",new Vector3(-9.3f,FLOOR,-9f));

        Bin(root,"Bin_A",new Vector3(-3.5f,FLOOR,-8.5f));
        Bin(root,"Bin_B",new Vector3( 3.5f,FLOOR,-8.5f));

        Box(root,"Pipe_Z1",new Vector3( 0f,9.2f, 4f),new Vector3(20f,0.1f,0.1f),matMetal);
        Box(root,"Pipe_Z2",new Vector3( 0f,9.2f,-4f),new Vector3(20f,0.1f,0.1f),matMetal);
        Box(root,"Pipe_X1",new Vector3( 4f,9.2f, 0f),new Vector3(0.1f,0.1f,20f),matMetal);
        Box(root,"Pipe_X2",new Vector3(-4f,9.2f, 0f),new Vector3(0.1f,0.1f,20f),matMetal);

        Sink(root,"Sink_A",new Vector3(-9.3f,FLOOR, 4f));
        Sink(root,"Sink_B",new Vector3(-9.3f,FLOOR,-4f));

        Box(root,"Sign_Exit", new Vector3(-9.6f,4f,0f),new Vector3(0.05f,0.4f,1f),matFEmit);
        Box(root,"Sign_Lab",  new Vector3( 9.6f,4f,0f),new Vector3(0.05f,0.4f,1f),matScreen);
        Box(root,"ElecBox",   new Vector3(9.6f,2.5f,-7.5f),new Vector3(0.05f,0.8f,0.6f),matMetal);
        Box(root,"ElecScreen",new Vector3(9.55f,2.5f,-7.5f),new Vector3(0.05f,0.5f,0.35f),matScreen);
    }

    // -----------------------------------------------------------------------
    // CONSTRUCTORES REUTILIZABLES
    // -----------------------------------------------------------------------
    static void Table(GameObject parent, string name, Vector3 pos, float w, float d)
    {
        var t = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(t, name);
        t.transform.parent   = parent.transform;
        t.transform.position = pos;
        // Altura real de mesa: 0.50 m (local) → 0.75 m desde el suelo (estandar real)
        const float H  = 0.50f;   // altura encimera
        const float LH = 0.48f;   // altura patas
        float hx = w/2f-0.07f, hz = d/2f-0.07f;

        Box(t,"Top",   new Vector3(0, H,        0),  new Vector3(w,       0.05f, d),       matTop);
        Box(t,"Shelf", new Vector3(0, H*0.36f,  0),  new Vector3(w-0.1f,  0.03f, d-0.05f), matTop);
        Box(t,"Leg_FL",new Vector3(-hx, LH/2f,  hz), new Vector3(0.05f,  LH, 0.05f), matLeg);
        Box(t,"Leg_FR",new Vector3( hx, LH/2f,  hz), new Vector3(0.05f,  LH, 0.05f), matLeg);
        Box(t,"Leg_BL",new Vector3(-hx, LH/2f, -hz), new Vector3(0.05f,  LH, 0.05f), matLeg);
        Box(t,"Leg_BR",new Vector3( hx, LH/2f, -hz), new Vector3(0.05f,  LH, 0.05f), matLeg);

        // Collider solido — el jugador no puede atravesar la mesa
        var col    = t.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, H / 2f, 0f);
        col.size   = new Vector3(w, H + 0.05f, d);
    }

    static void Flask(GameObject parent, string name, Vector3 pos, Color liquid)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        var gm = TransparentMat(new Color(0.55f,0.82f,0.92f,0.22f));
        Box(g,"Body",new Vector3(0,0.065f,0),new Vector3(0.11f,0.12f,0.11f),gm);
        Box(g,"Neck",new Vector3(0,0.148f,0),new Vector3(0.04f,0.055f,0.04f),gm);
        var lm = TransparentMat(liquid);
        lm.EnableKeyword("_EMISSION");
        lm.SetColor("_EmissionColor",new Color(liquid.r,liquid.g,liquid.b)*0.5f);
        Box(g,"Liquid",new Vector3(0,0.013f,0),new Vector3(0.09f,0.085f,0.09f),lm);
    }

    static void TubeRack(GameObject parent, string name, Vector3 pos)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        Box(g,"Base",Vector3.zero,new Vector3(0.22f,0.02f,0.08f),matMetal);
        Color[] cols = {
            new Color(0.2f,0.85f,0.3f,0.7f), new Color(0.9f,0.2f,0.2f,0.7f),
            new Color(0.2f,0.45f,0.95f,0.7f),new Color(0.95f,0.88f,0.1f,0.7f),
            new Color(0.7f,0.2f,0.9f,0.7f)
        };
        float[] txs = { -0.08f,-0.04f,0f,0.04f,0.08f };
        for (int i=0;i<5;i++)
            Box(g,$"T{i}",new Vector3(txs[i],0.06f,0),new Vector3(0.018f,0.1f,0.018f),
                TransparentMat(cols[i]));
    }

    static void Microscope(GameObject parent, string name, Vector3 pos)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        Box(g,"Base", new Vector3(0,    0.02f, 0),    new Vector3(0.18f,0.04f,0.14f),matMetal);
        Box(g,"Arm",  new Vector3(0.03f,0.12f, 0.02f),new Vector3(0.03f,0.20f,0.03f),matMetal);
        Box(g,"Head", new Vector3(0.03f,0.23f,-0.02f),new Vector3(0.08f,0.06f,0.06f),matRubber);
        Box(g,"Eye",  new Vector3(0.03f,0.27f,-0.02f),new Vector3(0.02f,0.05f,0.02f),matMetal);
        Box(g,"Obj",  new Vector3(0.03f,0.17f, 0.02f),new Vector3(0.02f,0.06f,0.02f),matMetal);
        Box(g,"Stage",new Vector3(0.03f,0.13f, 0.01f),new Vector3(0.07f,0.01f,0.06f),matRubber);
    }

    static void Centrifuge(GameObject parent, string name, Vector3 pos)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        Box(g,"Body", new Vector3(0,0.07f,0),      new Vector3(0.18f,0.14f,0.18f),matMetal);
        Box(g,"Lid",  new Vector3(0,0.15f,0),      new Vector3(0.16f,0.02f,0.16f),matRubber);
        Box(g,"Panel",new Vector3(0,0.08f,0.092f), new Vector3(0.08f,0.05f,0.01f),matScreen);
    }

    static void Extinguisher(GameObject parent, string name, Vector3 pos)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        Box(g,"Tank",  new Vector3(0,0.35f,0),     new Vector3(0.12f,0.5f,0.12f),   matRed);
        Box(g,"Top",   new Vector3(0,0.62f,0),     new Vector3(0.07f,0.05f,0.07f),  matMetal);
        Box(g,"Handle",new Vector3(0,0.66f,0),     new Vector3(0.1f,0.015f,0.015f), matMetal);
        Box(g,"Hose",  new Vector3(0.07f,0.35f,0), new Vector3(0.015f,0.3f,0.015f), matRubber);
    }

    static void Bin(GameObject parent, string name, Vector3 pos)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        Box(g,"Body",new Vector3(0,0.22f,0),new Vector3(0.28f,0.44f,0.28f),matRubber);
        Box(g,"Rim", new Vector3(0,0.45f,0),new Vector3(0.30f,0.02f,0.30f),matMetal);
    }

    static void Sink(GameObject parent, string name, Vector3 pos)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        Box(g,"Cabinet",new Vector3(0,0.45f,0.1f),  new Vector3(0.8f,0.9f,0.5f),   matTop);
        Box(g,"Basin",  new Vector3(0,0.905f,0.08f), new Vector3(0.55f,0.06f,0.35f),matMetal);
        Box(g,"FaucetV",new Vector3(0,1.04f,0f),     new Vector3(0.03f,0.1f,0.03f), matMetal);
        Box(g,"FaucetH",new Vector3(0,1.1f,0.06f),   new Vector3(0.03f,0.03f,0.12f),matMetal);
    }

    static void FluorPanel(GameObject parent, string name, Vector3 pos)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        Box(g,"Case",Vector3.zero,            new Vector3(1.2f,0.04f,0.3f), matFPanel);
        Box(g,"Tube",new Vector3(0,-0.025f,0), new Vector3(1.1f,0.01f,0.18f),matFEmit);
    }

    static void StatusLED(GameObject parent, string name, Vector3 pos)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        var lt = g.AddComponent<Light>();
        lt.type = LightType.Point; lt.color = new Color(0f,1f,0.2f);
        lt.intensity = 0.4f; lt.range = 1.5f;
    }

    static void EmergencyLight(GameObject parent, string name, Vector3 pos)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        Box(g,"Housing",Vector3.zero,new Vector3(0.15f,0.15f,0.15f),matMetal);
        var lg = new GameObject("Glow");
        lg.transform.parent = g.transform;
        lg.transform.localPosition = Vector3.zero;
        var lt = lg.AddComponent<Light>();
        lt.type = LightType.Point; lt.color = Color.red;
        lt.intensity = 0.6f; lt.range = 3f;
    }

    static void PointLight(GameObject parent, string name, Vector3 pos,
        Color color, float intensity, float range)
    {
        var g = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(g, name);
        g.transform.parent = parent.transform;
        g.transform.position = pos;
        var lt = g.AddComponent<Light>();
        lt.type = LightType.Point; lt.color = color;
        lt.intensity = intensity; lt.range = range;
        lt.shadows = LightShadows.Soft;
    }

    // -----------------------------------------------------------------------
    // PRIMITIVAS Y MATERIALES
    // -----------------------------------------------------------------------
    static GameObject Prefab(string guid, GameObject parent, string objName,
        Vector3 pos, Vector3 euler)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
            { Debug.LogWarning($"[Lab] GUID no encontrado: {guid}"); return null; }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!prefab)
            { Debug.LogWarning($"[Lab] Prefab no cargado: {path}"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(go, objName);
        go.name = objName;
        go.transform.parent        = parent.transform;
        go.transform.position      = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        return go;
    }

    static GameObject Box(GameObject parent, string name,
        Vector3 lpos, Vector3 lscale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.parent        = parent.transform;
        go.transform.localPosition = lpos;
        go.transform.localScale    = lscale;
        if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        return go;
    }

    static GameObject Root(string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        return go;
    }

    static Material Mat(string folder, string name, Color albedo, float met, float sm)
    {
        string p = $"{folder}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = albedo; m.SetFloat("_Metallic",met); m.SetFloat("_Smoothness",sm);
        AssetDatabase.CreateAsset(m, p); return m;
    }

    static Material MatEmit(string folder, string name, Color albedo, Color emit)
    {
        string p = $"{folder}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m) return m;
        m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = albedo;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emit);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        AssetDatabase.CreateAsset(m, p); return m;
    }

    static Material TransparentMat(Color albedo)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = albedo;
        m.SetFloat("_Surface", 1);
        m.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        return m;
    }
}
