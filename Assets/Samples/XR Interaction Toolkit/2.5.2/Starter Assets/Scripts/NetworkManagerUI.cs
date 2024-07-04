using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button m_serverButton;
    [SerializeField] private Button m_clientButton;
    [SerializeField] private Button m_hostButton;

    private string myAddressLocal;
    private string myAddressGlobal;

    private void Awake()
    {
        m_serverButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });

        m_clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });

        m_hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
    }
    private void OnTransportEvent(NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime)
    {
        Debug.Log("OnTransportEvent: " + eventType);
    }
}
