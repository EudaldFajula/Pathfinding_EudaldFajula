using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Calculs
{
    public const float LinearDistance = 1f;
    public const float DiagonalDistance = 1.5f;

    public static float CellSize;
    public static Vector2 FirstPosition;

    public static void CalculateDistances(BoxCollider2D coll, float Size)
    {
        CellSize = coll.size.x / Size;
        FirstPosition = new Vector2(-Size / 4f + CellSize / 2f - 0.1f,
                                     Size / 4f - CellSize / 2f + 0.1f);
    }

    public static Vector2 CalculatePoint(int x, int y)
    {
        return FirstPosition + new Vector2(x * CellSize, -y * CellSize);
    }

    public static float CalculateHeuristic(Node node, int finalx, int finaly)
    {
        // Octile distance — admissible heuristic for grids with diagonal cost 1.5
        int dx = Mathf.Abs(node.PositionX - finalx);
        int dy = Mathf.Abs(node.PositionY - finaly);
        return LinearDistance * (dx + dy) + (DiagonalDistance - 2 * LinearDistance) * Mathf.Min(dx, dy);
    }
}