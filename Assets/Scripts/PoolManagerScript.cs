using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManagerScript : MonoBehaviour
{

    [SerializeField] Transform tileParent;
    private Quaternion rot;


    ObjectPool<GameObject> tilePooler;

    public void InstantiatePooler(int poolSize)
    {
        rot = Quaternion.Euler(new Vector3(90f, 180f, 0));
        tilePooler = new ObjectPool<GameObject>(Create, ActionOnGet, ActionOnRelease, null, true, poolSize, poolSize);
    }

    public GameObject Get() => tilePooler.Get();

    public void Release(GameObject obj) => tilePooler.Release(obj); 

    private GameObject Create() => Instantiate((GameObject)Resources.Load("Prefabs/TilePrefab"), new Vector3(-1000f, -1000f),rot, tileParent);

    private void ActionOnGet(GameObject obj) {
        obj.GetComponent<TileScript>().SetRandomColor();
        obj.transform.position = new Vector3(-1000f, -1000f);
        obj.SetActive(true);
    }

    private void ActionOnRelease(GameObject obj) {
        obj.SetActive(false);
    }




}
