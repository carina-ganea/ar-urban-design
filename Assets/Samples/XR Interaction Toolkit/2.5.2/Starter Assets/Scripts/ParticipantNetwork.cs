using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class ParticipantNetwork : NetworkBehaviour
{
    public NetworkVariable<int> m_ObjectIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private ObjectSpawner m_spawner;

    public override void OnNetworkSpawn() 
    {
        m_spawner = GameObject.Find("Object Spawner").GetComponent<ObjectSpawner>();
    }

    void Update()
    {
        if (!IsOwner) return;

        m_ObjectIndex.Value = m_spawner.m_SpawnOptionIndex;
    }
}
