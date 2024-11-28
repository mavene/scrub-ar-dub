using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PlaneColorizer : MonoBehaviour
{
    [SerializeField]
    Color m_PlaneColor = Color.green;
    
    private bool hasColorized = false;
    
    public void ColorizePlane(GameObject planeObject)
    {
        if (hasColorized)
            return;
        
        var planeMeshRenderer = planeObject.GetComponent<MeshRenderer>();
        if (planeMeshRenderer != null)
        {
            // Create a new material instance to avoid affecting other planes
            Material planeMaterial = new Material(planeMeshRenderer.material);
            planeMaterial.color = m_PlaneColor;
            planeMeshRenderer.material = planeMaterial;
            hasColorized = true;
        }
    }
    
    public void ResetColorization()
    {
        hasColorized = false;
    }
}