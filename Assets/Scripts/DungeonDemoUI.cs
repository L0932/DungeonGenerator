using UnityEngine;

// simple imgui overlay for dungeon demo
public class DungeonDemoUI : MonoBehaviour
{
    [Header("Hook up your generator (optional)")]
    public DGenerator generator;

    [Header("Window Settings")]
    public string windowTitle = "Dungeon Demo Controls";
    public bool startVisible = true;

    private bool _visible;
    private Rect _windowRect;
    private Vector2 _scrollPos;

    // style fields
    private GUIStyle _windowStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _thinSeparator;
    private bool _stylesInitialized;

    void Awake()
    {
        // starting rect for the window
        _windowRect = new Rect(10, 10, 360, 260);
        _visible = startVisible;

        // do not call initstyles here, it uses gui.skin which must be inside ongui
        _stylesInitialized = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            _visible = !_visible;
    }

    void OnGUI()
    {
        if (!_visible)
            return;

        // ensure styles are initialized once, inside ongui
        if (!_stylesInitialized)
        {
            InitStyles();
            _stylesInitialized = true;
        }

        // draw the window with custom style
        _windowRect = GUI.Window(1001, _windowRect, DrawWindow, "", _windowStyle);

        // clamp so it stays inside screen bounds
        _windowRect = ClampToScreen(_windowRect);
    }

    Rect ClampToScreen(Rect r)
    {
        const float padding = 10f;

        r.x = Mathf.Clamp(r.x, padding, Screen.width - r.width - padding);
        r.y = Mathf.Clamp(r.y, padding, Screen.height - r.height - padding);

        return r;
    }

    // initialize basic styles to mimic a dear imgui like dark theme
    void InitStyles()
    {
        // create base styles using current gui skin
        _windowStyle = new GUIStyle(GUI.skin.window);
        _headerStyle = new GUIStyle(GUI.skin.label);
        _labelStyle = new GUIStyle(GUI.skin.label);
        _buttonStyle = new GUIStyle(GUI.skin.button);
        _thinSeparator = new GUIStyle(GUI.skin.box);

        // colors (dark theme)
        Color windowBg = new Color(0.11f, 0.11f, 0.13f, 0.96f);
        Color border = new Color(0.24f, 0.24f, 0.26f, 1f);
        Color header = new Color(0.85f, 0.86f, 0.89f, 1f);
        Color text = new Color(0.85f, 0.86f, 0.89f, 1f);
        Color accent = new Color(0.26f, 0.59f, 0.98f, 1f);

        Texture2D windowTex = MakeTex(1, 1, windowBg);
        Texture2D borderTex = MakeTex(1, 1, border);
        Texture2D buttonTex = MakeTex(1, 1, new Color(0.20f, 0.20f, 0.23f, 1f));
        Texture2D buttonHoverTex = MakeTex(1, 1, new Color(0.26f, 0.59f, 0.98f, 0.8f));

        // window style: force all states to use the same flat texture
        _windowStyle.normal.background = windowTex;
        _windowStyle.onNormal.background = windowTex;
        _windowStyle.hover.background = windowTex;
        _windowStyle.onHover.background = windowTex;
        _windowStyle.active.background = windowTex;
        _windowStyle.onActive.background = windowTex;
        _windowStyle.focused.background = windowTex;
        _windowStyle.onFocused.background = windowTex;

        _windowStyle.padding = new RectOffset(10, 10, 18, 10);
        _windowStyle.border = new RectOffset(1, 1, 1, 1);
        _windowStyle.fontSize = 14;
        _windowStyle.alignment = TextAnchor.UpperCenter;
        _windowStyle.normal.textColor = header;

        // header text
        _headerStyle.fontSize = 13;
        _headerStyle.fontStyle = FontStyle.Bold;
        _headerStyle.normal.textColor = accent;
        _headerStyle.richText = false;

        // normal labels
        _labelStyle.fontSize = 12;
        _labelStyle.normal.textColor = text;
        _labelStyle.richText = false;

        // buttons: same idea, unify state textures
        _buttonStyle.normal.background = buttonTex;
        _buttonStyle.onNormal.background = buttonTex;
        _buttonStyle.hover.background = buttonHoverTex;
        _buttonStyle.onHover.background = buttonHoverTex;
        _buttonStyle.active.background = buttonHoverTex;
        _buttonStyle.onActive.background = buttonHoverTex;
        _buttonStyle.focused.background = buttonTex;
        _buttonStyle.onFocused.background = buttonTex;

        _buttonStyle.normal.textColor = text;
        _buttonStyle.hover.textColor = Color.white;
        _buttonStyle.active.textColor = Color.white;
        _buttonStyle.fontSize = 12;
        _buttonStyle.padding = new RectOffset(8, 8, 4, 4);

        // thin separator bar
        _thinSeparator.normal.background = borderTex;
        _thinSeparator.fixedHeight = 1;
        _thinSeparator.margin = new RectOffset(0, 0, 4, 4);
    }


    Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        result.hideFlags = HideFlags.HideAndDontSave;
        return result;
    }

    void DrawWindow(int windowID)
    {
        GUILayout.BeginVertical();

        // manual title bar text
        GUILayout.Label(windowTitle, _headerStyle);

        // separator
        GUILayout.Box(GUIContent.none, _thinSeparator);

        // scrollable area
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(210));

        GUILayout.Label("movement", _headerStyle);
        GUILayout.Label("wasd / arrow keys   - move", _labelStyle);
        GUILayout.Label("right mouse drag    - rotate camera", _labelStyle);
        GUILayout.Label("mouse wheel         - zoom in/out", _labelStyle);
        GUILayout.Space(6);

        GUILayout.Label("dungeon controls", _headerStyle);
        GUILayout.Label("ctrl + r            - regenerate dungeon", _labelStyle);
        GUILayout.Label("tab                 - center camera", _labelStyle);
        GUILayout.Space(6);

        GUILayout.Label("ui", _headerStyle);
        GUILayout.Label("f1                  - toggle this help panel", _labelStyle);
        GUILayout.Space(8);

        if (generator != null)
        {
            GUILayout.Label("current dungeon", _headerStyle);
            GUILayout.Label("rooms: " + generator.TotalRooms, _labelStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("regenerate (r)", _buttonStyle))
            {
                generator.RegenerateDungeon();
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Space(6);
            GUILayout.Label("(assign generator in inspector)", _labelStyle);
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();

        // allow dragging by the top area
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }
}
