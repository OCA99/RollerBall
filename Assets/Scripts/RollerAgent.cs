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

    HashSet<MazeGenerator.Cell> visitedCells = new HashSet<MazeGenerator.Cell> ();
    MazeGenerator.Cell lastCell;

    Vector3 closestJunction;

    public int maxIterations = 300;
    public int currentIterations = 0;

    float reward = 0.0f;

    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        mazeGenerator.GenerateMaze();

    }

    private void Update()
    {
        frameCounter.text = currentIterations.ToString();
        cellCounter.text = visitedCells.Count.ToString();

        if (lastCell != mazeGenerator.PositionToCell(transform.localPosition))
        {
            //reward -= 0.04f;
            lastCell = mazeGenerator.PositionToCell(transform.localPosition);
            if (!visitedCells.Contains(lastCell))
            {
                AddReward(0.3f);
                visitedCells.Add(lastCell);
            } else
            {
                //reward -= 0.25f;
            }
        }

        HashSet<MazeGenerator.Cell> closeJunctions = new HashSet<MazeGenerator.Cell>();
        foreach (MazeGenerator.Cell cell in visitedCells)
        {
            List<MazeGenerator.Cell> adjacent = mazeGenerator.GetAdjacentCells(cell);
            adjacent = mazeGenerator.RemoveWallAdjacencies(cell, adjacent);

            foreach(MazeGenerator.Cell adj in adjacent)
            {
                if (!visitedCells.Contains(adj))
                {
                    closeJunctions.Add(cell);
                }
            }
        }

        closestJunction = Vector3.positiveInfinity;
        foreach(MazeGenerator.Cell cell in closeJunctions)
        {
            if (Vector3.Distance(mazeGenerator.CellToPosition(cell), transform.localPosition) < Vector3.Distance(closestJunction, transform.localPosition))
            {
                closestJunction = mazeGenerator.CellToPosition(cell);
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

        //mazeGenerator.ClearMaze();
        //mazeGenerator.GenerateMaze();

        this.transform.localPosition = mazeGenerator.GetStartPosition();
        target.localPosition = mazeGenerator.GetTargetPosition();
        lastPosition = this.transform.localPosition;
        lastPositionTime = Time.frameCount;

        lastCell = mazeGenerator.PositionToCell(transform.localPosition);
        visitedCells.Clear();
        visitedCells.Add(lastCell);

        closestJunction = transform.localPosition;

        currentIterations = 0;

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

        /*//sensor.AddObservation(Time.frameCount - lastPositionTime);

        float n = -1;
        float s = -1;
        float e = -1;
        float w = -1;

        RaycastHit hit;
        if (Physics.Raycast(transform.localPosition, Vector3.forward, out hit) && hit.collider.gameObject.tag == "wall")
            n = hit.distance;

        if (Physics.Raycast(transform.localPosition, -Vector3.forward, out hit) && hit.collider.gameObject.tag == "wall")
            s = hit.distance;

        if (Physics.Raycast(transform.localPosition, Vector3.right, out hit) && hit.collider.gameObject.tag == "wall")
            w = hit.distance;

        if (Physics.Raycast(transform.localPosition, -Vector3.right, out hit) && hit.collider.gameObject.tag == "wall")
            e = hit.distance;

        float sum = 0;
        if (n > 0) sum += n;
        if (s > 0) sum += s;
        if (e > 0) sum += e;
        if (w > 0) sum += w;

        if (n > 0) n /= sum;
        if (s > 0) s /= sum;
        if (e > 0) e /= sum;
        if (w > 0) w /= sum;

        sensor.AddObservation(n);
        sensor.AddObservation(s);
        sensor.AddObservation(e);
        sensor.AddObservation(w);

        //sensor.AddObservation(closestJunction);*/
    }

    public float forceMultiplier = 10;

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        rBody.AddForce(controlSignal * forceMultiplier);

        //AddReward(reward);
        //reward = 0;

        //AddReward(-0.01f);

        // Rewards
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, target.localPosition);
        //float distanceToTargetReward = ((mazeGenerator.mazeSize * mazeGenerator.cellSize) / distanceToTarget) / (mazeGenerator.mazeSize * mazeGenerator.cellSize) * 10f;

        //AddReward(-distanceToTarget / (mazeGenerator.mazeSize * mazeGenerator.cellSize));

        // Reached target
        if (distanceToTarget < 3f)
        {
            AddReward(10.0f);
            EndEpisode();
        }

        // Fell off platform
        else if (this.transform.localPosition.y < 0)
        {
            //SetReward(-10.0f);
            //reward = 0;
            //reward += visitedCells.Count / (mazeGenerator.mazeSize);
            AddReward(-10.0f);
            EndEpisode();
        }

        currentIterations++;
        if (currentIterations > maxIterations)
        {
            AddReward(-1.0f);
            EndEpisode();
        }

        /*if (Time.frameCount - lastPositionTime > 300)
        {
            //SetReward(-10.0f);
            //reward = 0;
            //reward += visitedCells.Count / (mazeGenerator.mazeSize) * 5;
            //AddReward(reward);
            EndEpisode();
        }

        if (reward < -0.5f * mazeGenerator.mazeSize)
        {
            //reward = 0;
            //reward += visitedCells.Count / (mazeGenerator.mazeSize) * 5;
            //AddReward(reward);
            //AddReward(distanceToTargetReward);
            //EndEpisode();
        }*/
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
            //reward -= 0.75f;
        }
    }
    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(closestJunction, 1);
    }
}
