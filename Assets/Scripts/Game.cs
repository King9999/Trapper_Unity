using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;

//This script is used to setup and control the game screen. It's also used to create new levels when the player completes the objective.

public class Game : MonoBehaviour
{
    //UI and game states
    int score;
    public int level;
    int levelIndex;         //used to navigate levels in XML file.
    int enemyCount;         //current # of uncaptured creatures remaining.
    int enemyTotal;
    bool gameOver;
    bool controlLocked;     //prevents any player input during win screen.
    int waitTimer;          //used to delay screen changes in frames.
    public UI ui;           //need this to make changes to UI such as stage number or lives.
    public AudioSource audioSource;

    //GameObjectManager objManager;       //used to create the level objects and tiles at runtime.
    GameObject player;                  //used to create a player at runtime so that I can get their position and move them when necessary.

    //prefabs
    public GameObject playerPrefab;
    public GameObject treePrefab;
    public GameObject creaturePrefab;
    public GameObject trapPrefab;
    public GameObject landPrefab;
    public GameObject waterPrefab;
    public GameObject landTopPrefab;
    public GameObject landBottomPrefab;
    public GameObject landLeftPrefab;
    public GameObject landRightPrefab;
    public GameObject landTopLeftPrefab;
    public GameObject landTopRightPrefab;
    public GameObject landBottomLeftPrefab;
    public GameObject landBottomRightPrefab;
    public GameObject landTopBottomPrefab;
    public GameObject landTopBottomLeftPrefab;
    public GameObject landTopBottomRightPrefab;

    //Level data
    string[,] mapArray;       //used to contain the map data from the XML file. This is used to check for collisions.
    string[,] objectArray;	   //used to contain the object data from the file. This is used to check for collisions.
    string[,] initMapArray;   //used to restart current level
    string[,] initObjArray;   //used to restart current level

    List<string> mapList;
    List<string> objectList;
    List<string> initMapList;           //These lists are used to draw the map and objects.
    List<string> initObjList;

    int playerRow, playerCol;           //tracks player's position in the object array. Used for collision checking.         

    const int MAX_ROWS = 12;
	const int MAX_COLS = 16;
	const int TILE_SIZE = 64;
    const float MOVE_SPEED = 4f;      //will be affected by deltaTime
	const int MAX_LEVEL = 10;
    public TextAsset levelFile;
    const float MAP_LAYER = 1;
    const float TRAP_LAYER = 0;         //want creatures and player to appear to be on top of a trap when they step on it.
    const float OBJECT_LAYER = -1;

    //map tiles
    const string WATER = "0";
    const string LAND = "1";
    const string LAND_BOTTOM = "2";
    const string LAND_TOP = "3";
    const string LAND_LEFT = "4";
    const string LAND_RIGHT = "5";
    const string LAND_UPLEFT = "6";
    const string LAND_UPRIGHT = "7";
    const string LAND_BTMLEFT = "8";
    const string LAND_BTMRIGHT = "9";
    const string LAND_TOPBTM = "A";
    const string LAND_TOPBTMLEFT = "B";
    const string LAND_TOPBTMRIGHT = "C";

    //objects
    const string TREE = "A";
	const string TRAP = "B";
	const string CREATURE = "C";
	const string PLAYER = "P";
    const string EMPTY = "0";

    //player direction frames.
    const int LEFT = 1;
	const int RIGHT = 10;
	const int UP = 30;
	const int DOWN = 20;

    //creature direction frames.
    const int CLEFT = 1;
	const int CRIGHT = 2;
	const int CUP = 3;
	const int CDOWN = 4;

    //player data
    bool playerDead;
    public int playerLives;
    Vector2 playerDestination;  //used to move player to new spot on map.
    int playerDirection;
    int frameAdvance;           //used to change animation frames.

    //creatures & objects
    List<GameObject> creatureList;
    List<bool> creatureTrapped;         //tracks when creatures land in traps.
    List<Vector2> creatureTrapLocations;        //tracks which creatures are trapped.
	List<GameObject> treeList;         //not sure how to use these yet
	List<GameObject> trapList;
    List<Vector2> trapPositions;            //needed to destroy traps when necessary.
    List<Vector2> treePositions;            //needed to check for collision
	List<Vector2> destinationList;		    //contains list of creature destinations on map.
    List<int> creatureRow;                 
    List<int> creatureCol;                   //tracks each creature's position in the object array.


