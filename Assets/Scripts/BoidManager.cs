using UnityEngine;

public class BoidManager : MonoBehaviour
{

    public GameObject[] basicBoids;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if(basicBoids == null){
            basicBoids = GameObject.FindGameObjectsWithTag("BoidBasic");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
