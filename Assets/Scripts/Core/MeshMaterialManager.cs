using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MeshMaterialData
{
    [Header("Configuration Name")]
    public string name;

    [Header("Mesh & Material")]
    public Mesh mesh;
    public Material[] materials;

    [Header("Optional Settings")]
    public Vector3 scale = Vector3.one;

    public bool IsValid()
    {
        return mesh != null && materials != null && materials.Length > 0;
    }
}

public class MeshMaterialManager : MonoBehaviour
{
    private static MeshMaterialManager _instance;

    [Header("Mesh Material Groups")]
    [SerializeField] private MeshMaterialData[] meshMaterialGroups;

    public static MeshMaterialManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MeshMaterialManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("MeshMaterialManager");
                    _instance = go.AddComponent<MeshMaterialManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public MeshMaterialData GetRandomMeshMaterial()
    {
        return meshMaterialGroups[(int)Random.Range(0, meshMaterialGroups.Length-1)];
    }

    public bool ApplyMeshMaterial(SkinnedMeshRenderer renderer, MeshMaterialData data)
    {
        if (renderer == null || data == null || !data.IsValid())
        {
            return false;
        }

        renderer.sharedMesh = data.mesh;
        renderer.materials = data.materials;
        //renderer.transform.localScale = data.scale;

        return true;
    }
}