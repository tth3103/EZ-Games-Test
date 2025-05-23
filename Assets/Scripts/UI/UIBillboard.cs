using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    Camera cam;
    void Start()
    {
        cam = Camera.main;
    }
    void LateUpdate()
    {
        transform.LookAt(transform.position + cam.transform.forward);
    }
}
