using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;

public class BoidFast : MonoBehaviour
{

    System.Random random;
    private GameObject[] fastBoids;
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

    public float cohesionStrength = 1.0f;
    public Vector3 cohesionVector = Vector3.zero;
    public float alignmentStrength = 1.25f;
    public Vector3 alignmentVector = Vector3.zero;
    public float separationStrength = 1.25f;
    public Vector3 separationVector = Vector3.zero;
    public float separationRadius = 2.5f;

    //Boid rule controls

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
    private List<GameObject> visiblePrey = new List<GameObject>();

    public float eatingRadius = 1.5f;

    private int appetite;
    public int appetiteMin = 7200;
    public int appetiteMax = 10800;

    public GameObject targetedFood = null;

    //Reproduction controls
    public GameObject childPrefab;
    private GameObject childToSpawn;
    private int reproductionCount;
    public int foodToReproduce = 3;

    private int age;
    public int ageMin = 35000;
    public int ageMax = 65000;

    public GameObject skeletonPrefab;
    private GameObject skeletonToSpawn;

    //Fear controls
    private GameObject[] badBoids;
    private List<GameObject> visibleBadBoids = new List<GameObject>();

    public float fearStrength = 2.0f;
    public float fearRadius = 5.0f;

    private float spawnObstructionRadius = 2.5f;

    //Bounding box values
    public float clampX = 15.0f;
    public float clampY = 15.0f;
    public float clampZ = 15.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        random = GameObject.Find("TankManager").GetComponent<SeaweedManager>().random;
        this.followTarget = GameObject.Find("Target");
        //this.basicBoids = GameObject.Find("BoidManager").GetComponent<BoidManager>().basicBoids;
        this.fastBoids = GameObject.FindGameObjectsWithTag("BoidFast");
        this.obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        this.prey = GameObject.FindGameObjectsWithTag("BoidFast_Prey");
        this.badBoids = GameObject.FindGameObjectsWithTag("BoidBad");

        this.age = random.Next(ageMin, ageMax);
        this.appetite = random.Next(appetiteMin, appetiteMax);

        this.reproductionCount = foodToReproduce;

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
        updateVisiblePrey();
        //Update visible flockmates
        updateVisibleFriends();

        //Update visible predators
        updateVisibleBadBoids();
        var separationForce = Vector3.zero;
        //If at least one potential flockmate is identified, begin applying flocking rules
        if(visibleFriends.Count > 0){
            //Cohesion rule - Steer towards the centre of nearby boids of the same species
            Vector3 centreOfFriends = centreOfKnownFlock();
            Vector3 distanceToCentreOfFriends = (centreOfFriends - transform.position);
            var cohesionForce = forceTowardsPoint(distanceToCentreOfFriends);
            //Debug.Log(cohesionForce);
            if(visibleFriends.Count <= 8){
                acceleration += cohesionForce*cohesionStrength;
                cohesionVector = cohesionForce*cohesionStrength;
            }else{
                acceleration -= cohesionForce*cohesionStrength;
                cohesionVector = cohesionForce*cohesionStrength;
            }
            

            //Alignment rule - Steer towards the average heading of nearby boids of the same species
            Vector3 alignmentOfFriends = alignmentOfKnownFlock();
            var alignmentForce = forceTowardsPoint(alignmentOfFriends);
            acceleration += alignmentForce*alignmentStrength;
            alignmentVector = alignmentForce*alignmentStrength;

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

        var fearForce = Vector3.zero;
        if(visibleBadBoids.Count() > 0){
            foreach(GameObject boid in visibleBadBoids){
                if(boid != null){
                    if(Vector3.Distance(transform.position, boid.transform.position) <= fearRadius){
                        Debug.Log(Vector3.Distance(transform.position, boid.transform.position));
                        fearForce += forceTowardsPoint(boid.transform.position - transform.position);                   
                    }
                }
            }
            Debug.Log("I'm scared!");
            acceleration -= fearForce*fearStrength;
        }

        

        //If hungry, hunt for prey
        appetite -= 1;
        //Debug.Log(visibleSeaweed.Count);
        if(appetite <= 0 && targetedFood == null){
            if(visiblePrey.Count() > 0){
                foreach(GameObject prey in visiblePrey){
                    if(prey != null && !prey.CompareTag("Claimed")){
                        prey.tag = "Claimed";
                        targetedFood = prey;
                        Debug.Log("I found something to eat!");
                        foreach(GameObject boid in fastBoids){
                            if(boid != null){
                                boid.GetComponent<BoidFast>().updateVisiblePrey();
                            }
                            
                        }
                        break;
                    }
                }
            }  
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
                skeletonToSpawn = Instantiate(skeletonPrefab, targetedFood.transform.parent.transform.position, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360))));
                skeletonToSpawn.name = "BoidDead (" + random.Next().ToString()+")";
                Debug.Log("Spawned new skeleton!");
                Destroy(targetedFood.transform.parent.gameObject);
                //TODO HUNTING LOGIC
                targetedFood = null;
                Debug.Log("I ate the food!");
            }
               
        }

        //If at least one obstacle is identified, begin avoiding obstacles
        separationForce = Vector3.zero;
        if(obstacles.Length > 0){
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
            reproductionCount = foodToReproduce;
            var spawnPoint = findUnobstructedPoint((int)-clampX, (int)clampX);
            childToSpawn = Instantiate(childPrefab, spawnPoint, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), 0)));
            childToSpawn.name = "BoidFast (" + random.Next().ToString()+")";
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
            acceleration = Vector3.zero;
            float startingSpeed = ((minimumSpeed+maximumSpeed)/2);
            transform.forward = UnityEngine.Random.rotation.eulerAngles;
            velocity = this.gameObject.transform.forward*startingSpeed;
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
        this.fastBoids = GameObject.FindGameObjectsWithTag("BoidFast");
        if(fastBoids == null || fastBoids.Length == 0){
            Debug.Log("fastBoids is null or empty!!");
        }else{
            foreach(GameObject boid in fastBoids){
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

    void updateVisiblePrey(){
        this.prey = GameObject.FindGameObjectsWithTag("BoidFast_Prey");
        if(prey == null || prey.Length == 0){
            Debug.Log("prey is null or empty!!");
        }else{
            foreach(GameObject boid in prey){
                if(boid != this.gameObject && boid != null){
                    if(Vector3.Distance(boid.transform.position, this.gameObject.transform.position) <= visionRadius){
                        if(!visiblePrey.Contains(boid)){
                            visiblePrey.Add(boid);
                        }
                    }else{
                        if(visiblePrey.Contains(boid)){
                            visiblePrey.Remove(boid);
                        }
                    }
                }
            
            }
        }
        
    }

    void updateVisibleBadBoids(){
        this.badBoids = GameObject.FindGameObjectsWithTag("BoidBad");
        if(badBoids == null || badBoids.Length == 0){
            //Debug.Log("obstacles is null or empty!!");
        }else{
            //Debug.Log("Obstacles in scene: "+obstacles.Length);
            foreach(GameObject boid in badBoids){
                if(Vector3.Distance(boid.transform.position, this.gameObject.transform.position) <= visionRadius){
                    if(!visibleBadBoids.Contains(boid)){
                        //Debug.Log("I saw an obstacle!");
                        visibleBadBoids.Add(boid);
                    }
                }else{
                    if(visibleBadBoids.Contains(boid)){
                        visibleBadBoids.Remove(boid);
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
