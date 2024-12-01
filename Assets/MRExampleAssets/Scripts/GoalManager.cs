using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;
using LazyFollow = UnityEngine.XR.Interaction.Toolkit.UI.LazyFollow;
using UnityEngine.XR.Hands;

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

    // MY OWN CODE
    // [SerializeField]
    // LayerMask m_PlaneLayerMask;

    // [SerializeField]
    // PlaneColorizer m_PlaneColoriser;

    [SerializeField]
    GameObject m_PalmObject;
    
    [SerializeField]
    Material m_DefaultPlaneMaterial;  // Original plane material from ARPlaneManager

    [SerializeField]
    Material m_SelectedPlaneMaterial;  // New material for selected planes

    [SerializeField]
    float m_TapThreshold = 0.05f;  // Distance threshold for tap detection

    private HashSet<GameObject> m_SelectedPlanes = new HashSet<GameObject>();

    private int m_SurfacesTapped = 0;
    private Camera m_MainCamera;
    private bool m_IsPalmTapEnabled = false;
    const int k_NumberOfSurfacesTappedToCompleteGoal = 1;
    Vector3 m_TargetOffset = new Vector3(-.5f, -.25f, 1.5f);

    void Start()
    {
        m_OnboardingGoals = new Queue<Goal>();
        var welcomeGoal = new Goal(OnboardingGoals.Empty);
        var findSurfaceGoal = new Goal(OnboardingGoals.FindSurfaces);
        var tapSurfaceGoal = new Goal(OnboardingGoals.TapSurface);
        var endGoal = new Goal(OnboardingGoals.Empty);

        m_OnboardingGoals.Enqueue(welcomeGoal);
        m_OnboardingGoals.Enqueue(findSurfaceGoal);
        m_OnboardingGoals.Enqueue(tapSurfaceGoal);
        m_OnboardingGoals.Enqueue(endGoal);

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
                    // TODO: Call whatever functions to process tapping of surface
                    // Enable hand tracking for surface interaction
                    m_IsPalmTapEnabled = true;
                    CheckSurfaceTap();
                    break;
                // TODO: Add cases to start CleanSurface goal
                    // TODO: Call whatever functions to "clean" (remove texture color at pixels)
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
            
            // if (m_PlaneColoriser != null)
            // {
            //     m_PlaneColoriser.enabled = true;
            //     m_PlaneColoriser.ResetColorization();
            // }
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
        var endGoal = new Goal(OnboardingGoals.Empty);

        m_OnboardingGoals.Enqueue(welcomeGoal);
        m_OnboardingGoals.Enqueue(findSurfaceGoal);
        m_OnboardingGoals.Enqueue(tapSurfaceGoal);
        m_OnboardingGoals.Enqueue(endGoal);

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
        m_SurfacesTapped = 0;
        m_CurrentGoalIndex = 0;
    }

    // void OnObjectSpawned(GameObject spawnedObject)
    // {
    //     m_SurfacesTapped++;
    //     if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface && m_SurfacesTapped >= k_NumberOfSurfacesTappedToCompleteGoal)
    //     {
    //         CompleteGoal();
    //         m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
    //     }
    // }   

    // TODO: Add all your helpers here
//     void CheckSurfaceTap()
// {
//     // Check if the Palm object is available and palm tap is enabled
//     if (m_IsPalmTapEnabled && m_PalmObject != null)
//     {
//         // Get the position and rotation of the palm
//         Vector3 palmPosition = m_PalmObject.transform.position;
//         Quaternion palmRotation = m_PalmObject.transform.rotation;

//         // Raycast from the palm's position in the direction the palm is facing
//         Ray ray = new Ray(palmPosition, palmRotation * Vector3.forward);
//         RaycastHit hit;

//         if (Physics.Raycast(ray, out hit, m_TapThreshold)) // Raycast with distance limit (m_TapThreshold)
//         {
//             // Check if the ray hits an ARPlane
//             ARPlane arPlane = hit.collider.GetComponent<ARPlane>();

//             if (arPlane != null && !m_SelectedPlanes.Contains(hit.collider.gameObject))
//             {
//                 // Check if the hit point is within the threshold distance from the palm
//                 float distanceToHit = Vector3.Distance(palmPosition, hit.point);
                
//                 if (distanceToHit <= m_TapThreshold)
//                 {
//                     // Change the material of the ARPlane to indicate selection
//                     Renderer planeRenderer = arPlane.GetComponent<Renderer>();
//                     if (planeRenderer != null)
//                     {
//                         planeRenderer.material = m_SelectedPlaneMaterial;
//                     }

//                     // Add to the set of selected planes
//                     m_SelectedPlanes.Add(hit.collider.gameObject);

//                     // Update the number of surfaces tapped
//                     m_SurfacesTapped++;

//                     // If the required number of surfaces is tapped, complete the goal
//                     if (m_SurfacesTapped >= k_NumberOfSurfacesTappedToCompleteGoal)
//                     {
//                         CompleteGoal();
//                     }
//                 }
//             }
//         }
//     }
// }

    void CheckSurfaceTap()
{
    // Check if the Palm object is available and palm tap is enabled
    if (m_IsPalmTapEnabled && m_PalmObject != null)
    {
        // Get the position and rotation of the palm
        Vector3 palmPosition = m_PalmObject.transform.position;
        Quaternion palmRotation = m_PalmObject.transform.rotation;

        // Raycast from the palm's position in the direction the palm is facing
        Ray ray = new Ray(palmPosition, palmRotation * Vector3.forward);
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





    
}
