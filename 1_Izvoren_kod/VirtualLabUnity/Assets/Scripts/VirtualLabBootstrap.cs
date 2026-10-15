using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VirtualLabBootstrap : MonoBehaviour
{
    private readonly List<string> vessel = new List<string>();
    private readonly Dictionary<string, Substance> substances = new Dictionary<string, Substance>();
    private readonly List<Renderer> liquidRenderers = new List<Renderer>();
    private Material liquidMaterial;
    private Renderer beakerLiquidRenderer;
    private Renderer tubeLiquidRenderer;
    private Renderer flaskLiquidRenderer;
    private Renderer precipitateRenderer;
    private Renderer scaleRenderer;
    private TextMesh selectedText;
    private TextMesh logText;
    private TextMesh temperatureText;
    private TextMesh scaleText;
    private ParticleSystem smoke;      // gas inside the beaker
    private ParticleSystem bubbles;    // bubbles inside the beaker
    private ParticleSystem heatSmoke;  // vapour in the flask neck
    private GameObject flame;
    private AudioSource audioSource;
    private Camera mainCamera;
    private VrGazeController vrController;
    private string selectedKey;
    private string tubeSampleKey;
    private bool tubeHasSample;
    private float temperature = 22f;
    private Color currentLiquidColor = new Color(.25f, .68f, .93f, .72f);
    private bool burnerOn;
    private LlmReactionService llm;
    private bool reacting;

    // Short chemistry hints sent to the LLM (the jar names alone are ambiguous)
    private readonly Dictionary<string, string> chem = new Dictionary<string, string>
    {
        {"acid", "Kiselina (acid, HCl)"},
        {"base", "Baza (base, NaOH)"},
        {"copper", "Bakar sulfat (CuSO4)"},
        {"carbonate", "Karbonat (Na2CO3)"}
    };

    private struct Substance
    {
        public string Name;
        public Color Color;
        public float Grams;

        public Substance(string name, Color color, float grams)
        {
            Name = name;
            Color = color;
            Grams = grams;
        }
    }

    private void Start()
    {
        substances["acid"] = new Substance("Kiselina", new Color(1f, .84f, .25f), 8.5f);
        substances["base"] = new Substance("Baza", new Color(.34f, .78f, .50f), 7.2f);
        substances["copper"] = new Substance("Bakar sulfat", new Color(.12f, .45f, 1f), 5.4f);
        substances["carbonate"] = new Substance("Karbonat", Color.white, 6.1f);

        audioSource = gameObject.AddComponent<AudioSource>();
        llm = gameObject.AddComponent<LlmReactionService>();
        llm.ApiKey = LoadApiKey();
        BuildScene();
        BuildPanel();
        SetupVrMode();
        Log("Laboratorijata e podgotvena. Izberi supstancija za pocetok.");
    }

    private void Update()
    {
        if (burnerOn)
        {
            temperature = Mathf.Min(95f, temperature + Time.deltaTime * 6f);
            UpdateTemperature();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSubstance("acid");
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSubstance("base");
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSubstance("copper");
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSubstance("carbonate");
        if (Input.GetKeyDown(KeyCode.M)) MeasureSelected();
        if (Input.GetKeyDown(KeyCode.P)) PourSelected();
        if (Input.GetKeyDown(KeyCode.X)) Mix();
        if (Input.GetKeyDown(KeyCode.H)) ToggleHeat();
        if (Input.GetKeyDown(KeyCode.U)) DeviceSignal();
        if (Input.GetKeyDown(KeyCode.R)) ResetExperiment();
        if (Input.GetKeyDown(KeyCode.V)) ToggleVrMode();

        // Quick-test keys: fill the beaker in one press, then press X to mix
        if (Input.GetKeyDown(KeyCode.F1)) QuickFill("copper", "carbonate"); // LLM
        if (Input.GetKeyDown(KeyCode.F2)) QuickFill("acid", "copper");      // LLM
        if (Input.GetKeyDown(KeyCode.F3)) QuickFill("base", "carbonate");   // LLM
        if (Input.GetKeyDown(KeyCode.F4)) QuickFill("acid", "base");        // static
    }

    private void BuildScene()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
        }
        mainCamera.transform.position = new Vector3(0f, 4.2f, -7.2f);
        mainCamera.transform.rotation = Quaternion.Euler(28f, 0f, 0f);
        mainCamera.backgroundColor = new Color(.78f, .86f, .92f);
        if (mainCamera.GetComponent<AudioListener>() == null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }

        var light = new GameObject("Key Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.3f;
        light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        CreateCube("Floor", new Vector3(0f, -0.08f, 0f), new Vector3(9f, .12f, 8f), new Color(.78f, .81f, .85f));
        CreateCube("Back Wall", new Vector3(0f, 2.2f, 2.8f), new Vector3(9f, 4.6f, .16f), new Color(.83f, .89f, .94f));
        CreateCube("Lab Bench", new Vector3(0f, .75f, .15f), new Vector3(7f, .38f, 2.15f), new Color(.52f, .31f, .16f));
        CreateCube("Bench Front", new Vector3(0f, .2f, -1f), new Vector3(7.15f, 1f, .26f), new Color(.42f, .24f, .12f));
        CreateCube("Shelf", new Vector3(-1.2f, 2.95f, 2.15f), new Vector3(4.4f, .12f, .42f), new Color(.38f, .43f, .48f));

        liquidMaterial = NewMaterial(new Color(.25f, .68f, .93f, .72f));

        Vector3 beakerPos = new Vector3(-1.65f, 1.25f, -.2f);
        Vector3 flaskPos = new Vector3(1.05f, 1.27f, -.15f);

        CreateBeaker(beakerPos);
        CreateTestTube(new Vector3(-.25f, 1.32f, -.18f));
        CreateFlask(flaskPos);
        CreateBurner(new Vector3(2.35f, 1.02f, -.25f));
        CreateScale(new Vector3(-2.95f, 1.08f, -.28f));

        CreateSubstanceJar("acid", new Vector3(-2.6f, 3.35f, 2.05f));
        CreateSubstanceJar("base", new Vector3(-1.75f, 3.35f, 2.05f));
        CreateSubstanceJar("copper", new Vector3(-.9f, 3.35f, 2.05f));
        CreateSubstanceJar("carbonate", new Vector3(-.05f, 3.35f, 2.05f));

        // Beaker: radius ~0.275, glass spans y 0.53-1.97, liquid spans y 0.85-1.29.
        // Particles are placed in WORLD space (not parented to the scaled glass,
        // because the glass has non-uniform scale which would distort them).

        // Bubbles: start at the bottom of the liquid, rise ~0.4 and die at the surface
        bubbles = CreateParticle("Bubbles", beakerPos + new Vector3(0f, -.35f, 0f),
            new Color(.9f, .97f, 1f, .8f), .035f, .3f, .17f, 1.3f, 25f);

        // Gas/smoke: starts at the liquid surface, stays below the rim
        smoke = CreateParticle("Smoke", beakerPos + new Vector3(0f, .05f, 0f),
            new Color(.82f, .86f, .88f, .45f), .12f, .12f, .15f, 1.5f, 8f);

        // Flask vapour: inside the flask neck (neck radius ~0.09)
        heatSmoke = CreateParticle("HeatSmoke", flaskPos + new Vector3(0f, .6f, 0f),
            new Color(.82f, .86f, .88f, .45f), .06f, .15f, .06f, 1.2f, 8f);

        smoke.Stop();
        bubbles.Stop();
        heatSmoke.Stop();
    }

    private void BuildPanel()
    {
        CreateCube("Info Panel", new Vector3(2.35f, 2.75f, 2.68f), new Vector3(2.25f, 2.30f, .08f), new Color(.94f, .97f, 1f));
        AddPanelText("Virtualna laboratorija", new Vector3(1.35f, 3.75f, 2.60f), .052f, FontStyle.Bold);
        selectedText = AddPanelText("Nema izbrana supstancija.", new Vector3(1.35f, 3.47f, 2.60f), .034f, FontStyle.Normal);
        temperatureText = AddPanelText("Temperatura: 22 C", new Vector3(1.35f, 3.23f, 2.60f), .036f, FontStyle.Bold);
        scaleText = AddPanelText("Vaga: 0.0 g", new Vector3(1.35f, 3.04f, 2.60f), .036f, FontStyle.Bold);

        CreateActionButton("Meri", "measure", new Vector3(1.58f, 2.73f, 2.54f));
        CreateActionButton("Dodaj", "pour", new Vector3(2.45f, 2.73f, 2.54f));
        CreateActionButton("Mesaj", "mix", new Vector3(1.58f, 2.46f, 2.54f));
        CreateActionButton("Zagrej", "heat", new Vector3(2.45f, 2.46f, 2.54f));
        CreateActionButton("Ured", "device", new Vector3(1.58f, 2.19f, 2.54f));
        CreateActionButton("Reset", "reset", new Vector3(2.45f, 2.19f, 2.54f));

        AddPanelText("Supstancii: 1 Kiselina, 2 Baza, 3 CuSO4, 4 Karbonat", new Vector3(1.35f, 1.88f, 2.60f), .024f, FontStyle.Normal);
        AddPanelText("Kratenci: M/P/X/H/U/R", new Vector3(1.35f, 1.74f, 2.60f), .024f, FontStyle.Normal);
        logText = AddPanelText("", new Vector3(1.35f, 1.52f, 2.60f), .030f, FontStyle.Bold);
    }

    private void SetupVrMode()
    {
        vrController = gameObject.AddComponent<VrGazeController>();
        vrController.Configure(mainCamera);
        bool startInVr = Application.isMobilePlatform;
        vrController.SetVrEnabled(startInVr);
        if (startInVr)
        {
            Log("VR 3D Box rezim e vklucen. Gledaj vo objekt za izbor.");
        }
    }

    private void ToggleVrMode()
    {
        if (vrController == null) return;
        vrController.SetVrEnabled(!vrController.IsVrEnabled);
        Log(vrController.IsVrEnabled ? "VR 3D Box rezim e vklucen. Gledaj vo objekt za izbor." : "VR rezim e isklucen. Koristi gluvce i tastatura.");
    }

    private GameObject CreateCube(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = NewMaterial(color);
        return obj;
    }

    private void CreateBeaker(Vector3 pos)
    {
        var glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glass.name = "Beaker";
        glass.transform.position = pos;
        glass.transform.localScale = new Vector3(.55f, .72f, .55f);
        glass.GetComponent<Renderer>().material = NewGlassMaterial(new Color(.8f, .95f, 1f, .24f));
        beakerLiquidRenderer = CreateLiquid("Beaker Liquid", pos + new Vector3(0f, -.18f, -.03f), new Vector3(.47f, .22f, .47f));
        // Thin solid layer at the bottom of the liquid (blue precipitate)
        precipitateRenderer = CreateLiquid("Beaker Precipitate", pos + new Vector3(0f, -.37f, -.03f), new Vector3(.46f, .035f, .46f));
        AddLabel("casa", pos + Vector3.up * .95f);
    }

    private void CreateTestTube(Vector3 pos)
    {
        var tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tube.name = "Test Tube";
        tube.transform.position = pos;
        tube.transform.localScale = new Vector3(.2f, .92f, .2f);
        tube.GetComponent<Renderer>().material = NewGlassMaterial(new Color(.8f, .95f, 1f, .22f));
        tubeLiquidRenderer = CreateLiquid("Tube Liquid", pos + new Vector3(0f, -.34f, -.025f), new Vector3(.17f, .24f, .17f));
        AddLabel("epruveta", pos + Vector3.up * 1.1f);
    }

    private void CreateFlask(Vector3 pos)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Flask";
        body.transform.position = pos;
        body.transform.localScale = new Vector3(.72f, .55f, .72f);
        body.GetComponent<Renderer>().material = NewGlassMaterial(new Color(.8f, .95f, 1f, .24f));
        var neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        neck.name = "Flask Neck";
        neck.transform.position = pos + new Vector3(0f, .52f, 0f);
        neck.transform.localScale = new Vector3(.18f, .38f, .18f);
        neck.GetComponent<Renderer>().material = body.GetComponent<Renderer>().material;
        flaskLiquidRenderer = CreateLiquid("Flask Liquid", pos + new Vector3(0f, -.18f, -.03f), new Vector3(.48f, .16f, .48f));
        AddLabel("kolba", pos + Vector3.up * .95f);
    }

    private Renderer CreateLiquid(string name, Vector3 pos, Vector3 scale)
    {
        var liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        liquid.name = name;
        liquid.transform.position = pos;
        liquid.transform.localScale = scale;
        var renderer = liquid.GetComponent<Renderer>();
        renderer.material = new Material(liquidMaterial);
        renderer.enabled = false;
        liquidRenderers.Add(renderer);
        return renderer;
    }

    private void CreateBurner(Vector3 pos)
    {
        CreateCube("Burner Base", pos, new Vector3(.75f, .18f, .5f), new Color(.18f, .22f, .27f));
        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Burner Body";
        body.transform.position = pos + new Vector3(0f, .32f, 0f);
        body.transform.localScale = new Vector3(.22f, .34f, .22f);
        body.GetComponent<Renderer>().material = NewMaterial(new Color(.36f, .41f, .48f));
        flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = "Flame";
        flame.transform.position = pos + new Vector3(0f, .82f, 0f);
        flame.transform.localScale = new Vector3(.22f, .48f, .22f);
        flame.GetComponent<Renderer>().material = NewMaterial(new Color(1f, .47f, .08f, .85f));
        flame.SetActive(false);
        AddLabel("plamenik", pos + Vector3.up * 1.05f);
    }

    private void CreateScale(Vector3 pos)
    {
        var scale = CreateCube("Scale", pos, new Vector3(.92f, .28f, .6f), new Color(.84f, .88f, .92f));
        scaleRenderer = scale.GetComponent<Renderer>();
        AddLabel("vaga", pos + Vector3.up * .45f);
    }

    private void CreateSubstanceJar(string key, Vector3 pos)
    {
        var jar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        jar.name = substances[key].Name;
        jar.transform.position = pos;
        jar.transform.localScale = new Vector3(.25f, .42f, .25f);
        jar.GetComponent<Renderer>().material = NewMaterial(substances[key].Color);
        var clickable = jar.AddComponent<ClickableSubstance>();
        clickable.Key = key;
        clickable.Lab = this;
        AddLabel(substances[key].Name, pos + Vector3.up * .65f);
    }

    private void CreateActionButton(string label, string action, Vector3 pos)
    {
        var button = CreateCube("Button " + label, pos, new Vector3(.68f, .18f, .08f), label == "Mesaj" ? new Color(.08f, .45f, .42f) : new Color(.88f, .93f, .96f));
        var actionButton = button.AddComponent<LabActionButton>();
        actionButton.Action = action;
        actionButton.Lab = this;
        AddPanelText(label, pos + new Vector3(-.24f, .065f, -.07f), .030f, FontStyle.Bold);
    }

    private void AddLabel(string text, Vector3 pos)
    {
        var label = new GameObject("Label " + text);
        label.transform.position = pos;
        var mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 40;
        mesh.characterSize = .045f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.color = Color.black;
        label.transform.rotation = Quaternion.Euler(28f, 0f, 0f);
    }

    private TextMesh AddPanelText(string text, Vector3 pos, float size, FontStyle style)
    {
        var label = new GameObject("Panel Text");
        label.transform.position = pos;
        label.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        var mesh = label.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.fontSize = 48;
        mesh.characterSize = size;
        mesh.anchor = TextAnchor.UpperLeft;
        mesh.alignment = TextAlignment.Left;
        mesh.fontStyle = style;
        mesh.color = new Color(.06f, .09f, .12f);
        return mesh;
    }

    // Emits upward from a world position, sized to fit inside the glass.
    private ParticleSystem CreateParticle(string name, Vector3 worldPos, Color color,
        float size, float speed, float radius, float lifetime, float rate)
    {
        var obj = new GameObject(name);
        obj.transform.position = worldPos;
        obj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // shape cone points up

        var ps = obj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = true;
        main.startColor = color;
        main.startSize = size;
        main.startSpeed = speed;
        main.startLifetime = lifetime;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 3f;
        shape.radius = radius;

        // Fixes magenta: Sprites/Default works in both Built-in and URP
        var r = obj.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        r.material = new Material(shader);

        return ps;
    }

    private Material NewMaterial(Color color)
    {
        var shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private Material NewGlassMaterial(Color color)
    {
        var mat = NewMaterial(color);
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        return mat;
    }

    public void SelectSubstance(string key)
    {
        selectedKey = key;
        selectedText.text = substances[key].Name + " e izbrana za merenje ili dodavanje.";
        Beep(620f, .08f);
    }

    public void RunAction(string action)
    {
        if (action == "measure") MeasureSelected();
        if (action == "pour") PourSelected();
        if (action == "mix") Mix();
        if (action == "heat") ToggleHeat();
        if (action == "device") DeviceSignal();
        if (action == "reset") ResetExperiment();
    }

    private void MeasureSelected()
    {
        if (string.IsNullOrEmpty(selectedKey))
        {
            Log("Prvo izberi supstancija za merenje.");
            return;
        }
        float grams = substances[selectedKey].Grams + Random.Range(-.3f, .3f);
        scaleText.text = "Vaga: " + grams.ToString("0.0") + " g";
        if (scaleRenderer != null) scaleRenderer.material.color = new Color(1f, .88f, .35f);
        tubeSampleKey = selectedKey;
        tubeHasSample = true;
        ShowTubeSample(substances[selectedKey].Color);
        Log("Vagata meri " + grams.ToString("0.0") + " g, a mal primerok e staven vo epruveta.");
        Beep(780f, .08f);
    }

    private void PourSelected()
    {
        if (!tubeHasSample || string.IsNullOrEmpty(tubeSampleKey))
        {
            Log("Prvo izmeri supstancija na vaga; primerokot ke se pojavi vo epruveta.");
            return;
        }

        vessel.Add(tubeSampleKey);
        UpdateBeakerLiquid(MixColors());
        string addedName = substances[tubeSampleKey].Name;
        ClearTubeSample();
        Log(addedName + " od epruveta e dodadena vo casa za mesanje.");
        Beep(420f, .12f);
    }

    // Reads the key from gemini_key.txt in the project root (add it to .gitignore!)
    private string LoadApiKey()
    {
        try
        {
            string path = Path.Combine(Application.dataPath, "..", "gemini_key.txt");
            if (File.Exists(path)) return File.ReadAllText(path).Trim();
        }
        catch (System.Exception) { }
        return "";
    }

    private void Mix()
    {
        if (vessel.Count < 2)
        {
            Log("Za reakcija se potrebni najmalku dve supstancii vo sadot.");
            return;
        }
        if (reacting) return;

        // 1) the three known reactions stay static (instant, always correct)
        if (TryStaticReaction()) return;

        // 2) anything else goes to the LLM
        StartCoroutine(MixWithLlm());
    }

    private void ClearReactionEffects()
    {
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        bubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (precipitateRenderer != null) precipitateRenderer.enabled = false;
    }

    private bool TryStaticReaction()
    {
        bool hasAcid = vessel.Contains("acid");
        bool hasBase = vessel.Contains("base");
        bool hasCopper = vessel.Contains("copper");
        bool hasCarbonate = vessel.Contains("carbonate");

        if (hasAcid && hasBase)
        {
            ClearReactionEffects();
            UpdateBeakerLiquid(new Color(.60f, .90f, .70f, .8f));
            Log("neutralizacija. Rastvorot menuva boja vo svetlo zelena i se slusa zvucen efekt.");
            Beep(660f, .25f);
            Beep(880f, .25f);
            return true;
        }
        if (hasAcid && hasCarbonate)
        {
            ClearReactionEffects();
            bubbles.Play();
            smoke.Play();
            Log("osloboduvanje gas. Se pojavuvaat meurcinja/cad i se slusa reakcija.");
            PlayFizz(1.4f);
            return true;
        }
        if (hasCopper && hasBase)
        {
            ClearReactionEffects();
            UpdateBeakerLiquid(new Color(.03f, .28f, .95f, .88f));
            if (precipitateRenderer != null)
            {
                precipitateRenderer.enabled = true;
                precipitateRenderer.material.color = new Color(.02f, .12f, .65f, 1f);
            }
            Log("formiranje sin talog. Rastvorot dobiva posilna sina boja i se slusa zvucen efekt.");
            Beep(330f, .25f);
            Beep(495f, .25f);
            return true;
        }
        return false;
    }

    private IEnumerator MixWithLlm()
    {
        reacting = true;

        if (string.IsNullOrEmpty(llm.ApiKey))
        {
            Log("Nepoznata reakcija, a nema API kluc (gemini_key.txt). Mala promena na boja.");
            Beep(440f, .1f);
            reacting = false;
            yield break;
        }

        Log("Se presmetuva reakcijata...");
        var names = new List<string>();
        foreach (var k in vessel) names.Add(chem[k]);

        ReactionResult res = null;
        yield return llm.GetReaction(names, r => res = r);

        if (res == null)
        {
            Log("Greska od LLM: " + llm.LastError);
            Beep(440f, .1f);
        }
        else
        {
            ApplyReaction(res);
        }
        reacting = false;
    }

    private void ApplyReaction(ReactionResult r)
    {
        ClearReactionEffects();
        UpdateBeakerLiquid(new Color(r.liquid_color[0], r.liquid_color[1], r.liquid_color[2], r.liquid_color[3]));
        if (r.gas) { bubbles.Play(); smoke.Play(); }
        if (r.precipitate)
        {
            precipitateRenderer.enabled = true;
            precipitateRenderer.material.color = new Color(r.precipitate_color[0], r.precipitate_color[1], r.precipitate_color[2], 1f);
        }
        temperature = Mathf.Clamp(temperature + r.temperature_change, 0f, 95f);
        UpdateTemperature();

        if (r.sound == "chime") { Beep(660f, .25f); Beep(880f, .25f); }
        else if (r.sound == "fizz") PlayFizz(1.4f);
        else if (r.sound == "low") { Beep(330f, .25f); Beep(495f, .25f); }

        Log(r.description);
    }

    private void ToggleHeat()
    {
        if (vessel.Count == 0)
        {
            Log("Prvo dodaj supstancija vo casa, potoa zagrevaj vo kolba.");
            return;
        }

        burnerOn = !burnerOn;
        flame.SetActive(burnerOn);
        if (burnerOn)
        {
            UpdateFlaskLiquid(Color.Lerp(currentLiquidColor, new Color(1f, .48f, .12f, .82f), .25f));
            temperature = Mathf.Max(temperature, 55f);
            UpdateTemperature();
            Log("Rastvorot od casa e prenesen vo kolba. Plamenikot ja zagreva kolbata.");
            heatSmoke.Play();
            Beep(240f, .16f);
        }
        else
        {
            heatSmoke.Stop();
            Log("Plamenikot e isklucen. Kolbata prestanuva da se zagreva.");
            Beep(180f, .08f);
        }
    }

    private void QuickFill(params string[] keys)
    {
        ResetExperiment();
        foreach (var k in keys) vessel.Add(k);
        UpdateBeakerLiquid(MixColors());
        Log("Test: dodadeni se " + string.Join(" + ", keys) + ". Pritisni X za mesanje.");
    }

    private void DeviceSignal()
    {
        Log("Laboratoriskiot ured e aktiviran i dava zvucen signal.");
        Beep(880f, .09f);
    }

    private void ResetExperiment()
    {
        vessel.Clear();
        selectedKey = null;
        tubeSampleKey = null;
        tubeHasSample = false;
        temperature = 22f;
        burnerOn = false;
        flame.SetActive(false);
        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        bubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        heatSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        UpdateTemperature();
        ClearLiquids();
        selectedText.text = "Nema izbrana supstancija.";
        scaleText.text = "Vaga: 0.0 g";
        if (scaleRenderer != null) scaleRenderer.material.color = new Color(.84f, .88f, .92f);
        Log("Eksperimentot e resetiran.");
    }

    private Color MixColors()
    {
        if (vessel.Count == 0) return new Color(.25f, .68f, .93f, .72f);
        Color color = Color.clear;
        foreach (var key in vessel) color += substances[key].Color;
        color /= vessel.Count;
        color.a = .78f;
        return color;
    }

    private void UpdateBeakerLiquid(Color color)
    {
        currentLiquidColor = color;
        liquidMaterial.color = color;
        if (beakerLiquidRenderer != null)
        {
            beakerLiquidRenderer.enabled = true;
            beakerLiquidRenderer.material.color = color;
        }
    }

    private void ShowTubeSample(Color color)
    {
        Color sampleColor = color;
        sampleColor.a = .78f;
        if (tubeLiquidRenderer != null)
        {
            tubeLiquidRenderer.enabled = true;
            tubeLiquidRenderer.material.color = sampleColor;
        }
    }

    private void ClearTubeSample()
    {
        tubeSampleKey = null;
        tubeHasSample = false;
        if (tubeLiquidRenderer != null) tubeLiquidRenderer.enabled = false;
    }

    private void UpdateFlaskLiquid(Color color)
    {
        if (flaskLiquidRenderer != null)
        {
            flaskLiquidRenderer.enabled = true;
            flaskLiquidRenderer.material.color = color;
        }
    }

    private void ClearLiquids()
    {
        currentLiquidColor = new Color(.25f, .68f, .93f, .72f);
        liquidMaterial.color = currentLiquidColor;
        foreach (var renderer in liquidRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = false;
                renderer.material.color = liquidMaterial.color;
            }
        }
    }

    private void UpdateTemperature()
    {
        temperatureText.text = "Temperatura: " + Mathf.RoundToInt(temperature) + " C";
    }

    private void Log(string message)
    {
        logText.text = "Rezultat: " + message;
    }

    private void Beep(float frequency, float duration)
    {
        audioSource.PlayOneShot(CreateTone(frequency, Mathf.Max(duration, .22f)), .75f);
    }

    private void PlayFizz(float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float envelope = Mathf.Clamp01(t * 12f) * (1f - t);
            data[i] = (Random.value * 2f - 1f) * .45f * envelope;
        }
        var clip = AudioClip.Create("fizz", sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        audioSource.PlayOneShot(clip, .8f);
    }

    private AudioClip CreateTone(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var data = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float envelope = 1f - ((float)i / sampleCount);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * .55f * envelope;
        }
        var clip = AudioClip.Create("tone", sampleCount, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

public class ClickableSubstance : MonoBehaviour
{
    public string Key;
    public VirtualLabBootstrap Lab;

    public void Activate()
    {
        Lab.SelectSubstance(Key);
    }

    private void OnMouseDown()
    {
        Activate();
    }
}

public class LabActionButton : MonoBehaviour
{
    public string Action;
    public VirtualLabBootstrap Lab;

    public void Activate()
    {
        Lab.RunAction(Action);
    }

    private void OnMouseDown()
    {
        Activate();
    }
}

public class VrGazeController : MonoBehaviour
{
    public bool IsVrEnabled { get; private set; }

    private const float EyeDistance = 0.064f;
    private const float GazeSeconds = 1.15f;
    private Camera leftCamera;
    private Camera rightCamera;
    private GameObject rig;
    private GameObject reticle;
    private Transform currentTarget;
    private float gazeTimer;
    private Vector3 normalCameraPosition;
    private Quaternion normalCameraRotation;
    private Rect normalCameraRect;
    private bool gyroAvailable;
    private float mouseYaw;
    private float mousePitch;

    public void Configure(Camera sourceCamera)
    {
        leftCamera = sourceCamera;
        normalCameraPosition = sourceCamera.transform.position;
        normalCameraRotation = sourceCamera.transform.rotation;
        normalCameraRect = sourceCamera.rect;
        mouseYaw = normalCameraRotation.eulerAngles.y;
        mousePitch = normalCameraRotation.eulerAngles.x;
        gyroAvailable = SystemInfo.supportsGyroscope;
        if (gyroAvailable) Input.gyro.enabled = true;
        BuildRig();
        BuildReticle();
    }

    public void SetVrEnabled(bool enabled)
    {
        IsVrEnabled = enabled;
        if (rig == null || rightCamera == null) return;

        rig.SetActive(enabled);
        rightCamera.gameObject.SetActive(enabled);
        reticle.SetActive(enabled);

        if (enabled)
        {
            leftCamera.transform.SetParent(rig.transform, false);
            leftCamera.transform.localPosition = new Vector3(-EyeDistance * .5f, 0f, 0f);
            leftCamera.transform.localRotation = Quaternion.identity;
            leftCamera.rect = new Rect(0f, 0f, .5f, 1f);
            leftCamera.fieldOfView = 72f;
        }
        else
        {
            leftCamera.transform.SetParent(null);
            leftCamera.transform.position = normalCameraPosition;
            leftCamera.transform.rotation = normalCameraRotation;
            leftCamera.rect = normalCameraRect;
            leftCamera.fieldOfView = 60f;
            gazeTimer = 0f;
            currentTarget = null;
        }
    }

    private void Update()
    {
        if (!IsVrEnabled) return;
        UpdateHeadRotation();
        UpdateGazeSelection();
    }

    private void BuildRig()
    {
        rig = new GameObject("Mobile 3D Box VR Rig");
        rig.transform.position = normalCameraPosition;
        rig.transform.rotation = normalCameraRotation;
        rig.SetActive(false);

        var rightObject = new GameObject("Right Eye Camera");
        rightObject.transform.SetParent(rig.transform, false);
        rightObject.transform.localPosition = new Vector3(EyeDistance * .5f, 0f, 0f);
        rightObject.transform.localRotation = Quaternion.identity;
        rightCamera = rightObject.AddComponent<Camera>();
        rightCamera.CopyFrom(leftCamera);
        rightCamera.rect = new Rect(.5f, 0f, .5f, 1f);
        rightCamera.tag = "Untagged";
        rightObject.SetActive(false);
    }

    private void BuildReticle()
    {
        reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        reticle.name = "VR Gaze Reticle";
        reticle.transform.localScale = Vector3.one * .035f;
        reticle.GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));
        reticle.GetComponent<Renderer>().material.color = new Color(.04f, .85f, .78f, .85f);
        Destroy(reticle.GetComponent<Collider>());
        reticle.SetActive(false);
    }

    private void UpdateHeadRotation()
    {
        if (gyroAvailable)
        {
            Quaternion attitude = Input.gyro.attitude;
            rig.transform.rotation = Quaternion.Euler(90f, 0f, 0f) * new Quaternion(-attitude.x, -attitude.y, attitude.z, attitude.w);
        }
        else
        {
            mouseYaw += Input.GetAxis("Mouse X") * 2.2f;
            mousePitch -= Input.GetAxis("Mouse Y") * 2.2f;
            mousePitch = Mathf.Clamp(mousePitch, -55f, 55f);
            rig.transform.rotation = Quaternion.Euler(mousePitch, mouseYaw, 0f);
        }
    }

    private void UpdateGazeSelection()
    {
        Ray ray = new Ray(leftCamera.transform.position, leftCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 8f))
        {
            reticle.transform.position = hit.point - ray.direction * .035f;
            reticle.transform.rotation = Quaternion.LookRotation(ray.direction);
            Transform target = hit.collider.transform;
            bool selectable = target.GetComponent<ClickableSubstance>() != null || target.GetComponent<LabActionButton>() != null;

            if (selectable)
            {
                if (currentTarget == target) gazeTimer += Time.deltaTime;
                else
                {
                    currentTarget = target;
                    gazeTimer = 0f;
                }

                float scale = Mathf.Lerp(.035f, .07f, gazeTimer / GazeSeconds);
                reticle.transform.localScale = Vector3.one * scale;

                if (gazeTimer >= GazeSeconds)
                {
                    ActivateTarget(target);
                    gazeTimer = 0f;
                }
                return;
            }
        }

        reticle.transform.position = leftCamera.transform.position + leftCamera.transform.forward * 2f;
        reticle.transform.rotation = Quaternion.LookRotation(leftCamera.transform.forward);
        reticle.transform.localScale = Vector3.one * .035f;
        currentTarget = null;
        gazeTimer = 0f;
    }

    private void ActivateTarget(Transform target)
    {
        var substance = target.GetComponent<ClickableSubstance>();
        if (substance != null) substance.Activate();

        var button = target.GetComponent<LabActionButton>();
        if (button != null) button.Activate();
    }
}