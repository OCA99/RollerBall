using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MazeGenerator : MonoBehaviour
{
    public int mazeSize = 10;
    public int cellSize = 2;
    public GameObject wall;
    public GameObject floor;

    private Maze maze;

    class Cell
    {
        public int x, y;
        public int set;

        public Cell(int x, int y, int set)
        {
            this.x = x;
            this.y = y;
            this.set = set;
        }
    }

    class Edge
    {
        public Cell a;
        public Cell b;

        public Edge(Cell a, Cell b)
        {
            this.a = a;
            this.b = b;
        }
    }

    class Maze
    {
        public List<List<Cell>> cells = new List<List<Cell>>();
        public List<Edge> edges = new List<Edge>();
        public int size;

        public Vector3 start;
        public Vector3 end;

        public Maze(int size)
        {
            this.size = size;

            int setCounter = 0;
            for (int i = 0; i < size; i++)
            {
                cells.Add(new List<Cell>());
                for (int j = 0; j < size; j++)
                {
                    cells[i].Add(new Cell(j, i, setCounter));
                    setCounter++;
                }
            }

            foreach(List<Cell> row in cells)
            {
                foreach(Cell cell in row)
                {
                    List<Cell> adjacent = GetAdjacentCells(cell);
                    foreach (Cell adj in adjacent)
                    {
                        if (adj.x > cell.x || adj.y > cell.y)
                        {
                            edges.Add(new Edge(adj, cell));
                        }
                    }
                }
            }
        }

        public List<Cell> GetAdjacentCells(Cell cell)
        {
            List<Cell> result = new List<Cell>();
            if (cell.x > 0) result.Add(cells[cell.y][cell.x - 1]);
            if (cell.x < size - 1) result.Add(cells[cell.y][cell.x + 1]);
            if (cell.y > 0) result.Add(cells[cell.y - 1][cell.x]);
            if (cell.y < size - 1) result.Add(cells[cell.y + 1][cell.x]);

            return result;
        }

        public void ShuffleEdges()
        {
            for (int i = 0; i < edges.Count; i++)
            {
                Edge temp = edges[i];
                int randomIndex = UnityEngine.Random.Range(i, edges.Count);
                edges[i] = edges[randomIndex];
                edges[randomIndex] = temp;
            }
        }

        public void Print()
        {
            string res = "";
            foreach (List<Cell> row in cells)
            {
                foreach (Cell cell in row)
                {
                    res += String.Format("{0}", (char)(cell.set + 65));
                }
                res += "\n";
            }

            res = "";
            foreach(Edge edge in edges)
            {
                res += String.Format("{0},{1} -> {2},{3}\n", edge.a.x.ToString(), edge.a.y.ToString(), edge.b.x.ToString(), edge.b.y.ToString());
            }
        }

        public void ReplaceAllSet(int old, int new_)
        {
            foreach (List<Cell> row in cells)
            {
                foreach (Cell cell in row)
                {
                    if (cell.set == old)
                    {
                        cell.set = new_;
                    }
                }
            }
        }

        public void Kruskal()
        {
            List<Edge> remainingEdges = new List<Edge>();

            while (edges.Count != 0)
            {
                Edge edge = edges[0];
                if (edge.a.set != edge.b.set)
                {
                    int aSet = edge.a.set;
                    int bSet = edge.b.set;
                    ReplaceAllSet(bSet, aSet);
                } else
                {
                    remainingEdges.Add(edge);
                }
                edges.RemoveAt(0);
            }

            edges = remainingEdges;
        }

        public void GenerateMesh(int cellSize, GameObject parent, GameObject wall)
        {
            int offset = -(size - 1) * cellSize / 2;

            foreach (Edge edge in edges)
            {
                Vector3 pos = 0.5f * (new Vector3(edge.a.x * cellSize, 2.5f, edge.a.y * cellSize) + new Vector3(edge.b.x * cellSize, 2.5f, edge.b.y * cellSize));
                pos += new Vector3(offset, 0, offset);
                GameObject instance = Instantiate(wall, pos, Quaternion.identity, parent.transform);

                if (edge.a.x != edge.b.x)
                {
                    instance.transform.Rotate(0, 90, 0, Space.World);
                }
            }
        }

        public Vector2Int PositionToCell(Vector3 position, int cellSize)
        {
            float offset = -(size - 1) * cellSize / 2;

            return new Vector2Int(Mathf.RoundToInt((position.x - offset) / cellSize), Mathf.RoundToInt((position.z - offset) / cellSize));
        }

        public void GenerateStartAndEnd(int cellSize)
        {
            float offset = -(size - 1) * cellSize / 2;

            int yStart = UnityEngine.Random.Range(0, size);
            int yEnd = UnityEngine.Random.Range(0, size);

            start = new Vector3(0 + offset, 0.5f, yStart * cellSize + offset);
            end = new Vector3((size - 1) * cellSize + offset, 0.5f, yEnd * cellSize + offset);
        }
    }

    public void GenerateMaze()
    {
        maze = new Maze(mazeSize);
        maze.ShuffleEdges();
        maze.Print();
        maze.Kruskal();
        maze.Print();
        maze.GenerateMesh(cellSize, gameObject, wall);
        maze.GenerateStartAndEnd(cellSize);
        floor.transform.localScale = new Vector3(mazeSize / 2, 1, mazeSize / 2);
    }

    public void ClearMaze()
    {
        foreach(Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    public Vector3 GetTargetPosition()
    {
        return maze.end;
    }

    public Vector3 GetStartPosition()
    {
        return maze.start;
    }

    public Vector2Int PositionToCell(Vector3 position)
    {
        return maze.PositionToCell(position, cellSize);
    }
}
