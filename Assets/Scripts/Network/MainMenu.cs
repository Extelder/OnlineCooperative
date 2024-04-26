using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Security.Cryptography;
using System.Text;
using Mirror;
using TMPro;

[System.Serializable]
public class Match : NetworkBehaviour
{
    public string ID;
    public readonly List<GameObject> players = new List<GameObject>();

    public Match(string id, GameObject player)
    {
        this.ID = id;
        players.Add(player);
    }
}

public static class MatchExtension
{
    public static Guid ToGuid(this string id)
    {
        MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();
        byte[] inputBytes = Encoding.Default.GetBytes(id);
        byte[] hasBytes = provider.ComputeHash(inputBytes);
        return new Guid(hasBytes);
    }
}

public class MainMenu : NetworkBehaviour
{
    [SerializeField] private InputField _joinInputField;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _openChangeNameButton;
    [SerializeField] private GameObject _changeNamePanel;
    [SerializeField] private GameObject _closeChangeNameButton;
    [SerializeField] private Button _changeNameReadyButton;
    [SerializeField] private InputField _enteredNameInputField;
    [SerializeField] private int _firstTime = 1;
    [SyncVar] public string DisplayName;
    [SerializeField] private Canvas _lobbyCanvas;
    [Space(10)] [SerializeField] private Transform _uiPlayerParent;
    [SerializeField] private GameObject _uiPlayerPrefab;
    [SerializeField] private GameObject _camera;
    [SerializeField] private TextMeshProUGUI _idText;
    [SerializeField] private Button BeginGameButton;
    [SerializeField] private GameObject _turnManager;
    [SerializeField] public bool InGame;

    public static MainMenu Instance { get; private set; }
    public readonly SyncList<Match> matches = new SyncList<Match>();
    public readonly SyncList<string> matchIds = new SyncList<string>();

    private NetworkManager _networkManager;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Invoke(nameof(EnableCursor), 1.3f);

        _networkManager = FindObjectOfType<NetworkManager>();

        _firstTime = PlayerPrefs.GetInt("firstTime", 1);

        if (!PlayerPrefs.HasKey("Name"))
        {
            return;
        }

        string defaultName = PlayerPrefs.GetString("Name");
        _enteredNameInputField.text = defaultName;
        DisplayName = defaultName;
        SetName(defaultName);
    }

    private void EnableCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SetName(string name)
    {
        _changeNameReadyButton.interactable = !string.IsNullOrEmpty(name);
    }

    public void SetFirstOpenedByMethod(int value)
    {
        _firstTime = value;
    }
    
    private void Update()
    {
        if (!InGame)
        {
            NetworkPlayer[] players = FindObjectsOfType<NetworkPlayer>();
            for (int i = 0; i < players.Length; i++)
            {
                players[i].gameObject.transform.localScale = Vector3.zero;
            }

            if (_firstTime == 1)
            {
                _changeNamePanel.SetActive(true);
            }

            PlayerPrefs.SetInt("firstTime", _firstTime);
        }
    }

    public void SaveName()
    {
        _joinInputField.interactable = false;
        _hostButton.interactable = false;
        _joinButton.interactable = false;
        _openChangeNameButton.interactable = false;
        _firstTime = 0;
        _changeNamePanel.SetActive(false);
        DisplayName = _enteredNameInputField.text;
        PlayerPrefs.SetString("Name", DisplayName);
        Invoke(nameof(Disconnect), 1f);
    }

    private void Disconnect()
    {
        if (_networkManager.mode == NetworkManagerMode.Host)
        {
            _networkManager.StopHost();
        }
        else if (_networkManager.mode == NetworkManagerMode.ClientOnly)
        {
            _networkManager.StopClient();
        }
    }

    public void DisableCamera()
    {
        _camera.SetActive(false);
    }

    public void Host()
    {
        _joinInputField.interactable = false;
        _hostButton.interactable = false;
        _joinButton.interactable = false;
        _openChangeNameButton.interactable = false;

        NetworkPlayer.localPlayer.HostGame();
    }

    public void HostSuccess(bool success, string mathcID)
    {
        if (success)
        {
            _lobbyCanvas.enabled = true;

            SpawnPlayerUIPrefab(NetworkPlayer.localPlayer);
            _idText.text = mathcID;
            BeginGameButton.interactable = true;
        }
        else
        {
            _joinInputField.interactable = true;
            _hostButton.interactable = true;
            _joinButton.interactable = true;
            _openChangeNameButton.interactable = true;
        }
    }

    public void Join()
    {
        _joinInputField.interactable = false;
        _hostButton.interactable = false;
        _joinButton.interactable = false;
        _openChangeNameButton.interactable = false;

        NetworkPlayer.localPlayer.JoinGame(_joinInputField.text.ToUpper());
    }

    public void JoinSuccess(bool success, string mathcID)
    {
        if (success)
        {
            _lobbyCanvas.enabled = true;

            SpawnPlayerUIPrefab(NetworkPlayer.localPlayer);
            _idText.text = mathcID;
            BeginGameButton.interactable = false;
        }
        else
        {
            _joinInputField.interactable = true;
            _hostButton.interactable = true;
            _joinButton.interactable = true;
            _openChangeNameButton.interactable = true;
        }
    }

    public bool HostGame(string matchId, GameObject player)
    {
        if (!matchIds.Contains(matchId))
        {
            matchIds.Add(matchId);
            matches.Add(new Match(matchId, player));
            return true;
        }

        return false;
    }

    public bool JoinGame(string matchId, GameObject player)
    {
        if (matchIds.Contains(matchId))
        {
            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].ID == matchId)
                {
                    matches[i].players.Add(player);
                    break;
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    public static string GetRandomID()
    {
        string id = string.Empty;
        for (int i = 0; i < 5; i++)
        {
            int rand = UnityEngine.Random.Range(0, 36);
            if (rand < 26)
            {
                id += (char) (rand + 65);
            }
            else
            {
                id += (rand - 26).ToString();
            }
        }

        return id;
    }

    public void SpawnPlayerUIPrefab(NetworkPlayer player)
    {
        GameObject newUiPlayer = Instantiate(_uiPlayerPrefab, _uiPlayerParent);
        newUiPlayer.GetComponent<PlayerUI>().SetPlayer(player.PlayerDisplayName);
    }

    public void StartGame()
    {
        NetworkPlayer.localPlayer.BeginGame();
    }

    public void BeginGame(string matchID)
    {
        GameObject newTurnManager = Instantiate(_turnManager);
        NetworkServer.Spawn(newTurnManager);
        newTurnManager.GetComponent<NetworkMatch>().matchId = matchID.ToGuid();
        TurnManager turnManager = newTurnManager.GetComponent<TurnManager>();

        for (int i = 0; i < matches.Count; i++)
        {
            if (matches[i].ID == matchID)
            {
                foreach (var player in matches[i].players)
                {
                    NetworkPlayer networkPlayer = player.GetComponent<NetworkPlayer>();
                    turnManager.AddPlayer(networkPlayer);
                    networkPlayer.StartGame();
                }

                break;
            }
        }
    }
}