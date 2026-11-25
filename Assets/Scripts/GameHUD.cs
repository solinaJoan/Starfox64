using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Interfície del joc (HUD) estil "rail shooter" de la N64.
// Es construeix tota per codi, així no cal muntar res a l'escena:
//  - A dalt a l'esquerra: barra d'escut (0..150) amb estela de dany.
//  - A dalt a la dreta: vides i bombes restants.
//  - Flaix vermell i tremolor quan la nau rep mal.
public class GameHUD : MonoBehaviour
{
    [Header("Colors de l'escut")]
    public Color shieldHigh = new Color(0.25f, 0.65f, 1f);
    public Color shieldMid = new Color(1f, 0.85f, 0.2f);
    public Color shieldLow = new Color(1f, 0.25f, 0.2f);
    public Color frameColor = new Color(1f, 0.82f, 0.3f);

    [Header("Animació")]
    public float fillSpeed = 2.5f;        // velocitat de la barra principal (fracció/s)
    public float trailDelay = 0.4f;       // temps que l'estela es queda quieta
    public float trailSpeed = 0.6f;       // velocitat de l'estela
    public float shakeDuration = 0.3f;
    public float shakeStrength = 12f;

    private PlayerStats stats;

    // Elements de la UI
    private RectTransform shieldGroup;
    private Image shieldFill, shieldTrail, shieldFrame;
    private TextMeshProUGUI shieldText, livesText, bombsText;
    private Image damageFlash;
    private RectTransform livesGroup, bombsGroup;

    // Estat de l'animació
    private float shownFill = 1f, trailFill = 1f, targetFill = 1f;
    private float trailWait, shakeTime, flashAlpha;
    private Vector2 shieldGroupPos;
    private float livesPop, bombsPop;

    private static Sprite whiteSprite, circleSprite, shipSprite;

    public void Bind(PlayerStats s)
    {
        if (stats != null)
        {
            stats.ShieldChanged -= OnShieldChanged;
            stats.LivesChanged -= OnLivesChanged;
            stats.BombsChanged -= OnBombsChanged;
            stats.Damaged -= OnDamaged;
        }

        stats = s;
        if (shieldFill == null)
            Build();

        stats.ShieldChanged += OnShieldChanged;
        stats.LivesChanged += OnLivesChanged;
        stats.BombsChanged += OnBombsChanged;
        stats.Damaged += OnDamaged;

        targetFill = shownFill = trailFill = (float)stats.Shield / PlayerStats.MaxShield;
        UpdateShieldText(stats.Shield);
        livesText.text = "× " + Mathf.Max(0, PlayerStats.Lives);
        bombsText.text = "× " + PlayerStats.Bombs;
    }

    void OnDestroy()
    {
        if (stats == null) return;
        stats.ShieldChanged -= OnShieldChanged;
        stats.LivesChanged -= OnLivesChanged;
        stats.BombsChanged -= OnBombsChanged;
        stats.Damaged -= OnDamaged;
    }

    // ───────────────────────── Esdeveniments ─────────────────────────

    private void OnShieldChanged(int value, int max)
    {
        float f = (float)value / max;
        if (f < targetFill)
            trailWait = trailDelay;   // l'estela es queda enrere un moment
        targetFill = f;
        UpdateShieldText(value);
    }

    private void OnLivesChanged(int lives)
    {
        livesText.text = "× " + Mathf.Max(0, lives);
        livesPop = 1f;
    }

    private void OnBombsChanged(int bombs)
    {
        bombsText.text = "× " + bombs;
        bombsPop = 1f;
    }

    private void OnDamaged(int amount)
    {
        shakeTime = shakeDuration;
        flashAlpha = Mathf.Clamp01(0.25f + amount / 150f);
    }

    private void UpdateShieldText(int value)
    {
        shieldText.text = value.ToString("000") + "<size=70%><color=#FFFFFF99> / " + PlayerStats.MaxShield + "</color></size>";
    }

    // ───────────────────────── Animació ─────────────────────────

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Barra principal: baixa/puja ràpid
        shownFill = Mathf.MoveTowards(shownFill, targetFill, fillSpeed * dt);
        shieldFill.fillAmount = shownFill;

        // Estela de dany: espera i després baixa lentament
        if (trailFill < shownFill)
            trailFill = shownFill;
        else if (trailWait > 0f)
            trailWait -= dt;
        else
            trailFill = Mathf.MoveTowards(trailFill, shownFill, trailSpeed * dt);
        shieldTrail.fillAmount = trailFill;

