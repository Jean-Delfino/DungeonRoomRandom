using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    [SerializeField] private LayerMask mask;
    public void SetupPathGraph(int rows, int columns)
    {
        GameObject astar = new GameObject("Astar");
        AstarPath path = astar.AddComponent<AstarPath>();
        GridGraph gridGraph = path.data.AddGraph(typeof(GridGraph)) as GridGraph;
        gridGraph.collision.mask = mask;
        gridGraph.center = new Vector3(columns / 2f-0.5f, rows / 2f-0.5f, 0f);
        gridGraph.SetDimensions(columns+2, rows+2, 1f);
        gridGraph.is2D = true;
        gridGraph.collision.use2D = true;
        gridGraph.collision.diameter = 0.1f;
        gridGraph.neighbours = NumNeighbours.Four;
        path.Scan();

    }
}
