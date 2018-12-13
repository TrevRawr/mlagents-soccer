using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoccerPlayerAgent : MLAgents.Agent, Observer, ObservableSubject {

    public List<GameObject> enemyGameObjects;
    public List<GameObject> teamMateGameObjects;
    public Transform homeNetTransform;
    public Transform awayNetTransform;
    public Transform ballTransform;
    public Rigidbody ballRigidbody;
    public StadiumMetaData stadiumMetadata;
    public NetBallDetector AwayNetObservable; //TODO: figure out a way to change this into an instance of the ObservableSubject interface
    public NetBallDetector HomeNetObservable;
    public float rotationSpeed = 10;
    public float movementAcceleration = 0.1f;
    public float maxSpeed = 1.0f;
    public float deceleration = 0.25f;
    public float ballControlRadius = 1.5f;
    public float ballControlFov = 45.0f;
    public float dribbleStrength = 3.0f;
    public float dribbleCooldownTimeInSeconds = 1.0f;
    public float shotStrength = 30.0f;

    private IList<Observer> observers = new List<Observer>();
    private Vector3 localRightAxis = new Vector3(0, 1, 0);
    private float currSpeed = 0;
    private float currAcceleration = 0;
    private float currRotation = 0.0f;
    private bool isDecelerating = true;
    private Vector3 startingPosition = new Vector3();
    private Quaternion startingRotation = new Quaternion();
    private MLAgents.Academy academy;
    private MLAgents.RayPerception rayPerception;

    private Rigidbody rigidBody;
    private float dribbleCooldownStartTime;

    // Use this for initialization
    void Start()
    {
        rayPerception = GetComponent<MLAgents.RayPerception>();

        AwayNetObservable.RegisterObserver(this);
        HomeNetObservable.RegisterObserver(this);
        startingPosition = this.transform.position;
        startingRotation = this.transform.rotation;

        academy = FindObjectOfType<MLAgents.Academy>();
        rigidBody = GetComponent<Rigidbody>();
    }

    enum PRACTICE_MODE
    {
        DRIBBLE,
        AVOID_OBSTACLES,
        MATCH,
        THREE_V_THREE
    }
    private PRACTICE_MODE practiceMode;

    public bool HasTouchedBall()
    {
        return hasTouchedBall;
    }
    
    private void SetRandomXZRotation()
    {
        this.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 359), 0f);
    }

    public override void AgentReset()
    {
        hasTouchedBall = false;
        if (academy == null)
        {
            academy = FindObjectOfType<MLAgents.Academy>();
        }

        if (academy.resetParameters.Count > 0)
        {
            if (academy.resetParameters[ResetParameters.THREE_V_THREE] >= 1f)
            {
                practiceMode = PRACTICE_MODE.THREE_V_THREE;
            }
            else if (academy.resetParameters[ResetParameters.ADD_ENEMIES] >= 1.0f)
            {
                practiceMode = PRACTICE_MODE.MATCH;
            }
            else if (academy.resetParameters[ResetParameters.ADD_OBSTACLES] >= 1.0f)
            {
                practiceMode = PRACTICE_MODE.AVOID_OBSTACLES;
            }
            else
            {
                practiceMode = PRACTICE_MODE.DRIBBLE;
            }
        }
        const float PADDING = 0.5f;
        float maxWidthStartingPosition = (stadiumMetadata.GetWidth() / 2.0f) - PADDING;
        if (practiceMode == PRACTICE_MODE.DRIBBLE)
        {
            float minDistanceFromBall = 1f;
            float currentDistanceFromBall = Mathf.Clamp(academy.resetParameters[ResetParameters.START_DISTANCE_FROM_BALL], minDistanceFromBall, maxWidthStartingPosition);

            this.transform.position = this.transform.parent.position + transform.TransformDirection(PolarToCartesian(currentDistanceFromBall, Random.Range(0, 359))) + new Vector3(0f, 0.5f, 0f);
            SetRandomXZRotation();
        }
        else if (practiceMode == PRACTICE_MODE.AVOID_OBSTACLES)
        {
            //Vector3 avoidObstaclesStartingPosition = startingPosition;
            //avoidObstaclesStartingPosition.z = Random.Range(-maxWidthStartingPosition, maxWidthStartingPosition);
            //const float avoidObstaclesStartingXPosition = 2f; // position bot in middle of obstacles
            //avoidObstaclesStartingPosition.x = avoidObstaclesStartingPosition.x + avoidObstaclesStartingXPosition;
            //transform.position = avoidObstaclesStartingPosition;
            //SetRandomXZRotation();
            transform.position = startingPosition;
            transform.rotation = startingRotation;
        }
        else if (practiceMode == PRACTICE_MODE.MATCH || practiceMode == PRACTICE_MODE.THREE_V_THREE)
        {
            if (academy.resetParameters[ResetParameters.START_RANDOM_Z] >= 1f)
            {
                Vector3 startingPositionRandomZ = startingPosition;
                startingPositionRandomZ.z = Random.Range(-maxWidthStartingPosition, maxWidthStartingPosition);
                this.transform.position = startingPositionRandomZ;
            }
            else
            {
                this.transform.position = startingPosition;
            }

            this.transform.rotation = startingRotation;
        }
        else
        {
            Debug.LogWarning("No mode detected");
        }

        this.rigidBody.angularVelocity = Vector3.zero;
        this.rigidBody.velocity = Vector3.zero;

        notify();
    }

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    public static float DegreeToRadian(float degree)
    {
        return degree * Mathf.PI / 180f;
    }

    /// <summary>
    /// Converts polar coordinate to cartesian coordinate.
    /// </summary>
    public static Vector3 PolarToCartesian(float radius, float angle)
    {
        float x = radius * Mathf.Cos(DegreeToRadian(angle));
        float z = radius * Mathf.Sin(DegreeToRadian(angle));
        return new Vector3(x, 0f, z);
    }

    float Vector3To2Angle(Vector3 from, Vector3 to)
    {
        return Vector2.Angle(new Vector2(from.x, from.z), new Vector2(to.x, to.z));
    }

    private void AddNetObservation(Transform netTransform, float normalizationConstant)
    {
        Vector3 directionToNet = (netTransform.position - transform.position) / normalizationConstant;
        AddVectorObs(directionToNet);

        const float actualNetPostDistanceFromCenter = 2f;
        const float netPostDistanceFromCenter = actualNetPostDistanceFromCenter - 0.6f; //try to aim towards the inside of the net instead of at the posts
        const float tolerance = 0.01f;  
        Vector3 postOnePosition = netTransform.position - new Vector3(0f, 0f, netPostDistanceFromCenter);
        Vector3 postTwoPosition = netTransform.position - new Vector3(0f, 0f, -netPostDistanceFromCenter);
        float angleBetweenPostsFromCurrentPosition = Vector3To2Angle(postOnePosition - transform.position, postTwoPosition - transform.position);
        float angleBetweenForwardsAndPostOne = Vector3To2Angle(transform.forward, postOnePosition - transform.position);
        float angleBetweenForwardsAndPostTwo = Vector3To2Angle(transform.forward, postTwoPosition - transform.position);
        float lookingAtNet = angleBetweenForwardsAndPostOne + angleBetweenForwardsAndPostTwo <= angleBetweenPostsFromCurrentPosition + tolerance ? 1f : 0f;
        AddVectorObs(lookingAtNet);
    }

    public override void CollectObservations()
    {
        float normalizationConstant = stadiumMetadata.GetCornerToCornerDistance();
        
        AddNetObservation(homeNetTransform, normalizationConstant);
        AddNetObservation(awayNetTransform, normalizationConstant);

        const float ballGotoFov = 15f; // narrower so agent only heads in direction of ball when he's fairly close to facing the correct direction
        Vector3 directionToBall = (ballTransform.position - transform.position) / normalizationConstant;
        AddVectorObs(directionToBall);
        float lookingAtBall = Vector3.Angle(transform.forward, directionToBall) <= ballGotoFov ? 1f : 0f;
        AddVectorObs(lookingAtBall);

        foreach (GameObject enemy in enemyGameObjects)
        {
            if (enemy == null)
            {
                AddVectorObs(new Vector3(1f, 1f, 1f));
            }
            else
            {
                //print(enemy.transform.position);
                AddVectorObs((enemy.transform.position - transform.position) / normalizationConstant);
            }
        }

        foreach (GameObject teamMateTransform in teamMateGameObjects)
        {
            if (teamMateTransform == null)
            {
                AddVectorObs(new Vector3(1f, 1f, 1f));
            }
            else
            {
                AddVectorObs((teamMateTransform.transform.position - transform.position) / normalizationConstant);
            }
        }

        AddVectorObs((transform.forward * currSpeed) / maxSpeed);

        float rayDistance = 3f;
        const int numRays = 20;
        float [] rayAngles = new float[numRays]; // one ray for each side of agent... except for front (90f) because that ray isn't working properly for some reason...
        for (int i = 0; i < numRays; i++)
        {
            rayAngles[i] = (360 / numRays) * i;
        }
        string[] detectableObjects = new string[] { Tags.AWAY_TEAM, Tags.WALL, Tags.HOME_TEAM };
        List<float> rayResults = rayPerception.Perceive(rayDistance, rayAngles, detectableObjects, 0f, 0f);
        AddVectorObs(rayResults);

        //float ballRayDistance = 5f;
        //float[] ballRayAngle = { 90f };
        //string[] ballDetectableObjects = new string[] { Tags.SOCCER_BALL };
        //List<float> ballRays = rayPerception.Perceive(ballRayDistance, ballRayAngle, ballDetectableObjects, 0f, 0f);
        ////print(ballRays[0]);
        //AddVectorObs(ballRays);

        //float netRayDistance = stadiumMetadata.GetCornerToCornerDistance();
        //float[] netRayAngle = { 90f };
        //string[] netDetectableObjects = new string[] { Tags.NET };
        //List<float> netRays = rayPerception.Perceive(netRayDistance, netRayAngle, netDetectableObjects, 0.5f, 0f);
        //AddVectorObs(netRays);
        //print(netRays[1]);
    }
    public void OnFixedUpdate()
    {
        foreach (ACTION action in actionsToPerform)
        {
            performAction(action);
        }

        if (isDecelerating && Mathf.Abs(currSpeed) < deceleration)
        {
            currSpeed = 0f;
            //currAcceleration = 0f;
        }
        else
        {
            currSpeed += currAcceleration;
            currSpeed = Mathf.Clamp(currSpeed, -maxSpeed, maxSpeed);
        }

        rigidBody.MovePosition(rigidBody.position + transform.forward * currSpeed * Time.fixedDeltaTime);
        rigidBody.MoveRotation(rigidBody.rotation * Quaternion.AngleAxis(currRotation * Time.fixedDeltaTime, localRightAxis));

        // when nothing's pushed, halt motions
        isDecelerating = true;
        currAcceleration = currSpeed > 0f ? -deceleration : deceleration;
        currRotation = 0f;
    }

    // Note: action order matters if one action needs to prevent another action from happening
    enum ACTION
    {
        FORWARD,
        //BACKWARD,
        ROTATE_LEFT,
        ROTATE_RIGHT,
        SHOOT_BALL,
        TRAP_BALL,
    }

    private void performAction(ACTION action)
    {
        switch (action)
        {
            case ACTION.FORWARD:
                isDecelerating = false;
                currAcceleration = movementAcceleration;
                break;
            //case ACTION.BACKWARD:
            //    isDecelerating = false;
            //    currAcceleration = -movementAcceleration;
            //    break;
            case ACTION.ROTATE_LEFT:
                currRotation = -rotationSpeed;
                break;
            case ACTION.ROTATE_RIGHT:
                currRotation = rotationSpeed;
                break;
            case ACTION.TRAP_BALL:
            case ACTION.SHOOT_BALL:
                if (action == ACTION.SHOOT_BALL)
                {
                    dribbleCooldownStartTime = Time.fixedTime;
                }
                if (action == ACTION.TRAP_BALL && (Time.fixedTime - dribbleCooldownStartTime) < dribbleCooldownTimeInSeconds)
                {
                    break;
                }
                if ((ballTransform.position - transform.position).magnitude < ballControlRadius) {
                    Vector3 directionToBall = (ballTransform.position - transform.position);
                    directionToBall.Normalize();
                    float angleBetweenPlayerAndBall = Mathf.Rad2Deg * Mathf.Acos(Mathf.Clamp(Vector3.Dot(transform.forward, directionToBall), 0.0f, 1.0f));
                    bool isColliderInFront = (angleBetweenPlayerAndBall < ballControlFov);
                    if (isColliderInFront)
                    {
                        if (action == ACTION.SHOOT_BALL)
                        {
                            ballRigidbody.AddForce(transform.forward * shotStrength);
                        }
                        else if (action == ACTION.TRAP_BALL)
                        {
                            ballRigidbody.AddForce(-directionToBall * dribbleStrength);
                        }
                    }
                }
                break;
        }
    }

    //private void stopAction(ACTION action)
    //{
    //    switch (action)
    //    {
    //        case ACTION.FORWARD:
    //            isDecelerating = true;
    //            currAcceleration = deceleration;
    //            break;
    //        case ACTION.BACKWARD:
    //            isDecelerating = true;
    //            currAcceleration = -deceleration;
    //            break;
    //        case ACTION.ROTATE_LEFT:
    //            currRotation = 0;
    //            break;
    //        case ACTION.ROTATE_RIGHT:
    //            currRotation = 0;
    //            break;
    //    }
    //}

    void ConvertDiscreteVectorToContinuousVector(float[] discreteVectorAction)
    {
        foreach (float discreteActionFloat in discreteVectorAction)
        {
            int discreteAction = Mathf.FloorToInt(discreteActionFloat);
            switch (discreteAction)
            {
                case 1:
                    actionsToPerform.Add(ACTION.FORWARD);
                    break;
                case 2:
                    actionsToPerform.Add(ACTION.ROTATE_LEFT);
                    break;
                case 3:
                    actionsToPerform.Add(ACTION.ROTATE_RIGHT);
                    break;
                case 4:
                    actionsToPerform.Add(ACTION.SHOOT_BALL);
                    break;
                case 5:
                    actionsToPerform.Add(ACTION.FORWARD);
                    actionsToPerform.Add(ACTION.ROTATE_LEFT);
                    break;
                case 6:
                    actionsToPerform.Add(ACTION.FORWARD);
                    actionsToPerform.Add(ACTION.ROTATE_RIGHT);
                    break;
            }
        }
        // trapping is no longer an action, it's automatic when user hits the ball
        actionsToPerform.Add(ACTION.TRAP_BALL);
    }
    private bool hasTouchedBall = false;

    private IList<ACTION> actionsToPerform = new List<ACTION>();
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        actionsToPerform.Clear();

        float distanceFromBall = Vector3.Distance(ballTransform.position, this.transform.position);

        bool isAtBall = distanceFromBall < 1.5f;
        if (isAtBall)
        {
            hasTouchedBall = true;
        }

        if (!hasTouchedBall)
        {
            //encourage agent to go for the ball quickly
            AddReward(-0.001f);
        }
        else
        {
            // Time penalty to encourage agents to shoot ball instead of walking into net with it
            // Also will hopefully help getting agents turning in both directions so they get places more efficiently
            AddReward(-0.00025f);
        }

        bool isCloseToBall = distanceFromBall < 1.9f;
        if (isCloseToBall && academy.resetParameters[ResetParameters.RESET_WHEN_BALL_TOUCHED] >= 1f) // hack to reset a bit before touching the ball due to an issue with the collision pushing the ball even after it resets
        {
            Done();
        }

        ConvertDiscreteVectorToContinuousVector(vectorAction);
    }

    void OnCollisionEnter(Collision collision)
    {
        // check for collisions with enemy
        if (collision.collider.tag != this.tag &&
            (collision.collider.CompareTag(Tags.HOME_TEAM) || collision.collider.CompareTag(Tags.AWAY_TEAM) || collision.collider.CompareTag(Tags.OBSTACLE)))
        {
            AddReward(-0.01f);
        }
        else if (collision.collider.CompareTag(Tags.WALL))
        {
            AddReward(-0.004f);
        }
    }

    static int homeTeamScore = 0;
    static int awayTeamScore = 0;
    // On Goal Scored
    public void OnNotify(OBSERVABLE_TYPE observableType)
    {
        //if (practiceMode != PRACTICE_MODE.DRIBBLE)
        if (observableType == AwayNetObservable.GetObserverType())
        {
            if (this.gameObject.CompareTag(Tags.HOME_TEAM))
            {
                homeTeamScore++;
            }
            else if (this.gameObject.CompareTag(Tags.AWAY_TEAM))
            {
                awayTeamScore++;
            }
            //Debug.Log("Home: " + homeTeamScore + " Away: " + awayTeamScore);
            // Called on goal scored against enemy team
            AddReward(1f);
            Done();
        }
        else if (observableType == HomeNetObservable.GetObserverType())
        {
            AddReward(-.5f);
            Done();
        }
    }

    public void RegisterObserver(Observer observer)
    {
        observers.Add(observer);
    }

    public void notify()
    {
        foreach (Observer observer in observers)
        {
            observer.OnNotify(GetObserverType());
        }
    }

    public OBSERVABLE_TYPE GetObserverType()
    {
        return OBSERVABLE_TYPE.SOCCER_PLAYER;
    }
}
