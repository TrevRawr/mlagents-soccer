using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StadiumMetaData : MonoBehaviour {

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public float GetWidth()
    {
        return 10;
    }

    public float GetLength()
    {
        return 15;
    }

    public float GetCornerToCornerDistance()
    {
        return Mathf.Sqrt(Mathf.Pow(GetWidth(), 2) + Mathf.Pow(GetLength(), 2));
    }
}
