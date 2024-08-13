using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using Unity.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using System;

public class NetworkParticipant : NetworkBehaviour
{
    private string[] names =
    {
        "SleepyKoala",
        "HappyFlamingo",
        "ExcitedArmadillo",
        "SpookedMouse",
        "GreenLizard"
    };
    public NetworkVariable<int> m_ObjectIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> m_isGroundSpawned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<FixedString4096Bytes> m_mapAnchorID = new NetworkVariable<FixedString4096Bytes>("");
    public NetworkVariable<FixedString128Bytes> m_name = new NetworkVariable<FixedString128Bytes>("");

    [SerializeField] private ObjectSpawner m_spawner;
    [SerializeField] private ARInteractorSpawnTrigger m_spawnTrigger;
    [SerializeField] private CloudAnchorHostingManager m_cloudAnchorHostingManager;
    ARTemplateMenuManager m_templateMenuManager;

    public override void OnNetworkSpawn() 
    {
        m_spawner = FindObjectsOfType<ObjectSpawner>(true)[0];
        m_spawnTrigger = FindObjectsOfType<ARInteractorSpawnTrigger>(true)[0];
        m_cloudAnchorHostingManager = GameObject.Find("Canvas").GetComponentInChildren<CloudAnchorHostingManager>(true);
        m_templateMenuManager = FindObjectOfType<ARTemplateMenuManager>();

        m_spawner.objectSpawned += SpawnObjectServerRpc;
        m_spawner.groundSpawned += SpawnGroundServerRpc;
        m_spawnTrigger.objectSelected += GetOwnershipServerRpc;
        m_templateMenuManager.deleteEntity += DespawnObjectServerRpc;

        m_name.Value = names[OwnerClientId % (ulong)names.Length];

        if(IsHost && NetworkManager.Singleton.ConnectedClientsIds.Count == 1)
        {
            foreach (var text in GameObject.Find("Canvas").GetComponentsInChildren<Text>(true))
            {
                if (text.transform.tag == "Instructions")
                {
                    text.text = "Host " + m_name.Value + " has successfully started the network client.";
                    text.transform.parent.gameObject.SetActive(true);

                    StartCoroutine(CloseConnectBar());
                }
            }

            return;
        }

        if( !IsHost)
        {
            foreach (var text in GameObject.Find("Canvas").GetComponentsInChildren<Text>(true))
            {
                if (text.transform.tag == "Instructions")
                {
                    text.text = "Welcome, " + m_name.Value;
                    text.transform.parent.gameObject.SetActive(true);

                    StartCoroutine(CloseConnectBar());
                }

            }
        }

    }

    private IEnumerator CloseConnectBar()
    {
        yield return new WaitForSeconds(3);

        GameObject.Find("Canvas/NetworkConnectBar").gameObject.SetActive(false);
    }

    void Update()
    {
        if (NetworkManager.Singleton == null) return;
        if (IsHost && m_cloudAnchorHostingManager != null) m_mapAnchorID.Value = m_cloudAnchorHostingManager.map_anchor_ID;
        if (!IsOwner) return;

        m_ObjectIndex.Value = m_spawner.m_SpawnOptionIndex;
    }

    [ServerRpc]
    public void SpawnObjectServerRpc(int i, Vector3 position, Quaternion rotation, ulong clientID)
    {
        Debug.Log("Spawn Object " + i.ToString() + " from Server side, Participant " + gameObject.GetComponent<NetworkObject>().NetworkObjectId.ToString());

        var obj = Instantiate(m_spawner.objectPrefabs[i], position, rotation);

        obj.GetComponent<NetworkObject>().SpawnWithOwnership(clientID);
    }

    [ServerRpc]
    public void DespawnObjectServerRpc(ulong id)
    {
        Debug.Log("Object " + id.ToString() + " destroyed");
        GetNetworkObject(id).Despawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetOwnershipServerRpc(ulong participantID, ulong objectID)
    {
        //var res = GetNetworkObject(objectID).TrySetParent(GetNetworkObject(participantID), true);

        GetNetworkObject(objectID).ChangeOwnership(participantID);

        Debug.Log("Object " + objectID.ToString() + " reparented to participant " + participantID.ToString());
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnGroundServerRpc(Vector3 position, Quaternion rotation)
    {
        if (m_isGroundSpawned.Value) return;  
        Debug.Log("Spawn Ground from Server side, Participant " + gameObject.GetComponent<NetworkObject>().NetworkObjectId.ToString());

        var obj = Instantiate(m_spawner.m_GroundPrefab, position, rotation);
        if(obj.GetComponent<XRSimpleInteractable>()) 
            obj.GetComponent<XRSimpleInteractable>().enabled = false;

        m_isGroundSpawned.Value = true;

        obj.GetComponent<NetworkObject>().Spawn(true);

    }
}
