using Auki.ConjureKit;
using Auki.ConjureKit.Manna;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    void Start()
    {
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

        _conjureKit.Connect();

        _manna = new Manna(_conjureKit);

        _conjureKit.OnEntityAdded += CreateCube;
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
