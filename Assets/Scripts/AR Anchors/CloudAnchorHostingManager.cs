using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using Google.XR.ARCoreExtensions.Samples.PersistentCloudAnchors;
using Google.XR.ARCoreExtensions;
using Unity.XR.CoreUtils;

public class CloudAnchorHostingManager : MonoBehaviour
{
    public PersistentCloudAnchorsController Controller;

    public GameObject CloudAnchorPrefab;

    public GameObject MapQualityIndicatorPrefab;

    private ARAnchor _anchor = null;

    private MapQualityIndicator _qualityIndicator = null;

    private HostCloudAnchorPromise _hostPromise = null;

    private HostCloudAnchorResult _hostResult = null;

    private IEnumerator _hostCoroutine = null;

    private float _timeSinceStart;

    private const float _startPrepareTime = 3.0f;

    public Text InstructionText;

    private List<ResolveCloudAnchorPromise> _resolvePromises =
    new List<ResolveCloudAnchorPromise>();

    private List<ResolveCloudAnchorResult> _resolveResults =
        new List<ResolveCloudAnchorResult>();

    private GameObject m_hostedAnchor = null;

    private GameObject m_resolvedAnchor = null;

    public string map_anchor_ID = "";

    [SerializeField] private GameObject _objectSpawner;

    [SerializeField] private GameObject _ui;

    public Pose GetCameraPose()
    {
        return new Pose(Controller.MainCamera.transform.position,
            Controller.MainCamera.transform.rotation);
    }

    public void Update()
    {
        // Give ARCore some time to prepare for hosting or resolving.
        if (_timeSinceStart < _startPrepareTime)
        {
            _timeSinceStart += Time.deltaTime;
            if (_timeSinceStart >= _startPrepareTime)
            {
                UpdateInitialInstruction();
            }

            return;
        }

        if( FindAnyObjectByType<NetworkParticipant>().m_mapAnchorID.Value != "")
        {
            Controller.Mode = PersistentCloudAnchorsController.ApplicationMode.Resolving;
        }

        if (m_resolvedAnchor != null)
        {
            UpdatePlaneVisibility(false);
        }

        if (Controller.Mode == PersistentCloudAnchorsController.ApplicationMode.Resolving)
        {
            ResolvingCloudAnchors();
        }
        else if (Controller.Mode == PersistentCloudAnchorsController.ApplicationMode.Hosting)
        {
            // Perform hit test and place an anchor on the hit test result.
            if( _anchor == null)
            {
                // If the player has not touched the screen then the update is complete.
                Touch touch;
                if (Input.touchCount < 1 ||
                    (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
                {
                    return;
                }

                // Ignore the touch if it's pointing on UI objects.
                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }

                // Perform hit test and place a pawn object.
                PerformHitTest(touch.position);

            }

            HostingCloudAnchor();
        }
    }

    private void PerformHitTest(Vector2 touchPos)
    {
        List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
        Controller.RaycastManager.Raycast(
            touchPos, hitResults, TrackableType.PlaneWithinPolygon);

        // If there was an anchor placed, then instantiate the corresponding object.
        var planeType = PlaneAlignment.HorizontalUp;
        if (hitResults.Count > 0)
        {
            ARPlane plane = Controller.PlaneManager.GetPlane(hitResults[0].trackableId);
            if (plane == null)
            {
                Debug.LogWarningFormat("Failed to find the ARPlane with TrackableId {0}",
                    hitResults[0].trackableId);
                return;
            }

            planeType = plane.alignment;
            var hitPose = hitResults[0].pose;
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                // Point the hitPose rotation roughly away from the raycast/camera
                // to match ARCore.
                hitPose.rotation.eulerAngles =
                    new Vector3(0.0f, Controller.MainCamera.transform.eulerAngles.y, 0.0f);
            }

            _anchor = Controller.AnchorManager.AttachAnchor(plane, hitPose);
        }

        if (_anchor != null)
        {
            m_hostedAnchor = Instantiate(CloudAnchorPrefab, _anchor.transform);

            // Attach map quality indicator to this anchor.
            var indicatorGO =
                Instantiate(MapQualityIndicatorPrefab, _anchor.transform);
            indicatorGO.transform.position += _anchor.transform.up * 0.25f;

            _qualityIndicator = indicatorGO.GetComponent<MapQualityIndicator>();
            _qualityIndicator.DrawIndicator(planeType, Controller.MainCamera);

            InstructionText.text = " To save this location, walk around the object to " +
                "capture it from different angles";

            // Hide plane generator so users can focus on the object they placed.
            UpdatePlaneVisibility(false);
        }
    }

