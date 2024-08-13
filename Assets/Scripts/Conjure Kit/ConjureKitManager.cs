using Auki.ConjureKit;
using Auki.ConjureKit.Manna;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Auki.Util;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Internal;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;

public class ConjureKitManager : MonoBehaviour
{
    private IConjureKit _conjureKit;
    private Manna _manna;

    public Camera _camera;

    //public GraphHandler _graphHandler;

    [SerializeField] private TMP_Text sessionState;
    [SerializeField] protected TMP_Text sessionID;

    [SerializeField] private GameObject _cube;
    [SerializeField] private Button _spawnButton;

    [SerializeField] private bool _qrCodeBool;
    [SerializeField] private Button _qrCodeButton;

    [SerializeField] private ObjectSpawner m_objectSpawner;

    [SerializeField]
    [Tooltip("The AR Interactor that determines where to spawn the object.")]
    Object m_ARInteractorObject;

    XRBaseControllerInteractor m_ARInteractorAsControllerInteractor;

    [SerializeField] private ARInteractorSpawnTrigger m_spawnTrigger;
    [SerializeField] private ARTemplateMenuManager m_menuManager;

    private ARCameraManager arCameraManager;
    private Texture2D _videoTexture;

    private GameObject m_lastSelected;
    void Start()
    {
        //_graphHandler.SetCornerValues(new Vector2(-1, -1), new Vector2(500, 700));
        m_ARInteractorAsControllerInteractor = m_ARInteractorObject as XRBaseControllerInteractor;
        m_lastSelected = null;

        arCameraManager = _camera.GetComponent<ARCameraManager>();

        _conjureKit = new ConjureKit(
            _camera.transform,
            "3e79d54a-a13d-4ed3-9d29-ace298095443",
            "utuX-U2fwyMLlE7M0F4EQP5qi2YRsI2iH_RFdpTyN5Sq6f2L"
            );

        _conjureKit.OnStateChanged += state =>
        {
            sessionState.text = state.ToString();
            
        };

        _conjureKit.OnJoined += session =>
        {
            sessionID.text = session.Id.ToString();
        };

        _conjureKit.OnLeft += state =>
        {
            sessionState.text = "";
        };

        _manna = new Manna(_conjureKit);

        _conjureKit.OnEntityAdded += CreateCube;

        _conjureKit.OnEntityUpdatePose += UpdateLocalEntity;

        _conjureKit.OnEntityDeleted += DeleteLocalEntity;

        _conjureKit.Init(ConjureKitConfiguration.DefaultConfigUri);

        _conjureKit.Connect();

        m_spawnTrigger.entitySelected += UpdateEntity;

        m_menuManager.deleteEntity += DeleteEntity;
    }


    private void Update()
    {
        FeedMannaWithVideoFrames();

        if (m_ARInteractorAsControllerInteractor.hasSelection)
        {
            var selected = m_ARInteractorAsControllerInteractor.interactablesSelected;
            foreach (var interactable in selected)
            {
                m_lastSelected = interactable.transform.gameObject;
            }
        }
        //_graphHandler.CreatePoint(new Vector2(Time.time, (float)_conjureKit.GetNetworkQuality().LastRoundtripTimeInMilliseconds));

        //_conjureKit.MeasurePing();

    }

