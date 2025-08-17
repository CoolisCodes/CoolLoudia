#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class HirearchyDecorator
{
    static HirearchyDecorator()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        // Draw separator if name is exactly "---"
        if (obj.name == "---")
        {
            Color sepColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            float sepY = selectionRect.y + selectionRect.height / 2f;
            EditorGUI.DrawRect(new Rect(selectionRect.x, sepY, selectionRect.width, 2f), sepColor);
            return;
        }

        bool isSelected = Selection.instanceIDs != null && System.Array.IndexOf(Selection.instanceIDs, instanceID) >= 0;
        bool isHovered = selectionRect.Contains(Event.current.mousePosition);

        // Row background with hover effect
        Color bg = isSelected ? HierarchyDecoratorConfigWindow.RowSelected : (isHovered ? HierarchyDecoratorConfigWindow.RowBg * 1.1f : HierarchyDecoratorConfigWindow.RowBg);
        bg.a = 1f; // Ensure alpha is 1
        EditorGUI.DrawRect(selectionRect, bg);

        // Draw enable/disable toggle (circle) with hover effect
        if (HierarchyDecoratorConfigWindow.ShowToggle)
        {
            float toggleSize = 14f;
            float togglePad = 4f;
            Rect toggleRect = new Rect(selectionRect.x + togglePad, selectionRect.y + (selectionRect.height - toggleSize) * 0.5f, toggleSize, toggleSize);

            bool toggleHovered = toggleRect.Contains(Event.current.mousePosition);
            Color toggleColor = obj.activeSelf ? new Color(0.2f, 0.8f, 0.2f, 1f) : new Color(0.85f, 0.2f, 0.2f, 1f);

            Handles.BeginGUI();
            Handles.color = toggleColor;
            Vector2 center = new Vector2(toggleRect.x + toggleRect.width / 2f, toggleRect.y + toggleRect.height / 2f);
            Handles.DrawSolidDisc(center, Vector3.forward, toggleSize * 0.5f);
            // Draw border: white or yellow if hovered
            Handles.color = toggleHovered ? Color.yellow : Color.white;
            Handles.DrawWireDisc(center, Vector3.forward, toggleSize * 0.5f);
            Handles.EndGUI();

            // Toggle click logic
            if (Event.current.type == EventType.MouseDown &&
                toggleRect.Contains(Event.current.mousePosition) &&
                Event.current.button == 0)
            {
                Undo.RecordObject(obj, "Toggle Active");
                obj.SetActive(!obj.activeSelf);
                EditorUtility.SetDirty(obj);
                if (!Application.isPlaying)
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(obj.scene);
                Event.current.Use();
            }
        }

        // Draw label with minimal style, shifted right for toggle
        float labelOffset = HierarchyDecoratorConfigWindow.ShowToggle
            ? (selectionRect.x + 4f + 14f + 4f)
            : (selectionRect.x + 4f);
        var style = new GUIStyle(EditorStyles.label)
        {
            fontStyle = obj.activeSelf ? FontStyle.Bold : FontStyle.Normal,
        };
        style.normal.textColor = obj.activeSelf ? HierarchyDecoratorConfigWindow.LabelColor : HierarchyDecoratorConfigWindow.LabelInactiveColor;
        Rect labelRect = new Rect(labelOffset, selectionRect.y, selectionRect.width - labelOffset, selectionRect.height);

        // Check prefab status for background modification
        PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(obj);
        PrefabInstanceStatus prefabStatus = PrefabUtility.GetPrefabInstanceStatus(obj);
        bool isPrefabRoot = PrefabUtility.IsAnyPrefabInstanceRoot(obj);
        bool hasPrefabOverrides = PrefabUtility.HasPrefabInstanceAnyOverrides(obj, false);

        // Modify background for prefabs
        if (prefabType != PrefabAssetType.NotAPrefab)
        {
            Color prefabBgTint = Color.white;

            if (prefabStatus == PrefabInstanceStatus.MissingAsset)
            {
                prefabBgTint = new Color(1f, 0.3f, 0.3f, 0.2f); // Red tint for missing
            }
            else if (prefabStatus == PrefabInstanceStatus.Disconnected)
            {
                prefabBgTint = new Color(1f, 1f, 0.3f, 0.2f); // Yellow tint for disconnected
            }
            else if (hasPrefabOverrides)
            {
                prefabBgTint = new Color(0.3f, 1f, 1f, 0.15f); // Cyan tint for modified
            }
            else if (isPrefabRoot)
            {
                prefabBgTint = new Color(0.3f, 0.3f, 1f, 0.1f); // Blue tint for prefab root
            }
            else
            {
                prefabBgTint = new Color(0.5f, 0.5f, 1f, 0.05f); // Light blue tint for nested prefab
            }

            // Apply prefab background tint
            Color currentBg = isSelected ? HierarchyDecoratorConfigWindow.RowSelected : (isHovered ? HierarchyDecoratorConfigWindow.RowBg * 1.1f : HierarchyDecoratorConfigWindow.RowBg);
            Color finalBg = Color.Lerp(currentBg, prefabBgTint, prefabBgTint.a);
            finalBg.a = 1f;
            EditorGUI.DrawRect(selectionRect, finalBg);
        }

        EditorGUI.LabelField(labelRect, obj.name, style);

        // Draw prefab indicator (shifted to the right so it doesn't overlap the toggle)
        if (prefabType != PrefabAssetType.NotAPrefab && HierarchyDecoratorConfigWindow.ShowPrefabIndicator)
        {
            float prefabSize = 12f;
            float prefabPadRight = 4f; // Move closer to right edge
            Rect prefabRect = new Rect(selectionRect.xMax - prefabSize - prefabPadRight, selectionRect.y + (selectionRect.height - prefabSize) * 0.5f, prefabSize, prefabSize);

            Color prefabColor = Color.blue; // Default prefab color
            string prefabIcon = "P";

            if (prefabStatus == PrefabInstanceStatus.MissingAsset)
            {
                prefabColor = Color.red;
                prefabIcon = "!";
            }
            else if (prefabStatus == PrefabInstanceStatus.Disconnected)
            {
                prefabColor = Color.yellow;
                prefabIcon = "D";
            }
            else if (hasPrefabOverrides)
            {
                prefabColor = Color.cyan;
                prefabIcon = "M"; // Modified
            }
            else if (isPrefabRoot)
            {
                prefabColor = Color.blue;
                prefabIcon = "P";
            }
            else
            {
                prefabColor = new Color(0.5f, 0.5f, 1f, 1f); // Lighter blue for nested
                prefabIcon = "p";
            }

            // Draw prefab background circle
            Handles.BeginGUI();
            Handles.color = prefabColor;
            Vector2 prefabCenter = new Vector2(prefabRect.x + prefabRect.width / 2f, prefabRect.y + prefabRect.height / 2f);
            Handles.DrawSolidDisc(prefabCenter, Vector3.forward, prefabSize * 0.5f);
            Handles.color = Color.white;
            Handles.DrawWireDisc(prefabCenter, Vector3.forward, prefabSize * 0.5f);
            Handles.EndGUI();

            // Draw prefab icon text
            var prefabStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 8,
                fontStyle = FontStyle.Bold
            };
            prefabStyle.normal.textColor = Color.white;
            EditorGUI.LabelField(prefabRect, prefabIcon, prefabStyle);
        }
    }
}

