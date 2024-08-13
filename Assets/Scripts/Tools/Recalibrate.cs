using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Recalibrate : MonoBehaviour
{
    private Transform _attach;
    private Vector3 _previousAttach;
    private Vector3 _snapPos;
    private Vector3 _originDiff;


    // Start is called before the first frame update
    void Start()
    {
        _attach = transform.Find("Root");
        _previousAttach = _attach.position;
        _originDiff = transform.position - _previousAttach;
        GetComponent<XRGrabInteractable>().selectExited.AddListener(Reposition);
    }

    // Update is called once per frame
    void Update()
    {
/*        RaycastHit groundHit = new RaycastHit();
        if (Physics.Raycast(_attach.position, Vector3.down, out groundHit) && groundHit.collider.tag == "Ground")
        {
            _previousAttach = _attach.position;
            transform.position = groundHit.point + groundHit.normal * 0.05f + _originDiff;
        }
        else if(Physics.Raycast(_attach.position, Vector3.up, out groundHit) && groundHit.collider.tag == "Ground")
        {
            _previousAttach = _attach.position;
            transform.position = groundHit.point + groundHit.normal * 0.05f + _originDiff;
        }*/

/*        if(!GetComponent<XRGrabInteractable>().isSelected)
        {
            if (Physics.Raycast(_previousAttach, Vector3.down, out groundHit) && groundHit.collider.tag == "Ground")
            {
                RaycastHit objectHit = new RaycastHit();

                if (Physics.Raycast(groundHit.point, Vector3.up, out objectHit) && groundHit.collider.tag == "Ground")
                {
                    Vector3 snapDiff = groundHit.point - objectHit.point;
                    _snapPos = transform.position;
                    _snapPos.y -= snapDiff.y;

                    transform.position = _snapPos;
                }
            }
            else if (Physics.Raycast(_previousAttach, Vector3.up, out groundHit) && groundHit.collider.tag == "Ground")
            {
                RaycastHit objectHit = new RaycastHit();

                if (Physics.Raycast(groundHit.point, Vector3.down, out objectHit) && groundHit.collider.tag == "Ground")
                {
                    Vector3 snapDiff = groundHit.point - objectHit.point;
                    _snapPos = transform.position;
                    _snapPos.y -= snapDiff.y;

                    transform.position = _snapPos;
                }
            }

        }*/
    }

    void Reposition(SelectExitEventArgs args)
    {
        Debug.Log("Repositioning");
        RaycastHit groundHit = new RaycastHit();
        if (Physics.Raycast(_attach.position, Vector3.down, out groundHit) && groundHit.collider.tag == "Ground")
        {
            _previousAttach = _attach.position;
            transform.position = groundHit.point + groundHit.normal * 0.05f + _originDiff;
        }
    }
}
