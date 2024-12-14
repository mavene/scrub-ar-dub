// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class ARPlaneChanger : MonoBehaviour
// {
//     public Material[] mats; // Array of materials
//     private int pos = 0;
//     public GoalManager goalManager; // Reference to GoalManager
//     public MeshRenderer currentGroundPlane; 
//     public List<GameObject> m_SelectedPlanes = new List<GameObject>();

//     [Header("Object Spawner")]
//     public RandomObjectSpawner objectSpawner; // Reference to RandomObjectSpawner

//     private Vector2 tiling = new Vector2(5, 5);

//     public void SetPosTo(int newPos)
//     {
//         pos = newPos;

//         MeshRenderer rend = m_SelectedPlanes[0].GetComponent<MeshRenderer>();
//         //MeshRenderer[] rends = transform.GetComponentsInChildren<MeshRenderer>();

//         // for (int i = 0; i < rends.Length; i++)
//         // {
//         //     rends[i].material = mats[pos];
//         // }

//         rend.material = mats[pos];
//         rend.material.mainTextureScale = tiling;

//         // currentGroundPlane.material = mats[pos];
//         // currentGroundPlane.material.mainTextureScale = tiling;

//         // Notify GoalManager that the material has been changed
//         if (goalManager != null)
//         {
//             goalManager.OnMaterialSelected();
//         }

//         // Trigger the object spawner to spawn objects linked to the new texture
//         if (objectSpawner != null)
//         {
//             objectSpawner.SpawnObjectsOnPlane();
//         }
//         else
//         {
//             Debug.LogError("Object Spawner is not assigned!");
//         }
//     }
// }
