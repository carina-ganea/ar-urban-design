using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class NetworkParticipant : NetworkBehaviour
{
    public NetworkVariable<int> m_ObjectIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private ObjectSpawner m_spawner;
    [SerializeField] private ARInteractorSpawnTrigger m_spawnTrigger;

    public override void OnNetworkSpawn() 
    {
        m_spawner = GameObject.Find("Object Spawner").GetComponent<ObjectSpawner>();
        m_spawnTrigger = GameObject.Find("Object Spawner").GetComponent<ARInteractorSpawnTrigger>();

        m_spawner.objectSpawned += SpawnObjectServerRpc;
        m_spawnTrigger.objectSelected += GetOwnershipServerRpc;
    }

    void Update()
    {
        if (!IsOwner) return;

        m_ObjectIndex.Value = m_spawner.m_SpawnOptionIndex;
    }

    [ServerRpc]
    public void SpawnObjectServerRpc(int i, Vector3 position, Quaternion rotation)
    {
        Debug.Log("Spawn Object " + i.ToString() + " from Server side, Participant " + gameObject.GetComponent<NetworkObject>().NetworkObjectId.ToString());

        var obj = Instantiate(m_spawner.objectPrefabs[i], position, rotation);

        obj.GetComponent<NetworkObject>().Spawn(true);

    }

    [ServerRpc]
    public void GetOwnershipServerRpc(ulong participantID, ulong objectID)
    {
        //var res = GetNetworkObject(objectID).TrySetParent(GetNetworkObject(participantID), true);

        GetNetworkObject(objectID).ChangeOwnership(participantID);

        Debug.Log("Object " + objectID.ToString() + " reparented to participant " + participantID.ToString());
    }
}
