using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetBallDetector : MonoBehaviour, ObservableSubject {
    public int observerType;

    private IList<Observer> observers = new List<Observer>();
    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(Tags.SOCCER_BALL))
        {
            notify();
            // TODO: move this somewhere else
            // if a goal is scored, reset unless it's dribble mode. Then, just ignore.
            MLAgents.Academy academy = GameObject.FindGameObjectWithTag(Tags.ACADEMY).GetComponentInChildren<MLAgents.Academy>();
            if (academy.resetParameters.Count > 0 && 
                (academy.resetParameters[ResetParameters.ADD_ENEMIES] >= 1.0f || academy.resetParameters[ResetParameters.ADD_OBSTACLES] >= 1.0f))
            {
                //academy.Done();
            }
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
        return observerType == 1 ? OBSERVABLE_TYPE.ZPOSITIVE_NET : OBSERVABLE_TYPE.ZNEGATIVE_NET;
    }
}
