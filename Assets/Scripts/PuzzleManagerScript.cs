using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

public class PuzzleManagerScript : MonoBehaviour {

    // For letting all managers reaching this script
    public static PuzzleManagerScript instance;
    

    // For storing coordinates in a list
    public class TileCoordinate{
        public TileCoordinate(int X, int Y) {
            x = X;
            y = Y;
        }
        public int x, y;
    }

    [Header("Expose Variables")]
    [SerializeField] [Range(2, 10)] private int row;
    [SerializeField] [Range(2, 10)] private int column;
    [SerializeField] [Range(1, 6)] private int color;
    [SerializeField] private int aCondition;
    [SerializeField] private int bCondition;
    [SerializeField] private int cCondition;


    [Header("Tile")]
    [SerializeField] private Transform tileParent;
    private List<List<TileScript>> tiles;
    private GameObject tilePrefab;
    private RaycastHit[] tileHit;
    private int tileLayer;
    private int minimumBlastableNumber = 2;
    private List<TileCoordinate> tileDestroyTemp;
    private List<TileCoordinate> adjacentTiles;


    [Header("Floor")]
    [SerializeField] private BoxCollider floorCollider;

    [Header("Pooler")]
    [SerializeField] private PoolManagerScript poolManager;


    private float screenHeight;
    private Camera mainCam;

    // Top of camera view which is outside of camera and has 'Z' coordinate same as floor to instantiate blocks on top of the screen
    private Vector3 topOfCameraView;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    private void OnValidate() {
        // Protect A-B-C condition constraints
        if (aCondition > bCondition || aCondition > cCondition || bCondition > cCondition)
            Debug.LogError("A-B-C conditions are not correct");
    }

    void Start()
    {
        // Instantiating
        tileDestroyTemp = new List<TileCoordinate>();
        adjacentTiles = new List<TileCoordinate>();
        tilePrefab = Resources.Load<GameObject>("Prefabs/TilePrefab");
        mainCam = Camera.main;
        tileHit = new RaycastHit[1];
        tileLayer = 1 << LayerMask.NameToLayer("Tile");

        // Start Pooler
        poolManager.InstantiatePooler(row * column);

        // Start game
        StartCoroutine(InstantiateTiles());
    }

    private void Update()
    {
        // RaycastNonAlloc is don't creates garbage and we dont need more than 1 input so 1 element array is great for us using tileHit as RaycastHit[1]
        if(Input.GetMouseButtonDown(0) && Physics.RaycastNonAlloc(mainCam.ScreenPointToRay(Input.mousePosition), tileHit, 100f, tileLayer) == 1)
        {
            // Clear blastable array
            tileDestroyTemp.Clear();

            // Check all blastables and add them to tileDestroyTemp
            CheckBlastable();

            // If Blastables are more than minimumBlastableNumber then destroy them
            if (tileDestroyTemp.Count >= minimumBlastableNumber)
                DestroyAddTiles();
        }
    }

    #region Blast

    // Starts from itself it controls its all 4 directions colors and if that coordinates it is not already in the tileDestroyTemp list
    // then add it to there and go to that coordinate with CheckBlastable() function as well recursively
    private void CheckBlastable(int x = -1, int y = -1){
        if (x == -1 && y == -1) {
            TileScript tapped = tileHit[0].transform.GetComponent<TileScript>();
            x = tiles.FindIndex(obj => (y = obj.FindIndex(obj => obj.Equals(tapped))) != -1);
            tileDestroyTemp.Add(new TileCoordinate(x, y));
        }
        if (x != 0 && tiles[x - 1][y].tileColor == tiles[tileDestroyTemp[0].x][tileDestroyTemp[0].y].tileColor && !tileDestroyTemp.Any(obj => obj.x == x - 1 && obj.y == y)) {
            tileDestroyTemp.Add(new TileCoordinate(x - 1, y));
            CheckBlastable(x - 1, y);
        }
        if (x != column - 1 && tiles[x + 1][y].tileColor == tiles[tileDestroyTemp[0].x][tileDestroyTemp[0].y].tileColor && !tileDestroyTemp.Any(obj => obj.x == x + 1 && obj.y == y)) {
            tileDestroyTemp.Add(new TileCoordinate(x + 1, y));
            CheckBlastable(x + 1, y);
        }
        if (y != 0 && tiles[x][y - 1].tileColor == tiles[tileDestroyTemp[0].x][tileDestroyTemp[0].y].tileColor && !tileDestroyTemp.Any(obj => obj.x == x && obj.y == y - 1)) {
            tileDestroyTemp.Add(new TileCoordinate(x, y - 1));
            CheckBlastable(x, y - 1);
        }
        if (y != row - 1 && tiles[x][y + 1].tileColor == tiles[tileDestroyTemp[0].x][tileDestroyTemp[0].y].tileColor && !tileDestroyTemp.Any(obj => obj.x == x && obj.y == y + 1)) {
            tileDestroyTemp.Add(new TileCoordinate(x, y + 1));
            CheckBlastable(x, y + 1);
        }
    }