// EditorWindow for config
public class HierarchyDecoratorConfigWindow : EditorWindow
{
    static Color rowBg;
    static Color rowSelected;
    static Color labelColor;
    static Color labelInactiveColor;

    static bool showToggle = true;
    static bool showPrefabIndicator = true;

    const string EditorPrefsPrefix = "HierarchyDecoratorConfig_";

    static bool loaded = false;

    [MenuItem("Tools/Hierarchy Decorator/Config")]
    public static void ShowWindow()
    {
        GetWindow<HierarchyDecoratorConfigWindow>("Hierarchy Decorator Config");
    }

    void OnEnable()
    {
        LoadPrefs();
    }

    static void LoadPrefs()
    {
        if (loaded) return;
        rowBg = LoadColor("rowBg", new Color(0.18f, 0.18f, 0.18f, 1f));
        rowSelected = LoadColor("rowSelected", new Color(0.24f, 0.48f, 0.90f, 1f));
        labelColor = LoadColor("labelColor", Color.white);
        labelInactiveColor = LoadColor("labelInactiveColor", new Color(0.6f, 0.6f, 0.6f, 1f));
        showToggle = EditorPrefs.GetBool(EditorPrefsPrefix + "showToggle", true);
        showPrefabIndicator = EditorPrefs.GetBool(EditorPrefsPrefix + "showPrefabIndicator", true);
        loaded = true;
    }

