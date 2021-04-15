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

    GameObjectManager objManager;       //used to create the level objects and tiles at runtime.
    GameObject player;                  //used to create a player at runtime so that I can get their position and move them when necessary.

    //prefabs
    public GameObject playerPrefab;
    public GameObject treePrefab;
    public GameObject creaturePrefab;
    public GameObject trapPrefab;

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
	List<GameObject> treeList;         //not sure how to use these yet
	List<GameObject> trapList;
    List<Vector2> trapPositions;        //needed to destroy traps when necessary.
    List<Vector2> treePositions;        //needed to check for collision
	List<Vector2> destinationList;		//contains list of creature destinations on map.


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

        objManager = new GameObjectManager();
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
        treeList = new List<GameObject>();
        destinationList = new List<Vector2>();
        trapPositions = new List<Vector2>();
        treePositions = new List<Vector2>();

        ui.SetLivesText(playerLives);
        ui.SetStageText(level);

        LoadLevel(level);
        //objManager.SetupTiles();
        BuildMap(mapList); 
        BuildObjects(objectList);
    }

    // Update is called once per frame
    void Update()
    {
        //check for player input
        if (!controlLocked)
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                //ensure there's noththing blocking player's movement
                if (playerCol > 0 && objectArray[playerRow,playerCol - 1] != TREE && mapArray[playerRow, playerCol - 1] == LAND)
                {
                    playerDestination = new Vector2(player.transform.position.x - 1, player.transform.position.y);
                    controlLocked = true;

                    //if player landed on a trap or a creature, then player dies.


                    Debug.Log("New Player Destination: " + playerDestination);
                }
            }
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                playerDestination = new Vector2(player.transform.position.x + 1, player.transform.position.y);
                controlLocked = true;
                Debug.Log("New Player Destination: " + playerDestination);
            }
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                playerDestination = new Vector2(player.transform.position.x, player.transform.position.y + 1);
                controlLocked = true;
                Debug.Log("New Player Destination: " + playerDestination);
            }
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                playerDestination = new Vector2(player.transform.position.x, player.transform.position.y - 1);
                controlLocked = true;
                Debug.Log("New Player Destination: " + playerDestination);
            }
        }
        UpdateObjects();
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
                mapList.Add(rowArray[i]);
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
                objectList.Add(rowArray[i]);
                objectArray[row, col] = rowArray[i];
                //Debug.Log(objectArray[row, col]);
                col++;
                if (col == MAX_COLS)
                    col = 0;

            }
           row++;
        }

        //Copy data so that levels can be quickly restarted
        //TODO: Might not need to do this step

    }


    void BuildMap(List<string> map)
    {
        /* NOTE: I have to iterate the first for loop in reverse because of how Unity displays tiles starting at the centre of the screen instead of the top left corner.
         If I don't iterate in reverse, then the level will be displayed upside down. */
        
        int i = 0;                  //used to iterate through map list.
        float xOffset = -7.5f; 
        float yOffset = -5.5f;      //Unity doesn't use screen coordinates (origin is in the middle of screen), so I have to use offset to position tiles properly.
        for (int row = MAX_ROWS - 1; row >= 0; row--)
		{
            for (int col = 0; col < MAX_COLS; col++) 
		    {
                GameObject obj = new GameObject();
                switch (map[i])
                {
                    
                    case WATER:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/tile_water"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/tile_land"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_BOTTOM:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_bottom"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_TOP:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_top"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_LEFT:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_left"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_RIGHT:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_right"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_UPLEFT:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_upperleft"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_UPRIGHT:  //TODO: Does not display correctly normally. Had to switch the orignial values to get it to work.
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_upperright"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_BTMLEFT:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_bottomleft"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_BTMRIGHT: //TODO: Does not display correctly normally. Had to switch the orignial values to get it to work.
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_bottomright"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_TOPBTM:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_topbottom"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_TOPBTMLEFT:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_topbottomleft"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    case LAND_TOPBTMRIGHT:
                        objManager.SetupObject(obj, Resources.Load<Sprite>("Tiles/landedge_topbottomright"), new Vector3((float)col + xOffset, (float)row + yOffset, 0));
                        break;

                    default:
                        break;
                }
                i++;
            }
        }
    }


    void BuildObjects(List<string> objects)
    {
        int i = 0;                  //used to iterate through map list.
        float xOffset = -7.5f; // -6.5f;
        float yOffset = -5.5f; // -5.25f;      //Unity doesn't use screen coordinates (origin is in the middle of screen), so I have to use offset to position tiles properly.
        for (int row = MAX_ROWS - 1; row >= 0; row--)
        {
            for (int col = 0; col < MAX_COLS; col++)
            {
                //GameObject obj = new GameObject();
                switch (objects[i])
                {

                    case TREE:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Objects/tree"), new Vector3((float)col + xOffset, (float)row + yOffset, -1));
                        //obj.name = "Tree";
                        Instantiate(treePrefab, new Vector3((float)col + xOffset, (float)row + yOffset, -1), new Quaternion(0, 0, 0, 0));
                        treeList.Add(treePrefab);
                        treePositions.Add(new Vector2((float)col + xOffset, (float)row + yOffset));
                        break;

                    case CREATURE:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Objects/creature_down"), new Vector3((float)col + xOffset, (float)row + yOffset, -1));
                        //obj.name = "Creature";
                        Instantiate(creaturePrefab, new Vector3((float)col + xOffset, (float)row + yOffset, -1), new Quaternion(0, 0, 0, 0));
                        creatureList.Add(creaturePrefab);
                        destinationList.Add(new Vector2((float)col + xOffset, (float)row + yOffset));
                        //Debug.Log("Creature's Starting Pos: " + destinationList[0]); //creaturePrefab.transform.position);
                        break;

                    case TRAP:
                        //objManager.SetupObject(obj, Resources.Load<Sprite>("Objects/trap"), new Vector3((float)col + xOffset, (float)row + yOffset, -1));
                        //obj.name = "Trap";
                        Instantiate(trapPrefab, new Vector3((float)col + xOffset, (float)row + yOffset, -1), new Quaternion(0, 0, 0, 0));
                        trapPositions.Add(new Vector2((float)col + xOffset, (float)row + yOffset));
                        break;

                    case PLAYER:
                        objManager.SetupObject(player, Resources.Load<Sprite>("Objects/player_down"), new Vector3((float)col + xOffset, (float)row + yOffset, -1));
                        player.name = "Player";
                        //Instantiate(playerPrefab, new Vector3((float)col + xOffset, (float)row + yOffset, -1), new Quaternion(0, 0, 0, 0));
                        playerDestination = new Vector2((float)col + xOffset, (float)row + yOffset);
                        Debug.Log("Starting Player Pos: " + playerDestination);
                        playerRow = row;
                        playerCol = col;
                        break;

                    default:
                        break;
                }
                i++;
            }
        }

        int index = 0;
        foreach (Vector2 tree in treePositions)
        {
            Debug.Log("Tree " + index + "'s position: " + tree);
            index++;
        }
    }


    void UpdateObjects()
    {
        //Used to move all objects when necessary.

        if (player.transform.position.x < playerDestination.x)  //player moves right
        {
            //The float values when the player moves are extremely precise, so need to do an additional check for when player is close to destination.
            //Without this check, the player sprite appears to jitter and never really finishes moving.
            float posDiffX = playerDestination.x - player.transform.position.x;
            if (posDiffX > 0 && posDiffX < 0.05f)
            {
                player.transform.position = new Vector3(playerDestination.x, player.transform.position.y, -1);
            }
            else
            {
                player.transform.position = new Vector3(player.transform.position.x + (MOVE_SPEED * Time.deltaTime), player.transform.position.y, -1);
                //TODO: while player is moving, player sprite animates
            }

        }
        else if (player.transform.position.x > playerDestination.x) //player moves left
        {
            float posDiffX = player.transform.position.x - playerDestination.x;
            if (posDiffX > 0 && posDiffX < 0.05f)
                player.transform.position = new Vector3(playerDestination.x, player.transform.position.y, -1);
            else
                player.transform.position = new Vector3(player.transform.position.x - (MOVE_SPEED * Time.deltaTime), player.transform.position.y, -1);
           
        }
        else if (player.transform.position.y < playerDestination.y) //player moves up
        {
            float posDiffY = playerDestination.y - player.transform.position.y;
            if (posDiffY > 0 && posDiffY < 0.05f)
                player.transform.position = new Vector3(player.transform.position.x, playerDestination.y, - 1);
            else
                player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + (MOVE_SPEED * Time.deltaTime), -1);

        }
        else if (player.transform.position.y > playerDestination.y) //player moves down
        {
            float posDiffY = player.transform.position.y - playerDestination.y;
            if (posDiffY > 0 && posDiffY < 0.05f)
                player.transform.position = new Vector3(player.transform.position.x, playerDestination.y, -1);
            else
                player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y - (MOVE_SPEED * Time.deltaTime), -1);

        }
        else //at current destination
        {
            controlLocked = false;
            //TODO: stop sprite animation
            
            Debug.Log("At current destination: " + player.transform.position);
        }

       
    }
}
