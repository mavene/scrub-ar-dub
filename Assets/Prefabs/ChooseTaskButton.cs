using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseTaskButton : MonoBehaviour
{
    [Header("UI References")]
    public GameObject taskCanvas;            // Reference to the Canvas with buttons
    // public Slider progressBar;               // Reference to the progress bar
    public Image taskNameImage;              // Image to display the task name
    public Sprite[] taskSprites;             // Array of sprites for each task name

    [Header("Task Duration")]
    public float taskDuration = 5f;          // Time for progress bar to fill

    void Start()
    {
        // Hide the progress bar and task name at the start
        //progressBar.gameObject.SetActive(false);
        taskNameImage.gameObject.SetActive(false);
    }

    // Show Task UI after plane selection
    public void ShowTaskUI()
    {
        taskCanvas.SetActive(true);
    }

    // Called when a button is clicked; taskIndex corresponds to the sprite
    public void StartTask(int taskIndex)
    {
        if (taskIndex < 0 || taskIndex >= taskSprites.Length)
        {
            Debug.LogError("Invalid task index!");
            return;
        }

        Debug.Log($"Task Started: {taskSprites[taskIndex].name}");

        // Update task name image
        taskNameImage.sprite = taskSprites[taskIndex];
        taskNameImage.gameObject.SetActive(true);

        // Show and reset the progress bar
        // progressBar.value = 0;
        //progressBar.gameObject.SetActive(true);

        // Start the progress coroutine
        // StartCoroutine(FillProgressBar(taskSprites[taskIndex].name));
    }
}

//     IEnumerator FillProgressBar(string taskName) // this script to modify when prog bar data is available
//     {
//         float elapsedTime = 0f;

//         while (elapsedTime < taskDuration)
//         {
//             elapsedTime += Time.deltaTime;
//             progressBar.value = Mathf.Lerp(0, 100, elapsedTime / taskDuration);
//             yield return null;
//         }

//         Debug.Log($"Task Completed: {taskName}");

//         // Hide progress bar and task name after completion
//         progressBar.gameObject.SetActive(false);
//         taskNameImage.gameObject.SetActive(false);
//     }
// }
