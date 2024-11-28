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
    [SerializeField]
    XRHandSubsystem m_HandSubsystem;

    [SerializeField]
    LayerMask m_PlaneLayerMask;

    [SerializeField]
    PlaneColorizer m_PlaneColoriser;

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

        if (m_HandSubsystem == null)
        {
            SubsystemManager.GetSubsystems(new List<XRHandSubsystem>());
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
                    CheckTap();
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
            
            if (m_PlaneColoriser != null)
            {
                m_PlaneColoriser.enabled = true;
                m_PlaneColoriser.ResetColorization();
            }
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

    // MY CODE
    void CheckTap()
    {
        if (!m_IsPalmTapEnabled || m_HandSubsystem == null || !m_HandSubsystem.running)
            return;

        // Get both hands
        XRHand leftHand = m_HandSubsystem.leftHand;
        XRHand rightHand = m_HandSubsystem.rightHand;

        // Check each hand for palm tap
        CheckHandPalmTap(leftHand);
        CheckHandPalmTap(rightHand);
    }

    // void CheckHandPalmTap(XRHand hand)
    // {
    //     if (!hand.isTracked)
    //         return;

    //     // Get palm position
    //     Vector3 palmPosition = hand.palm.position;
        
    //     // Cast ray from palm position in the forward direction of the camera
    //     Ray ray = new Ray(palmPosition, m_MainCamera.transform.forward);
    //     RaycastHit hit;

    //     if (Physics.Raycast(ray, out hit, 2.0f, m_PlaneLayerMask))
    //     {
    //         // Check if palm is close enough to the plane (tap threshold)
    //         float palmDistance = Vector3.Distance(palmPosition, hit.point);
    //         if (palmDistance < 0.05f) // 5cm threshold for tap
    //         {
    //             // Get the ARPlane component
    //             var planeObject = hit.collider.gameObject;
    //             if (planeObject != null)
    //             {
    //                 // Colorize the plane
    //                 if (m_PlaneColoriser != null)
    //                 {
    //                     m_PlaneColoriser.ColorizePlane(planeObject);
    //                 }
                    
    //                 // Increment tapped counter and check for goal completion
    //                 m_SurfacesTapped++;
    //                 if (m_SurfacesTapped >= k_NumberOfSurfacesTappedToCompleteGoal)
    //                 {
    //                     m_IsPalmTapEnabled = false; // Disable palm tap detection
    //                     CompleteGoal();
    //                     m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
    //                 }
    //             }
    //         }
    //     }
    // }

    void CheckHandPalmTap(XRHand hand)
    {
        if (!hand.isTracked)
            return;

        // Get palm position - corrected joint access
        XRHandJoint palmJoint;
        Vector3 palmPosition = palmJoint.;
        
        // Cast ray from palm position in the forward direction of the camera
        Ray ray = new Ray(palmPosition, m_MainCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 2.0f, m_PlaneLayerMask))
        {
            // Check if palm is close enough to the plane (tap threshold)
            float palmDistance = Vector3.Distance(palmPosition, hit.point);
            if (palmDistance < 0.05f) // 5cm threshold for tap
            {
                // Get the ARPlane component
                var planeObject = hit.collider.gameObject;
                if (planeObject != null)
                {
                    // Colorize the plane
                    if (m_PlaneColorizer != null)  // Fixed spelling
                    {
                        m_PlaneColorizer.ColorizePlane(planeObject);  // Fixed spelling
                    }
                    
                    // Increment tapped counter and check for goal completion
                    m_SurfacesTapped++;
                    if (m_SurfacesTapped >= k_NumberOfSurfacesTappedToCompleteGoal)
                    {
                        m_IsPalmTapEnabled = false; // Disable palm tap detection
                        CompleteGoal();
                        m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                    }
                }
            }
        }

}
