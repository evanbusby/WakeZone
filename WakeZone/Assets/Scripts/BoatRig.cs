using UnityEngine;

// Loads the 2008 Yamaha 212X wakeboat model (Resources/YamahaBoat, by BoatUS
// Foundation, CC BY - see README) and fits it to the boat object. Uses the
// project's Custom/SimpleLit shader (a Standard-shader stand-in that survives
// WebGL build stripping, see SimpleLit.shader) and keeps everything opaque
// (even the "glass" windshield, which just fakes the look with color and
// shininess) since transparency is expensive to render on low-end mobile GPUs.
public class BoatRig : MonoBehaviour
{
    // The model is ~7 units long and ~3 wide; this shrinks it to roughly the
    // size of the old hull cube plus a bit, so it reads correctly next to
    // the rider.
    const float ModelScale = 0.55f;

    // Lowers the model so its waterline (y = 0 in the model) sits at the
    // water surface, given the boat object floats 0.5 above it.
    const float ModelYOffset = -0.45f;

    // Top-rear corner of the wake tower canopy, in the model's own units
    // (centered, 2.4 up, 1.2 behind the middle).
    static readonly Vector3 CanopyRearTop = new Vector3(0f, 2.4f, -1.2f);

    static readonly Color GlassColor = new Color(0.55f, 0.68f, 0.72f);
    static readonly Color MirrorColor = new Color(0.8f, 0.82f, 0.85f);

    // Where the tow rope attaches, in the boat object's local space.
    public Vector3 RopeAnchor { get; private set; }

    void Awake()
    {
        Build();
    }

    void Build()
    {
        RopeAnchor = CanopyRearTop * ModelScale + new Vector3(0f, ModelYOffset, 0f);

        GameObject prefab = Resources.Load<GameObject>("YamahaBoat/YamahaBoat");
        if (prefab == null)
        {
            Debug.LogError("BoatRig: Resources/YamahaBoat/YamahaBoat model not found");
            return;
        }

        // Boat forward (+Z) is the bow, which is how the model is authored.
        GameObject model = Instantiate(prefab, transform);
        model.name = "BoatModel";
        model.transform.localPosition = new Vector3(0f, ModelYOffset, 0f);
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * ModelScale;

        Texture2D hullTexture = Resources.Load<Texture2D>("YamahaBoat/Yamaha_Base_D");

        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            Material[] source = r.sharedMaterials;
            Material[] result = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = MaterialFor(source[i] != null ? source[i].name : "", hullTexture);
            }
            r.materials = result;
        }
    }

    Material MaterialFor(string sourceName, Texture2D hullTexture)
    {
        // The "Alphas" layer is gauge and decal geometry that needs alpha
        // cutout to look right. It's tiny at game distance, so it's drawn
        // fully transparent rather than as opaque gray panels.
        if (sourceName.StartsWith("Yamaha_Alphas"))
        {
            Material hidden = new Material(Shader.Find("Sprites/Default"));
            hidden.color = new Color(0f, 0f, 0f, 0f);
            return hidden;
        }

        if (sourceName.StartsWith("Glass"))
            return LitMaterial(GlassColor, null, metallic: 0.1f, smoothness: 0.85f);

        if (sourceName.StartsWith("Mirror"))
            return LitMaterial(MirrorColor, null, metallic: 0.9f, smoothness: 0.8f);

        return LitMaterial(Color.white, hullTexture, metallic: 0.2f, smoothness: 0.55f);
    }

    Material LitMaterial(Color color, Texture2D texture, float metallic, float smoothness)
    {
        Material mat = new Material(Shader.Find("Custom/SimpleLit"));
        mat.color = color;
        if (texture != null) mat.mainTexture = texture;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        return mat;
    }
}
