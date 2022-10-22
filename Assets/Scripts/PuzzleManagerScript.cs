using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

public class PuzzleManagerScript : MonoBehaviour
{
    public static PuzzleManagerScript instance;

    public enum TileColor { Yellow, Blue, Green, Pink, Purple, Red};

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
    private List<TileScript> tileDestroyTemp;


    [Header("Floor")]
    [SerializeField] private BoxCollider floorCollider;

    private float screenHeight;
    private Camera mainCam;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this);
    }

    void Start()
    {
        tileDestroyTemp = new List<TileScript>();
        tilePrefab = Resources.Load<GameObject>("Prefabs/TilePrefab");
        mainCam = Camera.main;
        tileHit = new RaycastHit[1];
        tileLayer = 1 << LayerMask.NameToLayer("Tile");
        StartCoroutine(InstantiateTiles());
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0) && Physics.RaycastNonAlloc(mainCam.ScreenPointToRay(Input.mousePosition), tileHit, 20f, tileLayer) == 1)
        {
            tileDestroyTemp.Clear();
            tileDestroyTemp.Add(tileHit[0].transform.GetComponent<TileScript>());
            CheckBlastable();
            return;
            if (tileDestroyTemp.Count != 1)
                DestroyTiles();
            else
                print("DANG");
        }
    }

    #region Blast

    private void CheckBlastable(int x = -1, int y = -1)
    {
        if (x == -1 && y == -1)
            x = tiles.FindIndex(obj => (y = obj.FindIndex(obj => obj.Equals(tileDestroyTemp[0]))) != -1);
        print("x: " + x + " | y: " + y + " | ben: " + tiles[x][y].tileColor);
        print(tiles[x + 1][y].tileColor);
        return;
        if (x != 0 && tiles[x - 1][y].tileColor == tileDestroyTemp[0].tileColor && !tileDestroyTemp.Contains(tiles[x - 1][y]))
            tileDestroyTemp.Add(tiles[x - 1][y]);
        if (x != column - 1 && tiles[x + 1][y].tileColor == tileDestroyTemp[0].tileColor && !tileDestroyTemp.Contains(tiles[x + 1][y]))
            tileDestroyTemp.Add(tiles[x + 1][y]);
        if (y != 0 && tiles[x][y - 1].tileColor == tileDestroyTemp[0].tileColor && !tileDestroyTemp.Contains(tiles[x][y - 1]))
            tileDestroyTemp.Add(tiles[x][y - 1]);
        if (y != row - 1 && tiles[x][y + 1].tileColor == tileDestroyTemp[0].tileColor && !tileDestroyTemp.Contains(tiles[x][y + 1]))
            tileDestroyTemp.Add(tiles[x][y + 1]);
    }


    private void DestroyTiles()
    {
        for (int i = 0; i < tileDestroyTemp.Count; ++i)
            Destroy(tileDestroyTemp[i].gameObject);
    }

    #endregion


    #region Instantiate / End / Restart
    private IEnumerator InstantiateTiles()
    {
        tiles = new List<List<TileScript>>();
        floorCollider.size = new Vector3(column, 1f, 1f);
        screenHeight = Screen.height;
        float startX, startY;
        Renderer tileRenderer = tilePrefab.GetComponent<Renderer>();
        startX = floorCollider.bounds.min.x + tileRenderer.bounds.size.x / 2;
        startY = mainCam.ScreenToWorldPoint(new Vector3(0, screenHeight, (-mainCam.transform.position.z + floorCollider.transform.position.z))).y + tileRenderer.bounds.size.y;
        Quaternion rot = Quaternion.Euler(new Vector3(90f, 180f, 0));
        for (int i = 0; i < row; ++i){
            tiles.Add(new List<TileScript>());
            for (int j = 0; j < column; ++j){
                tiles[i].Add(Instantiate(tilePrefab, new Vector3(startX, startY, floorCollider.transform.position.z), rot, tileParent).GetComponent<TileScript>());
                startX += tileRenderer.bounds.size.x;
                yield return new WaitForSeconds(.05f);
            }
            startX = floorCollider.bounds.min.x + tileRenderer.bounds.size.x / 2;
            startY += tileRenderer.bounds.size.y;
        }
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
