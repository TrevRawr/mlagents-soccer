using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoccerBallReset : MonoBehaviour, Observer {

    public SoccerPlayerAgent[] agentObservable; //TODO: find a way to change this variable to an ObserverSubject instance
    public StadiumMetaData stadiumMetaData;
    private Rigidbody rigidBody;
    private Quaternion startRotation = new Quaternion();


    void OnCollisionEnter(Collision collision)
    {
        //if (collision.collider.CompareTag(Tags.HOME_TEAM) || collision.collider.CompareTag(Tags.AWAY_TEAM))
        //{
        //    ResetPosition();
        //}
    }
    void ResetPosition()
    {
        this.transform.position = transform.parent.position;
        this.transform.rotation = startRotation;

        //start in a random position to better explore solution space. Otherwise, agent only really cares about following x axis of ball direction vector
        //const float PADDING = 0.5f;
        //float maxLength = (stadiumMetaData.GetLength() / 2.0f) - PADDING;
        //float maxWidth = (stadiumMetaData.GetWidth() / 2.0f) - PADDING;
        //this.transform.position = this.transform.parent.position + new Vector3(Random.Range(-maxLength, maxLength), 0.5f, Random.Range(-maxWidth, maxWidth));

        this.rigidBody.angularVelocity = Vector3.zero;
        this.rigidBody.velocity = Vector3.zero;
    }

    public void OnNotify(OBSERVABLE_TYPE type)
    {
        ResetPosition();
    }

    // Use this for initialization
    void Start() {
        startRotation = this.transform.rotation;
        foreach (SoccerPlayerAgent agent in agentObservable)
        {
            agent.RegisterObserver(this);
        }
        rigidBody = GetComponent<Rigidbody>();
    }

    void AddForce(Vector3 force)
    {
        rigidBody.AddForce(force);
    }
}
