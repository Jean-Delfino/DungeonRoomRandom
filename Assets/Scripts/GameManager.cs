using System.Collections;
using System.Collections.Generic;
using Game.LevelManager.DungeonLoader;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private int columns;
    private int rows;
    private bool leftDoor;
    private bool rightDoor;
    private bool topDoor;
    private bool bottomDoor;

    public RoomGeneratorInput input;

    public BoardManager boardScript;

    // Start is called before the first frame update
    void Awake()
    {
        boardScript = GetComponent<BoardManager>();
        TranslateInput();
        InitGame();
    }

    void TranslateInput()
    {
        columns = (int)input.Size.x;
        rows = (int)input.Size.y;
        leftDoor = input.DoorExists(input.DoorEast);
        rightDoor = input.DoorExists(input.DoorWest);
        bottomDoor = input.DoorExists(input.DoorSouth);
        topDoor = input.DoorExists(input.DoorNorth);
    }

    void InitGame()
    {
        boardScript.SetupScene(columns,rows,leftDoor,rightDoor,topDoor,bottomDoor);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
