using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;

public class BoidBad : MonoBehaviour
{

    System.Random random;
    private GameObject[] badBoids;
    private GameObject[] obstacles;

    //STATE VARIABLES
    //The following represent the state of the boid within the world

    //XYZ position of the boid within a given 3D space
    //X = E/W, Y = U/D, Z = N/S
    public Vector3 position;

    //XYZ facing direction of the boid 
    public Vector3 forward;

    //XYZ velocity of the boid
    private Vector3 velocity;
    private Vector3 acceleration;

    //Speed controls
    public float minimumSpeed = 2.0f;
    public float maximumSpeed = 5.5f;

    //Boid rule controls
    public float separationStrength = 1.25f;
    public Vector3 separationVector = Vector3.zero;
    public float separationRadius = 2.5f;

    public float foodStrength = 5.0f;
    public Vector3 foodVector = Vector3.zero;
    //Vision controls
    public float visionRadius = 7.5f;
    private List<GameObject> visibleFriends = new List<GameObject>();

    private List<GameObject> visibleObstacles = new List<GameObject>();

    //Target to follow
    public GameObject followTarget;
    public Vector3 followOffset;

    //Hunger controls
    private GameObject[] prey;
    private List<GameObject> visibleSeaweed = new List<GameObject>();

    public float eatingRadius = 1.5f;

    private int appetite;
    public int appetiteMin = 7200;
    public int appetiteMax = 10800;

    public GameObject targetedFood = null;

    //Reproduction controls
    public GameObject childPrefab;
    private GameObject childToSpawn;
    private int reproductionCount = 2;

    private int age;
    public int ageMin = 30000;
    public int ageMax = 50000;

    public GameObject skeletonPrefab;
    private GameObject skeletonToSpawn;

    private float spawnObstructionRadius = 2.5f;

    //Bounding box values
    public float clampX = 15.0f;
    public float clampY = 15.0f;
    public float clampZ = 15.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        random = GameObject.Find("SeaweedManager").GetComponent<SeaweedManager>().random;
        this.followTarget = GameObject.Find("Target");
        //this.basicBoids = GameObject.Find("BoidManager").GetComponent<BoidManager>().basicBoids;
        this.badBoids = GameObject.FindGameObjectsWithTag("BoidBad");
        this.obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        //this.prey = GameObject.FindGameObjectsWithTag("Seaweed");

        this.age = random.Next(ageMin, ageMax);
        this.appetite = random.Next(appetiteMin, appetiteMax);


