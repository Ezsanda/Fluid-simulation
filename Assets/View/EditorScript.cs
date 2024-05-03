using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class EditorScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    #region Fields

    [SerializeField]
    private Button _startButton;

    [SerializeField]
    private Button _menuButton;

    [SerializeField]
    private Button _resetButton;

    [SerializeField]
    private Button _colorPickerButton;

    [SerializeField]
    private GameObject _editorQuad;

    [SerializeField]
    private GameObject _colorPickerQuad;

    [SerializeField]
    private Toggle _interpolateToggle;

    [SerializeField]
    private TMP_Dropdown _matterStateDropDown;

    [SerializeField]
    private TMP_Dropdown _matterTypeDropDown;

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

    [SerializeField]
    private Slider _stepCountSlider;

    [SerializeField]
    private TMP_Text _stepCountText;

    private int _gridSize;

    private MatterState _matterState;

    private MatterType _matterType;

    private float _timeStep;

    private float _viscosity;

    private float _gravity;

    private int _stepCount;

    private PaintHelper[,] _paintHelper;

    private Texture2D _grid;

    private Texture2D _colorPickerTexture;

    private Persistence _persistence;

    private bool _leftDown;

    private bool _rightDown;

    private bool _colorPickerOpen;

    private MatterTypeInfo _matterTypeInfo;

    private RayCaster _rayCaster;

    #endregion

    #region Game methods

    void Start()
    {
        AddEventHandlers();
        SetupUI();
    }

    void Update()
    {
        try
        {
            if (!_colorPickerOpen && (_leftDown || _rightDown))
            {
                (int x, int y) pixelHitCoordinates = _rayCaster.CalculatePixelCoordinates(_editorQuad, _grid, _paintHelper, _leftDown);
                PaintCoordinates(pixelHitCoordinates);
            }
            else if (_colorPickerOpen && _leftDown)
            {
                (int x, int y) pixelHitCoordinates = _rayCaster.CalculatePixelCoordinates(_colorPickerQuad, _colorPickerTexture);
                _colorPickerButton.image.color = _colorPickerTexture.GetPixel(pixelHitCoordinates.x, pixelHitCoordinates.y);
                UpdateDropDown();
            }
        }
        catch (Exception e) when (e is NotHitException || e is InValidCoordinateException || e is NotPaintableException) { }
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
        _persistence.SaveSettings(_gridSize, _grid, _colorPickerButton.image.color, _interpolateToggle.isOn, _matterState, _matterType, _timeStep, _viscosity, _gravity, _stepCount, _paintHelper);

        SceneManager.LoadScene(2);
    }

    private void OnMenuClick()
    {
        SceneManager.LoadScene(0);
    }

    private void OnResetClick()
    {
        SetupSliders(40, 0.2f, 0.0002f, 15, 40);
        SetupGrid();
        SetupColorPicker(Color.blue);
        SetupPainter();
        SetupDropDowns(MatterType.CUSTOM, MatterState.FLUID);
        _interpolateToggle.isOn = true;
    }

    private void OnColorPickerClick()
    {
        _colorPickerOpen = !_colorPickerOpen;
        _startButton.interactable = !_colorPickerOpen;

        float colorPickerZ = _colorPickerOpen ? -1 : 1;
        float editorZ = _colorPickerOpen ? 1 : -1;

        _colorPickerQuad.transform.position += new Vector3(0, 0, colorPickerZ);
        _editorQuad.transform.position += new Vector3(0, 0, editorZ);
    }

    private void OnMatterStateChanged(int value)
    {
        _matterState = (MatterState)value;

        UpdateDropDown();
    }

    private void OnMatterTypeChanged(int value)
    {
        if ((MatterType)value != MatterType.CUSTOM)
        {
            SetParameters((MatterType)value);
        }
        _matterType = (MatterType)value;
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
        _editorQuad.GetComponent<Renderer>().material.mainTexture = _grid;

        _paintHelper = new PaintHelper[_gridSize + 2, _gridSize + 2];

        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                _paintHelper[x, y] = PaintHelper.NONE;
            }
        }
    }

    private void OnTimeStepChanged(float value)
    {
        _timeStepText.text = value.ToString();
        _timeStep = value;

        UpdateDropDown();
    }

    private void OnViscosityChanged(float value)
    {
        _viscosityText.text = value.ToString();
        _viscosity = value;

        UpdateDropDown();
    }

    private void OnGravityChanged(float value)
    {
        _gravityText.text = value.ToString();
        _gravity = value;

        UpdateDropDown();
    }

    private void OnStepCountChanged(float value)
    {
        _stepCountText.text = value.ToString();
        _stepCount = (int)value;

        UpdateDropDown();
    }

    #endregion

    #region Private methods

    private void AddEventHandlers()
    {
        _startButton.onClick.AddListener(() => OnStartClick());
        _menuButton.onClick.AddListener(() => OnMenuClick());
        _resetButton.onClick.AddListener(() => OnResetClick());
        _colorPickerButton.onClick.AddListener(() => OnColorPickerClick());
        _matterStateDropDown.onValueChanged.AddListener((int value) => OnMatterStateChanged(value));
        _matterTypeDropDown.onValueChanged.AddListener((int value) => OnMatterTypeChanged(value));
        _resolutionSlider.onValueChanged.AddListener((float value) => OnResolutionChanged(value));
        _timeStepSlider.onValueChanged.AddListener((float value) => OnTimeStepChanged(value));
        _viscositySlider.onValueChanged.AddListener((float value) => OnViscosityChanged(value));
        _gravitySlider.onValueChanged.AddListener((float value) => OnGravityChanged(value));
        _stepCountSlider.onValueChanged.AddListener((float value) => OnStepCountChanged(value));
    }

    private void SetupUI()
    {
        _persistence = Persistence.Instance;
        _matterTypeInfo = MatterTypeInfo.Instance;
        _rayCaster = RayCaster.Instance;

        if (_persistence.FirstStart)
        {
            //TODO test
            SetupSliders(40, 0.2f, 0.0002f, 15, 40);
            SetupGrid();
            SetupColorPicker(Color.blue);
            SetupPainter();
            SetupDropDowns(MatterType.WATER, MatterState.FLUID);
        }
        else
        {
            SetupSliders(_persistence.GridSize, _persistence.TimeStep, _persistence.Viscosity, _persistence.Gravity, _persistence.StepCount);
            SetupGrid(_persistence.WallGrid);
            SetupColorPicker(_persistence.FluidColor);
            SetupDropDowns(_persistence.MatterType, _persistence.MatterState);
            _interpolateToggle.isOn = _persistence.Interpolate;
            _paintHelper = _persistence.PaintHelper;
        }
    }

    private void SetupSliders(int resolution_, float timeStep_, float viscosity_, float gravity_, int stepCount_)
    {
        _resolutionSlider.minValue = 20;
        _resolutionSlider.maxValue = 60;
        _gridSize = resolution_;
        _resolutionText.text = _gridSize.ToString();
        _resolutionSlider.value = _gridSize;

        _timeStepSlider.minValue = 0;
        _timeStepSlider.maxValue = 0.5f;
        _timeStep = timeStep_;
        _timeStepText.text = _timeStep.ToString();
        _timeStepSlider.value = _timeStep;

        _viscositySlider.minValue = 0.0001f;
        _viscositySlider.maxValue = 0.01f;
        _viscosity = viscosity_;
        _viscosityText.text = _viscosity.ToString();
        _viscositySlider.value = _viscosity;

        _gravitySlider.minValue = -20;
        _gravitySlider.maxValue = 20;
        _gravity = gravity_;
        _gravityText.text = _gravity.ToString();
        _gravitySlider.value = _gravity;

        _stepCountSlider.minValue = 10;
        _stepCountSlider.maxValue = 100;
        _stepCount = stepCount_;
        _stepCountText.text = _stepCount.ToString();
        _stepCountSlider.value = _stepCount;
    }

    private void SetupGrid()
    {
        _grid = new Texture2D(_gridSize + 2, _gridSize + 2, TextureFormat.ARGB32, false);
        _grid.filterMode = FilterMode.Point;

        for (int x = 0; x < _gridSize + 2; ++x)
        {
            for (int y = 0; y < _gridSize + 2; ++y)
            {
                Color pixelColor = x == 0 || x == _gridSize + 1 || y == 0 || y == _gridSize + 1 ? Color.black : Color.white;
                _grid.SetPixel(x, y, pixelColor);
            }
        }
        _grid.Apply();
        _editorQuad.GetComponent<Renderer>().material.mainTexture = _grid;
    }

    private void SetupGrid(Texture2D wallGrid_)
    {
        _grid = new Texture2D(_gridSize + 2, _gridSize + 2, TextureFormat.ARGB32, false);
        _grid.filterMode = FilterMode.Point;

        for (int x = 0; x < _gridSize + 2; ++x)
        {
            for (int y = 0; y < _gridSize + 2; ++y)
            {
                Color pixelColor = wallGrid_.GetPixel(x, y).a == 0 ? Color.white : Color.black;
                _grid.SetPixel(x, y, pixelColor);
            }
        }
        _grid.Apply();
        _editorQuad.GetComponent<Renderer>().material.mainTexture = _grid;
    }

    private void SetupColorPicker(Color colorPickerColor_)
    {
        _colorPickerTexture = new Texture2D(256, 256, TextureFormat.ARGB32, false);

        for (int x = 0; x < 255; ++x)
        {
            for (int y = 0; y < 255; ++y)
            {
                float hue = x / 255.0f;
                Color pixelColor = Color.HSVToRGB(hue, 1.0f, 1.0f);
                _colorPickerTexture.SetPixel(x, y, pixelColor);
            }
        }
        _colorPickerTexture.Apply();
        _colorPickerQuad.GetComponent<Renderer>().material.mainTexture = _colorPickerTexture;

        _colorPickerButton.image.color = colorPickerColor_;
        _colorPickerOpen = false;
    }

    private void SetupPainter()
    {
        _paintHelper = new PaintHelper[_gridSize + 2, _gridSize + 2];

        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                _paintHelper[x, y] = PaintHelper.NONE;
            }
        }
    }

    private void SetupDropDowns(MatterType matterType_, MatterState matterState_)
    {
        List<TMP_Dropdown.OptionData> matterTypeOptions = new List<TMP_Dropdown.OptionData>();
        foreach (var matterType in Enum.GetNames(typeof(MatterType)))
        {
            matterTypeOptions.Add(new TMP_Dropdown.OptionData(matterType));
        }
        _matterTypeDropDown.ClearOptions();
        _matterTypeDropDown.AddOptions(matterTypeOptions);
        _matterType = matterType_;
        _matterTypeDropDown.value = (int)_matterType;

        List<TMP_Dropdown.OptionData> matterStateOptions = new List<TMP_Dropdown.OptionData>();
        foreach (var matterState in Enum.GetNames(typeof(MatterState)))
        {
            matterStateOptions.Add(new TMP_Dropdown.OptionData(matterState));
        }
        _matterStateDropDown.ClearOptions();
        _matterStateDropDown.AddOptions(matterStateOptions);
        _matterState = matterState_;
        _matterStateDropDown.value = (int)_matterState;
    }

    private void PaintCoordinates((int x, int y) pixelCoordinate_)
    {
        Color pixelColor = _rightDown ? Color.white : Color.black;

        _grid.SetPixel(pixelCoordinate_.x, pixelCoordinate_.y, pixelColor);
        _grid.SetPixel(pixelCoordinate_.x + 1, pixelCoordinate_.y, pixelColor);
        _grid.SetPixel(pixelCoordinate_.x, pixelCoordinate_.y - 1, pixelColor);
        _grid.SetPixel(pixelCoordinate_.x + 1, pixelCoordinate_.y - 1, pixelColor);

        _paintHelper[pixelCoordinate_.x, pixelCoordinate_.y] = _rightDown ? PaintHelper.NONE : PaintHelper.TOPLEFT;
        _paintHelper[pixelCoordinate_.x + 1, pixelCoordinate_.y] = _rightDown ? PaintHelper.NONE : PaintHelper.TOPRIGHT;
        _paintHelper[pixelCoordinate_.x, pixelCoordinate_.y - 1] = _rightDown ? PaintHelper.NONE : PaintHelper.BOTTOMLEFT;
        _paintHelper[pixelCoordinate_.x + 1, pixelCoordinate_.y - 1] = _rightDown ? PaintHelper.NONE : PaintHelper.BOTTOMRIGHT;

        _grid.Apply();
    }

    private void SetParameters(MatterType matterType_)
    {
        _timeStepSlider.value = _matterTypeInfo.TimeStep(matterType_);
        _viscositySlider.value = _matterTypeInfo.Viscosity(matterType_);
        _gravitySlider.value = _matterTypeInfo.Gravity(matterType_);
        _stepCountSlider.value = _matterTypeInfo.StepCount(matterType_);
        _colorPickerButton.image.color = _matterTypeInfo.Color(matterType_);
        _matterStateDropDown.value = (int)_matterTypeInfo.MatterState(matterType_);
    }

    private void UpdateDropDown()
    {
        foreach (MatterType type in Enum.GetValues(typeof(MatterType)))
        {
            if (type != MatterType.CUSTOM && CheckParameters(type))
            {
                _matterTypeDropDown.value = (int)type;
                _matterType = type;
                return;
            }
        }
        _matterTypeDropDown.value = (int)MatterType.CUSTOM;
        _matterType = MatterType.CUSTOM;
    }

    private bool CheckParameters(MatterType matterType_)
    {
        return _timeStep == _matterTypeInfo.TimeStep(matterType_) &&
               _viscosity == _matterTypeInfo.Viscosity(matterType_) &&
               _gravity == _matterTypeInfo.Gravity(matterType_) &&
               _stepCount == _matterTypeInfo.StepCount(matterType_) &&
               _colorPickerButton.image.color == _matterTypeInfo.Color(matterType_) &&
               _matterState == _matterTypeInfo.MatterState(matterType_);
    }

    #endregion

}