        // Color segons el nivell d'escut; parpelleja quan està baix
        Color c = targetFill > 0.5f ? Color.Lerp(shieldMid, shieldHigh, (targetFill - 0.5f) * 2f)
                                    : Color.Lerp(shieldLow, shieldMid, Mathf.Clamp01((targetFill - 0.2f) / 0.3f));
        if (targetFill <= 0.25f)
        {
            float blink = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f));
            c *= blink;
            shieldFrame.color = Color.Lerp(frameColor, shieldLow, blink);
        }
        else
        {
            shieldFrame.color = frameColor;
        }
        c.a = 1f;
        shieldFill.color = c;

        // Tremolor del marc de l'escut
        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            float k = shakeTime / shakeDuration;
            shieldGroup.anchoredPosition = shieldGroupPos + Random.insideUnitCircle * shakeStrength * k;
        }
        else
        {
            shieldGroup.anchoredPosition = shieldGroupPos;
        }

        // Flaix vermell a tota la pantalla
        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, 2.5f * dt);
        damageFlash.color = new Color(1f, 0f, 0f, flashAlpha);

        // Petit "pop" quan canvien vides o bombes
        livesPop = Mathf.MoveTowards(livesPop, 0f, 3f * dt);
        bombsPop = Mathf.MoveTowards(bombsPop, 0f, 3f * dt);
        livesGroup.localScale = Vector3.one * (1f + 0.3f * livesPop);
        bombsGroup.localScale = Vector3.one * (1f + 0.3f * bombsPop);
    }

    // ───────────────────────── Construcció ─────────────────────────

    private void Build()
    {
        CreateSprites();

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = (RectTransform)transform;

        // Flaix de dany (darrere de tot)
        damageFlash = NewImage("DamageFlash", root, whiteSprite, new Color(1, 0, 0, 0));
        Stretch(damageFlash.rectTransform);
        damageFlash.raycastTarget = false;

        BuildShield(root);
        BuildCounters(root);
    }

    private void BuildShield(RectTransform root)
    {
        const float barW = 420f, barH = 34f;

        shieldGroup = NewRect("Shield", root);
        Anchor(shieldGroup, new Vector2(0, 1), new Vector2(60, -50), new Vector2(barW + 16, barH + 60));
        shieldGroupPos = shieldGroup.anchoredPosition;

        // Etiqueta
        TextMeshProUGUI label = NewText("Label", shieldGroup, "SHIELD", 30, TextAlignmentOptions.BottomLeft);
        label.color = frameColor;
        Anchor(label.rectTransform, new Vector2(0, 1), new Vector2(4, 0), new Vector2(200, 40));

        // Valor numèric
        shieldText = NewText("Value", shieldGroup, "150", 30, TextAlignmentOptions.BottomRight);
        Anchor(shieldText.rectTransform, new Vector2(1, 1), new Vector2(-4, 0), new Vector2(220, 40));

        // Marc (vora daurada) + fons fosc
        RectTransform barRoot = NewRect("Bar", shieldGroup);
        Anchor(barRoot, new Vector2(0, 1), new Vector2(0, -44), new Vector2(barW + 16, barH + 16));

        shieldFrame = NewImage("Frame", barRoot, whiteSprite, frameColor);
        Stretch(shieldFrame.rectTransform);

        Image bg = NewImage("Background", barRoot, whiteSprite, new Color(0.02f, 0.04f, 0.12f, 0.85f));
        Stretch(bg.rectTransform, 4f);

        // Estela (blanca/vermella) i farciment principal
        shieldTrail = NewImage("Trail", barRoot, whiteSprite, new Color(1f, 0.95f, 0.85f, 0.9f));
        Stretch(shieldTrail.rectTransform, 8f);
        MakeHorizontalFill(shieldTrail);

        shieldFill = NewImage("Fill", barRoot, whiteSprite, shieldHigh);
        Stretch(shieldFill.rectTransform, 8f);
        MakeHorizontalFill(shieldFill);

        // Brillantor a la meitat superior per donar volum
        Image shine = NewImage("Shine", barRoot, whiteSprite, new Color(1, 1, 1, 0.18f));
        RectTransform sr = shine.rectTransform;
        sr.anchorMin = new Vector2(0, 0.5f); sr.anchorMax = Vector2.one;
        sr.offsetMin = new Vector2(8, 0); sr.offsetMax = new Vector2(-8, -8);

        // Divisions cada 50 punts d'escut
        for (int i = 1; i < 3; i++)
        {
            Image tick = NewImage("Tick" + i, barRoot, whiteSprite, new Color(0, 0, 0, 0.55f));
            RectTransform tr = tick.rectTransform;
            float x = i / 3f;
            tr.anchorMin = new Vector2(x, 0); tr.anchorMax = new Vector2(x, 1);
            tr.offsetMin = new Vector2(-1.5f, 8); tr.offsetMax = new Vector2(1.5f, -8);
            tr.anchoredPosition = new Vector2(8 - 16 * x, 0);
        }
    }

    private void BuildCounters(RectTransform root)
    {
        RectTransform panel = NewRect("Counters", root);
        Anchor(panel, new Vector2(1, 1), new Vector2(-60, -50), new Vector2(260, 140));

        livesGroup = BuildCounter(panel, "Lives", shipSprite, new Color(0.85f, 0.9f, 1f), 0, out livesText);
        bombsGroup = BuildCounter(panel, "Bombs", circleSprite, new Color(1f, 0.45f, 0.2f), -70, out bombsText);
    }

    private RectTransform BuildCounter(RectTransform parent, string name, Sprite icon, Color iconColor,
                                       float y, out TextMeshProUGUI text)
    {
        RectTransform group = NewRect(name, parent);
        Anchor(group, new Vector2(1, 1), new Vector2(0, y), new Vector2(260, 64));
        group.pivot = new Vector2(1, 1);

        // Requadre semitransparent
        Image bg = NewImage("Background", group, whiteSprite, new Color(0.02f, 0.04f, 0.12f, 0.55f));
        Stretch(bg.rectTransform);

        // Icona (amb ombra)
        Image shadow = NewImage("IconShadow", group, icon, new Color(0, 0, 0, 0.6f));
        Anchor(shadow.rectTransform, new Vector2(0, 0.5f), new Vector2(18 + 3, -3), new Vector2(52, 52));
        shadow.rectTransform.pivot = new Vector2(0, 0.5f);

        Image img = NewImage("Icon", group, icon, iconColor);
        Anchor(img.rectTransform, new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(52, 52));
        img.rectTransform.pivot = new Vector2(0, 0.5f);
        img.preserveAspect = true;
        shadow.preserveAspect = true;

        // Si són bombes, hi afegim un petit reflex perquè sembli una esfera
        if (icon == circleSprite)
        {
            Image glint = NewImage("Glint", img.rectTransform, circleSprite, new Color(1, 1, 0.8f, 0.8f));
            Anchor(glint.rectTransform, new Vector2(0.32f, 0.68f), Vector2.zero, new Vector2(14, 14));
        }

        text = NewText("Count", group, "× 3", 46, TextAlignmentOptions.Right);
        Anchor(text.rectTransform, new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(170, 60));
        text.rectTransform.pivot = new Vector2(1, 0.5f);
        return group;
    }

    // ───────────────────────── Utilitats UI ─────────────────────────

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string value, float size,
                                           TextAlignmentOptions align)
    {
        TextMeshProUGUI t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        t.text = value;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold | FontStyles.Italic;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(0, 0, 30, 255);
        return t;
    }

    // Ancora un element a un punt (anchor = pivot) amb una posició i mida
    private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, float inset = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static void MakeHorizontalFill(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
    }

    // ───────────────────────── Sprites procedurals ─────────────────────────

    private static void CreateSprites()
    {
        if (whiteSprite != null) return;

        Texture2D white = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        white.SetPixels(px);
        white.Apply();
        whiteSprite = Sprite.Create(white, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

        circleSprite = MakeSprite(64, (x, y) =>
        {
            float d = new Vector2(x - 0.5f, y - 0.5f).magnitude;
            return Mathf.Clamp01((0.47f - d) * 64f);
        });

        // Icona de nau genèrica: punta de fletxa vista des de dalt
        shipSprite = MakeSprite(64, (x, y) =>
        {
            float cx = Mathf.Abs(x - 0.5f);
            float sides = (0.92f - y) - cx * 1.8f;              // vores inclinades, morro a dalt
            float bottom = y - 0.1f;                             // base
            float notch = (y - 0.1f) - (0.25f - cx * 1.5f);      // osca a la part de darrere
            float a = Mathf.Min(Mathf.Min(sides, bottom), notch);
            return Mathf.Clamp01(a * 64f);
        });
    }

    private static Sprite MakeSprite(int size, System.Func<float, float, float> alpha)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float a = alpha((x + 0.5f) / size, (y + 0.5f) / size);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
