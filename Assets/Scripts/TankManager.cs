using UnityEngine;
using System;
using System.Linq;
using System.IO;

public class SeaweedManager : MonoBehaviour
{

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

    [Range(0, 50)]
    public int basicBoidStartingPopulation = 13;
    private GameObject basicBoidToSpawn;

    public GameObject badBoidPrefab;

    [Range(0, 50)]
    public int badBoidStartingPopulation = 2;
    private GameObject badBoidToSpawn;

    public GameObject spikyBoidPrefab;

    [Range(0, 50)]
    public int spikyBoidStartingPopulation = 8;
    private GameObject spikyBoidToSpawn;

    public GameObject fastBoidPrefab;

    [Range(0, 50)]
    public int fastBoidStartingPopulation = 4;
    private GameObject fastBoidToSpawn;

    public GameObject deadBoidPrefab;


    [Range(1, 50)]
    public int optimalDeadBoidCount = 13;

    [Range(1, 50)]
    public int deadBoidStartingPopulation = 15;
    private GameObject deadBoidToSpawn;

    public GameObject seaweedPrefab;

    [Range(1, 50)]
    public int optimalSeaweedCount = 13;

    [Range(1, 50)]
    public int seaweedStartingPopulation = 13;
    private GameObject seaweedToSpawn;

    
    private float spawnObstructionRadius = 5.0f;

    private int seaweedSpawnDelay = 300;

    public int spawnBounds = 25;

    [Range(1, 5)]
    public int simulationSpeed = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private int loggingInterval = 3600;
    private string loggingPath;
    
    void Start()
    {
        var instance = StaticRandom.randomInt();
        this.loggingPath = "Assets/Resources/"+instance+"_PopulationLogs.txt";

        this.obstacles = GameObject.FindGameObjectsWithTag("Obstacle");

        for(int i = 0; i < basicBoidStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-spawnBounds, spawnBounds);
            basicBoidToSpawn = Instantiate(basicBoidPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            basicBoidToSpawn.name = "Basic Boid (" + StaticRandom.randomInt().ToString()+")";
            Debug.Log("Spawned new basic boid!");
        }

        for(int i = 0; i < badBoidStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-spawnBounds, spawnBounds);
            badBoidToSpawn = Instantiate(badBoidPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            badBoidToSpawn.name = "Bad Boid (" + StaticRandom.randomInt().ToString()+")";
            Debug.Log("Spawned new bad boid!");
        }

        for(int i = 0; i < spikyBoidStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-spawnBounds, spawnBounds);
            spikyBoidToSpawn = Instantiate(spikyBoidPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            spikyBoidToSpawn.name = "Spiky Boid (" + StaticRandom.randomInt().ToString()+")";
            Debug.Log("Spawned new spiky boid!");
        }

        for(int i = 0; i < fastBoidStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-spawnBounds, spawnBounds);
            fastBoidToSpawn = Instantiate(fastBoidPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            fastBoidToSpawn.name = "Fast Boid (" + StaticRandom.randomInt().ToString()+")";
            Debug.Log("Spawned new fast boid!");
        }

        for(int i = 0; i < deadBoidStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-spawnBounds, spawnBounds);
            deadBoidToSpawn = Instantiate(deadBoidPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            deadBoidToSpawn.name = "Dead Boid (" + StaticRandom.randomInt().ToString()+")";
            Debug.Log("Spawned new dead boid!");
        }

        for(int i = 0; i < seaweedStartingPopulation; i++){
            Vector3 spawnPoint = findUnobstructedPoint(-spawnBounds, spawnBounds);
            seaweedToSpawn = Instantiate(seaweedPrefab, spawnPoint, Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0)));
            seaweedToSpawn.name = "Seawwed (" + StaticRandom.randomInt().ToString()+")";
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
        

        for(int i = 0; i < simulationSpeed; i++){
            seaweedSpawnDelay -= 1;
            loggingInterval -=1;
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

            foreach(GameObject boid in basicBoidsInTank){
                if(boid != null){
                    boid.GetComponent<BoidCore>().UpdateBoid();
                }   
            }
                
            foreach(GameObject boid in badBoidsInTank){
                if(boid != null){
                    boid.GetComponent<BoidBad>().UpdateBoid();
                }
                
            }
            foreach(GameObject boid in spikyBoidsInTank){
                if(boid != null){
                    boid.GetComponent<BoidSpiky>().UpdateBoid();
                }
                
            }
            foreach(GameObject boid in fastBoidsInTank){
                if(boid != null){
                    boid.GetComponent<BoidFast>().UpdateBoid();
                }
                
            }

            if(seaweedInTank.Length < optimalSeaweedCount && seaweedSpawnDelay <= 0){
                Vector3 spawnPoint = findUnobstructedPoint(-spawnBounds, spawnBounds);
                seaweedToSpawn = Instantiate(seaweedPrefab, spawnPoint, Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(0, 360), 0)));
                seaweedToSpawn.name = "Seaweed (" + StaticRandom.randomInt().ToString()+")";
                Debug.Log("Spawned new seaweed!");
            }
            if(seaweedSpawnDelay <= 0){
                seaweedSpawnDelay = 300;
            }
            if(deadBoidsInTank.Length > optimalDeadBoidCount){
                while(deadBoidsInTank.Length > optimalDeadBoidCount){
                    if(deadBoidsInTank[deadBoidsInTank.Length-1] != null){
                        DestroyImmediate(deadBoidsInTank[deadBoidsInTank.Length-1]);
                    }
                }
            }

            if(loggingInterval <= 0){
                Debug.Log("Logging population values");
                loggingInterval = 3600;
                using(StreamWriter writer = new StreamWriter(loggingPath, true)){
                    writer.WriteLine("Basic Boid population: "+basicBoids);
                    writer.WriteLine("Bad Boid population: "+badBoids);
                    writer.WriteLine("Spiky Boid population: "+spikyBoids);
                    writer.WriteLine("Fast Boid population: "+fastBoids);
                    writer.WriteLine("Seaweed count: "+seaweed);
                    writer.WriteLine("Dead boid count: "+deadBoids);
                    writer.WriteLine("");
                }
            }

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
        Vector3 vector = new Vector3(StaticRandom.randomRange(lowerBound, upperBound), StaticRandom.randomRange(lowerBound, upperBound), StaticRandom.randomRange(lowerBound, upperBound));
        return vector;
    }


}