    private void HostingCloudAnchor()
    {
        // There is no anchor for hosting.
        if (_anchor == null)
        {
            return;
        }

        // There is a pending or finished hosting task.
        if (_hostPromise != null || _hostResult != null)
        {
            return;
        }

        // Update map quality:
        int qualityState = 2;
        // Can pass in ANY valid camera pose to the mapping quality API.
        // Ideally, the pose should represent users’ expected perspectives.
        FeatureMapQuality quality =
            Controller.AnchorManager.EstimateFeatureMapQualityForHosting(GetCameraPose());
        qualityState = (int)quality;
        _qualityIndicator.UpdateQualityState(qualityState);

        // Hosting instructions:
        var cameraDist = (_qualityIndicator.transform.position -
            Controller.MainCamera.transform.position).magnitude;
        if (cameraDist < _qualityIndicator.Radius * 1.5f)
        {
            InstructionText.text = "You are too close, move backward.";
            return;
        }
        else if (cameraDist > 10.0f)
        {
            InstructionText.text = "You are too far, come closer.";
            return;
        }
        else if (_qualityIndicator.ReachTopviewAngle)
        {
            InstructionText.text =
                "You are looking from the top view, move around from all sides.";
            return;
        }
        else if (!_qualityIndicator.ReachQualityThreshold)
        {
            InstructionText.text = "Save the object here by capturing it from all sides.";
            return;
        }

        // Start hosting:
        InstructionText.text = "Processing...";

        // Creating a Cloud Anchor with lifetime = 1 day.
        // This is configurable up to 365 days when keyless authentication is used.
        var promise = Controller.AnchorManager.HostCloudAnchorAsync(_anchor, 1);
        if (promise.State == PromiseState.Done)
        {
            Debug.LogFormat("Failed to host a Cloud Anchor.");
            OnAnchorHostedFinished(false);
        }
        else
        {
            _hostPromise = promise;
            _hostCoroutine = HostAnchor();
            StartCoroutine(_hostCoroutine);
        }
    }

    private IEnumerator HostAnchor()
    {
        yield return _hostPromise;
        _hostResult = _hostPromise.Result;
        _hostPromise = null;

        if (_hostResult.CloudAnchorState == CloudAnchorState.Success)
        {
            OnAnchorHostedFinished(true, _hostResult.CloudAnchorId);
        }
        else
        {
            OnAnchorHostedFinished(false, _hostResult.CloudAnchorState.ToString());
        }
    }

    private void OnAnchorHostedFinished(bool success, string response = null)
    {
        if (success)
        {
            InstructionText.text = "Finish!";
            Invoke("DoHideInstructionBar", 1.5f);

            map_anchor_ID = response;
        }
        else
        {
            InstructionText.text = "Host failed.";
        }
    }

    private void ResolvingCloudAnchors()
    {
        string map_ID = null;
        //FindAnyObjectByType<ARPlaneManager>().enabled = false;

        foreach (var part in GameObject.FindGameObjectsWithTag("Participant"))
        {
            map_ID = part.GetComponent<NetworkParticipant>().m_mapAnchorID.Value.ToString();
            if(map_ID != "") break;
        }
        // No Cloud Anchor for resolving.
        if (map_ID == null)
        {
            return;
        }

        // There are pending or finished resolving tasks.
        if (_resolvePromises.Count > 0 || _resolveResults.Count > 0)
        {
            return;
        }

        // ARCore session is not ready for resolving.
        if (ARSession.state != ARSessionState.SessionTracking)
        {
            return;
        }

        InstructionText.text = string.Format("Attempting to resolve Cloud Anchor(s): {0}", map_ID);

        var promise = Controller.AnchorManager.ResolveCloudAnchorAsync(map_ID);
        if (promise.State == PromiseState.Done)
        {
            Debug.LogFormat("Faild to resolve Cloud Anchor " + map_ID);
            OnAnchorResolvedFinished(false, map_ID);
        }
        else
        {
            _resolvePromises.Add(promise);
            var coroutine = ResolveAnchor(map_ID, promise);
            StartCoroutine(coroutine);
        }
        

        Controller.ResolvingSet.Clear();
    }

    private IEnumerator ResolveAnchor(string cloudId, ResolveCloudAnchorPromise promise)
    {
        yield return promise;
        var result = promise.Result;
        _resolvePromises.Remove(promise);
        _resolveResults.Add(result);

        if (result.CloudAnchorState == CloudAnchorState.Success)
        {
            OnAnchorResolvedFinished(true, cloudId);
            UpdatePlaneVisibility(false);

            foreach (var part in FindObjectsOfType<NetworkParticipant>())
            {
                part.m_isGroundSpawned.Value = true;
            }

            //m_resolvedAnchor = Instantiate(CloudAnchorPrefab, result.Anchor.transform);
            if(m_hostedAnchor != null)
            {
                Destroy(m_hostedAnchor);
            }
        }
        else
        {
            OnAnchorResolvedFinished(false, cloudId, result.CloudAnchorState.ToString());
        }
    }

    private void OnAnchorResolvedFinished(bool success, string cloudId, string response = null)
    {
        if (success)
        {
            InstructionText.text = "Resolve success!";

            _objectSpawner.SetActive(true);

            _objectSpawner.GetComponent<ObjectSpawner>().isGroundSpawned = true;

            _ui.SetActive(true);

            gameObject.GetComponentInChildren<SafeAreaScaler>().gameObject.SetActive(false);

            bool duplicates = false;

            foreach( var obj in GameObject.FindGameObjectsWithTag("Ground"))
            {
                List<GameObject> children = new List<GameObject>();
                obj.GetChildGameObjects(children);

                if (children.Count == 0) continue;

                if(duplicates)
                {
                    Destroy(obj);
                }
                duplicates = true;
            }
            
        }
        else
        {
            InstructionText.text = "Resolve failed.";
        }
    }

    private void UpdatePlaneVisibility(bool visible)
    {
        foreach (var plane in Controller.PlaneManager.trackables)
        {
            plane.gameObject.SetActive(visible);
        }
    }

    private void UpdateInitialInstruction()
    {
        switch (Controller.Mode)
        {
            case PersistentCloudAnchorsController.ApplicationMode.Hosting:
                // Initial instruction for hosting flow:
                InstructionText.text = "Tap to place an object.";
                return;
            case PersistentCloudAnchorsController.ApplicationMode.Resolving:
                // Initial instruction for resolving flow:
                InstructionText.text =
                    "Look at the location you expect to see the AR experience appear.";
                return;
            default:
                return;
        }
    }
}
