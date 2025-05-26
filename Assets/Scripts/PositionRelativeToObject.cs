using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionRelativeToObject : MonoBehaviour
{

    public GameObject objectToCopyPosition;
    public Vector3 Position;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.position = objectToCopyPosition.transform.TransformPoint(Position);

        if(this.tag!="arrow")
            transform.rotation = objectToCopyPosition.transform.rotation;
    }
}
