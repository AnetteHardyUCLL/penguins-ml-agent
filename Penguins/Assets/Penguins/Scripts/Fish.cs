using UnityEngine;

// This script makes the fish swim 
public class Fish: MonoBehaviour
{
    [Tooltip("fish swim speed")]
    public float fishSpeed;

    private float randomizedSpeed = 0f;
    private float nextActionTime = -1f;
    private Vector3 targetPosition;

    // called every timestep
    private void FixedUpdate()
    {
        if (fishSpeed > 0f)
        {
            Swim();
        }
    }

    // at any given update the fish will either pick a new speed and destination, or move toward its current destination
    private void Swim()
    {
        if(Time.fixedTime >= nextActionTime)
        {
            randomizedSpeed = fishSpeed * UnityEngine.Random.Range(.5f, 1.5f);
            targetPosition = PenguinArea.ChooseRandomPosition(transform.parent.position, 100f, 260f, 2f, 13f);
            transform.rotation = Quaternion.LookRotation(targetPosition - transform.position, Vector3.up);
            
            float timeToGetThere = Vector3.Distance(transform.position, targetPosition) / randomizedSpeed;
            
            nextActionTime = Time.fixedTime + timeToGetThere;
        }
        else
        {
            // make sure fish doesn't swim past the target destination
            Vector3 moveVector = randomizedSpeed * transform.forward * Time.fixedDeltaTime;
            if (moveVector.magnitude <= Vector3.Distance(transform.position, targetPosition))
            {
                transform.position += moveVector;
            }
            else
            {
                transform.position = targetPosition;
                
                nextActionTime = Time.fixedTime;
            }
        }
    }
}
