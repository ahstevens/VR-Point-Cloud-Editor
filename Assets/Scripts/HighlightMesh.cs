using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;

public class HighlightMesh : MonoBehaviour
{
    public float highlightRate = 1.0f;

    [SerializeField]
    private Material highlightMaterial;
    private Material highlightMaterialInstance;
    private Material originalMaterial;
    private Renderer meshRenderer;

    private float tick;

    Mesh highlightedMesh;

    private void Awake()
    {
        if (!meshRenderer) meshRenderer = GetComponent<Renderer>();
        originalMaterial = meshRenderer.material;
    }

    private void Update()
    {
        float dt = (tick + Time.deltaTime);
        float ratio = (dt % highlightRate) / highlightRate;
        tick = dt;

        var currentColor = highlightMaterial.color;
        currentColor.b = ratio;
        //highlightMaterialInstance.SetColor("_Color", currentColor);
    }

    private void OnEnable()
    {
        tick = 0f;

        highlightMaterialInstance = Instantiate(highlightMaterial);

        //meshRenderer.AddMaterial(highlightMaterialInstance);
        meshRenderer.material = highlightMaterialInstance;
    }

    private void OnDisable()
    {
        Destroy(highlightMaterialInstance);
        //meshRenderer.materials = meshRenderer.materials.SkipLast(1).ToArray();
        meshRenderer.material = originalMaterial;
    }
}
