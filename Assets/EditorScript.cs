using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.UI;
using Unity.VisualScripting;
using TMPro;
using System;
using TMPro.EditorUtilities;

public class EditorScript : MonoBehaviour
{

    #region Fields

    [SerializeField]
    private Button _startButton;

    [SerializeField]
    private Button _menuButton;

    [SerializeField]
    private Button _applyButton;

    [SerializeField]
    private GameObject _quad;

    [SerializeField]
    private TMP_InputField _input;

    [SerializeField]
    private TMP_Text _errorMsg;

    [SerializeField]
    private Toggle _clear;

    [SerializeField]
    private Toggle _interpolate;

    [SerializeField]
    private TMP_Dropdown _matterType;

    private int _gridSize;

    private Texture2D _grid;

    private bool _validSize = false;

    #endregion

    #region Properties

    public Texture2D Grid { get { return _grid; } }

    #endregion

    #region Game methods

    void Start()
    {
        AddEventHandlers();
        SetupUI();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            (int x, int y) pixelHitCoordinates = CalculatePixelCoordinates();

            if(pixelHitCoordinates == (-1, -1) || !ValidCoordinate(pixelHitCoordinates))
            {
                return;
            }

            Color pixelColor = _clear.isOn ? Color.white : Color.black;
            _grid.SetPixel(pixelHitCoordinates.x, pixelHitCoordinates.y, pixelColor);
            _grid.Apply();
        }
    }

    #endregion

    #region Private event methods

    private void OnStartClick()
    {
        //TODO error handling with exceptions!!!
        Persistence.GridSize = _gridSize;
        Persistence.Grid = _grid;
        Persistence.SaveCoordinates();
        Persistence.Interpolate = _interpolate.isOn;
        Persistence.MatterType = _matterType.options[_matterType.value].text == "FLUID" ? MatterType.FLUID : MatterType.GAS; //TODO change to switch
        SceneManager.LoadScene(1);

        SceneManager.LoadScene(2);
    }

    private void OnMenuClick()
    {
        SceneManager.LoadScene(0);
    }

    private void OnApplyClick()
    {
        if (_validSize)
        {
            _grid = new Texture2D(_gridSize + 2, _gridSize + 2, TextureFormat.ARGB32, false);
            _grid.filterMode = FilterMode.Point;

            for (int i = 0; i < _gridSize + 2; ++i)
            {
                for (int j = 0; j < _gridSize + 2; ++j)
                {
                    Color pixelColor = i == 0 || i == _gridSize + 1 || j == 0 || j == _gridSize + 1 ? Color.black : Color.white;
                    _grid.SetPixel(i, j, pixelColor);
                }
            }
            _grid.Apply();
            _quad.GetComponent<Renderer>().material.mainTexture = _grid;
        }
    }

    private void OnInputChanged(string value)
    {
        try
        {
            int parsed = int.Parse(value);
            if (parsed <= 0 || parsed > 100)
            {
                _validSize = false;
                throw new ArgumentException();
            }
            _errorMsg.text = "";
            _validSize = true;
            _gridSize = parsed;
        }
        catch (Exception e) when (e is ArgumentNullException || e is ArgumentException || e is FormatException || e is OverflowException)
        {
            _errorMsg.text = "Size must be between 1 and 100";
        }
    }

    #endregion

    #region Private methods

    private void AddEventHandlers()
    {
        _startButton.onClick.AddListener(() => OnStartClick());
        _menuButton.onClick.AddListener(() => OnMenuClick());
        _applyButton.onClick.AddListener(() => OnApplyClick());
        _input.onEndEdit.AddListener((string value) => OnInputChanged(value));
    }

    private void SetupUI()
    {
        _gridSize = 40;

        _grid = new Texture2D(42, 42, TextureFormat.ARGB32, false);
        _grid.filterMode = FilterMode.Point;

        for (int x = 0; x < 42; ++x)
        {
            for (int y = 0; y < 42; ++y)
            {
                Color pixelColor = x == 0 || x == 41 || y == 0 || y == 41 ? Color.black : Color.white;
                _grid.SetPixel(x, y, pixelColor);
            }
        }
        _grid.Apply();

        _quad.GetComponent<Renderer>().material.mainTexture = _grid;

        List<TMP_Dropdown.OptionData> matterOptions = new List<TMP_Dropdown.OptionData>();
        foreach (var matterType in Enum.GetNames(typeof(MatterType)))
        {
            matterOptions.Add(new TMP_Dropdown.OptionData(matterType));
        }
        _matterType.ClearOptions();
        _matterType.AddOptions(matterOptions);
    }

    private (int, int) CalculatePixelCoordinates()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector2 textCoord = hit.textureCoord;
            textCoord.x *= _grid.width;
            textCoord.y *= _grid.height;

            Vector2 tiling = _quad.GetComponent<Renderer>().material.mainTextureScale;

            int pixelHitX = Mathf.FloorToInt(textCoord.x * tiling.x);
            int pixelHitY = Mathf.FloorToInt(textCoord.y * tiling.y);

            return (pixelHitX, pixelHitY);
        }

        return (-1, -1);
    }

    private bool ValidCoordinate((int x, int y) pixelCoordinate_)
    {
        return pixelCoordinate_.x != 0 && pixelCoordinate_.x != _gridSize + 1 &&
               pixelCoordinate_.y != 0 && pixelCoordinate_.y != _gridSize + 1;
    }

    #endregion

}
