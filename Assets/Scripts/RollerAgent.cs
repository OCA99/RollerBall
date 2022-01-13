using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

using UnityEngine.UI;

public class RollerAgent : Agent
{
    Rigidbody rBody;

    Vector3 lastPosition;
    int lastPositionTime;

    public Text frameCounter;
    public Text cellCounter;

    HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int> ();
    Vector2Int lastCell;

    float reward = 0.0f;

    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        frameCounter.text = (Time.frameCount - lastPositionTime).ToString();
        cellCounter.text = visitedCells.Count.ToString();

        if (lastCell != mazeGenerator.PositionToCell(transform.localPosition))
        {
            reward -= 0.04f;
            lastCell = mazeGenerator.PositionToCell(transform.localPosition);
            if (!visitedCells.Contains(lastCell))
            {
                visitedCells.Add(lastCell);
            } else
            {
                reward -= 0.25f;
            }
        }
    }

    public Transform target;
    public MazeGenerator mazeGenerator;

    public override void OnEpisodeBegin()
    {
        // If the Agent fell, zero its momentum
        if (this.transform.localPosition.y < 0)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }

        mazeGenerator.ClearMaze();
        mazeGenerator.GenerateMaze();

        this.transform.localPosition = mazeGenerator.GetStartPosition();
        target.localPosition = mazeGenerator.GetTargetPosition();
        lastPosition = this.transform.localPosition;
        lastPositionTime = Time.frameCount;

        lastCell = mazeGenerator.PositionToCell(transform.localPosition);
        visitedCells.Clear();

        reward = 0.0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(target.localPosition);
        sensor.AddObservation(this.transform.localPosition);

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);

        sensor.AddObservation(Time.frameCount - lastPositionTime);

        RaycastHit hit;
        if (Physics.Raycast(transform.localPosition, Vector3.forward, out hit) && hit.collider.gameObject.tag == "wall")
            sensor.AddObservation(hit.distance);
        else
            sensor.AddObservation(-1);

        if (Physics.Raycast(transform.localPosition, -Vector3.forward, out hit) && hit.collider.gameObject.tag == "wall")
            sensor.AddObservation(hit.distance);
        else
            sensor.AddObservation(-1);

        if (Physics.Raycast(transform.localPosition, Vector3.right, out hit) && hit.collider.gameObject.tag == "wall")
            sensor.AddObservation(hit.distance);
        else
            sensor.AddObservation(-1);

        if (Physics.Raycast(transform.localPosition, -Vector3.right, out hit) && hit.collider.gameObject.tag == "wall")
            sensor.AddObservation(hit.distance);
        else
            sensor.AddObservation(-1);
    }

    public float forceMultiplier = 10;

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        rBody.AddForce(controlSignal * forceMultiplier);

        SetReward(reward);

        // Rewards
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, target.localPosition);
        float distanceToTargetReward = ((mazeGenerator.mazeSize * mazeGenerator.cellSize) / distanceToTarget) / (mazeGenerator.mazeSize * mazeGenerator.cellSize) * 10f;

        // Reached target
        if (distanceToTarget < 3f)
        {
            SetReward(50.0f);
            EndEpisode();
        }

        // Fell off platform
        else if (this.transform.localPosition.y < 0)
        {
            SetReward(-10.0f);
            reward = 0;
            reward += visitedCells.Count / (mazeGenerator.mazeSize);
            AddReward(reward);
            EndEpisode();
        }

        if (Vector3.Distance(lastPosition, transform.localPosition) < 2 && Time.frameCount - lastPositionTime > 50)
        {
            SetReward(-10.0f);
            reward = 0;
            reward += visitedCells.Count / (mazeGenerator.mazeSize) * 5;
            AddReward(reward);
            EndEpisode();
        } else if(Vector3.Distance(lastPosition, transform.localPosition) > 2)
        {
            lastPosition = transform.localPosition;
            lastPositionTime = Time.frameCount;
        }

        if (reward < -0.5f * mazeGenerator.mazeSize)
        {
            reward = 0;
            reward += visitedCells.Count / (mazeGenerator.mazeSize) * 5;
            AddReward(reward);
            //AddReward(distanceToTargetReward);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.other.tag == "wall")
        {
            reward -= 0.75f;
        }
    }
}
