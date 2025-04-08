using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;
using System.Linq;

public class SeaweedManager : MonoBehaviour
{
    public System.Random random = new System.Random(12345);

    private GameObject[] obstacles;
    
    

    private GameObject[] basicBoidsInTank;
    public int basicBoids;
    private GameObject[] badBoidsInTank;
    public int badBoids;

    private GameObject[] spikyBoidsInTank;

    public int spikyBoids;
    private GameObject[] fastBoidsInTank;

    public int fastBoids;

    private GameObject[] seaweedInTank;
    public int seaweed;

    private GameObject[] deadBoidsInTank;
    public int deadBoids;

    public GameObject basicBoidPrefab;

    [Range(1, 20)]
    public int basicBoidStartingPopulation = 13;
    private GameObject basicBoidToSpawn;

    public GameObject badBoidPrefab;

    [Range(1, 20)]
    public int badBoidStartingPopulation = 2;
    private GameObject badBoidToSpawn;

    public GameObject spikyBoidPrefab;

    [Range(1, 20)]
    public int spikyBoidStartingPopulation = 8;
    private GameObject spikyBoidToSpawn;

    public GameObject fastBoidPrefab;

    [Range(1, 20)]
    public int fastBoidStartingPopulation = 4;
    private GameObject fastBoidToSpawn;

    public GameObject deadBoidPrefab;

    [Range(1, 20)]
    public int deadBoidStartingPopulation = 15;
    private GameObject deadBoidToSpawn;

    public GameObject seaweedPrefab;

    [Range(1, 20)]
    public int seaweedStartingPopulation = 13;
    private GameObject seaweedToSpawn;

    private int optimalSeaweedCount = 13;
    private float spawnObstructionRadius = 5.0f;

    private int seaweedSpawnDelay = 300;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.obstacles = GameObject.FindGameObjectsWithTag("Obstacle");

        for(int i = 0; i < basicBoidStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-13, 13);
            basicBoidToSpawn = Instantiate(basicBoidPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            basicBoidToSpawn.name = "Basic Boid (" + random.Next().ToString()+")";
            Debug.Log("Spawned new basic boid!");
        }

        for(int i = 0; i < badBoidStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-13, 13);
            badBoidToSpawn = Instantiate(badBoidPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            badBoidToSpawn.name = "Bad Boid (" + random.Next().ToString()+")";
            Debug.Log("Spawned new bad boid!");
        }

        for(int i = 0; i < spikyBoidStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-13, 13);
            spikyBoidToSpawn = Instantiate(spikyBoidPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            spikyBoidToSpawn.name = "Spiky Boid (" + random.Next().ToString()+")";
            Debug.Log("Spawned new spiky boid!");
        }

        for(int i = 0; i < fastBoidStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-13, 13);
            fastBoidToSpawn = Instantiate(fastBoidPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            fastBoidToSpawn.name = "Fast Boid (" + random.Next().ToString()+")";
            Debug.Log("Spawned new fast boid!");
        }

        for(int i = 0; i < deadBoidStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-13, 13);
            deadBoidToSpawn = Instantiate(deadBoidPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            deadBoidToSpawn.name = "Dead Boid (" + random.Next().ToString()+")";
            Debug.Log("Spawned new dead boid!");
        }

        for(int i = 0; i < seaweedStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-13, 13);
            seaweedToSpawn = Instantiate(seaweedPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            seaweedToSpawn.name = "Seawwed (" + random.Next().ToString()+")";
            Debug.Log("Spawned new seaweed!");
        }




        this.seaweedInTank = GameObject.FindGameObjectsWithTag("Seaweed");
        this.basicBoidsInTank = GameObject.FindGameObjectsWithTag("BoidBasic");
        this.badBoidsInTank = GameObject.FindGameObjectsWithTag("BoidBad");
        this.spikyBoidsInTank = GameObject.FindGameObjectsWithTag("BoidSpiky");
        this.fastBoidsInTank = GameObject.FindGameObjectsWithTag("BoidFast");
        this.deadBoidsInTank = GameObject.FindGameObjectsWithTag("BoidDead");
    }

    // Update is called once per frame
    void Update()
    {
        seaweedSpawnDelay -= 1;
        this.seaweedInTank = GameObject.FindGameObjectsWithTag("Seaweed");
        this.seaweed = seaweedInTank.Count();
        this.basicBoidsInTank = GameObject.FindGameObjectsWithTag("BoidBasic");
        this.basicBoids = basicBoidsInTank.Count();
        this.badBoidsInTank = GameObject.FindGameObjectsWithTag("BoidBad");
        this.badBoids = badBoidsInTank.Count();
        this.spikyBoidsInTank = GameObject.FindGameObjectsWithTag("BoidSpiky");
        this.spikyBoids = spikyBoidsInTank.Count();
        this.fastBoidsInTank = GameObject.FindGameObjectsWithTag("BoidFast");
        this.fastBoids = fastBoidsInTank.Count();
        this.deadBoidsInTank = GameObject.FindGameObjectsWithTag("BoidDead");
        this.deadBoids = deadBoidsInTank.Count();

        if(seaweedInTank.Length < optimalSeaweedCount && seaweedSpawnDelay <= 0){
            Vector3 spawnPoint = findUnobstructedPoint(-13, 13);
            seaweedToSpawn = Instantiate(seaweedPrefab, spawnPoint, Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0)));
            seaweedToSpawn.name = "Seaweed (" + random.Next().ToString()+")";
            Debug.Log("Spawned new seaweed!");
        }
        if(seaweedSpawnDelay <= 0){
            seaweedSpawnDelay = 300;
        }
    }

    Vector3 findUnobstructedPoint(int lowerBound, int upperBound){
        Vector3 vector = Vector3.zero;
        bool acceptablePoint = false;
        while(!acceptablePoint){
            Vector3 test = randomVector(lowerBound, upperBound);
            if(obstacles.Any()){
                foreach(GameObject obstacle in obstacles){
                    if(Vector3.Distance(test, obstacle.transform.position) > spawnObstructionRadius){
                        acceptablePoint = true;
                        vector = test;
                        Debug.Log("Found point!");
                        break;
                    }
                }
            }else{
                acceptablePoint = true;
                vector = test;
                Debug.Log("Found point!");
                break;
            }
            
        }
        return vector;
    }

    Vector3 randomVector(int lowerBound, int upperBound){
        Vector3 vector = new Vector3(random.Next(lowerBound, upperBound), random.Next(lowerBound, upperBound), random.Next(lowerBound, upperBound));
        return vector;
    }

}
