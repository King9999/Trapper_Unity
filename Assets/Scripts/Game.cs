using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using UnityEngine.UI;

//This script is used to setup and control the game screen. It's also used to create new levels when the player completes the objective.

public class Game : MonoBehaviour
{
    //UI and game states
    int score;
    public UI ui;           //need this to make changes to UI such as stage number or lives.
    int levelIndex;         //used to navigate levels in XML file.
    string currentState;    //used to prevent animations from repeating itself

    [Header("Level Info")]
    public TextAsset levelFile;
    public int level;
    public int playerLives;
    int enemyCount;         //current # of uncaptured creatures remaining.
    int enemyTotal;
    bool gameOver;
    bool controlLocked;     //prevents any player input during win screen.
    float waitTimer;          //used to delay screen changes in frames.
    bool levelComplete;
    bool levelReset;        //used to check when level is reloaded.
    bool audioPlayed;       //used to ensure the win audio is played only once.

    [Header("Audio & Animation")]
    public AudioSource audioSource;
    public AudioClip audioFall;
    public AudioClip audioWin;
    public Animator transition;
    public Animator winImage;
    

    //GameObjectManager objManager;       //used to create the level objects and tiles at runtime.
    GameObject player;                  //used to create a player at runtime so that I can get their position and move them when necessary.

    //prefabs
    [Header("Prefabs")]
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

    List<GameObject> mapList;               //contains map game objects (water, land)
    List<string> objectList;
    List<string> initMapList;           //These lists are used to draw the map and objects.
    List<string> initObjList;

    int playerRow, playerCol;           //tracks player's position in the object array. Used for collision checking.         

    const string ANIM_WIN_STATE = "Win_Anim";
    const string ANIM_STOP_STATE = "Win_Stop";
    const int MAX_ROWS = 12;
	const int MAX_COLS = 16;
	const int TILE_SIZE = 64;
    const float MOVE_SPEED = 4f;      //will be affected by deltaTime
	const int MAX_LEVEL = 10;
    
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

    //player data
    bool playerDead;
    bool setNewLevel;

    Vector2 playerDestination;  //used to move player to new spot on map.
  

    //creatures & objects
    List<GameObject> creatureList;
    List<bool> creatureTrapped;         //tracks when creatures land in traps.
    List<Vector2> creatureTrapLocations;        //tracks which creatures are trapped.
	List<GameObject> treeList;         //not sure how to use these yet
	List<GameObject> trapList;
	List<Vector2> destinationList;		    //contains list of creature destinations on map.
    List<int> creatureRow;                 
    List<int> creatureCol;                   //tracks each creature's position in the object array.


    // Start is called before the first frame update
    void Start()
    {
        setNewLevel = false;
        currentState = ANIM_STOP_STATE;
        levelComplete = false;
        levelIndex = 0;
        //playerLives = 2;
        playerDead = false;
        controlLocked = false;
        waitTimer = 0;
        playerRow = 0;
        playerCol = 0;
        creatureRow = new List<int>();
        creatureCol = new List<int>();
        levelReset = false;
        audioPlayed = false;

        //objManager = new GameObjectManager();
        player = new GameObject();

        mapArray = new string[MAX_ROWS, MAX_COLS];
        objectArray = new string[MAX_ROWS, MAX_COLS];
        initMapArray = new string[MAX_ROWS, MAX_COLS];
        initObjArray = new string[MAX_ROWS, MAX_COLS];

        mapList = new List<GameObject>();
        //objectList = new List<string>();
        //initMapList = new List<string>();
        //initObjList = new List<string>();

        creatureList = new List<GameObject>();
        creatureTrapped = new List<bool>();
        creatureTrapLocations = new List<Vector2>();
        treeList = new List<GameObject>();
        trapList = new List<GameObject>();
        destinationList = new List<Vector2>();

        ui.SetLivesText(playerLives);
        ui.SetStageText(level);

        LoadLevel(level);
        //objManager.SetupTiles();
        BuildMap(mapArray); 
        BuildObjects(objectArray);
    }

