using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Game.LevelManager.DungeonLoader;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour
{
    [Serializable]
    public class Count
    {
        public int minimum;
        public int maximum;

        public Count(int min, int max)
        {
            minimum = min;
            maximum = max;
        }
    }
    
    public class MatrixMap
    {
        
        private int[,] matrixMap; //0 for floor, 1 for wall, 2 for items, 3 for outer wall, 4 for door
        private int columns;
        private int rows;

        public MatrixMap(int columns, int rows)
        {
            this.rows = rows;
            this.columns = columns;
            matrixMap = new int[rows+2,columns+2];
        }
        public MatrixMap(MatrixMap original)
        {
            rows = original.rows;
            columns = original.columns;
            matrixMap = new int[rows+2,columns+2];
            for (int i = -1; i < rows + 1; i++)
            {
                for (int j = -1; j < columns + 1; j++)
                {
                    this[i, j] = original[i, j];
                }
            }
        }

        public int this[int indexY, int indexX]
        {
            get
            {
                return matrixMap[indexY + 1,indexX + 1];
            }
            set
            {
                matrixMap[indexY+1, indexX+1] = value;
            }
        }

    }

    private bool leftDoor;
    private bool rightDoor;
    private bool topDoor;
    private bool bottomDoor;
    private int columns;
    private int rows;
    private int doorsNumber;
    private Count wallCount;
    private MatrixMap boardMap;
    public GameObject door;
    public GameObject[] floorTiles;
    public GameObject[] wallTiles;
    public GameObject[] outerWallTiles;
    
    private float initialDensity = 0.2f;
    private int rule;
    private int numberOfGens = 10;
    private float erosion;
    
    [SerializeField] private int boardDensity; //1 for low, 2 for medium, 3 for high
    private float boardFinalErosion;

    private Transform boardHolder;
    private List<Vector3> gridPositions = new List<Vector3>();

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject playerPrefab;
    
    void InitialiseList()
    {
        gridPositions.Clear();

        for (int x = 0; x<columns; x++)
        {
            for (int y = 0; y< rows; y++)
            {
                boardMap[y, x]=0;
                gridPositions.Add(new Vector3(x,y,0f));
            }
        }
    }

    void PutObjects()
    {
        RoomData roomData = ScriptableObject.CreateInstance<RoomData>();
        roomData.Init(rows,columns);

        for (int x = -1; x < columns + 1; x++)
        {
            for (int y = -1; y < rows + 1; y++)
            {
                GameObject toInstantiate;
                switch (boardMap[y, x])
                {
                    case 0: toInstantiate = floorTiles[Random.Range(0, floorTiles.Length)];
                        roomData[y, x] = new Tile(TileTypes.Floor);
                        break;
                    case 1: toInstantiate = wallTiles[Random.Range(0, wallTiles.Length)];
                        roomData[y, x] = new Tile(TileTypes.Block);
                        break;
                    case 2: toInstantiate = floorTiles[Random.Range(0, floorTiles.Length)];
                        roomData[y, x] = new Tile(TileTypes.Floor);
                        break;
                    case 3: toInstantiate = outerWallTiles[Random.Range(0, outerWallTiles.Length)];
                        break;
                    case 4: toInstantiate = door;
                        break;
                    default: toInstantiate = wallTiles[Random.Range(0, wallTiles.Length)];
                        roomData[y, x] = new Tile(TileTypes.Block);
                        break;
                }
                GameObject instance =
                    Instantiate(toInstantiate, new Vector3(x, y, 0f), Quaternion.identity) as GameObject;
                instance.transform.SetParent(boardHolder);
            }
        }
        var uniquePath = AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/RoomGenerator/MyBoard.asset");
        AssetDatabase.CreateAsset(roomData, uniquePath);
        
        SetupEntities();
    }

    void BoardSetup()
    {
        boardMap = new MatrixMap(columns, rows);
        doorsNumber = 0;
        boardHolder = new GameObject("Board").transform;
        int tile;
        for (int x = -1; x < columns + 1; x++)
        {
            for (int y = -1; y < rows + 1; y++)
            {
                tile = 0;
                
                if (x == -1 || x == columns || y == -1 || y == rows)
                {
                    tile = 3;
                }

                boardMap[y, x] = tile;
            }
        }
        
        tile = 4;
        if (topDoor)
        {
            int pos = Random.Range(0, columns);
            for (int y = rows, x = pos, i = 0; i<1; i++, x++)
            {
                boardMap[y, x] = tile;
                doorsNumber++;
            }
        }
        if (leftDoor)
        {
            int pos = Random.Range(0, rows);
            for (int y = pos, x = -1, i = 0; i<1; i++, y++)
            {
                boardMap[y, x] = tile;
                doorsNumber++;
            }
        }
        if (bottomDoor)
        {
            int pos = Random.Range(0, columns);
            for (int y = -1, x = pos, i = 0; i<1; i++, x++)
            {
                boardMap[y, x] = tile;
                doorsNumber++;
            }
        }
        if (rightDoor)
        {
            int pos = Random.Range(0, rows);
            for (int y = pos, x = columns, i = 0; i<1; i++, y++)
            {
                boardMap[y, x] = tile;
                doorsNumber++;
            }
        }
        
        InitialiseList();
        
        while (gridPositions.Count != 0)
        {
            Vector3 randomPosition = RandomPosition();
            boardMap[(int) randomPosition.y, (int) randomPosition.x] = 1;
        }
    }

    Vector3 RandomPosition()
    {
        Vector3 randomPosition;
        do
        {
            int randomIndex = Random.Range(0, gridPositions.Count);
            randomPosition = gridPositions[randomIndex];
            gridPositions.RemoveAt(randomIndex);
        } while (gridPositions.Count!=0&&(Random.Range(0f, 1f) < initialDensity));
        
        return randomPosition;
    }

    int AdjacentWalls(int y, int x)
    {
        int number = 0;
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if (i != 0 || j != 0)
                {
                    if (boardMap[Mathf.Clamp((y + i), 0, rows-1), Mathf.Clamp((x + j), 0, columns-1)] != 0)
                    {
                        number++;
                    }
                }
            }
        }

        return number;
    }
    
    int AdjacentWallsNonDiagonal(int y, int x)
    {
        int number = 0;
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if ((i == 0 && j != 0)||(i != 0 && j == 0))
                {
                    if (boardMap[y + i, x + j] != 0)
                    {
                        number++;
                    }
                }
            }
        }

        return number;
    }

    List<Vector3> FindAllDoors()
    {
        List<Vector3> doorPositions = new List<Vector3>();
        for (int i = -1; i< rows+1; i++)
        {
            for (int j = -1; j < columns + 1; j++)
            {
                if (boardMap[i, j] == 4)
                {
                    Vector3 doorPosition = new Vector3(j,i,0f);
                    doorPositions.Add(doorPosition);
                }
            }
        }
        return doorPositions;
    }

    bool VerifyIfDoorExists(Vector3 position)
    {
        int x = (int) position.x;
        int y = (int) position.y;
        
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if (i != 0 || j != 0)
                {
                    if (boardMap[y + i, x + j] == 4)
                    {
                        return true;
                    }
                }
            }
        }

        return false;

    }
    void CleanSetup()
    {
        int k = 0;
        erosion = 1f;
        ApplyErosion();
        
        while (k<numberOfGens) 
        {
            k++;
            MatrixMap nextGenMap = new MatrixMap(boardMap);
            Debug.Log("Clean: "+k);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    
                    int numberOfAdjacentWalls = AdjacentWalls(y, x);

                    ApplyRules(y,x,boardMap[y, x], numberOfAdjacentWalls , ref nextGenMap, rule);
                }
            }
            
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    boardMap[y, x] = nextGenMap[y, x];
                }
            }
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 position = new Vector3(x, y, 0f);
                if (boardMap[y, x] == 1)
                {
                    if (VerifyIfDoorExists(position))
                    {
                        boardMap[y, x] = 0;
                    }
                }
            }
        }
    }

    void ApplyRules(int y, int x, int value, int neighbors, ref MatrixMap nextGenMap, int rule)
    {
        switch (rule)
        {
            case 1: switch (value)
            {
                case 1:
                    if (neighbors < 2)
                    {
                        nextGenMap[y, x] = 0;
                    }

                    if (neighbors >= 2 && neighbors <= 5)
                    {
                        nextGenMap[y, x] = 1;
                    }

                    if (neighbors > 5)
                    {
                        nextGenMap[y, x] = 0;
                    }
                            
                    break;
                case 0:
                    if ((neighbors >= 3 && neighbors <= 5))
                    {
                        nextGenMap[y, x] = 1;
                    }
                    break;

            }

                break;

            case 3: switch (value)
                {
                    case 1:
                        if (neighbors < 2)
                        {
                            nextGenMap[y, x] = 0;
                        }

                        if (neighbors >= 2 && neighbors <= 3)
                        {
                            nextGenMap[y, x] = 1;
                        }

                        if (neighbors > 3)
                        {
                            nextGenMap[y, x] = 0;
                        }
                            
                        break;
                    case 0:
                        if ((neighbors >= 3 && neighbors <= 3))
                        {
                            nextGenMap[y, x] = 1;
                        }
                        break;

                }

                break;
        }
    }

    void ApplyErosion()
    {
        MatrixMap nextGenMap = new MatrixMap(boardMap);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int numberOfAdjacentWallsNonDiagonal = AdjacentWallsNonDiagonal(y, x);
                if (boardMap[y, x] == 1 && numberOfAdjacentWallsNonDiagonal!=4 && Random.value < erosion)
                {
                    nextGenMap[y, x] = 0;
                }
            }
        }
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                boardMap[y, x] = nextGenMap[y, x];
            }
        }
    }

    void SetupWalls()
    {
        List<Vector3> doorPositions = FindAllDoors();
        List<Vector3> foundDoors = new List<Vector3>();
        
        foundDoors.Clear();

        CleanSetup();

        if (!IsPossible(doorPositions[0], ref foundDoors))
        {
            for (int i = 0; i < doorPositions.Count; i++)
            {
                if (!foundDoors.Contains(doorPositions[i]))
                {
                    DigTo(GetRandomDiscoveredPositionFromDoor(doorPositions[0]), doorPositions[i]);
                    if (IsPossible(doorPositions[0], ref foundDoors))
                    {
                        break;
                    }
                }
            }
        }

        FillClosedAreas(GetRandomDiscoveredPositionFromDoor(doorPositions[0]));
        
        erosion = boardFinalErosion;
        ApplyErosion(); // Apply final erosion

        Debug.Log(IsPossible(doorPositions[0], ref foundDoors));
    }

    void FillClosedAreas(Vector3 startPosition)
    {
        MatrixMap observableMap = new MatrixMap(boardMap);
        
        FloodFill(ref observableMap, startPosition, -1);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (observableMap[y, x] != -1)
                {
                    boardMap[y, x] = 1;
                }
            }
        }
    }

    void FloodFill(ref MatrixMap map, Vector3 referencePosition, int fillValue)
    {
        int x = (int) referencePosition.x;
        int y = (int) referencePosition.y;

        int beforeValue = map[y, x];
        
        _FloodFill(ref map, referencePosition, fillValue, beforeValue);
    }

    void _FloodFill(ref MatrixMap map, Vector3 referencePosition, int fillValue, int beforeValue)
    {
        int x = (int) referencePosition.x;
        int y = (int) referencePosition.y;
        
        if (map[y, x] != beforeValue)
        {
            return;
        }

        map[y, x] = fillValue;
        
        _FloodFill(ref map, referencePosition+Vector3.up, fillValue, beforeValue);
        _FloodFill(ref map, referencePosition+Vector3.down, fillValue, beforeValue);
        _FloodFill(ref map, referencePosition+Vector3.right, fillValue, beforeValue);
        _FloodFill(ref map, referencePosition+Vector3.left, fillValue, beforeValue);
    }

    Vector3 GetRandomDiscoveredPositionFromDoor(Vector3 doorPosition)
    {
        Vector3 referencePosition = new Vector3();
        
        List<Vector3> possibleGround = new List<Vector3>();
        possibleGround.Add(doorPosition+Vector3.up);
        possibleGround.Add(doorPosition+Vector3.down);
        possibleGround.Add(doorPosition+Vector3.right);
        possibleGround.Add(doorPosition+Vector3.left);
        foreach (var position in possibleGround)
        {
            int x = (int) position.x;
            int y = (int) position.y;
            
            if (!(x < -1 || x > columns || y < -1 || y > rows))
            {
                if (boardMap[y, x] == 0)
                {
                    referencePosition = position;
                    break;
                }
            }
        }

        List<Vector3> discoveredPositions = new List<Vector3>();
        MatrixMap observableMap = new MatrixMap(boardMap);
        FloodFill(ref observableMap, referencePosition, -1);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (observableMap[y, x] == -1)
                {
                    Vector3 position = new Vector3(x, y, 0f);
                    discoveredPositions.Add(position);
                }
            }
        }

        return discoveredPositions[Random.Range(0,discoveredPositions.Count)];
    }

    void DigTo(Vector3 initialPosition, Vector3 finalPosition)
    {
        MatrixMap observableMap = new MatrixMap(boardMap);
        bool flag = false;
        _DigTo(ref observableMap, initialPosition, finalPosition, ref flag);
    }

    void _DigTo(ref MatrixMap map, Vector3 currentPosition, Vector3 finalPosition, ref bool flag)
    {
        if (flag)
        {
            return;
        }
        
        if (currentPosition == finalPosition)
        {
            flag = true;
            return;
        }
        
        int x = (int) currentPosition.x;
        int y = (int) currentPosition.y;
        
        if (y < -1 || y > rows || x < -1 || x > columns)
        {
            return;
        }
        if (map[y, x] == -1|| map[y, x] == 3)
        {
            return;
        }

        map[y, x] = -1;
        
        if (y >= 0 && y < rows && x >= 0 && x < columns)
        {
            boardMap[y, x] = 0; //Dig
        }

        List<Vector3> positions = new List<Vector3>();
        positions.Add(currentPosition+Vector3.down);
        positions.Add(currentPosition+Vector3.left);
        positions.Add(currentPosition+Vector3.up);
        positions.Add(currentPosition+Vector3.right);

        positions.Sort((x, y) =>
        {
            return (finalPosition - x).sqrMagnitude.CompareTo((finalPosition - y).sqrMagnitude);
        });

        foreach (var position in positions)
        {
            _DigTo(ref map, position, finalPosition, ref flag);
        }
        

    }

    bool IsPossible(Vector3 initialPosition, ref List<Vector3> foundDoors)
    {
        MatrixMap observableMap = new MatrixMap(boardMap);
        _TryPath(observableMap, ref foundDoors, (int)initialPosition.y, (int)initialPosition.x);
        Debug.Log("Number of doors: "+foundDoors.Count);
        return foundDoors.Count == doorsNumber;
    }

    void _TryPath(MatrixMap map, ref List<Vector3> foundDoors, int y, int x)
    {
        if (y < -1 || y > rows || x < -1 || x > columns)
        {
            return;
        }
        else
        {
            if (map[y, x] == -1||map[y, x] == 3||map[y, x] == 1)
            {
                return;
            }
        }

        if (map[y, x] == 4)
        {
            Vector3 doorPosition = new Vector3(x,y,0);
            if (!foundDoors.Contains(doorPosition))
            {
                foundDoors.Add(doorPosition);
            }
        }
        map[y, x] = -1;
        
        _TryPath(map,ref foundDoors, y, x+1);
        _TryPath(map,ref foundDoors, y, x-1);
        _TryPath(map,ref foundDoors, y+1, x);
        _TryPath(map,ref foundDoors, y-1, x);
        
    }

    void SetupEntities()
    {
        EnemySO enemySO = ScriptableObject.CreateInstance<EnemySO>();
        MovementTypeSO movementTypeSO = ScriptableObject.CreateInstance<MovementTypeSO>();
        
        List<Vector3> doorPositions = FindAllDoors();

        enemySO.movementSpeed = 10f;
        enemySO.restTime = 0.1f;
        enemySO.activeTime = 0.1f;
        
        GameObject player = Instantiate(playerPrefab, GetRandomDiscoveredPositionFromDoor(doorPositions[0]), Quaternion.identity);

        for (int i = 0; i < 3; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab, GetRandomDiscoveredPositionFromDoor(doorPositions[0]), Quaternion.identity);
        
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
        
            enemyController.movement = movementTypeSO;
            enemyController.playerObj = player;
            enemyController.LoadEnemyData(enemySO);
        }


    }

    public void SetupScene(int columns, int rows, bool leftDoor, bool rightDoor, bool topDoor, bool bottomDoor)
    {
        this.columns = columns;
        this.rows = rows;
        this.leftDoor = leftDoor;
        this.rightDoor = rightDoor;
        this.topDoor = topDoor;
        this.bottomDoor = bottomDoor;

        boardHolder = new GameObject("Board").transform;

        switch (boardDensity)
        {
            case 1: boardFinalErosion = 0f;
                    rule = 3;
                    break;
            case 2: boardFinalErosion = 1f;
                    rule = 1;
                    break;
            case 3: boardFinalErosion = 0.5f;
                    rule = 1;
                    break;
        }
        
        BoardSetup();
        SetupWalls();
        PutObjects();

        EmptySpaces();
    }

    void EmptySpaces()
    {
        int numberOfSpaces = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (boardMap[y, x] == 0)
                {
                    numberOfSpaces++;
                }
            }
        }
        Debug.Log("Espacos: "+numberOfSpaces+"/"+rows*columns+" "+(float)numberOfSpaces/(rows*columns));
    }
}