    // Start is called before the first frame update
    void Start()
    {
        //level = 2;
        levelIndex = 0;
        playerLives = 2;
        playerDead = false;
        controlLocked = false;
        waitTimer = 0;
        //previousTime = 0;
        //deltaTime = 0;
        playerDirection = LEFT;
        frameAdvance = 0;
        playerRow = 0;
        playerCol = 0;
        creatureRow = new List<int>();
        creatureCol = new List<int>();

        //objManager = new GameObjectManager();
        player = new GameObject();

        mapArray = new string[MAX_ROWS, MAX_COLS];
        objectArray = new string[MAX_ROWS, MAX_COLS];
        initMapArray = new string[MAX_ROWS, MAX_COLS];
        initObjArray = new string[MAX_ROWS, MAX_COLS];
        //waterTile = new char[MAX_ROWS, MAX_COLS];
        //landTile = new char[MAX_ROWS, MAX_COLS];

        mapList = new List<string>();
        objectList = new List<string>();
        initMapList = new List<string>();
        initObjList = new List<string>();

        creatureList = new List<GameObject>();
        creatureTrapped = new List<bool>();
        creatureTrapLocations = new List<Vector2>();
        treeList = new List<GameObject>();
        trapList = new List<GameObject>();
        destinationList = new List<Vector2>();
        //trapPositions = new List<Vector2>();
        treePositions = new List<Vector2>();

        ui.SetLivesText(playerLives);
        ui.SetStageText(level);

        LoadLevel(level);
        //objManager.SetupTiles();
        BuildMap(mapArray); 
        BuildObjects(objectArray);
    }

    // Update is called once per frame
    void Update()
    {
        CheckForInput();
        UpdateObjects();

        if (creatureTrapLocations.Count > 0)
            RemoveCreatures();
    }

