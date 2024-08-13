using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Google.XR.ARCoreExtensions.Samples.PersistentCloudAnchors
{
    public class Host : MonoBehaviour
    {
        ARViewManager Manager;
        PersistentCloudAnchorsController Controller;
        void Start()
        {
            Manager = FindAnyObjectByType<ARViewManager>();
            if( Manager != null )
            {
                Debug.Log("Found AR Anchor Manager");
            }
            else
            { 
                Debug.Log("Not Found AR Anchor Manager");
            }
            Controller = FindAnyObjectByType<PersistentCloudAnchorsController>();

            if (Controller != null)
            {
                Debug.Log("Found AR Anchor Controller");
            }
            else
            {
                Debug.Log("Not Found AR Anchor Controller");
            }
        }

        public void OnHostButtonClicked()
        {
            Manager._toHost = gameObject;
            Controller.Mode = PersistentCloudAnchorsController.ApplicationMode.Hosting;
        }
    }
}

