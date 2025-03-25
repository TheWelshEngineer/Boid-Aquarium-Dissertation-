using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;

public class BoidCore : MonoBehaviour
{

    System.Random random = new System.Random(54321);
    //Boid Manager's list of Basic Boids
    private GameObject[] basicBoids;
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
    public float minimumSpeed = 1.0f;
    public float maximumSpeed = 5.0f;

    //Boid rule controls
    public float cohesionStrength = 1.0f;
    public Vector3 cohesionVector = Vector3.zero;
    public float alignmentStrength = 1.25f;
    public Vector3 alignmentVector = Vector3.zero;
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
    private GameObject[] seaweed;
    private List<GameObject> visibleSeaweed = new List<GameObject>();

    public float eatingRadius = 1.5f;

    private int appetite = 4800;
    public GameObject targetedFood = null;

    //Bounding box values
    public float clampX = 15.0f;
    public float clampY = 15.0f;
    public float clampZ = 15.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.followTarget = GameObject.Find("Target");
        //this.basicBoids = GameObject.Find("BoidManager").GetComponent<BoidManager>().basicBoids;
        this.basicBoids = GameObject.FindGameObjectsWithTag("BoidBasic");
        this.obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        this.seaweed = GameObject.FindGameObjectsWithTag("Seaweed");

