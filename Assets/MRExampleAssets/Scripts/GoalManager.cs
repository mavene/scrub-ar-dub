using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using LazyFollow = UnityEngine.XR.Interaction.Toolkit.UI.LazyFollow;

public struct Goal
{
    public GoalManager.OnboardingGoals CurrentGoal;
    public bool Completed;

    public Goal(GoalManager.OnboardingGoals goal)
    {
        CurrentGoal = goal;
        Completed = false;
    }
}

public class GoalManager : MonoBehaviour
{
    public enum OnboardingGoals
    {
        Empty,
        FindSurfaces,
        TapSurface,
        CleanSurface
    }

    Queue<Goal> m_OnboardingGoals;
    Goal m_CurrentGoal;
    bool m_AllGoalsFinished;
    int m_CurrentGoalIndex = 0;

    [Serializable]
    class Step
    {
        [SerializeField]
        public GameObject stepObject;

        [SerializeField]
        public string buttonText;

        public bool includeSkipButton;
    }

    [SerializeField]
    List<Step> m_StepList = new List<Step>();

    [SerializeField]
    public TextMeshProUGUI m_StepButtonTextField;

    [SerializeField]
    public GameObject m_SkipButton;

    [SerializeField]
    GameObject m_LearnButton;

    [SerializeField]
    GameObject m_LearnModal;

    [SerializeField]
    Button m_LearnModalButton;

    [SerializeField]
    GameObject m_CoachingUIParent;

    [SerializeField]
    FadeMaterial m_FadeMaterial;

    [SerializeField]
    Toggle m_PassthroughToggle;

    [SerializeField]
    LazyFollow m_GoalPanelLazyFollow;

    [SerializeField]
    GameObject m_TapTooltip;

    [SerializeField]
    ARPlaneManager m_ARPlaneManager;

    [SerializeField]
    GameObject m_PalmObject;
    
    [SerializeField]
    Material m_DefaultPlaneMaterial;  // Original plane material from ARPlaneManager

    [SerializeField]
    Material m_SelectedPlaneMaterial;  // New material for selected planes

    [SerializeField]
    float m_TapThreshold = 0.05f;  // Distance threshold for tap detection
    
    [SerializeField]
    float m_RaycastThreshold = 5f; // Distance threshold for ray cast

    [SerializeField] 
    private TMP_Dropdown m_materialSelector;

    [SerializeField]
    RandomObjectSpawner m_objectSpawner;

    [SerializeField]
    private Material m_IceMaterial; 

    [SerializeField]
    private Material m_FarmMaterial; 

    [SerializeField]
    private Material m_StoneMaterial; 

    public List<GameObject> m_SelectedPlanes = new List<GameObject>();

    private int m_SurfacesTapped = 0;
    private int k_NumberOfSurfacesTappedToCompleteGoal = 2;
    private Camera m_MainCamera;
    private bool m_IsPalmTapEnabled = false;
     private bool m_IsEraserEnabled = false;
    Vector3 m_TargetOffset = new Vector3(-.5f, -.25f, 1.5f);

    void Start()
    {
        Debug.Log("Can I see this in Android Logcat");
        
        m_OnboardingGoals = new Queue<Goal>();
        var welcomeGoal = new Goal(OnboardingGoals.Empty);
        var findSurfaceGoal = new Goal(OnboardingGoals.FindSurfaces);
        var tapSurfaceGoal = new Goal(OnboardingGoals.TapSurface);
        var cleanSurfaceGoal = new Goal(OnboardingGoals.CleanSurface);
        //var endGoal = new Goal(OnboardingGoals.Empty);

        m_OnboardingGoals.Enqueue(welcomeGoal);
        m_OnboardingGoals.Enqueue(findSurfaceGoal);
        m_OnboardingGoals.Enqueue(tapSurfaceGoal);
        m_OnboardingGoals.Enqueue(cleanSurfaceGoal);
        //m_OnboardingGoals.Enqueue(endGoal);

        m_CurrentGoal = m_OnboardingGoals.Dequeue();
        m_MainCamera = Camera.main;
        if (m_TapTooltip != null)
            m_TapTooltip.SetActive(false);

        if (m_FadeMaterial != null)
        {
            m_FadeMaterial.FadeSkybox(false);

            if (m_PassthroughToggle != null)
                m_PassthroughToggle.isOn = false;
        }

        if (m_LearnButton != null)
        {
            m_LearnButton.GetComponent<Button>().onClick.AddListener(OpenModal); ;
            m_LearnButton.SetActive(false);
        }

        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.zero;
        }

