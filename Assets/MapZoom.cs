using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MapZoom : MonoBehaviour
{
    private GameObject _ground;
    private Slider _slider;

    void Start()
    {
        _ground = null;
        _slider = GetComponent<Slider>();

        _slider.onValueChanged.AddListener(delegate { Zoom(); });
    }
    void Update()
    {
        if(_ground == null)
        {
            _ground = GameObject.FindWithTag("Ground");
        }
    }

    void Zoom()
    {
        if (_ground == null)
            return;

        _ground.transform.localScale = new Vector3(_slider.value, _slider.value, _slider.value);
    }
}