        float startingSpeed = ((minimumSpeed+maximumSpeed)/2);
        transform.forward = UnityEngine.Random.rotation.eulerAngles;
        velocity = this.gameObject.transform.forward*startingSpeed;
        
    }

    // Update is called once per frame
    void Update()
    {
        //this.basicBoids = GameObject.FindGameObjectsWithTag("BoidBasic");
        acceleration = Vector3.zero;
        cohesionVector = Vector3.zero;
        alignmentVector = Vector3.zero;
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
        updateVisibleSeaweed();
        //Update visible flockmates
        updateVisibleFriends();
        //If at least one potential flockmate is identified, begin applying flocking rules
        if(visibleFriends.Count > 0){
            //Cohesion rule - Steer towards the centre of nearby boids of the same species
            Vector3 centreOfFriends = centreOfKnownFlock();
            Vector3 distanceToCentreOfFriends = (centreOfFriends - transform.position);
            var cohesionForce = forceTowardsPoint(distanceToCentreOfFriends);
            //Debug.Log(cohesionForce);
            acceleration += cohesionForce*cohesionStrength;
            cohesionVector = cohesionForce*cohesionStrength;

            //Alignment rule - Steer towards the average heading of nearby boids of the same species
            Vector3 alignmentOfFriends = alignmentOfKnownFlock();
            var alignmentForce = forceTowardsPoint(alignmentOfFriends);
            acceleration += alignmentForce*alignmentStrength;
            alignmentVector = alignmentForce*alignmentStrength;

            //If hungry, hunt for seaweed
            appetite -= 1;
            //Debug.Log(visibleSeaweed.Count);
            if(visibleSeaweed.Count > 0 && appetite <= 0 && targetedFood == null){
                foreach(GameObject weed in visibleSeaweed){
                    if(weed != null && !weed.CompareTag("Claimed")){
                        weed.tag = "Claimed";
                        targetedFood = weed;
                        Debug.Log("I found something to eat!");
                        foreach(GameObject boid in basicBoids){
                            boid.GetComponent<BoidCore>().updateVisibleSeaweed();
                        }
                        break;
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
                cohesionVector = Vector3.zero;
                alignmentVector = Vector3.zero;
                foodVector = foodForce*foodStrength;
                //Debug.Log("I'm moving towards food!");
                if(Vector3.Distance(transform.position, targetedFood.transform.position) <= (eatingRadius)){
                    appetite = random.Next(7200, 10800);
                    Destroy(targetedFood);
                    updateVisibleSeaweed();
                    targetedFood = null;
                    Debug.Log("I ate the food!");
                }
               
            }

            

            //Separation rule - Avoid collisions with other boids and the environment
            var separationForce = Vector3.zero;
            foreach(GameObject boid in visibleFriends){
                if(Vector3.Distance(transform.position, boid.transform.position) <= separationRadius){
                    //Debug.Log("Too close!");
                    separationForce += forceTowardsPoint(boid.transform.position - transform.position);
                    
                }
            
            }
            
            //If at least one obstacle is identified, begin avoiding obstacles
            if(visibleObstacles.Any()){
                foreach(GameObject obstacle in visibleObstacles){
                    if(Vector3.Distance(transform.position, obstacle.transform.position) <= separationRadius*3){
                        Debug.Log("Too close!");
                        separationForce += forceTowardsPoint(obstacle.transform.position - transform.position);
                    }
            
                }
            }
            
            acceleration -= separationForce*separationStrength;
            separationVector = -(separationForce*separationStrength);

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

        //Clamp position to within bounding box
        //Clamp positive X
        if(transform.position.x > clampX){
            var keepY = transform.position.y;
            var keepZ = transform.position.z;
            transform.position = new Vector3(-clampX, keepY, keepZ);
        }
        //Clamp negative X
        if(transform.position.x < -clampX){
            var keepY = transform.position.y;
            var keepZ = transform.position.z;
            transform.position = new Vector3(clampX, keepY, keepZ);
        }
        //Clamp positive Y
        if(transform.position.y > clampY){
            var keepX = transform.position.x;
            var keepZ = transform.position.z;
            transform.position = new Vector3(keepX, -clampY, keepZ);
        }
        //Clamp negative Y
        if(transform.position.y < -clampY){
            var keepX = transform.position.x;
            var keepZ = transform.position.z;
            transform.position = new Vector3(keepX, clampY, keepZ);
        }
        //Clamp positive Z
        if(transform.position.z > clampZ){
            var keepX = transform.position.x;
            var keepY = transform.position.y;
            transform.position = new Vector3(keepX, keepY, -clampZ);
        }
        //Clamp negative Z
        if(transform.position.z < -clampZ){
            var keepX = transform.position.x;
            var keepY = transform.position.y;
            transform.position = new Vector3(keepX, keepY, clampZ);
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
        this.basicBoids = GameObject.FindGameObjectsWithTag("BoidBasic");
        if(basicBoids == null || basicBoids.Length == 0){
            Debug.Log("basicBoids is null or empty!!");
        }else{
            foreach(GameObject boid in basicBoids){
                if(boid != this.gameObject){
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
            Debug.Log("obstacles is null or empty!!");
        }else{
            foreach(GameObject obstacle in obstacles){
                if(Vector3.Distance(obstacle.transform.position, this.gameObject.transform.position) <= visionRadius){
                    if(!visibleObstacles.Contains(obstacle)){
                        Debug.Log("I saw an obstacle!");
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

    void updateVisibleSeaweed(){
        this.seaweed = GameObject.FindGameObjectsWithTag("Seaweed");
        if(seaweed == null || seaweed.Length == 0){
            Debug.Log("seaweed is null or empty!!");
        }else{
            foreach(GameObject weed in seaweed){
                if(Vector3.Distance(weed.transform.position, this.gameObject.transform.position) <= visionRadius){
                    if(!visibleSeaweed.Contains(weed)){
                        visibleSeaweed.Add(weed);
                    }
                }else{
                    if(visibleSeaweed.Contains(weed)){
                        visibleSeaweed.Remove(weed);
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
            flockCentre += boid.transform.position;
        }
        return flockCentre/visibleFriends.Count;  
    }

    Vector3 alignmentOfKnownFlock(){
        var flockAlignment = Vector3.zero;

        if(visibleFriends == null || visibleFriends.Count == 0){
            return flockAlignment;
        }

        foreach(GameObject boid in visibleFriends){
            flockAlignment += boid.transform.forward;
        }
        return flockAlignment/visibleFriends.Count; 
    }
}
