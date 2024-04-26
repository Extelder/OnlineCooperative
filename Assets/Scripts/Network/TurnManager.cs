using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    private List<NetworkPlayer> _players = new List<NetworkPlayer>();

    public void AddPlayer(NetworkPlayer networkPlayer)
    {
        _players.Add(networkPlayer);
    }
}
