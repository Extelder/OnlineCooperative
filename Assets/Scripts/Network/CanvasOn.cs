using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasOn : MonoBehaviour
{
    [SerializeField] private GameObject _canvas;

    private void Start()
    {
        _canvas.SetActive(true);
    }
}