using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StadiumResetter : MonoBehaviour {

    // Use this for initialization
    void Start () {
		
	}

    int localStepCount = 0;
	// Update is called once per frame
	void FixedUpdate () {
        if (localStepCount >= 1500)
        {
            SoccerPlayerAgent[] agents = GetComponentsInChildren<SoccerPlayerAgent>();
            foreach (SoccerPlayerAgent agent in agents)
            {
                if (!agent.HasTouchedBall())
                {
                    agent.AddReward(-0.25f);
                }
                agent.Done();
            }
            localStepCount = 0;
        }
        else
        {
            bool shouldTick = true;
            SoccerPlayerAgent[] agents = GetComponentsInChildren<SoccerPlayerAgent>();
            foreach (SoccerPlayerAgent agent in agents)
            {
                if (agent.IsDone())
                {

                    shouldTick = false;
                    localStepCount = 0;
                }
            }
            if (shouldTick)
            {
                localStepCount++;
            }
        }
	}
}
