using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;

public class SeaweedManager : MonoBehaviour
{
    System.Random random = new System.Random(12345);
    public GameObject seaweed;
    private GameObject[] seaweedInTank;
    private GameObject[] obstacles;
    private int optimalSeaweedCount = 13;
    private float spawnObstructionRadius = 2.5f;

    private int seaweedSpawnDelay = 180;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.seaweedInTank = GameObject.FindGameObjectsWithTag("Seaweed");
        this.obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
    }

    // Update is called once per frame
    void Update()
    {
        seaweedSpawnDelay -= 1;
        this.seaweedInTank = GameObject.FindGameObjectsWithTag("Seaweed");
        if(seaweedInTank.Length < optimalSeaweedCount && seaweedSpawnDelay <= 0){
            Vector3 spawnPoint = findUnobstructedPoint(-15, 15);
            Instantiate(seaweed, spawnPoint, Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0)));
            Debug.Log("Spawned new seaweed!");
        }
        if(seaweedSpawnDelay <= 0){
            seaweedSpawnDelay = 180;
        }
    }

    Vector3 findUnobstructedPoint(int lowerBound, int upperBound){
        Vector3 vector = Vector3.zero;
        bool acceptablePoint = false;
        while(!acceptablePoint){
            Vector3 test = randomVector(lowerBound, upperBound);
            foreach(GameObject obstacle in obstacles){
                if(Vector3.Distance(test, obstacle.transform.position) > spawnObstructionRadius){
                    acceptablePoint = true;
                    vector = test;
                    Debug.Log("Found seaweed point!");
                    break;
                }
            }
        }
        return vector;
    }

    Vector3 randomVector(int lowerBound, int upperBound){
        Vector3 vector = new Vector3(random.Next(lowerBound, upperBound), random.Next(lowerBound, upperBound), random.Next(lowerBound, upperBound));
        return vector;
    }

}
