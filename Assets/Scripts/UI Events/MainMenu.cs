using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
public class MainMenu : MonoBehaviour
{
    private UIDocument _uiDocument;
    private Button _startButton;

    private List<Button> _menuButtons;
    private AudioSource _audioSource;
    private void Awake()
    {

        _uiDocument = GetComponent<UIDocument>();
        _audioSource = GetComponent<AudioSource>();

        _startButton = _uiDocument.rootVisualElement.Q("StartButton") as Button;
        _startButton.RegisterCallback<ClickEvent>(OnStartGameClick);

        _menuButtons = _uiDocument.rootVisualElement.Query<Button>().ToList();
        for (int i = 0; i < _menuButtons.Count; i++)
        {
            _menuButtons[i].RegisterCallback<ClickEvent>(OnAllButtonClick);
        }
    }
    private void OnDisable()
    {
        _startButton.UnregisterCallback<ClickEvent>(OnStartGameClick);
        for (int i = 0; i < _menuButtons.Count; i++)
        {
            _menuButtons[i].UnregisterCallback<ClickEvent>(OnAllButtonClick);
        }
    }

    private void OnStartGameClick(ClickEvent evt)
    {
        Debug.Log("Start");
    }

    private void OnAllButtonClick(ClickEvent evt)
    {
        _audioSource.Play();
    }
}
