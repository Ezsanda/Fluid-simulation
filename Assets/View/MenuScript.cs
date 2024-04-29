using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{

    #region Fields

    [SerializeField]
    private Button _editorButton;

    [SerializeField]
    private Button _exitButton;

    #endregion

    #region Game Methods

    private void Start()
    {
        _exitButton.onClick.AddListener(() => OnExitClick());
        _editorButton.onClick.AddListener(() => OnEditorClick());
    }

    #endregion

    #region EventHandlers

    private void OnEditorClick()
    {
        SceneManager.LoadScene(1);
    }

    private void OnExitClick()
    {
        Application.Quit();
    }

    #endregion

}
