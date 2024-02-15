using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class HouseScatter : EditorWindow
{
    [MenuItem("Tools/HouseScatterer")]
    public static void OpenWindow() => GetWindow(typeof(HouseScatter));

    public GameObject spawnPrefab = null;
    public Material previewMaterial;

    SerializedObject so;
    SerializedProperty spawnPrefabP;
    SerializedProperty previewMatP;


    GameObject[] prefabs;

    private void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;

        so = new SerializedObject(this);
        spawnPrefabP = so.FindProperty("spawnPrefab");
        previewMatP = so.FindProperty("previewMaterial");


        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs/PropScatterer" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

    }

    private void OnDisable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;
    }


    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(spawnPrefabP);

        EditorGUILayout.PropertyField(previewMatP);

    }


    void DuringSceneGUI(SceneView sceneView)
    {
        if (prefabs == null && prefabs.Length == 0) return;

        Handles.BeginGUI();

        Rect rect = new Rect(8, 8, 50, 50);

        foreach (GameObject prefab in prefabs)
        {
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            if (GUI.Toggle(rect, spawnPrefab == prefab, new GUIContent(icon)))
            {
                spawnPrefab = prefab;
                Repaint();
            }

            rect.y += rect.height + 2;
        }

        Handles.EndGUI();

        if (spawnPrefab == null) return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        Transform cam = sceneView.camera.transform;

        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        RotatePrefab();

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {






            if (Event.current.keyCode == KeyCode.Space && Event.current.type == EventType.KeyDown)
            {
                TrySpawnObjects(hit);
            }
        }

    }


    void preview()
    {

    }

    void TrySpawnObjects(RaycastHit hit)
    {
        if (spawnPrefab == null) return;

        GameObject thingToSpawn = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefab);
        Undo.RegisterCreatedObjectUndo(thingToSpawn, "Object Spawn");
        thingToSpawn.transform.position = hit.point;

    }

    private Quaternion RotatePrefab()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
        {
            so.Update();

            spawnPrefab.transform.Rotate(Vector3.up, 90f);
            
            if (so.ApplyModifiedProperties())
            {
                Repaint();
            }

            Event.current.Use();
        }

        return spawnPrefab.transform.rotation;
    }

}
