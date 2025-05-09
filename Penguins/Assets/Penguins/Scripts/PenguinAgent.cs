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
    public float parentDrainPerSecond = 0.01f;
    #endregion

    #region Private State
    private bool isCarryingFish = false;
    private float carryTimer = 0f;
    private bool feedRequested = false;

    private Rigidbody rb;
    private GameObject baby;
    private BabyHunger babyHunger;
    private PenguinArea penguinArea;
    #endregion

    #region ML-Agents Overrides
    public override void Initialize()
    {
        base.Initialize();

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
        feedRequested = false;
        // baby
        babyHunger.hungerLevel = 1f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 10 floats total:
        sensor.AddObservation(isCarryingFish ? 1f : 0f);      // 1
        sensor.AddObservation(Vector3.Distance(baby.transform.position, transform.position));   // 1
        sensor.AddObservation((baby.transform.position - transform.position).normalized);       // 3
        sensor.AddObservation(transform.forward);             // 3
        sensor.AddObservation(hungerLevel);                   // 1
        sensor.AddObservation(babyHunger.hungerLevel);        // 1
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var prevDist = Vector3.Distance(transform.position, baby.transform.position);
        HandleMovement(actions); // 0: forward {0,1}, 1: turn {0,left,right}, 2: eatSelf {0,1}, 3: feedBaby {0,1}
        var newDist = Vector3.Distance(transform.position, baby.transform.position);

        if (isCarryingFish)
        {
            AddReward((prevDist - newDist) * 0.1f);
        }

        DrainHungerAndApplyPenalties();
        HandleEatAction(actions.DiscreteActions[2]);
        HandleFeedAction(actions.DiscreteActions[3]);

        Debug.Log(
            $"[Parent] Hunger={hungerLevel:F2} | " +
            $"[Baby] Hunger={babyHunger.hungerLevel:F2} | " +
            $"CarryingFish={isCarryingFish}"
        );
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
        else if (collision.transform.CompareTag("baby")
             && isCarryingFish
             && feedRequested)
        {
            RegurgitateFish();
            feedRequested = false;
        }
    }
    #endregion

    #region Private Helpers
    private void HandleMovement(ActionBuffers actions)
    {
        float forward = actions.DiscreteActions[0]; // convert 1st action to forward

        int turn = actions.DiscreteActions[1]; // convert 2nd action to turning left or right
        float turnAmt = turn == 1 ? -1f : turn == 2 ? 1f : 0f;

        // apply movement
        rb.MovePosition(transform.position + transform.forward * forward * moveSpeed * Time.fixedDeltaTime);
        transform.Rotate(transform.up * turnAmt * turnSpeed * Time.fixedDeltaTime);

        // Step penalty to encourage efficiency
        if (MaxStep > 0) AddReward(-1f / MaxStep);
    }

    private void DrainHungerAndApplyPenalties()
    {
        // 1) Drain
        hungerLevel = Mathf.Max(0f, hungerLevel - parentDrainPerSecond * Time.deltaTime);

        // 2) Death: If parent or baby starves - negative terminal reward & end episode
        if (hungerLevel <= 0f || babyHunger.hungerLevel <= 0f)
        {
            AddReward(-1f);
            EndEpisode();
            return;
        }

        // 3) Continuous urgency penalty
        float parentUrgency = 1f - hungerLevel;
        float babyUrgency = 1f - babyHunger.hungerLevel;
        AddReward(-(0.5f * parentUrgency + 1.0f * babyUrgency) * Time.deltaTime);

        // 4) if carrying a fish, penalize holding it
        if (isCarryingFish)
        {
            carryTimer += Time.deltaTime;
            AddReward(-0.1f * Time.deltaTime);  // -0.1 per second carrying
        }
        else
        {
            carryTimer = 0f;
        }
    }

    private void HandleEatAction(int eatAction)
    {
        if (eatAction != 1 || !isCarryingFish) return;

        isCarryingFish = false;
        feedRequested = false;
        hungerLevel = 1f;
        AddReward(-0.5f);
    }

    private void HandleFeedAction(int feedAction)
    {
        if (feedAction != 1 || !isCarryingFish) return;

        feedRequested = true;
    }

    private void EatFish(GameObject fishObject)
    {
        if (isCarryingFish) return;
        isCarryingFish = true;
        AddReward(1f);         // Encourage pickup
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
        if (penguinArea.FishRemaining <= 0) EndEpisode();
    }
    #endregion
}