    static void SavePrefs()
    {
        SaveColor("rowBg", rowBg);
        SaveColor("rowSelected", rowSelected);
        SaveColor("labelColor", labelColor);
        SaveColor("labelInactiveColor", labelInactiveColor);
        EditorPrefs.SetBool(EditorPrefsPrefix + "showToggle", showToggle);
        EditorPrefs.SetBool(EditorPrefsPrefix + "showPrefabIndicator", showPrefabIndicator);
    }

    static Color LoadColor(string key, Color fallback)
    {
        string k = EditorPrefsPrefix + key;
        if (EditorPrefs.HasKey(k + "_r"))
        {
            return new Color(
                EditorPrefs.GetFloat(k + "_r"),
                EditorPrefs.GetFloat(k + "_g"),
                EditorPrefs.GetFloat(k + "_b"),
                EditorPrefs.GetFloat(k + "_a")
            );
        }
        return fallback;
    }

    static void SaveColor(string key, Color color)
    {
        string k = EditorPrefsPrefix + key;
        EditorPrefs.SetFloat(k + "_r", color.r);
        EditorPrefs.SetFloat(k + "_g", color.g);
        EditorPrefs.SetFloat(k + "_b", color.b);
        EditorPrefs.SetFloat(k + "_a", color.a);
    }

    void OnGUI()
    {
        LoadPrefs();
        GUILayout.Label("Hierarchy Decorator Colors", EditorStyles.boldLabel);
        var newRowBg = EditorGUILayout.ColorField("Row Background", rowBg);
        var newRowSelected = EditorGUILayout.ColorField("Row Selected", rowSelected);
        var newLabelColor = EditorGUILayout.ColorField("Label Color", labelColor);
        var newLabelInactiveColor = EditorGUILayout.ColorField("Label Inactive Color", labelInactiveColor);

        GUILayout.Space(8);
        GUILayout.Label("Options", EditorStyles.boldLabel);
        bool newShowToggle = EditorGUILayout.Toggle("Show Enable/Disable Toggle", showToggle);
        bool newShowPrefabIndicator = EditorGUILayout.Toggle("Show Prefab Indicator", showPrefabIndicator);

        bool changed = false;
        if (newRowBg != rowBg) { rowBg = newRowBg; changed = true; }
        if (newRowSelected != rowSelected) { rowSelected = newRowSelected; changed = true; }
        if (newLabelColor != labelColor) { labelColor = newLabelColor; changed = true; }
        if (newLabelInactiveColor != labelInactiveColor) { labelInactiveColor = newLabelInactiveColor; changed = true; }
        if (newShowToggle != showToggle) { showToggle = newShowToggle; changed = true; }
        if (newShowPrefabIndicator != showPrefabIndicator) { showPrefabIndicator = newShowPrefabIndicator; changed = true; }

        if (changed)
        {
            SavePrefs();
            EditorApplication.RepaintHierarchyWindow();
        }

        if (GUILayout.Button("Reset to Defaults"))
        {
            rowBg = new Color(0.18f, 0.18f, 0.18f, 1f);
            rowSelected = new Color(0.24f, 0.48f, 0.90f, 1f);
            labelColor = Color.white;
            labelInactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            showToggle = true;
            showPrefabIndicator = true;
            SavePrefs();
            EditorApplication.RepaintHierarchyWindow();
        }
    }

    // Expose current config to decorator
    public static Color RowBg { get { LoadPrefs(); return rowBg; } }
    public static Color RowSelected { get { LoadPrefs(); return rowSelected; } }
    public static Color LabelColor { get { LoadPrefs(); return labelColor; } }
    public static Color LabelInactiveColor { get { LoadPrefs(); return labelInactiveColor; } }
    public static bool ShowToggle { get { LoadPrefs(); return showToggle; } }
    public static bool ShowPrefabIndicator { get { LoadPrefs(); return showPrefabIndicator; } }
}
#endif