    private void FeedMannaWithVideoFrames()
    {
        var imageAcquired = arCameraManager.TryAcquireLatestCpuImage(out var cpuImage);
        if (!imageAcquired)
        {
            AukiDebug.LogInfo("Couldn't acquire CPU image");
            return;
        }

        if (_videoTexture == null) _videoTexture = new Texture2D(cpuImage.width, cpuImage.height, TextureFormat.R8, false);

        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, TextureFormat.R8);
        cpuImage.ConvertAsync(
            conversionParams,
            (status, @params, buffer) =>
            {
                _videoTexture.SetPixelData(buffer, 0, 0);
                _videoTexture.Apply();
                cpuImage.Dispose();

                _manna.ProcessVideoFrameTexture(
                    _videoTexture,
                    _camera.projectionMatrix,
                    _camera.worldToCameraMatrix
                );
            }
        );
    }
    private void ToggleControls(bool interactable)
    {
        if(_spawnButton)
        {
            _spawnButton.interactable = interactable;
        }
        if( _qrCodeButton)
        {
            _qrCodeButton.interactable = interactable;
        }
    }

    public void ToggleLighthouse()
    {
        _qrCodeBool = !_qrCodeBool;
        _manna.SetLighthouseVisible( _qrCodeBool );
    }

    public void CreateCubeEntity()
    {
        if(_conjureKit.GetState() != State.Calibrated)
        {
            return;
        }

        if( m_lastSelected)
        {
            Vector3 position = m_lastSelected.transform.localPosition;
            Quaternion rotation = m_lastSelected.transform.localRotation;

            Pose entityPose = new Pose(position, rotation);

            foreach (var participant in GameObject.FindGameObjectsWithTag("Participant"))
            {
                Debug.Log("Participant: " + participant.GetComponent<NetworkObject>().OwnerClientId);
                if (participant.GetComponent<NetworkParticipant>().IsOwner)
                {
                    Debug.Log("Participant: " + participant.GetComponent<NetworkParticipant>().NetworkBehaviourId.ToString());
                    if(m_lastSelected.tag != "Ground")
                    {
                        participant.GetComponent<NetworkParticipant>().m_ObjectIndex.Value = int.Parse(m_lastSelected.transform.name.Substring(4, 1)) - 1;
                    }
                    else
                    {
                        m_lastSelected.transform.GetComponent<XRSimpleInteractable>().enabled = false;
                    }
                    
                }
            }

            _conjureKit.GetSession().AddEntity(
                entityPose,
                onComplete: entity => HostSuccess(entity),
                onError: error => Debug.Log(error)
                );
        }

    }

    private void HostSuccess(Entity entity)
    {
        Debug.Log("Successfully hosted entity");

        m_lastSelected.transform.GetComponent<ConjureKitEntity>().EntityID = entity.Id;
    }
    private void CreateCube(Entity entity)
    {
        if (_conjureKit.GetState() != State.Calibrated)
        {
            return;
        }

        if (entity.Flag == EntityFlag.EntityFlagParticipantEntity)
        {
            return;
        }

        var pose = _conjureKit.GetSession().GetEntityPose(entity);

        var index = -1;

        if(m_objectSpawner.isGroundSpawned)
        {
            foreach (var participant in GameObject.FindGameObjectsWithTag("Participant"))
            {
                Debug.Log("Participant: " + participant.name);
                if (!participant.GetComponent<NetworkParticipant>().IsOwner)
                {
                    Debug.Log("Propagating Participant: " + participant.GetComponent<NetworkParticipant>().OwnerClientId.ToString());
                    index = participant.GetComponent<NetworkParticipant>().m_ObjectIndex.Value;
                }
            }

            var obj = Instantiate(m_objectSpawner.objectPrefabs[index], pose.position, pose.rotation);

            obj.GetComponent<ConjureKitEntity>().EntityID = entity.Id;
            obj.GetComponent<ConjureKitEntity>().OwnerID = entity.ParticipantId;
        }
        else
        {
            var obj = Instantiate(m_objectSpawner.m_GroundPrefab, pose.position, pose.rotation);

            m_objectSpawner.isGroundSpawned = true;

            obj.GetComponent<XRSimpleInteractable>().enabled = false;

            obj.GetComponent<ConjureKitEntity>().EntityID = entity.Id;
            obj.GetComponent<ConjureKitEntity>().OwnerID = entity.ParticipantId;
        }
    }

    void UpdateEntity(uint id, Vector3 position, Quaternion rotation)
    {
        _conjureKit.GetSession().SetEntityPose(id, new Pose(position, rotation));
    }

    void UpdateLocalEntity(Entity entity)
    {
        var pose = _conjureKit.GetSession().GetEntityPose(entity);

        foreach(var obj in FindObjectsOfType<ConjureKitEntity>())
        {
            if(obj.EntityID == entity.Id)
            {
                obj.transform.position = pose.position;
                obj.transform.rotation = pose.rotation;
                break;
            }
        }
    }

    void DeleteEntity(ulong id)
    {
        _conjureKit.GetSession().DeleteEntity((uint)id, () => { 
            Debug.Log("Entity " + id + " deleted.");
        }) ;

        DeleteLocalEntity((uint)id);
    }

    void DeleteLocalEntity(uint id)
    {
        foreach (var obj in FindObjectsOfType<ConjureKitEntity>())
        {
            if (obj.EntityID == id)
            {
                Destroy(obj.transform.gameObject);
                break;
            }
        }
    }

    private void OnDestroy()
    {
        _conjureKit.GetSession().UnregisterAllSystems();
        _conjureKit.Disconnect();
    }

    public uint GetClientID()
    {
        return _conjureKit.GetSession().ParticipantId;
    }
}
