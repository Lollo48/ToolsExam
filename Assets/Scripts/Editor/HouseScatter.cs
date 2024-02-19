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

    Vector3 previewPosition;
    Quaternion previewRotation;

    GameObject[] prefabs;

    SerializedObject so;
    SerializedProperty spawnPrefabP;

   

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
            PositionSetter(hit);

            Drawpreview();

            if (Event.current.keyCode == KeyCode.Space && Event.current.type == EventType.KeyDown)
            {
                SpawnObjects();
            }
        }

        RotatePrefab();

        
    }

    /// <summary>
    /// Generates the houses that can be selected on the left of the scene
    /// </summary>
    void SpawnToggle()
    {
        Rect rect = new Rect(1400, 100, 100, 100);

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

    /// <summary>
    /// I draw the preview of the house that I will spawn
    /// </summary>
    void Drawpreview()
    {
        if (spawnPrefab == null) return;

        MeshFilter[] filters = spawnPrefab.GetComponentsInChildren<MeshFilter>();
        Matrix4x4 poseToWorldMtx = Matrix4x4.TRS(previewPosition, previewRotation, Vector3.one);

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

    /// <summary>
    /// spawn the selected object in editor 
    /// </summary>
    /// <param name="hit"></param>
    void SpawnObjects()
    {
        if (spawnPrefab == null) return;

        GameObject thingToSpawn = (GameObject)PrefabUtility.InstantiatePrefab(spawnPrefab);
        Undo.RegisterCreatedObjectUndo(thingToSpawn, "Object Spawn");
        
        thingToSpawn.transform.SetPositionAndRotation(previewPosition, previewRotation);


    }

   /// <summary>
   /// rotate the prefab in 90° in y
   /// </summary>
    private void RotatePrefab()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
        {
            previewRotation *= Quaternion.Euler(0f, 90f, 0f);
        } 
    }

    /// <summary>
    /// This function sets the positions of the preview and the spawn of the object and also checks whether the house can be snapped
    /// </summary>
    /// <param name="hit"></param>
    void PositionSetter(RaycastHit hit)
    {
        spawnPrefab.TryGetComponent(out House house);
        Vector3 position = Vector3.zero;

        for(int i=0; i < house.DoorsNumber; i++)
        {
            Vector3 direction = previewRotation * Quaternion.Euler(0f, 90f * i, 0f) * Vector3.forward * 10f;

            // spawn raycast for each door
            Ray ray = new Ray(previewPosition + Vector3.up * 2, direction);
            Debug.DrawRay(previewPosition + Vector3.up * 2, direction , Color.red);

            if (Physics.Raycast(ray, out RaycastHit hit2) && hit2.collider.gameObject.layer == 6)
            {
                position = hit2.collider.transform.position;
            }
        }
        
        //Set the new position
        previewPosition = position != Vector3.zero ? position : hit.point + new Vector3(0, 0.2f, 0);
       
    }

}
