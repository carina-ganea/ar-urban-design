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

    private ARCameraManager arCameraManager;
    private Texture2D _videoTexture;
    void Start()
    {
        arCameraManager = _camera.GetComponent<ARCameraManager>();

        _conjureKit = new ConjureKit(
            _camera.transform,
            "3e79d54a-a13d-4ed3-9d29-ace298095443",
            "utuX-U2fwyMLlE7M0F4EQP5qi2YRsI2iH_RFdpTyN5Sq6f2L"
            );

        _conjureKit.OnStateChanged += state =>
        {
            sessionState.text = state.ToString();
            ToggleControls(state == State.Calibrated);
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

        Vector3 position = _camera.transform.position + _camera.transform.forward * 0.5f;
        Quaternion rotation = Quaternion.Euler(0, _camera.transform.eulerAngles.y, 0);

        Pose entityPose = new Pose(position, rotation);

        _conjureKit.GetSession().AddEntity(
            entityPose,
            onComplete: entity => CreateCube(entity),
            onError: error => Debug.Log(error)
            );
    }

    public void CreateCubeEntity(Vector3 position, Quaternion rotation)
    {
        if (_conjureKit.GetState() != State.Calibrated)
        {
            return;
        }

        Pose entityPose = new Pose(position, rotation);

        _conjureKit.GetSession().AddEntity(
            entityPose,
            onComplete: entity => CreateCube(entity),
            onError: error => Debug.Log(error)
            );
    }

    private void CreateCube(Entity entity)
    {
        if(entity.Flag == EntityFlag.EntityFlagParticipantEntity)
        {
            return;
        }

        var pose = _conjureKit.GetSession().GetEntityPose(entity);
        Instantiate(_cube, pose.position, pose.rotation);
    }
}