    private void DestroyAddTiles(){
        // To use prefabs bounds
        Renderer tileRenderer = tilePrefab.GetComponent<Renderer>();

        Vector3 instantiatePos = default;
        Quaternion rot = Quaternion.Euler(new Vector3(90f, 180f, 0));

        // On top of camera view to transform blocks position to outside of camera
        float YPos = topOfCameraView.y + tileRenderer.bounds.size.y;

        // Sorts tileDestroyTemp from biggest 'y' to smallest because when we List.RemoveAt() we change upper blocks coordinates so we need to destroy them up to down
        tileDestroyTemp.Sort((t1, t2) => t2.y.CompareTo(t1.y));

        for (int i = 0; i < tileDestroyTemp.Count;++i) {
            // X of erased block, Y from camera view, Z from floorColliders z
            instantiatePos = new Vector3(tiles[tileDestroyTemp[i].x][tileDestroyTemp[i].y].transform.position.x, YPos, floorCollider.transform.position.z);
            YPos += 1.1f;

            // Release that tile back to pooler
            poolManager.Release(tiles[tileDestroyTemp[i].x][tileDestroyTemp[i].y].gameObject);

            // Get new tile from pooler, reposition it and randomize its color.
            tiles[tileDestroyTemp[i].x].Add(poolManager.Get().GetComponent<TileScript>());
            tiles[tileDestroyTemp[i].x][tiles[tileDestroyTemp[i].x].Count - 1].transform.position = instantiatePos;
            tiles[tileDestroyTemp[i].x][tiles[tileDestroyTemp[i].x].Count - 1].SetRandomColor();

            // Remove old tile from list
            tiles[tileDestroyTemp[i].x].RemoveAt(tileDestroyTemp[i].y);
        }

        // Check new A-B-C conditions
        CheckImages();
    }

    #endregion

    #region Images / Deadlock

    bool isDeadlock;

    // Check A-B-C conditions
    private void CheckImages() {
        isDeadlock = true;
        for (int i = 0;i < column;++i) {
            for (int j = 0;j < row;++j) {
                adjacentTiles.Clear();
                adjacentTiles.Add(new TileCoordinate(i, j));

                // Count Adjacent blocks recursively and adds them to adjacentTiles list
                CountAdjacents(i, j);

                // Set images according to adjacentTiles list element count
                if (adjacentTiles.Count <= aCondition)
                    SetAdjacentImages(Icon._Default);
                else if(adjacentTiles.Count > aCondition && adjacentTiles.Count <= bCondition)
                    SetAdjacentImages(Icon._A);
                else if (adjacentTiles.Count > bCondition && adjacentTiles.Count <= cCondition)
                    SetAdjacentImages(Icon._B);
                else if (adjacentTiles.Count > cCondition)
                    SetAdjacentImages(Icon._C);
            }
        }

        // If there aren't any blastable tiles then shuffles them
        if (isDeadlock)
            ShuffleTiles();
    }


    private void ShuffleTiles() {
        // First Checks if there are enough color to play. For instance if 2x3 game has 6 colors then no colors will be blastable even infinite amount of shuffling
        // If there arent than reinstantiates board
        if (!CheckColorAmounts())
            return;

        TileScript tempObj;
        Vector3 tempPos;
        int iIndex, jIndex;
        for(int i = 0;i < tiles.Count;++i) {
            for(int j = 0;j < tiles[i].Count;++j) {
                jIndex = Random.Range(0, tiles[iIndex = Random.Range(0, tiles.Count)].Count);

                // Checks if random indexes are same with itself
                if(iIndex == i && jIndex == j) {
                    --j;
                    continue;
                }

                // Change position and list indexes from another element
                tempPos = tiles[i][j].transform.position;
                tiles[i][j].transform.position = tiles[iIndex][jIndex].transform.position;
                tiles[iIndex][jIndex].transform.position = tempPos;

                tempObj = tiles[i][j];
                tiles[i][j] = tiles[iIndex][jIndex];
                tiles[iIndex][jIndex] = tempObj;
            }
        }
        Debug.Log("Tiles Shuffled");
        CheckImages();
    }