        if (m_LearnModalButton != null)
        {
            m_LearnModalButton.onClick.AddListener(CloseModal);
        }

        
    
    }

    void OpenModal()
    {
        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.one;
        }
    }

    void CloseModal()
    {
        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.zero;
        }
    }

    void Update()
    {
        if (!m_AllGoalsFinished)
        {
            Debug.Log($"Current Goal: " + m_CurrentGoal);
            ProcessGoals();
        }

        // Debug Input
#if UNITY_EDITOR
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CompleteGoal();
        }
#endif
    }

    void ProcessGoals()
    {
        if (!m_CurrentGoal.Completed)
        {
            switch (m_CurrentGoal.CurrentGoal)
            {
                case OnboardingGoals.Empty:
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                    break;
                case OnboardingGoals.FindSurfaces:
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                    break;
                case OnboardingGoals.TapSurface:
                    if (m_TapTooltip != null)
                    {
                        m_TapTooltip.SetActive(true);
                    }
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.None;
                    m_IsPalmTapEnabled = true;
                    CheckSurfaceTap();
                    break;
                case OnboardingGoals.CleanSurface:
                    if (m_TapTooltip != null)
                    {
                        m_TapTooltip.SetActive(false);
                    }
                    m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.None;
                    m_IsEraserEnabled = true;
                    CleanSurfaceRaycast();
                    break;
            }
        }
    }

    void CompleteGoal()
    {
        // if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
        //     m_ObjectSpawner.objectSpawned -= OnObjectSpawned;

        // disable tooltips before setting next goal
        DisableTooltips();

        m_CurrentGoal.Completed = true;
        m_CurrentGoalIndex++;
        if (m_OnboardingGoals.Count > 0)
        {
            m_CurrentGoal = m_OnboardingGoals.Dequeue();
            m_StepList[m_CurrentGoalIndex - 1].stepObject.SetActive(false);
            m_StepList[m_CurrentGoalIndex].stepObject.SetActive(true);
            m_StepButtonTextField.text = m_StepList[m_CurrentGoalIndex].buttonText;
            m_SkipButton.SetActive(m_StepList[m_CurrentGoalIndex].includeSkipButton);
        }
        else
        {
            m_AllGoalsFinished = true;
            ForceEndAllGoals();
        }

        if (m_CurrentGoal.CurrentGoal == OnboardingGoals.FindSurfaces)
        {
            if (m_FadeMaterial != null)
                m_FadeMaterial.FadeSkybox(true);

            if (m_PassthroughToggle != null)
                m_PassthroughToggle.isOn = true;

            if (m_LearnButton != null)
            {
                m_LearnButton.SetActive(true);
            }

            StartCoroutine(TurnOnPlanes());
        }
        else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
        {
            if (m_LearnButton != null)
            {
                m_LearnButton.SetActive(false);
            }

            // MY CODE
            // Enable palm tap detection
            m_IsPalmTapEnabled = true;
            m_SurfacesTapped = 0;
        }
        else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.CleanSurface)
        {
            if (m_LearnButton != null)
            {
                m_LearnButton.SetActive(false);
            }
            m_IsEraserEnabled = true;
        }
    }
    
    public IEnumerator TurnOnPlanes()
    {
        yield return new WaitForSeconds(1f);
        m_ARPlaneManager.enabled = true;
    }

    void DisableTooltips()
    {
        if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
        {
            if (m_TapTooltip != null)
            {
                m_TapTooltip.SetActive(false);
            }
        }
    }

    public void ForceCompleteGoal()
    {
        CompleteGoal();
    }

    public void ForceEndAllGoals()
    {
        m_CoachingUIParent.transform.localScale = Vector3.zero;

        if (m_FadeMaterial != null)
        {
            m_FadeMaterial.FadeSkybox(true);

            if (m_PassthroughToggle != null)
                m_PassthroughToggle.isOn = true;
        }

        if (m_LearnButton != null)
        {
            m_LearnButton.SetActive(false);
        }

        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.zero;
        }

        StartCoroutine(TurnOnPlanes());
    }

    public void ResetCoaching()
    {
        m_CoachingUIParent.transform.localScale = Vector3.one;

        m_OnboardingGoals.Clear();
        m_OnboardingGoals = new Queue<Goal>();
        var welcomeGoal = new Goal(OnboardingGoals.Empty);
        var findSurfaceGoal = new Goal(OnboardingGoals.FindSurfaces);
        var tapSurfaceGoal = new Goal(OnboardingGoals.TapSurface);
        var cleanSurfaceGoal = new Goal(OnboardingGoals.CleanSurface);
        //var endGoal = new Goal(OnboardingGoals.Empty);

        m_OnboardingGoals.Enqueue(welcomeGoal);
        m_OnboardingGoals.Enqueue(findSurfaceGoal);
        m_OnboardingGoals.Enqueue(tapSurfaceGoal);
        m_OnboardingGoals.Enqueue(cleanSurfaceGoal);
        //m_OnboardingGoals.Enqueue(endGoal);

        for (int i = 0; i < m_StepList.Count; i++)
        {
            if (i == 0)
            {
                m_StepList[i].stepObject.SetActive(true);
                m_SkipButton.SetActive(m_StepList[i].includeSkipButton);
                m_StepButtonTextField.text = m_StepList[i].buttonText;
            }
            else
            {
                m_StepList[i].stepObject.SetActive(false);
            }
        }

        m_CurrentGoal = m_OnboardingGoals.Dequeue();
        m_AllGoalsFinished = false;

        if (m_TapTooltip != null)
            m_TapTooltip.SetActive(false);

        if (m_LearnButton != null)
        {
            m_LearnButton.SetActive(false);
        }

        if (m_LearnModal != null)
        {
            m_LearnModal.transform.localScale = Vector3.zero;
        }

        m_IsPalmTapEnabled = false;
        m_IsEraserEnabled = false;
        m_SurfacesTapped = 0;
        m_CurrentGoalIndex = 0;
    }

    void CheckSurfaceTap()
    {
    // Check if the Palm object is available and palm tap is enabled
    if (m_IsPalmTapEnabled && m_PalmObject != null)
    {
        // Get the position and rotation of the palm
        Vector3 palmPosition = m_PalmObject.transform.position;
        Quaternion palmRotation = m_PalmObject.transform.rotation;

        // Raycast from the palm's position in the direction the palm is facing
        Ray ray = new Ray(palmPosition, palmRotation * Vector3.forward); // TODO: Change to correct calculation
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, m_TapThreshold)) // Raycast with distance limit (m_TapThreshold)
        {
            // Check if the ray hits an ARPlane
            ARPlane arPlane = hit.collider.GetComponent<ARPlane>();

            if (arPlane != null)
            {
                GameObject planeObject = hit.collider.gameObject;

                // If the plane is already selected, toggle back to the original material
                if (m_SelectedPlanes.Contains(planeObject))
                {
                    // Reset to the original material (m_DefaultPlaneMaterial)
                    Renderer planeRenderer = arPlane.GetComponent<Renderer>();
                    if (planeRenderer != null)
                    {
                        planeRenderer.material = m_DefaultPlaneMaterial;  // Revert to the original material
                        m_objectSpawner.DeleteObjects();
                    }

                    // Remove from selected planes
                    m_SelectedPlanes.Remove(planeObject);
                    m_SurfacesTapped--;
                }
                else
                {
                    // If the plane is not selected, change it to the selected material
                    Renderer planeRenderer = arPlane.GetComponent<Renderer>();
                    if (planeRenderer != null)
                    {
                        planeRenderer.material = m_SelectedPlaneMaterial;  // Change to selected material
                        m_objectSpawner.SpawnObjectsOnPlane(planeRenderer, 0);
                    }

                    // Add to the set of selected planes
                    m_SelectedPlanes.Add(planeObject);

                    // Update the number of surfaces tapped
                    m_SurfacesTapped++;

                    // If the required number of surfaces is tapped, complete the goal
                    if (m_SurfacesTapped >= k_NumberOfSurfacesTappedToCompleteGoal)
                    {
                        CompleteGoal();
                    }
                }
            }
        }
    }
}

