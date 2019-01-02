﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sensor : MonoBehaviour {

    public GameObject sensorObject;
    public Material originalMaterial;
    public bool isLamp;
    public GameObject Lamp;
    public string resource;
    public bool sample;
    public string db_time_stamp;

    // Use this for initialization
    void Start () {
        sensorObject = gameObject;
        originalMaterial = gameObject.GetComponent<Renderer>().material;
        if (resource == null) resource = "_no_resource_found_";
        sample = false;
        db_time_stamp = "";
	}

}
