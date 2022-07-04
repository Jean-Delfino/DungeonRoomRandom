using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementTypeSO : ScriptableObject
{
    public Vector3 MovementType(Vector3 playerPos, Vector3 enemyPos)
    {
        Vector3 movementDir = playerPos - enemyPos;
        if (Random.value < 0.5f)
            movementDir.x = 0;
        else
            movementDir.y = 0;
        return movementDir;
    }
}
