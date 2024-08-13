using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EnterScene : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown _dropdown;
    public void ChangeScene()
    {
        if (_dropdown.value == 0) return;

        SceneManager.LoadScene(_dropdown.value, LoadSceneMode.Single);
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}
