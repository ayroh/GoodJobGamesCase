using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileScript : MonoBehaviour
{
    [SerializeField] private Renderer rend;
    public PuzzleManagerScript.TileColor tileColor;



    public void SetRandomColor() {
        tileColor = (PuzzleManagerScript.TileColor)Random.Range(0, PuzzleManagerScript.instance.GetColorNumber());
        rend.material.SetTexture("_MainTex", Resources.Load<Texture2D>("Images/" + tileColor + "_Default"));
    }

    public void SetImage(int iconNumber) {
        string iconType;
        switch (iconNumber) {
            case 0:
                iconType = new string("_Default");
                break;
            case 1:
                iconType = new string("_A");
                break;
            case 2:
                iconType = new string("_B");
                break;
            case 3:
                iconType = new string("_C");
                break;
            default:
                Debug.LogError("Wrong Texture Name");
                return;
        }
        rend.material.SetTexture("_MainTex", Resources.Load<Texture2D>("Images/" + tileColor + iconType));
    }

}
