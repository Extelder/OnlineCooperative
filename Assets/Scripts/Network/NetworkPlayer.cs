using System;
using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private GameObject[] _gameObjectsToDisable;
    [SerializeField] private MonoBehaviour[] monoBehavioursToDisable;
    [SerializeField] private GameObject[] _gameObjectsToVisualEnableAndSetParent;

    private NetworkMatch _networkMatch;

    public static NetworkPlayer localPlayer;
    public TextMesh NameDisplayText;
    [SyncVar(hook = "DisplayPlayerName")] public string PlayerDisplayName;
    [SyncVar] public string matchId;


    private void Start()
    {
        _networkMatch = GetComponent<NetworkMatch>();

        if (isLocalPlayer)
        {
            localPlayer = this;
            
            CmdSendName(MainMenu.Instance.DisplayName);
        }
        else
        {
            MainMenu.Instance.SpawnPlayerUIPrefab(this);
        }

        Invoke(nameof(EnableCursor), 1.3f);

        if (!isOwned)
        {
            for (int i = 0; i < monoBehavioursToDisable.Length; i++)
            {
                monoBehavioursToDisable[i].enabled = false;
            }
        }
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            for (int i = 0; i < _gameObjectsToDisable.Length; i++)
            {
                _gameObjectsToDisable[i].SetActive(false);
            }

            return;
        }

        
    }

    [Command]
    public void CmdSendName(string name)
    {
        PlayerDisplayName = name;
    }

    public void DisplayPlayerName(string name, string playerName)
    {
        name = PlayerDisplayName;
        Debug.Log("Имя " + name + " : " + playerName);
        NameDisplayText.text = playerName;
    }

    private void EnableCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HostGame()
    {
        string id = MainMenu.GetRandomID();
        CmdHostGame(id);
    }

    [Command]
    public void CmdHostGame(string id)
    {
        matchId = id;
        if (MainMenu.Instance.HostGame(id, gameObject))
        {
            Debug.Log("Лобби создано на заебца");
            _networkMatch.matchId = id.ToGuid();
            TargetHostGame(true, id);
        }
        else
        {
            Debug.Log("не заебца");
            TargetHostGame(false, id);
        }
    }

    [TargetRpc]
    public void TargetHostGame(bool succes, string id)
    {
        matchId = id;
        Debug.Log($"ID {matchId} == {id}");
        MainMenu.Instance.HostSuccess(succes, id);
    }

    public void JoinGame(string inputId)
    {
        CmdJoinGame(inputId);
    }

    [Command]
    public void CmdJoinGame(string id)
    {
        matchId = id;
        if (MainMenu.Instance.JoinGame(id, gameObject))
        {
            Debug.Log("Подключено к лобби на заебца");
            _networkMatch.matchId = id.ToGuid();
            TargetJoinGame(true, id);
        }
        else
        {
            Debug.Log("не подключенно к лобби заебца");
            TargetJoinGame(false, id);
        }
    }

    [TargetRpc]
    public void TargetJoinGame(bool succes, string id)
    {
        matchId = id;
        Debug.Log($"ID {matchId} == {id}");
        MainMenu.Instance.JoinSuccess(succes, id);
    }

    public void BeginGame()
    {
        CmdBeginGame();
    }

    [Command]
    public void CmdBeginGame()
    {
        MainMenu.Instance.BeginGame(matchId);
        Debug.Log("Игра запущенна");
    }

    public void StartGame()
    {
        TargetBeginGame();
    }

    [TargetRpc]
    public void TargetBeginGame()
    {
        Debug.Log($"ID {matchId} | Начало");
        if (isOwned)
        {
            for (int i = 0; i < _gameObjectsToDisable.Length; i++)
            {
                _gameObjectsToDisable[i].SetActive(true);
            }
        }
        else
        {
            Debug.Log("Not me");
            for (int i = 0; i < _gameObjectsToVisualEnableAndSetParent.Length; i++)
            {
                _gameObjectsToVisualEnableAndSetParent[i].SetActive(true);
                _gameObjectsToVisualEnableAndSetParent[i].transform.parent = transform;
            }
        }

        DontDestroyOnLoad(gameObject);
        MainMenu.Instance.InGame = true;
        MainMenu.Instance.DisableCamera();
        NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();

        foreach (var player in players)
        {
            player.transform.localScale= new Vector3(1, 1, 1);
        }
    }
}