    IEnumerator PlayerCreatureCollision()
    {
        int i = 0;
        while (!playerDead && i < creatureList.Count)
        {
            float diffX = player.transform.position.x - creatureList[i].transform.position.x;
            float diffY = player.transform.position.y - creatureList[i].transform.position.y;
            
            if (Mathf.Abs(diffX) >= 0 && Mathf.Abs(diffX) <= 0.05f && Mathf.Abs(diffY) >= 0 && Mathf.Abs(diffY) <= 0.05f)
            {
                audioSource.PlayOneShot(audioFall);
                playerDead = true;
               
            }
            else
                i++;
        }
        yield return new WaitForSeconds(0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        
        if (!gameOver)
        {
           
            //lose condition
            if (playerDead)
            {
                if (playerLives <= 0)
                    gameOver = true;
                else
                {
                    if (!levelReset)
                    {
                        ResetLevel();
                    }
                    else
                    {
                        //once we get here, that means new level has finished reloading.
                        playerLives--;
                        ui.SetLivesText(playerLives);
                        playerDead = false;
                        levelReset = false;
                    }
                }
            }


            //win condition
            if (levelComplete)
            {
                controlLocked = true;
                                
                StartCoroutine(PlayWinAnimation());
               
                //load up next level
                if(!levelReset)
                {
                    setNewLevel = true;
                    ResetLevel();
                }
                else
                {
                    playerLives++;
                    ui.SetLivesText(playerLives);
                    ui.SetStageText(level);
                    controlLocked = false;
                    playerDead = false;
                    levelReset = false;
                    levelComplete = false;
                    audioPlayed = false;
                }
               
            }
            else
            {
                //game is ongoing
                CheckForInput();
               // UpdateObjects();

                if (creatureTrapLocations.Count > 0)
                {
                    RemoveCreatures();
                    if (creatureList.Count <= 0)
                        levelComplete = true;
                }
            }
        }
        else  
        {
            //game is over. Send player back to title screen.
        }
    }

    private void FixedUpdate()  //used this method for any movement or physics
    {
        UpdateObjects();
        StartCoroutine(PlayerCreatureCollision());
    }

    void ChangeAnimationState(Animator anim, string animState)
    {
        if (currentState == animState)
            return;

        anim.Play(animState);

        currentState = animState;
    }

    IEnumerator PlayWinAnimation()
    {
        //setNewLevel = false;
        ChangeAnimationState(winImage, ANIM_WIN_STATE);
       
        if (!audioPlayed)
        {
            audioSource.PlayOneShot(audioWin);
            audioPlayed = true;
        }

        yield return new WaitForSeconds(2f);


        ChangeAnimationState(winImage, ANIM_STOP_STATE);
        
        //setNewLevel = true;
    }

    //All user input is checked here. Must go into Update method
    void CheckForInput()
    {
        //reset level quickly but lose a life
        if (Input.GetKeyDown(KeyCode.Space))
            playerDead = true;

        //check for player input
        if (!controlLocked && !playerDead)
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                //ensure there's noththing blocking player's movement
                if (playerCol > 0 && !objectArray[playerRow, playerCol - 1].Equals(TREE) && mapArray[playerRow, playerCol - 1].Equals(LAND))
                {
                    //Debug.Log("Player Row: " + playerRow + " Player Col: " + playerCol);
                    //Debug.Log("ObjectArray value at " + playerRow + ", " + (playerCol - 1) + ": " + objectArray[playerRow, playerCol - 1]);
                    //Debug.Log("MapArray value at " + playerRow + ", " + (playerCol - 1) + ": " + mapArray[playerRow, playerCol - 1]);

                    //update player animation
                    player.GetComponent<Animator>().SetTrigger("Left");

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
                            //update creature animation
                            creature.GetComponent<Animator>().SetTrigger("Right");
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
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                if (playerCol < MAX_COLS - 1 && !objectArray[playerRow, playerCol + 1].Equals(TREE) && mapArray[playerRow, playerCol + 1].Equals(LAND))
                {
                    //Debug.Log("Player Row: " + playerRow + " Player Col: " + playerCol);
                    // Debug.Log("ObjectArray value at " + playerRow + ", " + (playerCol + 1) + ": " + objectArray[playerRow, playerCol + 1]);
                    //Debug.Log("MapArray value at " + playerRow + ", " + (playerCol + 1) + ": " + mapArray[playerRow, playerCol + 1]);

                    //update player animation
                    player.GetComponent<Animator>().SetTrigger("Right");
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

                            //update creature animation
                            creature.GetComponent<Animator>().SetTrigger("Left");
                            objectArray[creatureRow[i], creatureCol[i]] = EMPTY;
                            creatureCol[i]--;
                            objectArray[creatureRow[i], creatureCol[i]] = CREATURE;
                            destinationList[i] = new Vector2(creature.transform.position.x - 1, creature.transform.position.y);
                            //Debug.Log("New Creature Pos: " + destinationList[i]);
                        }
                    }
                }
            }
            else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                if (playerRow > 0 && !objectArray[playerRow - 1, playerCol].Equals(TREE) && mapArray[playerRow - 1, playerCol].Equals(LAND))
                {
                    //Debug.Log("Player Row: " + playerRow + " Player Col: " + playerCol);
                    // Debug.Log("ObjectArray value at " + (playerRow - 1) + ", " + playerCol + ": " + objectArray[playerRow - 1, playerCol]);
                    //Debug.Log("MapArray value at " + (playerRow - 1) + ", " + playerCol + ": " + mapArray[playerRow - 1, playerCol]);

                    //update player animation
                    player.GetComponent<Animator>().SetTrigger("Up");
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
                            //update creature animation
                            creature.GetComponent<Animator>().SetTrigger("Down");
                            objectArray[creatureRow[i], creatureCol[i]] = EMPTY;
                            creatureRow[i]++;
                            objectArray[creatureRow[i], creatureCol[i]] = CREATURE;
                            destinationList[i] = new Vector2(creature.transform.position.x, creature.transform.position.y - 1);
                            //Debug.Log("New Creature Pos: " + destinationList[i]);
                        }
                    }
                }
            }
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                if (playerRow < MAX_ROWS - 1 && !objectArray[playerRow + 1, playerCol].Equals(TREE) && mapArray[playerRow + 1, playerCol].Equals(LAND))
                {
                    //Debug.Log("Player Row: " + playerRow + " Player Col: " + playerCol);
                    // Debug.Log("ObjectArray value at " + (playerRow + 1) + ", " + playerCol + ": " + objectArray[playerRow + 1, playerCol]);
                    // Debug.Log("MapArray value at " + (playerRow + 1) + ", " + playerCol + ": " + mapArray[playerRow + 1, playerCol]);

                    //update player animation
                    player.GetComponent<Animator>().SetTrigger("Down");
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
                            //update creature animation
                            creature.GetComponent<Animator>().SetTrigger("Up");
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

        //remove any previous map game objects in case there was any left over.
        for (int i = 0; i < mapList.Count; i++)
            Destroy(mapList[i]);

        mapList.Clear();

        if (level < 1 || level > MAX_LEVEL)
            level = 1;

        this.level = level;

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
                        mapList.Add(Instantiate(waterPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));                       
                        //Debug.Log("Water Location: " + waterObj.transform.position);
                        break;

                    case LAND:
                        mapList.Add(Instantiate(landPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_BOTTOM:
                        mapList.Add(Instantiate(landBottomPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_TOP:
                        mapList.Add(Instantiate(landTopPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_LEFT:
                        mapList.Add(Instantiate(landLeftPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_RIGHT:
                        mapList.Add(Instantiate(landRightPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_UPLEFT:
                        mapList.Add(Instantiate(landTopLeftPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_UPRIGHT: 
                        mapList.Add(Instantiate(landTopRightPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_BTMLEFT:
                        mapList.Add(Instantiate(landBottomLeftPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_BTMRIGHT: 
                        mapList.Add(Instantiate(landBottomRightPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_TOPBTM:
                        mapList.Add(Instantiate(landTopBottomPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_TOPBTMLEFT:
                        mapList.Add(Instantiate(landTopBottomLeftPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    case LAND_TOPBTMRIGHT:
                        mapList.Add(Instantiate(landTopBottomRightPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, MAP_LAYER), new Quaternion(0, 0, 0, 0)));
                        break;

                    default:
                        break;

                        
                }
                i++;
            }
        }
    }


    void BuildObjects(string[,] objects)
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
                        GameObject tree = Instantiate(treePrefab, new Vector3((float)col + xOffset, yOffset - (float)row, OBJECT_LAYER), new Quaternion(0, 0, 0, 0));
                        treeList.Add(tree);
                        break;

                    case CREATURE:
                        GameObject creature = Instantiate(creaturePrefab, new Vector3((float)col + xOffset, yOffset - (float)row, OBJECT_LAYER), new Quaternion(0, 0, 0, 0));
                        creatureList.Add(creature);
                        destinationList.Add(creature.transform.position);
                        creatureRow.Add(row);
                        creatureCol.Add(col);
                        creatureTrapped.Add(false);
                        //Debug.Log("Creature's Starting Pos: " + destinationList[0]); //creaturePrefab.transform.position);
                        break;

                    case TRAP:
                        GameObject trap = Instantiate(trapPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, TRAP_LAYER), new Quaternion(0, 0, 0, 0));
                        trapList.Add(trap);
                        break;

                    case PLAYER:
                        player = Instantiate(playerPrefab, new Vector3((float)col + xOffset, yOffset - (float)row, OBJECT_LAYER), new Quaternion(0, 0, 0, 0));
                        playerDestination = player.transform.position;
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


    //Used to move all objects when necessary. Must go into FixedUpdate method
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
           
            //check if player is on trap or creature

            /*int i = 0;
            while (!playerDead && i < creatureList.Count)
            {
                if (player.transform.position.x == creatureList[i].transform.position.x && player.transform.position.y == creatureList[i].transform.position.y)
                {
                    audioSource.PlayOneShot(audioFall);
                    playerDead = true;
                }
                else
                    i++;
            }*/

            int i = 0;
            while (!playerDead && i < trapList.Count)
            {
                if (player.transform.position.x == trapList[i].transform.position.x && player.transform.position.y == trapList[i].transform.position.y)
                {
                    audioSource.PlayOneShot(audioFall);
                    playerDead = true;
                }
                else
                    i++;
            }

           // Debug.Log("At current destination: " + player.transform.position);
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
                //creature is at current destination. Check if they're standing on a trap. In situations where the player and creature land on a
                //trap at the same time, player dies first. Without this check, the traps may not come back on map reset, causing a soft lock.
                if (!playerDead)
                {
                    for (int j = 0; j < trapList.Count; j++)
                    {
                        if (creature.transform.position.x == trapList[j].transform.position.x && creature.transform.position.y == trapList[j].transform.position.y)
                        {
                            creatureTrapLocations.Add(creature.transform.position);
                            objectArray[creatureRow[i], creatureCol[i]] = EMPTY;
                        }
                    }
                }
               
            }
        }
       
    }


    void RemoveCreatures()
    {
        /*IMPORTANT NOTE: when destroying objects, do not use foreach loops as Unity will try to use an object that no longer exists on the next iteration. */

        //NOTE: creature must be destroyed before trap
        
        for (int i = 0; i < creatureList.Count; i++)
        {

            for (int j = 0; j < creatureTrapLocations.Count; j++)
            {
                if (creatureList[i].transform.position.x == creatureTrapLocations[j].x && creatureList[i].transform.position.y == creatureTrapLocations[j].y)
                {
                    //destroy trap and remove from list.
                    Debug.Log("Removing creature " + i + " at " + creatureList[i].transform.position);
                    audioSource.PlayOneShot(audioFall);
                    Destroy(creatureList[i]);
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
                    trapList.RemoveAt(i);

                    //Update the trapped creatures list. It's fine to do this here because creature was already removed
                    creatureTrapLocations.RemoveAt(j);
                }
            }
        }
       

    }



    void ResetLevel()
    {
        StartCoroutine(RestartStage());

    }

    IEnumerator RestartStage()
    {
        ChangeAnimationState(transition, "Crossfade_Start");

        //waiting until screen is fully black
        while (transition.GetComponent<CanvasGroup>().alpha < 1.0f)
        {
            yield return null;
        }
        

        //clear all objects and rebuild level.
        for (int i = 0; i < trapList.Count; i++)
            Destroy(trapList[i]);

        for (int i = 0; i < creatureList.Count; i++)
            Destroy(creatureList[i]);

        for (int i = 0; i < treeList.Count; i++)
            Destroy(treeList[i]);

        trapList.Clear();
        treeList.Clear();
        creatureList.Clear();
        creatureRow.Clear();
        creatureCol.Clear();
        destinationList.Clear();
        creatureTrapped.Clear();

        Destroy(player);

        if (levelComplete && setNewLevel == true)   //want to ensure this step is only performed once
        {
            LoadLevel(++level);
            BuildMap(mapArray);
            setNewLevel = false;
        }
        objectArray = (string[,])initObjArray.Clone();
        BuildObjects(objectArray);

        levelReset = true;
        ChangeAnimationState(transition, "Crossfade_End");
    }

}
