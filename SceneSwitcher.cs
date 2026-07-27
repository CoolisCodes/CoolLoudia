using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

#if UNITY_EDITOR
[InitializeOnLoad]
public static class SceneSwitcher
{
    private const string ContainerName = "scene-switcher-toolbar-container";

    private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");

    private static IMGUIContainer container;
    private static string[] scenePaths = Array.Empty<string>();
    private static string[] sceneNames = Array.Empty<string>();
    private static int selectedIndex;
    private static bool needsRefresh = true;
    private static GUIStyle standoutLabelStyle;
    private static GUIStyle standoutPopupStyle;
    private static GUIStyle activeSceneStyle;

    static SceneSwitcher()
    {
        EditorApplication.update += EnsureToolbarHook;
        EditorApplication.projectChanged += MarkSceneListDirty;
        EditorBuildSettings.sceneListChanged += MarkSceneListDirty;
        EditorSceneManager.activeSceneChangedInEditMode += (_, _) => MarkSceneListDirty();
    }

    private static void EnsureToolbarHook()
    {
        if (ToolbarType == null)
        {
            return;
        }

        if (container != null && container.panel != null)
        {
            return;
        }

        UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
        if (toolbars.Length == 0)
        {
            return;
        }

        FieldInfo rootField = ToolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        if (rootField == null)
        {
            return;
        }

        VisualElement root = rootField.GetValue(toolbars[0]) as VisualElement;
        if (root == null)
        {
            return;
        }

        VisualElement rightZone = root.Q("ToolbarZoneRightAlign");
        if (rightZone == null)
        {
            return;
        }

        container = rightZone.Q<IMGUIContainer>(ContainerName);
        if (container != null)
        {
            return;
        }

        container = new IMGUIContainer(DrawSceneDropdown)
        {
            name = ContainerName,
        };
        container.style.flexShrink = 0;
        container.style.marginLeft = 6;
        rightZone.Add(container);
    }

    private static void DrawSceneDropdown()
    {
        if (needsRefresh)
        {
            RefreshScenes();
        }

        if (standoutLabelStyle == null)
        {
            standoutLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
            };

            standoutPopupStyle = new GUIStyle(EditorStyles.toolbarPopup)
            {
                fontStyle = FontStyle.Bold,
            };

            activeSceneStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.96f, 0.52f) : new Color(0.63f, 0.26f, 0f),
                },
            };
        }

        if (scenePaths.Length == 0)
        {
            GUILayout.Label("No scenes found", EditorStyles.miniLabel, GUILayout.Width(110));
            return;
        }

        Color previousColor = GUI.color;
        float pulse = 0.85f + (Mathf.Sin((float)EditorApplication.timeSinceStartup * 4f) * 0.15f);
        GUI.color = EditorGUIUtility.isProSkin
            ? new Color(0.2f * pulse, 0.56f * pulse, 1f * pulse, 1f)
            : new Color(0.35f, 0.7f * pulse, 1f, 1f);
        GUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Width(500));
        GUI.color = previousColor;

        GUIContent icon = EditorGUIUtility.IconContent("SceneAsset Icon");
        GUILayout.Label(icon, GUILayout.Width(18));
        GUILayout.Label("SCENE SWITCHER", standoutLabelStyle, GUILayout.Width(112));

        string activeScene = selectedIndex >= 0 && selectedIndex < sceneNames.Length
            ? sceneNames[selectedIndex]
            : "Unknown";
        GUILayout.Label($"ACTIVE: {activeScene}", activeSceneStyle, GUILayout.Width(200));

        int newIndex = EditorGUILayout.Popup(
            selectedIndex,
            sceneNames,
            standoutPopupStyle,
            GUILayout.Width(170)
        );

        if (newIndex != selectedIndex)
        {
            OpenSceneByIndex(newIndex);
        }

        GUILayout.EndHorizontal();
    }

    private static void RefreshScenes()
    {
        string[] buildScenePaths = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        HashSet<string> buildSceneSet = new HashSet<string>(buildScenePaths, StringComparer.OrdinalIgnoreCase);

        string[] allProjectScenePaths = AssetDatabase.FindAssets("t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        scenePaths = buildScenePaths
            .Concat(allProjectScenePaths.Where(path => !buildSceneSet.Contains(path)))
            .ToArray();

        sceneNames = scenePaths
            .Select(path => FormatSceneLabel(path, buildSceneSet.Contains(path)))
            .ToArray();

        string activeScenePath = SceneManager.GetActiveScene().path;
        selectedIndex = Array.IndexOf(scenePaths, activeScenePath);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        needsRefresh = false;
    }

    private static string FormatSceneLabel(string scenePath, bool isInBuildSettings)
    {
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);
        string directory = Path.GetDirectoryName(scenePath)?.Replace('\\', '/') ?? "Assets";
        string folderHint = directory.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
            ? directory.Substring("Assets/".Length)
            : directory;

        if (string.IsNullOrWhiteSpace(folderHint))
        {
            folderHint = "Assets";
        }

        string tag = isInBuildSettings ? "[Build]" : "[Project]";
        return $"{tag} {sceneName} ({folderHint})";
    }

    private static void OpenSceneByIndex(int index)
    {
        if (index < 0 || index >= scenePaths.Length)
        {
            return;
        }

        string targetScenePath = scenePaths[index];
        if (targetScenePath == SceneManager.GetActiveScene().path)
        {
            selectedIndex = index;
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            needsRefresh = true;
            return;
        }

        EditorSceneManager.OpenScene(targetScenePath);
        selectedIndex = index;
        needsRefresh = true;
    }

    private static void MarkSceneListDirty()
    {
        needsRefresh = true;
    }
}
#endif
