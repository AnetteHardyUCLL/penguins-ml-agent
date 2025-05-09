using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

// This script handles observing the environment, taking action, interacting and accepting player input

// “ A good parent doesn’t eat unless they need it to survive <3 ”
public class PenguinAgent : Agent
{
    #region Inspector Fields
    [Header("Movement")]
    public float moveSpeed = 5f; // how fast the agent moves forward
    public float turnSpeed = 180f; // how fast the agent turns

    [Header("Prefabs")]
    public GameObject heartPrefab;
    public GameObject regurgitatedFishPrefab;

    [Header("Hunger Settings")]
    [Range(0f, 1f)]
    public float hungerLevel = 1f; // 1 = full, 0 = starving
    public float parentDrainPerSecond = 0.0002f;

    [Header("Feed Settings")]
    public float feedRange = 1.5f;

    [Header("Observation Settings")]
    [Tooltip("Must match the RayPerceptionSensor3D Ray Length.")]
    public float maxSightDistance = 20f;
    #endregion

    #region Private State
    private bool isCarryingFish = false;

    private Rigidbody rb;
    private GameObject baby;
    private BabyHunger babyHunger;
    private PenguinArea penguinArea;

    private int feedSuccessCount = 0; // for debugging 
    #endregion

    #region ML-Agents Overrides
    public override void Initialize()
    {
        penguinArea = GetComponentInParent<PenguinArea>();
        baby = penguinArea.penguinBaby;
        babyHunger = baby.GetComponent<BabyHunger>();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        penguinArea.ResetArea();

        // parent penguin
        hungerLevel = 1f;
        isCarryingFish = false;

        // baby
        babyHunger.hungerLevel = 1f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        // Carry / feed flags
        sensor.AddObservation(isCarryingFish ? 1f : 0f);

        // Baby: distance (normalized) + local direction
        Vector3 toBaby = baby.transform.position - transform.position;
        float dist = toBaby.magnitude / maxSightDistance;
        Vector3 dir = transform.InverseTransformDirection(toBaby).normalized;
        sensor.AddObservation(dist);
        sensor.AddObservation(dir);

        // [RayPerceptionSensor3D auto-appends its ray floats]

        // Hunger levels
        sensor.AddObservation(hungerLevel);
        sensor.AddObservation(babyHunger.hungerLevel);

        // (Total floats = 2 flags + 1 dist + 3 dir + N rays + 2 hungers)
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Movement
        int forwardAction = actions.DiscreteActions[0]; // 0 or 1
        int turnAction = actions.DiscreteActions[1]; // 0,1,2
        float forwardAmt = forwardAction;
        float turnAmt = (turnAction == 1 ? -1f : turnAction == 2 ? 1f : 0f);

        Vector3 newPos = transform.position + transform.forward * forwardAmt * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
        transform.Rotate(transform.up * turnAmt * turnSpeed * Time.fixedDeltaTime);

        // Step penalty
        if (MaxStep > 0)
        {
            AddReward(-1f / MaxStep);
        }

        // Approach reward when carrying
        if (isCarryingFish)
        {
            float prevDist = Vector3.Distance(transform.position, baby.transform.position);
            float newDist = Vector3.Distance(newPos, baby.transform.position);
            float approach = (prevDist - newDist) * 0.1f;
            AddReward(approach);
        }

        // Hunger / penalties
        DrainHungerAndApplyPenalties();

        // Eat / Feed
        bool eatSelf = (actions.DiscreteActions[2] == 1);
        bool feedBaby = (actions.DiscreteActions[3] == 1);

        // Self-eat
        if (eatSelf && isCarryingFish)
        {
            isCarryingFish = false;
            hungerLevel = 1f;
            AddReward(-0.5f);
        }

        // Feed‐baby
        if (feedBaby)
        {
            float distToBaby = Vector3.Distance(transform.position, baby.transform.position);

            if (isCarryingFish)
            {
                if (distToBaby <= feedRange)
                {
                    AddReward(0.05f);   
                    RegurgitateFish();   
                }
                else
                {
                    // penalize spamming Feed when too far away
                    AddReward(-0.05f);
                }
            }
            else
            {
                // penalize pressing Feed when parent has no fish
                AddReward(-0.1f);
            }
        }

        // Debug
        if (StepCount % 50000 == 0 && StepCount > 0)
        {
            Debug.Log($"[Training] Step {StepCount}: feedSuccessCount = {feedSuccessCount}");
        }

        Debug.Log($"[Parent] Hunger={hungerLevel:F2} | [Baby] Hunger={babyHunger.hungerLevel:F2} | Carrying={isCarryingFish}");
    }

    // Manual control for debugging: set behavior Type to "Heuristic Only" in the Behavior Parameters inspector
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = Input.GetKey(KeyCode.W) ? 1 : 0; // move forward
        d[1] = Input.GetKey(KeyCode.A) ? 1 : Input.GetKey(KeyCode.D) ? 2 : 0; // turn
        d[2] = Input.GetKey(KeyCode.Space) ? 1 : 0; // eat
        d[3] = Input.GetKey(KeyCode.F) ? 1 : 0;  // feed baby 
    }
    #endregion

    #region Collision
    // when agent collides with something, take action
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("fish"))
        {
            EatFish(collision.gameObject);
        }
        //else if (collision.transform.CompareTag("baby")
        //     && isCarryingFish)
        //{
        //    RegurgitateFish();
        //}
    }
    #endregion

    #region Private Helpers
    private void DrainHungerAndApplyPenalties()
    {
        // 1) Drain parent hunger
        hungerLevel = Mathf.Max(0f, hungerLevel - parentDrainPerSecond * Time.deltaTime);

        // 2) Death: If parent or baby starves - negative terminal reward & end episode
        if (hungerLevel <= 0f || babyHunger.hungerLevel <= 0f)
        {
            AddReward(-1f);
            EndEpisode();
            return;
        }

        // Small per-step hunger penalty
        AddReward(-0.0001f);
    }

    private void EatFish(GameObject fishObject)
    {
        if (isCarryingFish) return;
        isCarryingFish = true;
        AddReward(1f); // encourage pickup
        penguinArea.RemoveFishAfterEaten(fishObject);
    }

    private void RegurgitateFish()
    {
        isCarryingFish = false;
        babyHunger.Feed();

        var rf = Instantiate(regurgitatedFishPrefab,
                             baby.transform.position,
                             Quaternion.identity,
                             transform.parent);
        Destroy(rf, 4f);

        var heart = Instantiate(heartPrefab,
                                baby.transform.position + Vector3.up,
                                Quaternion.identity,
                                transform.parent);
        Destroy(heart, 4f);

        AddReward(10f);
        feedSuccessCount++;

        if (penguinArea.FishRemaining <= 0)
        {
            EndEpisode();
        }
    }
    #endregion
}
