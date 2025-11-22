using UnityEngine;

// simple imgui overlay for dungeon demo with custom styling
public class DungeonDemoUI : MonoBehaviour
{
    [Header("hook up your generator (optional)")]
    public DGenerator generator;

    [Header("window settings")]
    public string windowTitle = "Dungeon Demo Controls";
    public bool startVisible = true;

    private bool _visible;
    private Rect _windowRect;
    private Vector2 _scrollPos;

    // style fields
    private GUIStyle _windowStyle;
    private GUIStyle _titleLabelStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _buttonStyle;
    private bool _stylesInitialized;

    // textures
    private Texture2D _windowTex;
    private Texture2D _titleBarTex;
    private Texture2D _buttonTex;
    private Texture2D _buttonHoverTex;
    private Texture2D _scrollTrackTex;
    private Texture2D _scrollThumbTex;

    // optional custom font
    private Font _uiFont;

    void Awake()
    {
        _windowRect = new Rect(20, 20, 340, 220);
        _visible = startVisible;
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

        if (!_stylesInitialized)
            InitStyles();

        // always fall back to default style if something went wrong
        GUIStyle styleToUse = _windowStyle != null ? _windowStyle : GUI.skin.window;

        _windowRect = GUI.Window(1001, _windowRect, DrawWindow, GUIContent.none, styleToUse);
        _windowRect = ClampToScreen(_windowRect);
    }

    Rect ClampToScreen(Rect r)
    {
        const float padding = 10f;

        r.x = Mathf.Clamp(r.x, padding, Screen.width - r.width - padding);
        r.y = Mathf.Clamp(r.y, padding, Screen.height - r.height - padding);

        return r;
    }

    Texture2D MakeTex(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    void InitStyles()
    {
        // prevent reentry
        _stylesInitialized = true;

        // load a custom font
        _uiFont = Resources.Load<Font>("JetBrainsMono-VariableFont_wght");

        // colors
        Color windowBg = new Color(0.09f, 0.09f, 0.10f, 0.95f);
        Color titleBg = new Color(0.18f, 0.27f, 0.43f, 1.0f);
        Color text = new Color(0.85f, 0.86f, 0.89f, 1.0f);
        Color headerText = new Color(0.35f, 0.65f, 1.0f, 1.0f);
        Color buttonBg = new Color(0.20f, 0.30f, 0.50f, 1.0f);
        Color buttonHoverBg = new Color(0.30f, 0.50f, 0.85f, 1.0f);
        Color scrollTrack = new Color(0.10f, 0.10f, 0.12f, 1.0f);
        Color scrollThumb = new Color(0.30f, 0.50f, 0.85f, 1.0f);

        _windowTex = MakeTex(windowBg);
        _titleBarTex = MakeTex(titleBg);
        _buttonTex = MakeTex(buttonBg);
        _buttonHoverTex = MakeTex(buttonHoverBg);
        _scrollTrackTex = MakeTex(scrollTrack);
        _scrollThumbTex = MakeTex(scrollThumb);

        // window style
        _windowStyle = new GUIStyle(GUI.skin.window);
        _windowStyle.normal.background = _windowTex;
        _windowStyle.onNormal.background = _windowTex;
        _windowStyle.hover.background = _windowTex;
        _windowStyle.active.background = _windowTex;
        _windowStyle.focused.background = _windowTex;
        _windowStyle.padding = new RectOffset(6, 6, 6, 6);
        _windowStyle.border = new RectOffset(1, 1, 1, 1);
        _windowStyle.normal.textColor = text;
        _windowStyle.fontSize = 12;
        if (_uiFont != null)
            _windowStyle.font = _uiFont;

        // title label
        _titleLabelStyle = new GUIStyle(GUI.skin.label);
        _titleLabelStyle.fontSize = 13;
        _titleLabelStyle.fontStyle = FontStyle.Bold;
        _titleLabelStyle.normal.textColor = Color.white;
        _titleLabelStyle.alignment = TextAnchor.MiddleLeft;
        _titleLabelStyle.padding = new RectOffset(8, 0, 0, 0);
        if (_uiFont != null)
            _titleLabelStyle.font = _uiFont;

        // section headers
        _headerStyle = new GUIStyle(GUI.skin.label);
        _headerStyle.fontSize = 12;
        _headerStyle.fontStyle = FontStyle.Bold;
        _headerStyle.normal.textColor = headerText;
        if (_uiFont != null)
            _headerStyle.font = _uiFont;

        // normal labels
        _labelStyle = new GUIStyle(GUI.skin.label);
        _labelStyle.fontSize = 11;
        _labelStyle.normal.textColor = text;
        if (_uiFont != null)
            _labelStyle.font = _uiFont;

        // buttons
        _buttonStyle = new GUIStyle(GUI.skin.button);
        _buttonStyle.normal.background = _buttonTex;
        _buttonStyle.hover.background = _buttonHoverTex;
        _buttonStyle.active.background = _buttonHoverTex;
        _buttonStyle.normal.textColor = text;
        _buttonStyle.hover.textColor = Color.white;
        _buttonStyle.active.textColor = Color.white;
        _buttonStyle.fontSize = 11;
        _buttonStyle.padding = new RectOffset(6, 6, 3, 3);
        if (_uiFont != null)
            _buttonStyle.font = _uiFont;

        // simple scrollbar styling on the current gui skin
        GUI.skin.verticalScrollbar.normal.background = _scrollTrackTex;
        GUI.skin.verticalScrollbar.hover.background = _scrollTrackTex;
        GUI.skin.verticalScrollbar.active.background = _scrollTrackTex;
        GUI.skin.verticalScrollbar.fixedWidth = 10;

        GUI.skin.verticalScrollbarThumb.normal.background = _scrollThumbTex;
        GUI.skin.verticalScrollbarThumb.hover.background = _scrollThumbTex;
        GUI.skin.verticalScrollbarThumb.active.background = _scrollThumbTex;
    }

    void DrawWindow(int windowID)
    {
        GUILayout.BeginVertical();

        // custom title bar
        Rect titleRect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
        GUI.DrawTexture(titleRect, _titleBarTex);
        GUI.Label(titleRect, windowTitle, _titleLabelStyle);

        GUILayout.Space(4);

        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(170));

        GUILayout.Label("Movement", _headerStyle);
        GUILayout.Label("WASD / Arrow Keys   - Move", _labelStyle);
        GUILayout.Label("Right Mouse Drag    - Rotate Camera", _labelStyle);
        GUILayout.Label("Mouse Wheel         - Zoom in / out", _labelStyle);
        GUILayout.Space(6);

        GUILayout.Label("Dungeon Controls", _headerStyle);
        GUILayout.Label("CTRL + R            - Regenerate Dungeon", _labelStyle);
        GUILayout.Label("Tab                 - Center Camera", _labelStyle);
        GUILayout.Space(6);

        GUILayout.Label("UI", _headerStyle);
        GUILayout.Label("F1                  - Toggle this help panel", _labelStyle);
        GUILayout.Space(8);

        if (generator != null)
        {
            GUILayout.Label("Current Dungeon", _headerStyle);
            GUILayout.Label("Rooms: " + generator.TotalRooms, _labelStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Regenerate (r)", _buttonStyle))
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

        // drag window by the title bar area
        GUI.DragWindow(new Rect(0, 0, _windowRect.width, 22));
    }
}
