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

public class ConjureKitManager : MonoBehaviour
{
    private IConjureKit _conjureKit;
    private Manna _manna;

    public Camera _camera;

    [SerializeField] private TMP_Text sessionState;
    [SerializeField] protected TMP_Text sessionID;

    [SerializeField] private GameObject _cube;
    [SerializeField] private Button _spawnButton;

    [SerializeField] private bool _qrCodeBool;
    [SerializeField] private Button _qrCodeButton;

    [SerializeField] private ObjectSpawner m_objectSpawner;

    [SerializeField]
    [RequireInterface(typeof(IARInteractor))]
    [Tooltip("The AR Interactor that determines where to spawn the object.")]
    Object m_ARInteractorObject;

    XRBaseControllerInteractor m_ARInteractorAsControllerInteractor;

    private ARCameraManager arCameraManager;
    private Texture2D _videoTexture;

    private GameObject m_lastSelected;
    void Start()
    {
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

        _conjureKit.Init(ConjureKitConfiguration.DefaultConfigUri);

        _conjureKit.Connect();
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
                Debug.Log("Participant: " + participant.name);
                if (participant.GetComponent<ParticipantNetwork>().IsOwner)
                {
                    Debug.Log("Participant: " + participant.GetComponent<ParticipantNetwork>().NetworkBehaviourId.ToString());
                    participant.GetComponent<ParticipantNetwork>().m_ObjectIndex.Value = m_objectSpawner.m_SpawnOptionIndex;
                }
            }

            _conjureKit.GetSession().AddEntity(
                entityPose,
                onComplete: entity => HostSuccess(),
                onError: error => Debug.Log(error)
                );
        }

    }

    private void HostSuccess()
    {
        Debug.Log("Successfully hosted entity");
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

        foreach ( var participant in GameObject.FindGameObjectsWithTag("Participant"))
        {
            Debug.Log("Participant: " + participant.name);
            if( !participant.GetComponent<ParticipantNetwork>().IsOwner) 
            {
                Debug.Log("Propagating Participant: " + participant.GetComponent<ParticipantNetwork>().NetworkBehaviourId.ToString());
                index = participant.GetComponent<ParticipantNetwork>().m_ObjectIndex.Value;
            }
        }

        if(index >= 0 && index <= 4)
        {
            Instantiate(m_objectSpawner.objectPrefabs[index], pose.position, pose.rotation);
        }
        else
        {
            Instantiate(m_objectSpawner.objectPrefabs[0], pose.position, pose.rotation);
        }
        
    }
}
