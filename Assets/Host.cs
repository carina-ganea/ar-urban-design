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
            Controller = FindAnyObjectByType<PersistentCloudAnchorsController>();
        }

        public void OnHostButtonClicked()
        {
            Manager._toHost = gameObject;
            Controller.Mode = PersistentCloudAnchorsController.ApplicationMode.Hosting;
        }
    }
}

