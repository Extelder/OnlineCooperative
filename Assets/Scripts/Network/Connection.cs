using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Connection : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;

    private void Start()
    {
        if (!Application.isBatchMode)
        {
            _networkManager.StartClient();
        }
    }

    public void JoinClient()
    {
        _networkManager.networkAddress = "localhost";
        _networkManager.StartClient();
    }
}
