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

    Vector3 previewPosition;
    Quaternion previewRotation;


    GameObject[] prefabs;

    private void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;

        so = new SerializedObject(this);
        spawnPrefabP = so.FindProperty("spawnPrefab");

        previewRotation = Quaternion.identity;

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs/PropScatterer" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }


    private void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(spawnPrefabP);

    }


    void DuringSceneGUI(SceneView sceneView)
    {
        if (prefabs == null && prefabs.Length == 0) return;

        Handles.BeginGUI();

        SpawnToggle();

        if (spawnPrefab == null) return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        Transform cam = sceneView.camera.transform;

        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {

            preview(hit);

            if (Event.current.keyCode == KeyCode.Space && Event.current.type == EventType.KeyDown)
            {
                TrySpawnObjects(hit);
            }
        }

        RotatePrefab();
    }

    void SpawnToggle()
    {
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
    }

    void preview(RaycastHit hit)
    {
        if (spawnPrefab == null) return;

        MeshFilter[] filters = spawnPrefab.GetComponentsInChildren<MeshFilter>();

        Matrix4x4 poseToWorldMtx = Matrix4x4.TRS(hit.point + new Vector3(0,0.2f,0), previewRotation, Vector3.one);

        foreach (MeshFilter filter in filters)
        {
            Mesh mesh = filter.sharedMesh;
            Material mat = filter.GetComponent<MeshRenderer>().sharedMaterial;
            mat.SetPass(0);

            Matrix4x4 childToPose = filter.transform.localToWorldMatrix;
            Matrix4x4 childToWorldMtx = poseToWorldMtx * childToPose;

            Graphics.DrawMeshNow(mesh, childToWorldMtx);

        }
    }

    void TrySpawnObjects(RaycastHit hit)
    {
        if (spawnPrefab == null) return;

        GameObject thingToSpawn = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefab);
        Undo.RegisterCreatedObjectUndo(thingToSpawn, "Object Spawn");
        thingToSpawn.transform.SetPositionAndRotation(hit.point + new Vector3(0, 0.2f, 0), previewRotation);

    }

    private void RotatePrefab()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
        {
            previewRotation *= Quaternion.Euler(0f, 90f, 0f);
        } 
    }

}