private Texture2D maskTexture; // Runtime mask texture
private Material planeMaterial; // Duplicated material

public void CleanSurfaceRaycast()
{
    if (m_PalmObject != null && m_IsEraserEnabled)
    {
        // Perform raycast from the palm
        Vector3 palmPosition = m_PalmObject.transform.position;
        Ray ray = new Ray(palmPosition, -m_PalmObject.transform.up);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, m_RaycastThreshold))
        {
            ARPlane arPlane = hit.collider.GetComponent<ARPlane>();
            if (arPlane != null)
            {
                Renderer planeRenderer = arPlane.GetComponent<Renderer>();

                // Ensure texture is correctly mapped
                if (planeRenderer != null)
                {
                    AdjustTextureTiling(planeRenderer, arPlane);

                    if (maskTexture == null)
                    {
                        InitializeEraseMaterial(planeRenderer);
                    }

                    // Erase texture at correct UV coordinates
                    EraseAtPoint(hit.textureCoord, 20, planeRenderer);
                }
            }
        }
    }
}

void EraseAtPoint(Vector2 uv, int radius, Renderer planeRenderer)
{
    Debug.Log($"I can see Erasing at UV: {uv}");

    if (maskTexture == null) return;

    // Correct UV scaling based on texture tiling
    Vector2 tiling = planeRenderer.material.mainTextureScale;
    uv.x *= tiling.x;
    uv.y *= tiling.y;

    // Clamp UV coordinates to ensure they're within range
    uv.x = Mathf.Repeat(uv.x, 1.0f);
    uv.y = Mathf.Repeat(uv.y, 1.0f);

    // Convert UV to texture pixel coordinates
    int hitX = Mathf.FloorToInt(uv.x * maskTexture.width);
    int hitY = Mathf.FloorToInt(uv.y * maskTexture.height);

    for (int x = -radius; x <= radius; x++)
    {
        for (int y = -radius; y <= radius; y++)
        {
            if (x * x + y * y <= radius * radius)
            {
                int px = Mathf.Clamp(hitX + x, 0, maskTexture.width - 1);
                int py = Mathf.Clamp(hitY + y, 0, maskTexture.height - 1);
                maskTexture.SetPixel(px, py, new Color(0, 0, 0, 0)); // Transparent
            }
        }
    }

    maskTexture.Apply();

    Debug.Log($"I can see Erased pixels around UV: ({hitX}, {hitY})");
}

