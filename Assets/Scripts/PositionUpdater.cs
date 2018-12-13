using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionUpdater : MonoBehaviour {

    private SoccerPlayerAgent soccerPlayerAgent;
	// Use this for initialization
	void Start () {
        soccerPlayerAgent = GetComponent<SoccerPlayerAgent>();
	}
	
	// Update is called once per frame
	void FixedUpdate() {
        soccerPlayerAgent.OnFixedUpdate();
	}
}
