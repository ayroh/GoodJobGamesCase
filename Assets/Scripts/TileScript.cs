using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileScript : MonoBehaviour
{
    public PuzzleManagerScript.TileColor tileColor;

    void Start()
    {
        tileColor = (PuzzleManagerScript.TileColor)Random.Range(0, PuzzleManagerScript.instance.GetColorNumber());
        GetComponent<Renderer>().material.SetTexture("_MainTex", Resources.Load<Texture2D>("Images/" + tileColor + "_Default"));
    }

    

}