void InitializeEraseMaterial(Renderer planeRenderer)
{
    if (planeMaterial == null)
    {
        planeMaterial = new Material(Shader.Find("Custom/EraseTexture"));
        planeMaterial.SetTexture("_MainTex", planeRenderer.material.mainTexture);
        planeRenderer.material = planeMaterial;

        int texWidth = 1024; // Higher resolution for better erasure
        int texHeight = 1024;
        maskTexture = new Texture2D(texWidth, texHeight, TextureFormat.ARGB32, false);

        // Initialize all pixels to white (fully opaque)
        Color[] pixels = new Color[texWidth * texHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        maskTexture.SetPixels(pixels);
        maskTexture.Apply();

        planeMaterial.SetTexture("_MaskTex", maskTexture);
    }
}

void AdjustTextureTiling(Renderer planeRenderer, ARPlane arPlane)
{
    // Calculate tiling based on the plane's dimensions
    Vector2 planeSize = arPlane.size;
    planeRenderer.material.mainTextureScale = planeSize;
}

public void HandleMaterialSelection()
{
    Material selectedMaterial = null;

    switch(m_materialSelector.value)
    {
        case 0:
            selectedMaterial = m_SelectedPlaneMaterial;
            break;
        case 1:
            selectedMaterial = m_IceMaterial;
            break;
        case 2:
            selectedMaterial = m_FarmMaterial;
            break;
        case 3:
            selectedMaterial = m_StoneMaterial;
            break;
    }

    if (selectedMaterial != null)
    {
        //planeMaterial = new Material(selectedMaterial);
        
        // If you have any currently selected planes, update their materials
        foreach (GameObject plane in m_SelectedPlanes)
        {
            Renderer planeRenderer = plane.GetComponent<Renderer>();
            if (planeRenderer != null)
            {
                planeRenderer.material = selectedMaterial;
                AdjustTextureTiling(planeRenderer, plane.GetComponent<ARPlane>());
                m_objectSpawner.SpawnObjectsOnPlane(planeRenderer, m_materialSelector.value);
                InitializeEraseMaterial(planeRenderer);
            }
        }
    }
}

}