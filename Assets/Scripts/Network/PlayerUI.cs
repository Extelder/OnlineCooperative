using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private NetworkPlayer _player;

    public void SetPlayer(string name)
    {
        _nameText.text = name;
    }
    
}