    // Counts colors of tiles. If there aren't enough reinstantiates board
    private bool CheckColorAmounts() {
        bool gameCanContinue = false;
        int[] colors = new int[color];
        for (int i = 0;i < tiles.Count;++i) {
            for (int j = 0;j < tiles[i].Count;++j) {
                switch (tiles[i][j].tileColor) {
                    case TileColor.Yellow:
                        ++colors[0];
                        break;
                    case TileColor.Blue:
                        ++colors[1];
                        break;
                    case TileColor.Green:
                        ++colors[2];
                        break;
                    case TileColor.Pink:
                        ++colors[3];
                        break;
                    case TileColor.Purple:
                        ++colors[4];
                        break;
                    case TileColor.Red:
                        ++colors[5];
                        break;
                }
            }
        }
        for (int i = 0;i < colors.Length;++i)
            if (colors[i] > 1)
                gameCanContinue = true;
        if (!gameCanContinue) {
            Debug.LogError("There is not enough color. Reinstantiating tiles.");
            RestartGame();
        }

        return gameCanContinue;

    }


    private void SetAdjacentImages(Icon iconType) {
        for (int i = 0;i < adjacentTiles.Count;++i)
            tiles[adjacentTiles[i].x][adjacentTiles[i].y].SetImage(iconType);
    }

    // Counts adjacent images colors while checking for deadlock
    private void CountAdjacents(int x = -1, int y = -1) {
        if (x != 0 && tiles[x - 1][y].tileColor == tiles[adjacentTiles[0].x][adjacentTiles[0].y].tileColor && !adjacentTiles.Any(obj => obj.x == x - 1 && obj.y == y)) {
            adjacentTiles.Add(new TileCoordinate(x - 1, y));
            isDeadlock = false;
            CountAdjacents(x - 1, y);
        }
        if (x != column - 1 && tiles[x + 1][y].tileColor == tiles[adjacentTiles[0].x][adjacentTiles[0].y].tileColor && !adjacentTiles.Any(obj => obj.x == x + 1 && obj.y == y)) {
            adjacentTiles.Add(new TileCoordinate(x + 1, y));
            isDeadlock = false;
            CountAdjacents(x + 1, y);
        }
        if (y != 0 && tiles[x][y - 1].tileColor == tiles[adjacentTiles[0].x][adjacentTiles[0].y].tileColor && !adjacentTiles.Any(obj => obj.x == x && obj.y == y - 1)) {
            adjacentTiles.Add(new TileCoordinate(x, y - 1));
            isDeadlock = false;
            CountAdjacents(x, y - 1);
        }
        if (y != row - 1 && tiles[x][y + 1].tileColor == tiles[adjacentTiles[0].x][adjacentTiles[0].y].tileColor && !adjacentTiles.Any(obj => obj.x == x && obj.y == y + 1)) {
            adjacentTiles.Add(new TileCoordinate(x, y + 1));
            isDeadlock = false;
            CountAdjacents(x, y + 1);
        }
    }

    #endregion

    #region Instantiate / End / Restart

    // Resizes floor according to column count and starts from its start position. Instantiates all tiles to pooler.
    private IEnumerator InstantiateTiles(){
        tiles = new List<List<TileScript>>();
        floorCollider.size = new Vector3(column, 3f, 1f);
        screenHeight = Screen.height;
        Renderer tileRenderer = tilePrefab.GetComponent<Renderer>();
        topOfCameraView = mainCam.ScreenToWorldPoint(new Vector3(0, screenHeight, (-mainCam.transform.position.z + floorCollider.transform.position.z)));

        float startX = floorCollider.bounds.min.x + tileRenderer.bounds.size.x / 2;
        float startY = topOfCameraView.y + tileRenderer.bounds.size.y;
        for (int i = 0; i < column; ++i){
            tiles.Add(new List<TileScript>());
            for (int j = 0; j < row; ++j){
                tiles[i].Add(poolManager.Get().GetComponent<TileScript>());
                tiles[i][j].transform.position = new Vector3(startX, startY, floorCollider.transform.position.z);
                tiles[i][j].SetRandomColor();
                startY += tileRenderer.bounds.size.y + 0.01f;
                yield return new WaitForSeconds(.02f);
            }
            startX += tileRenderer.bounds.size.x + 0.01f;
            startY = topOfCameraView.y + tileRenderer.bounds.size.y;
        }
        CheckImages();
    }




    public void RestartGame()
    {
        for(int i = 0;i < tiles.Count;++i)
            for (int j = 0; j < tiles[i].Count; ++j)
                Destroy(tiles[i][j].gameObject);
        tiles = null;
        StartCoroutine(InstantiateTiles());
    }

    #endregion

    #region Getter / Setter

    public int GetColorNumber() => color;

    #endregion
}