        float startingSpeed = ((minimumSpeed+maximumSpeed)/2);
        transform.forward = UnityEngine.Random.rotation.eulerAngles;
        velocity = this.gameObject.transform.forward*startingSpeed;
        
    }

    // Update is called once per frame
    void Update()
    {
        //this.basicBoids = GameObject.FindGameObjectsWithTag("BoidBasic");
        acceleration = Vector3.zero;
        foodVector = Vector3.zero;
        separationVector = Vector3.zero;

        //Target rule - If there is an identified target, steer towards it
        if(followTarget != null){
            

            Vector3 distanceToTarget = (followTarget.transform.position - transform.position);
            acceleration = forceTowardsPoint(distanceToTarget);

            //Temporary rotation to face target
            var rotationStep = ((minimumSpeed+maximumSpeed)/2);
            Quaternion rotationToTarget = Quaternion.LookRotation(distanceToTarget.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationToTarget, rotationStep);
            
        }

        //Update visible obstacles
        updateVisibleObstacles();
        //Update visible food
        //TODO HUNTING LOGIC
        //Update visible flockmates
        updateVisibleFriends();
        var separationForce = Vector3.zero;
        //If at least one potential flockmate is identified, begin applying flocking rules
        if(visibleFriends.Count > 0){
            //Alignment and Cohesion: Bad Boids are territorial and do not school together

            //Separation rule - Avoid collisions with other boids and the environment       
            foreach(GameObject boid in visibleFriends){
                if(boid != null){
                    if(Vector3.Distance(transform.position, boid.transform.position) <= separationRadius){
                        separationForce += forceTowardsPoint(boid.transform.position - transform.position);                   
                    }
                }
            }
            acceleration -= separationForce*separationStrength;
        }else{
            acceleration += forceTowardsPoint(Vector3.zero - transform.position);
        }

        

        //If hungry, hunt for prey
        appetite -= 1;
        //Debug.Log(visibleSeaweed.Count);
        if(visibleSeaweed.Count > 0 && appetite <= 0 && targetedFood == null){
            //TODO HUNTING LOGIC
        }
        var foodForce = Vector3.zero;
        if(targetedFood != null){
            foodForce = forceTowardsPoint(targetedFood.transform.position - transform.position);
            //Debug.Log(foodForce);
            //Debug.Log(foodForce*foodStrength);
            //acceleration += foodForce*foodStrength;
            acceleration = foodForce*foodStrength;
            foodVector = foodForce*foodStrength;
            //Debug.Log("I'm moving towards food!");
            if(Vector3.Distance(transform.position, targetedFood.transform.position) <= (eatingRadius)){
                appetite = random.Next(appetiteMin, appetiteMax);
                reproductionCount -= 1;
                Destroy(targetedFood);
                //TODO HUNTING LOGIC
                targetedFood = null;
                Debug.Log("I ate the food!");
            }
               
        }

        //If at least one obstacle is identified, begin avoiding obstacles
        if(obstacles.Length > 0){
            separationForce = Vector3.zero;
            foreach(GameObject obstacle in obstacles){
                if(Vector3.Distance(transform.position, obstacle.transform.position) <= separationRadius*2){
                    Debug.Log("Too close!");
                    separationForce += forceTowardsPoint(obstacle.transform.position - transform.position);
                }
            
            }
        }
            
        acceleration -= separationForce*separationStrength;
        separationVector = -(separationForce*separationStrength);

        if(reproductionCount <= 0){
            reproductionCount = 2;
            var spawnPoint = findUnobstructedPoint((int)-clampX, (int)clampX);
            childToSpawn = Instantiate(childPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            childToSpawn.name = "BoidBad (" + random.Next().ToString()+")";
            if(random.Next(1, 20) == 20){
                childToSpawn.gameObject.GetComponent<MeshRenderer>().material = Resources.Load("Materials/BoidBlue_Shiny") as Material;
            }
            Debug.Log("Spawned new child!");
        }

        //Clamp position to within bounding box
        //Clamp X
        if(transform.position.x + acceleration.x > clampX || transform.position.x + acceleration.x < -clampX){
            var accelX = -acceleration.x*2;
            var accelY = acceleration.y;
            var accelZ = acceleration.z;
            acceleration = new Vector3(accelX, accelY, accelZ);
        }
        //Clamp Y
        if(transform.position.y + acceleration.y > clampY || transform.position.y + acceleration.y < -clampY){
            var accelX = acceleration.x;
            var accelY = -acceleration.y*2;
            var accelZ = acceleration.z;
            acceleration = new Vector3(accelX, accelY, accelZ);
        }
        //Clamp Z
        if(transform.position.z + acceleration.z > clampZ || transform.position.z + acceleration.z < -clampZ){
            var accelX = acceleration.x;
            var accelY = acceleration.y;
            var accelZ = -acceleration.z*2;
            acceleration = new Vector3(accelX, accelY, accelZ);
        }
        //Clamp position in emergencies
        if(Vector3.Distance(transform.position, Vector3.zero) > 3*((clampX + clampY + clampZ)/3)){
            Debug.Log("Returning escapee boid to tank!");
            transform.position = Vector3.zero;
        }
        
        //Speed and direction storage
        velocity += acceleration * Time.deltaTime;
        float rawSpeed = velocity.magnitude;
        Vector3 directionOfMovement = velocity / rawSpeed;
        rawSpeed = Mathf.Clamp (rawSpeed, minimumSpeed, maximumSpeed);
        velocity = directionOfMovement * rawSpeed;

        //Apply velocity and direction to GameObject
        this.gameObject.transform.position += velocity * Time.deltaTime;
        this.gameObject.transform.forward = directionOfMovement;

        
        
        

        age -= 1;
        if(age <= 0 && random.Next(1, 100) == 1){
            skeletonToSpawn = Instantiate(skeletonPrefab, transform.position, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360))));
            skeletonToSpawn.name = "BoidDead (" + random.Next().ToString()+")";
            Debug.Log("Spawned new skeleton!");
            Destroy(this.gameObject);
        }
    }



    Vector3 forceTowardsPoint (Vector3 vector) {
        Vector3 v = vector.normalized * maximumSpeed - velocity;
        return Vector3.ClampMagnitude (v, maximumSpeed);
    }

    //Vector3 forceTowardsFoodPoint (Vector3 vector) {
        //Vector3 v = (vector.normalized) * (maximumSpeed) * 5 - velocity;
        //return Vector3.ClampMagnitude (v, maximumSpeed);
    //}

    void updateVisibleFriends(){
        this.badBoids = GameObject.FindGameObjectsWithTag("BoidBad");
        if(badBoids == null || badBoids.Length == 0){
            Debug.Log("badBoids is null or empty!!");
        }else{
            foreach(GameObject boid in badBoids){
                if(boid != this.gameObject && boid != null){
                    if(Vector3.Distance(boid.transform.position, this.gameObject.transform.position) <= visionRadius){
                        if(!visibleFriends.Contains(boid)){
                            visibleFriends.Add(boid);
                        }
                    }else{
                        if(visibleFriends.Contains(boid)){
                            visibleFriends.Remove(boid);
                        }
                    }
                }
            
            }
        }
        
    }

    void updateVisibleObstacles(){
        this.obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        if(obstacles == null || obstacles.Length == 0){
            //Debug.Log("obstacles is null or empty!!");
        }else{
            //Debug.Log("Obstacles in scene: "+obstacles.Length);
            foreach(GameObject obstacle in obstacles){
                if(Vector3.Distance(obstacle.transform.position, this.gameObject.transform.position) <= visionRadius){
                    if(!visibleObstacles.Contains(obstacle)){
                        //Debug.Log("I saw an obstacle!");
                        visibleObstacles.Add(obstacle);
                    }
                }else{
                    if(visibleObstacles.Contains(obstacle)){
                        visibleObstacles.Remove(obstacle);
                    }
                }
            
            }
        }
        
    }

    Vector3 centreOfKnownFlock(){
        var flockCentre = Vector3.zero;

        if(visibleFriends == null || visibleFriends.Count == 0){
            return flockCentre;
        }

        foreach(GameObject boid in visibleFriends){
            if(boid != null){
                flockCentre += boid.transform.position;
            }
        }
        return flockCentre/visibleFriends.Count;  
    }

    Vector3 alignmentOfKnownFlock(){
        var flockAlignment = Vector3.zero;

        if(visibleFriends == null || visibleFriends.Count == 0){
            return flockAlignment;
        }

        foreach(GameObject boid in visibleFriends){
            if(boid != null){
                flockAlignment += boid.transform.forward;
            }
        }
        return flockAlignment/visibleFriends.Count; 
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
                        Debug.Log("Found spawn point!");
                        break;
                    }
                }
            }else{
                acceptablePoint = true;
                vector = test;
                Debug.Log("Found spawn point!");
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
