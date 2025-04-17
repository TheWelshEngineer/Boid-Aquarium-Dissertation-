using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class BoidBad : MonoBehaviour
{

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
    private List<GameObject> visiblePrey = new List<GameObject>();

    private Vector3 territory;

    private int boredom;
    public int boredomMin = 600;
    public int boredomMax = 900;

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

    private float spawnObstructionRadius = 2.5f;

    //Fear controls
    private GameObject[] spikyBoids;
    private List<GameObject> visibleSpikyBoids = new List<GameObject>();

    public float fearStrength = 2.0f;
    public float fearRadius = 5.0f;

    //Bounding box values
    public float clampX = 30.0f;
    public float clampY = 30.0f;
    public float clampZ = 30.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.followTarget = GameObject.Find("Target");
        //this.basicBoids = GameObject.Find("BoidManager").GetComponent<BoidManager>().basicBoids;
        this.badBoids = GameObject.FindGameObjectsWithTag("BoidBad");
        this.obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        this.prey = GameObject.FindGameObjectsWithTag("BoidBad_Prey");

        this.age = StaticRandom.randomRange(ageMin, ageMax);
        this.appetite = StaticRandom.randomRange(appetiteMin, appetiteMax);

        this.reproductionCount = foodToReproduce;

        this.territory = findUnobstructedPoint((int)-clampX+10,(int)clampX+10);
        this.boredom = StaticRandom.randomRange(boredomMin, boredomMax);

        float startingSpeed = ((minimumSpeed+maximumSpeed)/2);
        transform.forward = UnityEngine.Random.rotation.eulerAngles;
        velocity = this.gameObject.transform.forward*startingSpeed;
        
    }

    // Update is called once per frame
    public void UpdateBoid()
    {

        boredom -= 1;
        if(boredom <= 0){
            this.territory = findUnobstructedPoint((int)-clampX+10,(int)clampX-10);
            boredom = StaticRandom.randomRange(boredomMin, boredomMax);
        }
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
        //Update visible spiky boids
        updateVisibleSpikyBoids();
        var separationForce = Vector3.zero;
        //If at least one potential flockmate is identified, begin applying flocking rules
        if(visibleFriends.Count > 0){
            //Alignment and Cohesion: Bad Boids are territorial and do not school together
            acceleration += forceTowardsPoint(territory - transform.position);
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
            acceleration += forceTowardsPoint(territory - transform.position);
        }

        //Avoid spiky boids
        var fearForce = Vector3.zero;
        if(visibleSpikyBoids.Count() > 0){
            foreach(GameObject boid in visibleSpikyBoids){
                if(boid != null){
                    if(Vector3.Distance(transform.position, boid.transform.position) <= fearRadius){
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
                        foreach(GameObject boid in badBoids){
                            if(boid != null){
                                boid.GetComponent<BoidBad>().updateVisiblePrey();
                            }
                            
                        }
                        break;
                    }
                }
            }else{
                this.territory = findUnobstructedPoint((int)-clampX,(int)clampX);
                this.boredom = StaticRandom.randomRange(boredomMin, boredomMax);
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
                appetite = StaticRandom.randomRange(appetiteMin, appetiteMax);
                reproductionCount -= 1;
                skeletonToSpawn = Instantiate(skeletonPrefab, targetedFood.transform.parent.transform.position, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360))));
                skeletonToSpawn.name = "BoidDead (" + StaticRandom.randomInt().ToString()+")";
                Debug.Log("Spawned new skeleton!");
                DestroyImmediate(targetedFood.transform.parent.gameObject);
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
            childToSpawn.name = "BoidBad (" + StaticRandom.randomInt().ToString()+")";
            Debug.Log("Spawned new baby Bad Boid!");
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
        if(age <= 0 && StaticRandom.randomRange(1, 100) == 1){
            skeletonToSpawn = Instantiate(skeletonPrefab, transform.position, Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360))));
            skeletonToSpawn.name = "BoidDead (" + StaticRandom.randomInt().ToString()+")";
            Debug.Log("Spawned new skeleton!");
            DestroyImmediate(this.gameObject);
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

    void updateVisiblePrey(){
        this.prey = GameObject.FindGameObjectsWithTag("BoidBad_Prey");
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

    void updateVisibleSpikyBoids(){
        this.spikyBoids = GameObject.FindGameObjectsWithTag("BoidSpiky");
        if(spikyBoids == null || badBoids.Length == 0){
            //Debug.Log("obstacles is null or empty!!");
        }else{
            //Debug.Log("Obstacles in scene: "+obstacles.Length);
            foreach(GameObject boid in spikyBoids){
                if(Vector3.Distance(boid.transform.position, this.gameObject.transform.position) <= visionRadius){
                    if(!visibleSpikyBoids.Contains(boid)){
                        //Debug.Log("I saw an obstacle!");
                        visibleSpikyBoids.Add(boid);
                    }
                }else{
                    if(visibleSpikyBoids.Contains(boid)){
                        visibleSpikyBoids.Remove(boid);
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
            if(obstacles != null && obstacles.Any()){
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
