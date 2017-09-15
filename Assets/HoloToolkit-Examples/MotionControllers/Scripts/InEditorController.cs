
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InEditorController : MonoBehaviour
{
    public Material controllerMaterial = null;
    public UnityEngine.Object controllerGLTFModel = null;

	// Use this for initialization
	IEnumerator Start ()
    {
#if !UNITY_EDITOR
        yield return null;
#else
        if (controllerGLTFModel == null)
            yield return null;

        string path = UnityEditor.AssetDatabase.GetAssetPath(controllerGLTFModel.GetInstanceID());
        if (string.IsNullOrEmpty(path))
            yield return null;

        var fileBytes = File.ReadAllBytes(path);
        var controllerModelGameObject = gameObject;

        GLTF.GLTFComponentStreamingAssets gltfScript = controllerModelGameObject.AddComponent<GLTF.GLTFComponentStreamingAssets>();
        gltfScript.ColorMaterial = controllerMaterial;
        gltfScript.NoColorMaterial = controllerMaterial;
        gltfScript.GLTFData = fileBytes;

        yield return gltfScript.LoadModel();
#endif
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
