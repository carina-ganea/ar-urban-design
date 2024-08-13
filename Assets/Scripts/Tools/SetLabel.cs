using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UIElements;

public class SetLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text m_labelText;
    private string[] names =
{
        "SleepyKoala",
        "HappyFlamingo",
        "ExcitedArmadillo",
        "SpookedMouse",
        "GreenLizard"
    };
    private void Start()
    {
        if(transform.parent.GetComponent<ConjureKitEntity>() != null)
        {
            m_labelText.text = names[transform.parent.GetComponent<ConjureKitEntity>().OwnerID % 5];
            return;
        }
        foreach(var part in FindObjectsOfType<NetworkParticipant>())
        {
            if(transform.parent.GetComponent<NetworkObject>().OwnerClientId == part.OwnerClientId)
            {
                m_labelText.text = part.m_name.Value.ToString();
            }
        }
    }

    private void Update()
    {
        transform.rotation = Camera.main.transform.rotation;
    }
}
