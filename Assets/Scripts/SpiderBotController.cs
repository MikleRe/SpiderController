using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SpiderBotController : MonoBehaviour
{
    public List<Transform> targets;

    private List<bool> _movingLegs = new() { false, false, false, false };
    private float _rotationSpeed = 100f;
    private float _speed = 4f;

    private List<GameObject> spheres = new();

    void Awake()
    {
        // Unleash the targets
        foreach (var target in targets)
        {
            target.SetParent(null);
            
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Collider>().enabled = false;
            sphere.transform.localScale = new Vector3(.1f, .1f, .1f);
            sphere.GetComponent<Renderer>().material.color = Color.red;
            spheres.Add(sphere);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        UpdateIK();
    }

    void Move()
    {
        // Remove redundant access to transform variable
        Transform currentTransform = transform;
        
        // Get inputs
        float inputVertical = Input.GetAxis("Vertical");
        float inputHorizontal = Input.GetAxis("Horizontal");
        
        // Rotate the Spider Bot around itself
        transform.Rotate(Vector3.up * (inputHorizontal * Time.deltaTime * _rotationSpeed));
        
        // Radius of the arc used to do the movement
        float arcRadius = _speed/4 * Time.deltaTime;
        // Precision of the arc
        int arcResolution = 6;
        
        // If we are going forward and the raycast hits something
        if(inputVertical > .1f && ArcCast(currentTransform.position, currentTransform.rotation, arcRadius, 
               arcResolution, LayerMask.GetMask("Terrain"), out RaycastHit hit, 0))
        {
            transform.position = hit.point;
            
            Quaternion newRotation = Quaternion.FromToRotation(currentTransform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Lerp(currentTransform.rotation, newRotation, Time.deltaTime * 2);
        }
    }

    void UpdateIK()
    {
        float arcRadius = 0.35f; // Radius so the arc fit the leg basic position
        int arcResolution = 10; // Precision of the arc

        Transform currentTransform = transform;
        
        for (int i = 0; i < targets.Count; i++)
        {
            Quaternion rayCastRotation = currentTransform.rotation;
            
            Vector3 rayCastPosition = currentTransform.position + currentTransform.forward * arcRadius/2;

            if (ArcCast(rayCastPosition, rayCastRotation, arcRadius, arcResolution,
                    LayerMask.GetMask("Terrain"), out RaycastHit hit, 45 + 90 * i))
            {
                float distanceTargetLanding = Vector3.Distance(targets[i].position, hit.point);

                spheres[i].transform.position = hit.point;
                //Debug.DrawLine(hit.point, targets[i].position, Color.blue);
                Debug.DrawLine(currentTransform.position + transform.up/4, spheres[i].transform.position, Color.green);
                
                if (distanceTargetLanding > .4f) // if legs should move
                    _movingLegs[i] = MovingLegs() == 0;
            }

            if (_movingLegs[i])
            {
                targets[i].position = Vector3.Lerp(targets[i].position, hit.point, 40f * Time.deltaTime);

                float destinationDistance = Vector3.Distance(targets[i].position, hit.point);
                
                if (destinationDistance < 0.05f) _movingLegs[i] = false;
            }
        }
    }
    
    bool ArcCast(Vector3 center, Quaternion rotation, float radius, int resolution, LayerMask layer,
        out RaycastHit hit, float yAngle)
    {
        float angle = 270;
        
        rotation *= Quaternion.Euler(-angle/2, yAngle, 0);

        for (int i = 0; i < resolution; i++)
        {
            Vector3 a = center + rotation * Vector3.forward * radius;
            rotation *= Quaternion.Euler(angle / resolution, 0, 0);
            Vector3 b = center + rotation * Vector3.forward * radius;
            Vector3 ab = b - a;
            
            //Debug.DrawLine(a, b, Color.green);
            
            if (Physics.Raycast(a, ab, out hit, ab.magnitude * 1.001f, layer))
                return true;
        }

        hit = new RaycastHit();
        return false;
    }

    int MovingLegs()
    {
        int number = 0;

        foreach (var movingLeg in _movingLegs)
            if (movingLeg)
                number++;

        return number;
    }
}
