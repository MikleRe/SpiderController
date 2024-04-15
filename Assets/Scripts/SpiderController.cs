using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.ReorderableList;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class SpiderController : MonoBehaviour
{
    public float walkSpeed = 2f;
    public Transform pivotPoint;
    
    [Serializable]
    public struct Leg { 
        public Transform end; 
        public Transform target;
    }
    public List<Leg> legs;

    private List<GameObject> _legSpheres = new();
    private List<bool> _legSticked = new();
    
    private float _rotationSpeed = 50f;

    private List<float> _rayCastRotations = new();

    private void Start()
    {
        _rayCastRotations.Add(50);
        _rayCastRotations.Add(90);
        _rayCastRotations.Add(150);
        _rayCastRotations.Add(10);
        _rayCastRotations.Add(330);
        _rayCastRotations.Add(270);
        _rayCastRotations.Add(210);
        _rayCastRotations.Add(350);
        
        foreach (Leg leg in legs)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Renderer>().material.color = Color.red;
            
            //sphere.GetComponent<Renderer>().enabled = false;
            
            sphere.transform.localScale = new Vector3(.05f, .05f, .05f);
            sphere.GetComponent<Collider>().enabled = false;
            _legSpheres.Add(sphere);

            leg.target.parent = null;
            _legSticked.Add(true);
        }
        
        ComputeLegsLanding();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        ComputeLegsLanding();
        ProceduralAnimation();
    }

    void Move()
    {
        Transform currentTransform = transform;
        
        float inputVertical = Input.GetAxis("Vertical");
        float inputHorizontal = Input.GetAxis("Horizontal");
        
        transform.Rotate(currentTransform.up * (inputHorizontal * Time.deltaTime * _rotationSpeed));

        float arcAngle = 270;
        float arcRadius = 50f * Time.deltaTime;
        int arcResolution = 6;
        
        if(inputVertical > .1f && ArcCastV2(transform.position, transform.rotation * Quaternion.Euler(currentTransform.up * -90), arcAngle, arcRadius, arcResolution, LayerMask.GetMask("Terrain"), out RaycastHit hit))
        {
            transform.position = hit.point;
            transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
        }

        /* ArcCast()
        RaycastHit hit;
        if (ArcCast(currentTransform.rotation * Quaternion.Euler(currentTransform.up * -90), .9f, out hit, Color.cyan) && inputVertical > .1f)
        {
            // Move the spider
            transform.position = Vector3.Lerp(transform.position, hit.point, 1f * Time.deltaTime);
            
            // Rotate the spider
            Quaternion newRotation = Quaternion.FromToRotation(currentTransform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Lerp(currentTransform.rotation, newRotation, Time.deltaTime * 2);
        }
        */
    }

    void ComputeLegsLanding()
    {
        Quaternion currentRotation = pivotPoint.rotation; // current rotation
        
        Vector3 rotations = Vector3.zero; // average rotations of legs

        int hitNumber = 0;
        
        for (int i = 0; i < legs.Count; i++)
        {
            RaycastHit hit;
            
            /* old raycast implementation
             
            Leg leg = legs[i];
            Vector3 controllerBonePosition = controllerBone.position;
            Vector3 direction = leg.front.position - controllerBonePosition;
            Debug.DrawRay(controllerBonePosition, direction, Color.magenta);
            Physics.Raycast(controllerBonePosition, direction, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain"));
            
            */
            
            Quaternion rayCastRotation = currentRotation * Quaternion.Euler(pivotPoint.up * (_rayCastRotations[i]-90));
            
            Debug.Log(currentRotation);
            
            //currentRotation += transform.up * _rayCastRotations[i];
            //Quaternion rayCastRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, currentRotation.z);
            
            if (ArcCast(rayCastRotation, 1.3f, out hit, Color.red))
            {
                //rotations += hit.normal;
                //hitNumber++;
                _legSpheres[i].transform.position = hit.point;
            }
        }
        
        // Quaternion newRotation = Quaternion.FromToRotation(transform.up, rotations/hitNumber) * transform.rotation;
        // transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * 2);
    }
    
    void ProceduralAnimation()
    {
        // list of float to get the average height
        List<float> legsHeightSticked = new();
        
        // for each legs
        for (int i = 0; i < legs.Count; i++)
        {
            Leg leg = legs[i];
            GameObject sphere = _legSpheres[i];
            bool sticked = _legSticked[i];

            float distanceLegSphere = Vector3.Distance(sphere.transform.position, leg.target.position);
            
            /* ça serait cool de faire un truc du style mais ça ne marche pas forcément
            
            float distanceLegTarget = Vector3.Distance(leg.target.position, leg.end.position);
            if ((distanceLegSphere > 1 || distanceLegTarget > 0.1) && sticked)
                
            */
                
            // if the leg is too far away we say that the leg should come back
            if (distanceLegSphere > 1 && sticked)
                _legSticked[i] = false;

            // if the leg is coming back
            if (!sticked)
            {
                leg.target.position = Vector3.MoveTowards(leg.target.position, sphere.transform.position, 8 * Time.deltaTime);
                if (distanceLegSphere < .1f)
                {
                    _legSticked[i] = true;
                }
            }
            else
            {
                legsHeightSticked.Add(leg.end.position.y);
            }
        }
        
        // Compute body height
        /*
        if (legsHeightSticked.Count > 0)
        {
            Vector3 oldPosition = transform.position;

            Vector3 newPosition = new Vector3(oldPosition.x, legsHeightSticked.Average() + 0.35f,
                oldPosition.z);
            transform.position = Vector3.Lerp(oldPosition, newPosition, 0.2f);
        }
        */
    }
    
    bool ArcCast(Quaternion rotation, float radius, out RaycastHit hit, Color color)
    {
        // Layer that the raycast should hit
        LayerMask mask = LayerMask.GetMask("Terrain");
        
        // current position of the spider
        Vector3 position = pivotPoint.position;
        
        float angle = 200;
        // number of step to achieve the angle variable
        int resolution = 10;
        
        rotation *= Quaternion.Euler(angle/2f, 0, 0);
        
        for (int i = 0; i < resolution; i++)
        {
            Vector3 a = position - rotation * Vector3.forward * radius;
            rotation *= Quaternion.Euler(-angle / resolution, 0, 0);
            Vector3 b = position - rotation * Vector3.forward * radius;
            Vector3 ab = b - a;

            Debug.DrawLine(a, b, color);
            
            if (Physics.Raycast(a, ab, out hit, ab.magnitude * 1.001f, mask))
                return true;
        }

        hit = new RaycastHit();
        return false;
    }

    bool ArcCastV2(Vector3 center, Quaternion rotation, float angle, float radius, int resolution, LayerMask layer,
        out RaycastHit hit)
    {
        rotation *= Quaternion.Euler(-angle/2, 0, 0);

        for (int i = 0; i < resolution; i++)
        {
            Vector3 A = center + rotation * Vector3.forward * radius;
            rotation *= Quaternion.Euler(angle / resolution, 0, 0);
            Vector3 B = center + rotation * Vector3.forward * radius;
            Vector3 AB = B - A;

            if (Physics.Raycast(A, AB, out hit, AB.magnitude * 1.001f, layer))
                return true;
        }

        hit = new RaycastHit();
        return false;
    }
}