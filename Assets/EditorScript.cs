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
using UnityEngine.EventSystems;

public class EditorScript : MonoBehaviour,IPointerDownHandler,IPointerUpHandler
{

    #region Fields

    [SerializeField]
    private Button _startButton;

    [SerializeField]
    private Button _menuButton;

    [SerializeField]
    private GameObject _quad;

    [SerializeField]
    private Toggle _interpolate;

    [SerializeField]
    private TMP_Dropdown _matterDropDown;

    [SerializeField]
    private Slider _resolutionSlider;

    [SerializeField]
    private TMP_Text _resolutionText;

    [SerializeField]
    private Slider _timeStepSlider;

    [SerializeField]
    private TMP_Text _timeStepText;

    [SerializeField]
    private Slider _viscositySlider;

    [SerializeField]
    private TMP_Text _viscosityText;

    [SerializeField]
    private Slider _gravitySlider;

    [SerializeField]
    private TMP_Text _gravityText;

    private int _gridSize;

    private MatterType _matterType;

    private float _timeStep;

    private float _viscosity;

    private float _gravity;

    private Texture2D _grid;

    private Persistence _persistence;

    private bool _leftDown;

    private bool _rightDown;

    #endregion

    #region Game methods

    void Start()
    {
        AddEventHandlers();
        SetupUI();
    }

    void Update()
    {
        if(_leftDown || _rightDown)
        {
            (int x, int y) pixelHitCoordinates = CalculatePixelCoordinates();

            if (pixelHitCoordinates == (-1, -1) || !ValidCoordinate(pixelHitCoordinates))
            {
                return;
            }

            Color pixelColor = _rightDown ? Color.white : Color.black;
            _grid.SetPixel(pixelHitCoordinates.x, pixelHitCoordinates.y, pixelColor);
            _grid.Apply();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _leftDown = true;
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            _rightDown = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _leftDown = false;
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            _rightDown = false;
        }
    }

    #endregion

    #region Private event methods

    private void OnStartClick()
    {
        _persistence = Persistence.GetInstance();
        _persistence.SaveSettings(_gridSize, _grid, _interpolate.isOn, _matterType, _timeStep, _viscosity, _gravity);

        SceneManager.LoadScene(2);
    }

    private void OnMenuClick()
    {
        SceneManager.LoadScene(0);
    }

    private void OnMatterChanged(int value)
    {
        _matterType = _matterDropDown.options[value].text == "FLUID" ? MatterType.FLUID : MatterType.GAS;
    }

    private void OnResolutionChanged(float value)
    {
        _resolutionText.text = value.ToString();
        _gridSize = (int)value;

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

    private void OnTimeStepChanged(float value)
    {
        _timeStepText.text = value.ToString();
        _timeStep = value;
    }

    private void OnViscosityChanged(float value)
    {
        _viscosityText.text = value.ToString();
        _viscosity = value;
    }

    private void OnGravityChanged(float value)
    {
        _gravityText.text = value.ToString();
        _gravity = value;
    }

    #endregion

    #region Private methods

    private void AddEventHandlers()
    {
        _startButton.onClick.AddListener(() => OnStartClick());
        _menuButton.onClick.AddListener(() => OnMenuClick());
        _matterDropDown.onValueChanged.AddListener((int value) => OnMatterChanged(value));
        _resolutionSlider.onValueChanged.AddListener((float value) => OnResolutionChanged(value));
        _timeStepSlider.onValueChanged.AddListener((float value) => OnTimeStepChanged(value));
        _viscositySlider.onValueChanged.AddListener((float value) => OnViscosityChanged(value));
        _gravitySlider.onValueChanged.AddListener((float value) => OnGravityChanged(value));
    }

    private void SetupUI()
    {
        SetupSliders();
        SetupGrid();
        SetupDropDown();
    }

    private void SetupSliders()
    {
        _resolutionSlider.minValue = 1;
        _resolutionSlider.maxValue = 100;
        _gridSize = 40;
        _resolutionText.text = _gridSize.ToString();
        _resolutionSlider.value = _gridSize;

        _timeStepSlider.minValue = 0;
        _timeStepSlider.maxValue = 0.02f;
        _timeStep = 0.01f;
        _timeStepText.text = _timeStep.ToString();
        _timeStepSlider.value = _timeStep;

        _viscositySlider.minValue = 0.01f;
        _viscositySlider.maxValue = 0.03f;
        _viscosity = 0.02f;
        _viscosityText.text = _viscosity.ToString();
        _viscositySlider.value = _viscosity;

        _gravitySlider.minValue = 90000;
        _gravitySlider.maxValue = 110000;
        _gravity = 100000;
        _gravityText.text = _gravity.ToString();
        _gravitySlider.value = _gravity;
    }

    private void SetupGrid()
    {
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
    }

    private void SetupDropDown()
    {
        List<TMP_Dropdown.OptionData> matterOptions = new List<TMP_Dropdown.OptionData>();
        foreach (var matterType in Enum.GetNames(typeof(MatterType)))
        {
            matterOptions.Add(new TMP_Dropdown.OptionData(matterType));
        }
        _matterDropDown.ClearOptions();
        _matterDropDown.AddOptions(matterOptions);
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
