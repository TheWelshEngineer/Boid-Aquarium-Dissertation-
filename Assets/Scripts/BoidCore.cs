using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class BoidCore : MonoBehaviour
{
    //Boid Manager's list of Basic Boids
    private GameObject[] basicBoids;

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
    public float maximumSpeed = 3.0f;

    //Boid rule controls
    public float cohesionStrength = 1.0f;
    public float alignmentStrength = 1.25f;
    public float separationStrength = 1.25f;
    public float separationRadius = 2.5f;

    //Vision controls
    public float visionRadius = 7.5f;
    private List<GameObject> visibleFriends = new List<GameObject>();

    //Target to follow
    public GameObject followTarget;
    public Vector3 followOffset;

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

        float startingSpeed = ((minimumSpeed+maximumSpeed)/2);
        transform.forward = Random.rotation.eulerAngles;
        velocity = this.gameObject.transform.forward*startingSpeed;
        
    }

    // Update is called once per frame
    void Update()
    {
        //this.basicBoids = GameObject.FindGameObjectsWithTag("BoidBasic");
        acceleration = Vector3.zero;

        //Target rule - If there is an identified target, steer towards it
        if(followTarget != null){
            

            Vector3 distanceToTarget = (followTarget.transform.position - transform.position);
            acceleration = forceTowardsPoint(distanceToTarget);

            //Temporary rotation to face target
            var rotationStep = ((minimumSpeed+maximumSpeed)/2);
            Quaternion rotationToTarget = Quaternion.LookRotation(distanceToTarget.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotationToTarget, rotationStep);
            
        }

        //Update visible flockmates
        updateVisibleFriends();
        //If at least one potential flockmate is identified, begin applying flocking rules
        if(visibleFriends.Count > 0){
            //Cohesion rule - Steer towards the centre of nearby boids of the same species
            Vector3 centreOfFriends = centreOfKnownFlock();
            Vector3 distanceToCentreOfFriends = (centreOfFriends - transform.position);
            var cohesionForce = forceTowardsPoint(distanceToCentreOfFriends);
            acceleration += cohesionForce*cohesionStrength;

            //Alignment rule - Steer towards the average heading of nearby boids of the same species
            Vector3 alignmentOfFriends = alignmentOfKnownFlock();
            var alignmentForce = forceTowardsPoint(alignmentOfFriends);
            acceleration += alignmentForce*alignmentStrength;

            //Separation rule - Avoid collisions with other boids and the environment
            var separationForce = Vector3.zero;
            foreach(GameObject boid in visibleFriends){
                if(Vector3.Distance(transform.position, boid.transform.position) <= separationRadius){
                    Debug.Log("Too close!");
                    separationForce += forceTowardsPoint(boid.transform.position - transform.position);
                    
                }
            
            }
            acceleration -= separationForce*separationStrength;

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

    void updateVisibleFriends(){
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
