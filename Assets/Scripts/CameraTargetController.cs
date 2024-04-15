using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTargetController : MonoBehaviour
{
    public Transform spider;
    
    private Vector3 _fromSpider;
    
    // Start is called before the first frame update
    void Start()
    {
        _fromSpider = transform.position - spider.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = spider.position + _fromSpider;
    }
}
