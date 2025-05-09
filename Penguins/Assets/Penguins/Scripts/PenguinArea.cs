using System.Collections.Generic;
using UnityEngine;
using TMPro;

//This script manages a training area with 1 penguin, one baby, and multiple fish. 
//It removes fish, spawns fish, and place the penguins randomly
public class PenguinArea : MonoBehaviour
{
    [Tooltip("agent inside the area")]
    public PenguinAgent penguinAgent;

    [Tooltip("baby penguin inside the area")]
    public GameObject penguinBaby;

    [Tooltip("TextMeshPro text that shows the cumulative reward of the ml agent")]
    public TextMeshPro cumulativeRewardText;

    [Tooltip("prefab of a live fish")]
    public Fish fishPrefab;

    private List<GameObject> fishList;

    // resets area
    public void ResetArea()
    {
        RemoveAllFish();
        RandomPlacePenguin();
        RandomPlaceBaby();
        SpawnFish(4, .5f);
    }

    // remove fish when it is eaten
    public void RemoveFishAfterEaten(GameObject fishObject)
    {
        fishList.Remove(fishObject);
        Destroy(fishObject);
    }

    // number of fish remaining
    public int FishRemaining
    {
        get { return fishList.Count; }
    }

    // choose random position on x-z plane within set limits (donut shape)
    // parameters:
    // center -> center of the donut
    // minAngle -> min angle of the wedge
    // maxAngle -> max angle of the wedge
    // minRadius -> min distance to the center
    // maxRadius -> max distance to the center
    // returns a position within a specified region
    public static Vector3 ChooseRandomPosition(Vector3 center, float minAngle, float maxAngle, float minRadius, float maxRadius) {
        float radius = minRadius;
        float angle = minAngle;

        if (maxRadius > minRadius)
        {
            radius = UnityEngine.Random.Range(minRadius, maxRadius);
        }

        if (maxAngle > minAngle)
        {
            angle = UnityEngine.Random.Range(minAngle, maxAngle);
        }

        return center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
    }

    // remove all fish from the area
    private void RemoveAllFish()
    {
        if (fishList != null)
        {
            for (int i = 0; i < fishList.Count; i++)
            {
                if (fishList[i] != null)
                {
                    Destroy(fishList[i]);
                }
            }
        }

        fishList = new List<GameObject>();
    }

    // place penguin randomly in the area
    private void RandomPlacePenguin()
    {
        Rigidbody rigidbody = penguinAgent.GetComponent<Rigidbody>();
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        penguinAgent.transform.position = ChooseRandomPosition(transform.position, 0f, 360f, 0f, 9f) + Vector3.up * .5f;
        penguinAgent.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
    }

    // place baby randomly in the area
    private void RandomPlaceBaby()
    {
        Rigidbody rigidbody = penguinBaby.GetComponent<Rigidbody>();
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        penguinBaby.transform.position = ChooseRandomPosition(transform.position, -45f, 45f, 4f, 9f) + Vector3.up * .5f;
        penguinBaby.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    // spawn fish in the area and set swim speed
    private void SpawnFish(int count, float fishSpeed)
    {
        for (int i = 0; i < count; i++)
        {
            // Spawn and place the fish
            GameObject fishObject = Instantiate<GameObject>(fishPrefab.gameObject);
            fishObject.transform.position = ChooseRandomPosition(transform.position, 100f, 260f, 2f, 13f) + Vector3.up * .5f;
            fishObject.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            // Set the fish's parent to this area's transform
            fishObject.transform.SetParent(transform);

            // Keep track of the fish
            fishList.Add(fishObject);

            // Set the fish speed
            fishObject.GetComponent<Fish>().fishSpeed = fishSpeed;
        }
    }

    private void Start()
    {
        ResetArea();
    }

    private void Update()
    {
        // update cumulative reward text
        cumulativeRewardText.text = penguinAgent.GetCumulativeReward().ToString("0.00");
    }

}
