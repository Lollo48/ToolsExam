using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class HouseScatter : EditorWindow
{
    [MenuItem("Tools/HouseScatterer")]
    public static void OpenWindow() => GetWindow(typeof(HouseScatter));

    /// <summary>
    /// prefabs to spawn
    /// </summary>
    public GameObject spawnPrefab = null;

    /// <summary>
    /// the material that will be used for the preview
    /// </summary>
    public Material previewMaterial = null;

    /// <summary>
    /// bool to turn off the preview
    /// </summary>
    public bool wantPreview = false;
    /// <summary>
    /// bool to turn off the snapping
    /// </summary>
    public bool wantSnap = false;

    Vector3 previewPosition;
    Quaternion previewRotation;

    GameObject[] prefabs;

    SerializedObject so;
    SerializedProperty spawnPrefabP;
    SerializedProperty previewMaterialP;
    SerializedProperty wantPreviewP;
    SerializedProperty wantSnapP;


    private void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;

        so = new SerializedObject(this);
        spawnPrefabP = so.FindProperty("spawnPrefab");
        previewMaterialP = so.FindProperty("previewMaterial");
        wantPreviewP = so.FindProperty("wantPreview");
        wantSnapP = so.FindProperty("wantSnap");

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
        EditorGUILayout.PropertyField(previewMaterialP);
        EditorGUILayout.PropertyField(wantPreviewP);
        EditorGUILayout.PropertyField(wantSnapP);

        if (so.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }
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

        float rectPosX = SceneView.lastActiveSceneView.camera.pixelWidth * 0.01f;
        float rectPosY = SceneView.lastActiveSceneView.camera.pixelHeight * 0.2f;
        Rect rect = new Rect(rectPosX, rectPosY, 100, 100);
        Rect backgroundRect = new Rect(rect.x - 17, rect.y - 12, rect.width + 30, rect.height + 333); 
        GUI.Box(backgroundRect, ""); 

        foreach (GameObject prefab in prefabs)
        {
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(10, 10, 10, 10);
            buttonStyle.hover.background = buttonStyle.active.background;

            if (GUI.Button(rect, new GUIContent(icon), buttonStyle))
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
        if (!wantPreview) return;

        MeshFilter[] filters = spawnPrefab.GetComponentsInChildren<MeshFilter>();
        Matrix4x4 poseToWorldMtx = Matrix4x4.TRS(previewPosition, previewRotation, Vector3.one);

        foreach (MeshFilter filter in filters)
        {
            
            Mesh mesh = filter.sharedMesh;
            //Material mat = filter.GetComponent<MeshRenderer>().sharedMaterial;

            if (previewMaterial == null) return;
            else previewMaterial.SetPass(0);

            //mat.SetPass(0);
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
        if (!wantPreview) return;
        if (!wantSnap) previewPosition = hit.point + new Vector3(0, 0.2f, 0);

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
