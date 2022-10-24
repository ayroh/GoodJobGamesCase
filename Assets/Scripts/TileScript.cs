using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileScript : MonoBehaviour
{
    [SerializeField] private Renderer rend;
    public TileColor tileColor;



    public void SetRandomColor() {
        tileColor = (TileColor)Random.Range(0, PuzzleManagerScript.instance.GetColorNumber());
        rend.material.SetTexture("_MainTex", Resources.Load<Texture2D>("Images/" + tileColor + "_Default"));
    }

    public void SetImage(Icon iconType) => rend.material.SetTexture("_MainTex", Resources.Load<Texture2D>("Images/" + tileColor + iconType));

}