    //All user input is checked here. Must go into Update method
    void CheckForInput()
    {
        //check for player input
        if (!controlLocked)
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                //ensure there's noththing blocking player's movement
                if (playerCol > 0 && !objectArray[playerRow, playerCol - 1].Equals(TREE) && mapArray[playerRow, playerCol - 1].Equals(LAND))
                {
                    //Debug.Log("Player Row: " + playerRow + " Player Col: " + playerCol);
                    //Debug.Log("ObjectArray value at " + playerRow + ", " + (playerCol - 1) + ": " + objectArray[playerRow, playerCol - 1]);
                    //Debug.Log("MapArray value at " + playerRow + ", " + (playerCol - 1) + ": " + mapArray[playerRow, playerCol - 1]);

                    //update player position in array
                    objectArray[playerRow, playerCol] = EMPTY;
                    playerCol--;
                    objectArray[playerRow, playerCol] = PLAYER;
                    playerDestination = new Vector2(player.transform.position.x - 1, player.transform.position.y);
                    controlLocked = true;

                    //Move all creatures in opposite direction (right)
                    foreach (GameObject creature in creatureList)
                    {
                        //gets current index in foreach loop
                        int i = creatureList.IndexOf(creature);

                        //creature has the same movement restrictions as player, but other creatures prevent them from moving also.
                        if (creatureCol[i] < MAX_COLS - 1 && !objectArray[creatureRow[i], creatureCol[i] + 1].Equals(TREE) && !objectArray[creatureRow[i], creatureCol[i] + 1].Equals(CREATURE)
                            && mapArray[creatureRow[i], creatureCol[i] + 1].Equals(LAND))
                        {                               
                            objectArray[creatureRow[i], creatureCol[i]] = EMPTY;
                            creatureCol[i]++;
                            objectArray[creatureRow[i], creatureCol[i]] = CREATURE;
                            destinationList[i] = new Vector2(creature.transform.position.x + 1, creature.transform.position.y);
                            //Debug.Log("New Creature Pos: " + destinationList[i]);
                        }
                    }

                    //if player landed on a trap or a creature, then player dies.


                    //Debug.Log("New Player Destination: " + playerDestination);
                }
            }
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                if (playerCol < MAX_COLS - 1 && !objectArray[playerRow, playerCol + 1].Equals(TREE) && mapArray[playerRow, playerCol + 1].Equals(LAND))
                {
                    //Debug.Log("Player Row: " + playerRow + " Player Col: " + playerCol);
                   // Debug.Log("ObjectArray value at " + playerRow + ", " + (playerCol + 1) + ": " + objectArray[playerRow, playerCol + 1]);
                    //Debug.Log("MapArray value at " + playerRow + ", " + (playerCol + 1) + ": " + mapArray[playerRow, playerCol + 1]);

                    objectArray[playerRow, playerCol] = EMPTY;
                    playerCol++;
                    objectArray[playerRow, playerCol] = PLAYER;
                    playerDestination = new Vector2(player.transform.position.x + 1, player.transform.position.y);
                    controlLocked = true;
                    //Debug.Log("New Player Destination: " + playerDestination);

                    //Move all creatures in opposite direction (left)
                    foreach (GameObject creature in creatureList)
                    {
                        //gets current index in foreach loop
                        int i = creatureList.IndexOf(creature);

                        //creature has the same movement restrictions as player, but other creatures prevent them from moving also.
                        if (creatureCol[i] > 0 && !objectArray[creatureRow[i], creatureCol[i] - 1].Equals(TREE) && !objectArray[creatureRow[i], creatureCol[i] - 1].Equals(CREATURE)
                            && mapArray[creatureRow[i], creatureCol[i] - 1].Equals(LAND))
                        {
                            //are they on a trap?
                            if (objectArray[creatureRow[i], creatureCol[i] - 1].Equals(TRAP))
                            {
                                //creatureTrapped[i] = true;
                                //creatureTrapLocations.Add(creature.transform.position);
                            }

                            objectArray[creatureRow[i], creatureCol[i]] = EMPTY;
                            creatureCol[i]--;
                            objectArray[creatureRow[i], creatureCol[i]] = CREATURE;
                            destinationList[i] = new Vector2(creature.transform.position.x - 1, creature.transform.position.y);
                            //Debug.Log("New Creature Pos: " + destinationList[i]);
                        }
                    }
                }
            }
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                if (playerRow > 0 && !objectArray[playerRow - 1, playerCol].Equals(TREE) && mapArray[playerRow - 1, playerCol].Equals(LAND))
                {
                    //Debug.Log("Player Row: " + playerRow + " Player Col: " + playerCol);
                   // Debug.Log("ObjectArray value at " + (playerRow - 1) + ", " + playerCol + ": " + objectArray[playerRow - 1, playerCol]);
                    //Debug.Log("MapArray value at " + (playerRow - 1) + ", " + playerCol + ": " + mapArray[playerRow - 1, playerCol]);

                    objectArray[playerRow, playerCol] = EMPTY;
                    playerRow--;
                    objectArray[playerRow, playerCol] = PLAYER;
                    playerDestination = new Vector2(player.transform.position.x, player.transform.position.y + 1);
                    controlLocked = true;
                    //Debug.Log("New Player Destination: " + playerDestination);

                    //Move all creatures in opposite direction (down)
                    foreach (GameObject creature in creatureList)
                    {
                        
                        int i = creatureList.IndexOf(creature);

                        if (creatureRow[i] < MAX_ROWS - 1 && !objectArray[creatureRow[i] + 1, creatureCol[i]].Equals(TREE) && !objectArray[creatureRow[i] + 1, creatureCol[i]].Equals(CREATURE)
                            && mapArray[creatureRow[i] + 1, creatureCol[i]].Equals(LAND))
                        {
                            objectArray[creatureRow[i], creatureCol[i]] = EMPTY;
                            creatureRow[i]++;
                            objectArray[creatureRow[i], creatureCol[i]] = CREATURE;
                            destinationList[i] = new Vector2(creature.transform.position.x, creature.transform.position.y - 1);
                            //Debug.Log("New Creature Pos: " + destinationList[i]);
                        }
                    }
                }
            }
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                if (playerRow < MAX_ROWS - 1 && !objectArray[playerRow + 1, playerCol].Equals(TREE) && mapArray[playerRow + 1, playerCol].Equals(LAND))
                {
                    //Debug.Log("Player Row: " + playerRow + " Player Col: " + playerCol);
                   // Debug.Log("ObjectArray value at " + (playerRow + 1) + ", " + playerCol + ": " + objectArray[playerRow + 1, playerCol]);
                   // Debug.Log("MapArray value at " + (playerRow + 1) + ", " + playerCol + ": " + mapArray[playerRow + 1, playerCol]);

                    objectArray[playerRow, playerCol] = EMPTY;
                    playerRow++;
                    objectArray[playerRow, playerCol] = PLAYER;
                    playerDestination = new Vector2(player.transform.position.x, player.transform.position.y - 1);
                    controlLocked = true;
                    //Debug.Log("New Player Destination: " + playerDestination);

                    //Move all creatures in opposite direction (up)
                    foreach (GameObject creature in creatureList)
                    {

                        int i = creatureList.IndexOf(creature);

                        if (creatureRow[i] > 0 && !objectArray[creatureRow[i] - 1, creatureCol[i]].Equals(TREE) && !objectArray[creatureRow[i] - 1, creatureCol[i]].Equals(CREATURE)
                            && mapArray[creatureRow[i] - 1, creatureCol[i]].Equals(LAND))
                        {
                            objectArray[creatureRow[i], creatureCol[i]] = EMPTY;
                            creatureRow[i]--;
                            objectArray[creatureRow[i], creatureCol[i]] = CREATURE;
                            destinationList[i] = new Vector2(creature.transform.position.x, creature.transform.position.y + 1);
                            //Debug.Log("New Creature Pos: " + destinationList[i]);
                        }
                    }
                }
            }
        }
    }

    void LoadLevel(int level)
    {
        mapArray = new string[MAX_ROWS, MAX_COLS];
        objectArray = new string[MAX_ROWS, MAX_COLS];
        initObjArray = new string[MAX_ROWS, MAX_COLS];

        if (level < 1 || level > 10)
            level = 1;

        //Debug.Log(levelFile.text);

        //Start reading the XML file so we can create the levels
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(levelFile.text);

        //Look for the selected level within the XML file 
        XmlNode levelNode = xmlDoc.SelectSingleNode("levels/lvl[@number='" + level + "']"); //the @number is the attribute in the "lvl" node. Can use this to find specific items in
                                                                                            //the file without searching the entire file.
        XmlNode mapNode = levelNode.SelectSingleNode("mapData");                            //This line lets me access the child nodes within mapData.                           

        //Map data
        int row = 0;
        int col = 0;
        foreach (XmlNode rowNode in mapNode.ChildNodes)
        {
            string[] rowArray = rowNode.InnerText.Split(',');       //Split separates values. I do this to prevent commas from being added to list.

            for (int i = 0; i < rowArray.Length; i++)
            { 
                //mapList.Add(rowArray[i]);
                mapArray[row, col] = rowArray[i];
                //Debug.Log(mapArray[row, col]);
                col++;
                if (col == MAX_COLS)
                    col = 0;
            }
            row++;     
        }


        //Object data
        row = 0;
        col = 0;
       
        XmlNode objectNode = levelNode.SelectSingleNode("objects");    //Accessing objects and its child nodes
        foreach (XmlNode rowNode in objectNode.ChildNodes)
        {
            string[] rowArray = rowNode.InnerText.Split(',');

            for (int i = 0; i < rowArray.Length; i++)
            {
                //objectList.Add(rowArray[i]);
                objectArray[row, col] = rowArray[i];
               
                //Debug.Log(objectArray[row, col]);
                col++;
                if (col == MAX_COLS)
                    col = 0;

            }
           row++;
           
        }

        //Copy data so that levels can be quickly restarted
        initObjArray = (string[,])objectArray.Clone();
        

    }


    void BuildMap(string[,] map)//List<string> map)
    {
        
        int i = 0;                  //used to iterate through map list.
        float xOffset = -7.5f; 
        float yOffset = 5.5f;      //Unity doesn't use screen coordinates (origin is in the middle of screen), so I have to use offset to position tiles properly.
       
        for (int row = 0; row < MAX_ROWS; row++)// int row = MAX_ROWS - 1; row >= 0; row--)
		{
            for (int col = 0; col < MAX_COLS; col++) 
		    {
                //GameObject obj = new GameObject();
                
                switch (map[row, col])
                {
                    
                    case WATER:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/tile_water"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(waterPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        //Debug.Log("Water Location: " + waterObj.transform.position);
                        break;

                    case LAND:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/tile_land"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_BOTTOM:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_bottom"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landBottomPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_TOP:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_top"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landTopPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_LEFT:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_left"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landLeftPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_RIGHT:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_right"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landRightPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_UPLEFT:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_upperleft"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landTopLeftPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_UPRIGHT:  //TODO: Does not display correctly normally. Had to switch the orignial values to get it to work.
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_upperright"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landTopRightPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_BTMLEFT:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_bottomleft"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landBottomLeftPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_BTMRIGHT: //TODO: Does not display correctly normally. Had to switch the orignial values to get it to work.
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_bottomright"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landBottomRightPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_TOPBTM:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_topbottom"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landTopBottomPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_TOPBTMLEFT:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_topbottomleft"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landTopBottomLeftPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    case LAND_TOPBTMRIGHT:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_topbottomright"), new Vector3((float)col + xOffset, yOffset - (float)row, 0));
                        Instantiate(landTopBottomRightPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0));
                        break;

                    default:
                        break;
                }
                i++;
            }
        }
    }


    void BuildObjects(string[,] objects)//List<string> objects)
    {
        int i = 0;                  //used to iterate through map list.
        float xOffset = -7.5f; 
        float yOffset = 5.5f;       //Unity doesn't use screen coordinates (origin is in the middle of screen), so I have to use offset to position tiles properly.
        //string rows = "";
        for (int row = 0; row < MAX_ROWS; row++)
        {
            for (int col = 0; col < MAX_COLS; col++)
            {
                //GameObject obj = new GameObject();
                //rows += objectArray[row, col];
                switch (objects[row, col])
                {

                    case TREE:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Objects/tree"), new Vector3((float)col + xOffset, (float)row + yOffset, OBJECT_LAYER));
                        //obj.name = "Tree";
                        Instantiate(treePrefab, new Vector3((float)col + xOffset, yOffset - (float)row, OBJECT_LAYER), new Quaternion(0, 0, 0, 0));
                        //treeList.Add(treePrefab);
                        //treePositions.Add(new Vector2((float)col + xOffset, yOffset - (float)row));
                        break;

                    case CREATURE:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Objects/creature_down"), new Vector3((float)col + xOffset, (float)row + yOffset, OBJECT_LAYER));
                        //obj.name = "Creature";
                        GameObject creature = Instantiate(creaturePrefab, new Vector3((float)col + xOffset, yOffset - (float)row, OBJECT_LAYER), new Quaternion(0, 0, 0, 0));
                        creatureList.Add(creature);
                        destinationList.Add(creature.transform.position);// new Vector2((float)col + xOffset, yOffset - (float)row));
                        creatureRow.Add(row);
                        creatureCol.Add(col);
                        creatureTrapped.Add(false);
                        //Debug.Log("Creature's Starting Pos: " + destinationList[0]); //creaturePrefab.transform.position);
                        break;

                    case TRAP:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Objects/trap"), new Vector3((float)col + xOffset, (float)row + yOffset, OBJECT_LAYER));
                        //obj.name = "Trap";
                        GameObject trap = Instantiate(trapPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, TRAP_LAYER), new Quaternion(0, 0, 0, 0));
                        trapList.Add(trap);
                        //trapPositions.Add(trap.transform.position);// new Vector2((float)col + xOffset, yOffset - (float)row));
                        break;

                    case PLAYER:
                        //objManager.SetupObject(player, Resources.Load<Sprite>("Objects/player_down"), new Vector3((float)col + xOffset, yOffset - (float)row, OBJECT_LAYER));
                        //player.name = "Player";
                        player = Instantiate(playerPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, OBJECT_LAYER), new Quaternion(0, 0, 0, 0));
                        playerDestination = player.transform.position; // new Vector2((float)col + xOffset, yOffset - (float)row);
                        //Debug.Log("Starting Player Pos: " + playerDestination);
                        playerRow = row;
                        playerCol = col;
                        //Debug.Log("Player Row: " + playerRow);
                        break;

                    default:
                        break;
                }
                i++;               
            }
            //rows += "\n";
        }

        //Debug.Log(rows);

    }


    //Used to move all objects when necessary. Must go into Update method
    void UpdateObjects()
    {
       
        /****************PLAYER MOVEMENT*******************/
        if (player.transform.position.x < playerDestination.x)  //player moves right
        {
            //The float values when the player moves are extremely precise, so need to do an additional check for when player is close to destination.
            //Without this check, the player sprite appears to jitter and never really finishes moving.
            float posDiffX = playerDestination.x - player.transform.position.x;
            if (posDiffX > 0 && posDiffX < 0.05f)
            {
                player.transform.position = new Vector3(playerDestination.x, player.transform.position.y, OBJECT_LAYER);
            }
            else
            {
                player.transform.position = new Vector3(player.transform.position.x + (MOVE_SPEED * Time.deltaTime), player.transform.position.y, OBJECT_LAYER);
                //TODO: while player is moving, player sprite animates
            }

        }
        else if (player.transform.position.x > playerDestination.x) //player moves left
        {
            float posDiffX = player.transform.position.x - playerDestination.x;
            if (posDiffX > 0 && posDiffX < 0.05f)
                player.transform.position = new Vector3(playerDestination.x, player.transform.position.y, OBJECT_LAYER);
            else
                player.transform.position = new Vector3(player.transform.position.x - (MOVE_SPEED * Time.deltaTime), player.transform.position.y, OBJECT_LAYER);
           
        }
        else if (player.transform.position.y < playerDestination.y) //player moves up
        {
            float posDiffY = playerDestination.y - player.transform.position.y;
            if (posDiffY > 0 && posDiffY < 0.05f)
                player.transform.position = new Vector3(player.transform.position.x, playerDestination.y, - 1);
            else
                player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + (MOVE_SPEED * Time.deltaTime), OBJECT_LAYER);

        }
        else if (player.transform.position.y > playerDestination.y) //player moves down
        {
            float posDiffY = player.transform.position.y - playerDestination.y;
            if (posDiffY > 0 && posDiffY < 0.05f)
                player.transform.position = new Vector3(player.transform.position.x, playerDestination.y, OBJECT_LAYER);
            else
                player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y - (MOVE_SPEED * Time.deltaTime), OBJECT_LAYER);

        }
        else //at current destination
        {
            controlLocked = false;
            //TODO: stop sprite animation
            
            //Debug.Log("At current destination: " + player.transform.position);
        }

        /****************CREATURE MOVEMENT*******************/

        foreach (GameObject creature in creatureList)
        {
            int i = creatureList.IndexOf(creature);

            if (creature.transform.position.x < destinationList[i].x)   //move right
            {
                float posDiffX = destinationList[i].x - creature.transform.position.x;
                if (posDiffX > 0 && posDiffX < 0.05f)
                {
                    creature.transform.position = new Vector3(destinationList[i].x, creature.transform.position.y, OBJECT_LAYER);
                }
                else
                {
                    creature.transform.position = new Vector3(creature.transform.position.x + (MOVE_SPEED * Time.deltaTime), creature.transform.position.y, OBJECT_LAYER);

                }
            }
            else if (creature.transform.position.x > destinationList[i].x)   //move left
            {
                float posDiffX = creature.transform.position.x - destinationList[i].x;
                if (posDiffX > 0 && posDiffX < 0.05f)
                {
                    creature.transform.position = new Vector3(destinationList[i].x, creature.transform.position.y, OBJECT_LAYER);
                }
                else
                {
                    creature.transform.position = new Vector3(creature.transform.position.x - (MOVE_SPEED * Time.deltaTime), creature.transform.position.y, OBJECT_LAYER);

                }
            }
            else if (creature.transform.position.y < destinationList[i].y)   //move up
            {
                float posDiffY = destinationList[i].y - creature.transform.position.y;
                if (posDiffY > 0 && posDiffY < 0.05f)
                {
                    creature.transform.position = new Vector3(creature.transform.position.x, destinationList[i].y, OBJECT_LAYER);
                }
                else
                {
                    creature.transform.position = new Vector3(creature.transform.position.x, creature.transform.position.y + (MOVE_SPEED * Time.deltaTime), OBJECT_LAYER);

                }
            }
            else if (creature.transform.position.y > destinationList[i].y)   //move down
            {
                float posDiffY = creature.transform.position.y - destinationList[i].y;
                if (posDiffY > 0 && posDiffY < 0.05f)
                {
                    creature.transform.position = new Vector3(creature.transform.position.x, destinationList[i].y, OBJECT_LAYER);
                }
                else
                {
                    creature.transform.position = new Vector3(creature.transform.position.x, creature.transform.position.y - (MOVE_SPEED * Time.deltaTime), OBJECT_LAYER);

                }
            }
            else
            {
                //creature is at current destination. Check if they're standing on a trap.
                for (int j = 0; j < trapList.Count; j++)
                {
                    if (creature.transform.position.x == trapList[j].transform.position.x && creature.transform.position.y == trapList[j].transform.position.y)
                        creatureTrapLocations.Add(creature.transform.position);
                }
                /*if (creatureTrapped[i])
                {
                    //remove creature and trap at current position
                    Debug.Log("Removing creature " + i + " at " + creature.transform.position);
                    
                    objectArray[creatureRow[i], creatureCol[i]] = EMPTY;

                    //creature must be destroyed before the trap, otherwise the creature will remain when trap is gone.
                    Destroy(creatureList[i]);
                    //creatureList[i].SetActive(false);
                    RemoveTrap(creature.transform.position);
                                       
                    creatureList.Remove(creature);
                    creatureRow.RemoveAt(i);
                    creatureCol.RemoveAt(i);
                    //trapList.RemoveAt(i);
                    //trapPositions.RemoveAt(i);
                    destinationList.RemoveAt(i);
                    creatureTrapped.RemoveAt(i);

                }*/
            }
        }
       
    }

    void RemoveTrap(Vector2 targetPos)
    {
        foreach(GameObject trap in trapList)
        {
            int i = trapList.IndexOf(trap);
            if (trap.transform.position.x == targetPos.x && trap.transform.position.y == targetPos.y)
            {
                //destroy trap and remove from list.
                Debug.Log("Removing trap " + i + " at " + trap.transform.position);
                Destroy(trapList[i]);
                //trapList[i].SetActive(false);
                trapList.Remove(trap);
            }
        }
    }

    void RemoveCreatures()
    {
        /*IMPORTANT NOTE: when destroying objects, do not use foreach loops as Unity will try to use an object that no longer exists on the next iteration. */
        
        //NOTE: creature must be destroyed before trap
        for (int i = 0; i < creatureList.Count; i++)// GameObject creature in creatureList)
        {
            //int i = creatureList.IndexOf(creature);

            for (int j = 0; j < creatureTrapLocations.Count; j++)
            {
                if (creatureList[i].transform.position.x == creatureTrapLocations[j].x && creatureList[i].transform.position.y == creatureTrapLocations[j].y)
                {
                    //destroy trap and remove from list.
                    Debug.Log("Removing creature " + i + " at " + creatureList[i].transform.position);
                    Destroy(creatureList[i]);
                    //trapList[i].SetActive(false);
                    creatureList.RemoveAt(i);
                    creatureRow.RemoveAt(i);
                    creatureCol.RemoveAt(i);
                    destinationList.RemoveAt(i);
                }
            }
        }

        for (int i = 0; i < trapList.Count; i++)
        {
            for (int j = 0; j < creatureTrapLocations.Count; j++)
            {
                if (trapList[i].transform.position.x == creatureTrapLocations[j].x && trapList[i].transform.position.y == creatureTrapLocations[j].y)
                {
                    //destroy trap and remove from list.
                    Debug.Log("Removing trap " + i + " at " + trapList[i].transform.position);
                    Destroy(trapList[i]);
                    //trapList[i].SetActive(false);
                    trapList.RemoveAt(i);

                    //Update the trapped creatures list. It's fine to do this here because creature was already removed
                    creatureTrapLocations.RemoveAt(j);
                }
            }
        }

    }
}
