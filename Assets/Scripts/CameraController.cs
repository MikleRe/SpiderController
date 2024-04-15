using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform cameraTarget;
    public float distance = 5f;
    public float rotationSpeed = 10f;
    
    // Update is called once per frame
    void Update()
    {
        if (cameraTarget == null) return;

        Vector2 turn = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * rotationSpeed;
        cameraTarget.Rotate(turn.y, turn.x, 0);

        ClampAngle();
        
        RaycastHit hit;

        Vector3 newPosition;
        
        if (Physics.Raycast(cameraTarget.position, cameraTarget.forward, out hit, distance, LayerMask.GetMask("Terrain")))
            newPosition = cameraTarget.position + cameraTarget.forward * hit.distance;
        else
            newPosition = cameraTarget.position + cameraTarget.forward * distance;

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * 10f);
        
        transform.LookAt(cameraTarget);
    }

    void ClampAngle()
    {
        // xAngle < 60 && xAngle > 300
        Vector3 angles = cameraTarget.rotation.eulerAngles;

        Debug.Log(angles);
        
        // if clamping is needed
        if (angles.x is > 60 and < 300)
        {
            if (Math.Abs(angles.x - 60) < Math.Abs(angles.x - 300))
            {
                cameraTarget.rotation = Quaternion.Euler(60, angles.y, angles.z);
            }
            else
            {
                cameraTarget.rotation = Quaternion.Euler(300, angles.y, angles.z);
            }
        } 
    }
}