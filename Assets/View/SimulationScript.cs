using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.Animations;
using System.Runtime.CompilerServices;
using UnityEngine.PlayerLoop;

public class SimulationScript : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

    #region Fields

    [SerializeField]
    private GameObject _fluidQuad;

    [SerializeField]
    private GameObject _wallQuad;

    [SerializeField]
    private Slider _densitySlider;

    [SerializeField]
    private TextMeshProUGUI _densityText;

    [SerializeField]
    private Button _editorButton;

    [SerializeField]
    private Button _pauseButton;

    [SerializeField]
    private Button _restartButton;

    [SerializeField]
    private TMP_Dropdown _toolDropDown;

    [SerializeField]
    private Toggle _moveToggle;

    private int _gridSize;

    private bool _isSimulating = true;

    private bool _leftDown;

    private Texture2D _fluidGrid;

    private Texture2D _wallGrid;

    private PDESolver _solver;

    private Gradient _colorRange;

    private Persistence _persistence;

    private Color _fluidColor;

    private Tool _selectedTool;

    private (int x, int y) _previousMousePosition;

    private (int, int)[] _toolPositions;

    private RayCaster _rayCaster;

    #endregion

    #region Game methods

    void Start()
    {
        AddEventHandlers();
        SetupUI();
    }

    void FixedUpdate()
    {
        if (_isSimulating)
        {
            try
            {
                if (!_moveToggle.isOn && _leftDown)
                {
                    (int x, int y) pixelHitCoordinates = _rayCaster.CalculatePixelCoordinates(_fluidQuad, _fluidGrid, _solver.Boundary.WallTypes);

                    _solver.UpdateVelocity();
                    _solver.UpdateDensity(_densitySlider.value, pixelHitCoordinates.x, pixelHitCoordinates.y);
                }
                else if (_moveToggle.isOn)
                {
                    (int x, int y) pixelHitCoordinates = _rayCaster.CalculatePixelCoordinates(_fluidQuad, _fluidGrid, _solver.Boundary.WallTypes);
                    UpdateToolPositions(pixelHitCoordinates);
                    PaintToolPositions(pixelHitCoordinates);

                    if (_leftDown)
                    {
                        (float x, float y) direction = CalculateDirection(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                        _solver.UpdateVelocity(direction.x, direction.y, _toolPositions);
                    }
                    else
                    {
                        _solver.UpdateVelocity();
                    }
                    _solver.UpdateDensity();
                }
                else
                {
                    _solver.UpdateVelocity();
                    _solver.UpdateDensity();
                }

                UpdateColors();
            }
            catch (NotHitException) { }
            catch (Exception e) when (e is InValidCoordinateException || e is NotPaintableException)
            {
                if (_previousMousePosition != (0, 0))
                {
                    ClearLastToolPositions();
                }
            }
            finally
            {
                _solver.UpdateVelocity();
                _solver.UpdateDensity();
                UpdateColors();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _leftDown = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _leftDown = false;
        }
    }

    #endregion

    #region Private event methods

    private void OnDensityChanged(float value)
    {
        _densityText.text = value.ToString();
    }

    private void OnEditorClick()
    {
        SceneManager.LoadScene(1);
    }

    private void OnPauseClick()
    {
        _isSimulating = !_isSimulating;
    }

    private void OnRestartClick()
    {
        _gridSize = _persistence.GridSize;
        _fluidGrid = _persistence.FluidGrid;
        _wallGrid = _persistence.WallGrid;
        _solver = new PDESolver(_gridSize, _persistence.TimeStep, _persistence.MatterState, _persistence.Viscosity, _persistence.StepCount, _persistence.Gravity, _persistence.WallTypes);
    }

    private void OnToolChanged(int value)
    {
        _selectedTool = (Tool)value;

        switch (_selectedTool)
        {
            case Tool.POINT:
                _toolPositions = new (int, int)[1];
                break;
            case Tool.SQUARE:
                _toolPositions = new (int, int)[4];
                break;
            case Tool.RECTANGLE:
                _toolPositions = new (int, int)[6];
                break;
            default:
                break;
        }
    }

    #endregion

    #region Private UI methods

    private void AddEventHandlers()
    {
        _densitySlider.onValueChanged.AddListener((float value) => OnDensityChanged(value));
        _editorButton.onClick.AddListener(() => OnEditorClick());
        _pauseButton.onClick.AddListener(() => OnPauseClick());
        _restartButton.onClick.AddListener(() => OnRestartClick());
        _toolDropDown.onValueChanged.AddListener((int value) => OnToolChanged(value));
    }

    private void SetupUI()
    {
        _rayCaster = RayCaster.Instance;
        _persistence = Persistence.Instance;
        _persistence.LoadSettings();

        _gridSize = _persistence.GridSize;
        _fluidGrid = _persistence.FluidGrid;
        _wallGrid = _persistence.WallGrid;
        _fluidColor = _persistence.FluidColor;
        _solver = new PDESolver(_gridSize, _persistence.TimeStep, _persistence.MatterState, _persistence.Viscosity, _persistence.StepCount, _persistence.Gravity, _persistence.WallTypes);
        _colorRange = new Gradient();

        _fluidQuad.GetComponent<Renderer>().material.mainTexture = _fluidGrid;
        _wallQuad.GetComponent<Renderer>().material.mainTexture = _wallGrid;

        _toolPositions = new (int, int)[1];

        SetupSlider();
        SetupDropDown();
    }

    private void SetupSlider()
    {
        _densitySlider.minValue = 0.1f;
        _densitySlider.maxValue = 2;
        _densitySlider.value = 1;

        _densityText.text = _densitySlider.value.ToString();
    }

    private void SetupDropDown()
    {
        List<TMP_Dropdown.OptionData> matterOptions = new List<TMP_Dropdown.OptionData>();
        foreach (var matterType in Enum.GetNames(typeof(Tool)))
        {
            matterOptions.Add(new TMP_Dropdown.OptionData(matterType));
        }
        _toolDropDown.ClearOptions();
        _toolDropDown.AddOptions(matterOptions);
    }

    private void UpdateColors()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                if (_solver.Boundary.WallTypes[x, y] == WallType.NONE && _fluidGrid.GetPixel(x, y) != Color.red)
                {
                    Color pixelColor = CalculatePixelColor(x, y);
                    _fluidGrid.SetPixel(x, y, pixelColor);
                }
            }
        }
        _fluidGrid.Apply();
    }

    private Color CalculatePixelColor(int x_, int y_)
    {
        (float minDensity, float maxDensity) = (0, 0.0001F);

        float pixelIntensity = (_solver.Grid.Density[x_, y_] - minDensity) / (maxDensity - minDensity);

        pixelIntensity = pixelIntensity < 0 ? 0 : pixelIntensity > 1 ? 1 : pixelIntensity;

        GradientColorKey[] colorKeys = new GradientColorKey[2];

        colorKeys[0] = new GradientColorKey(_fluidColor, 1.0F);
        colorKeys[1] = new GradientColorKey(Color.white, 0.0F);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0F, 1.0F);
        alphaKeys[1] = new GradientAlphaKey(1.0F, 1.0F);

        _colorRange.SetKeys(colorKeys, alphaKeys);
        Color pixelColor = _colorRange.Evaluate(pixelIntensity);

        return pixelColor;
    }

    #endregion

    #region Private movement/drawing methods

    private (float, float) CalculateDirection(float horizontal_, float vertical_)
    {
        float dirX = 0;
        float dirY = 0;

        switch (_selectedTool)
        {
            case Tool.POINT:
                dirX = 1.2f;
                dirY = 1.2f;
                break;
            case Tool.SQUARE:
                dirX = 0.3f;
                dirY = 0.3f;
                break;
            case Tool.RECTANGLE:
                dirX = 0.2f;
                dirY = 0.2f;
                break;
            default:
                break;
        }

        dirX = horizontal_ < 0 ? -dirX : dirX;
        dirY = vertical_ < 0 ? -dirY : dirY;

        return (dirX, dirY);
    }

    private void PaintToolPositions((int x, int y) pixelCoordinate_)
    {
        try
        {
            switch (_selectedTool)
            {
                case Tool.POINT:
                    DrawPoint(pixelCoordinate_);
                    break;
                case Tool.SQUARE:
                    DrawSquare(pixelCoordinate_);
                    break;
                case Tool.RECTANGLE:
                    DrawRectangle(pixelCoordinate_);
                    break;
                default:
                    break;
            }

            _previousMousePosition = pixelCoordinate_;
        }
        catch (NotPaintableException e)
        {
            throw e;
        }
    }

    private void DrawPoint((int x, int y) pixelCoordinate_)
    {
        Color pixelColor = CalculatePixelColor(_previousMousePosition.x, _previousMousePosition.y);
        _fluidGrid.SetPixel(_previousMousePosition.x, _previousMousePosition.y, pixelColor);
        _fluidGrid.SetPixel(pixelCoordinate_.x, pixelCoordinate_.y, Color.red);
        _fluidGrid.Apply();
    }

    private void DrawSquare((int x, int y) pixelCoordinate_)
    {
        if (pixelCoordinate_.x == _gridSize || pixelCoordinate_.y == 1)
        {
            throw new NotPaintableException();
        }

        for (int x = 0; x < 2; ++x)
        {
            for (int y = 0; y > -2; --y)
            {
                if (_previousMousePosition != (0, 0))
                {
                    Color pixelColor = CalculatePixelColor(_previousMousePosition.x + x, _previousMousePosition.y + y);
                    _fluidGrid.SetPixel(_previousMousePosition.x + x, _previousMousePosition.y + y, pixelColor);
                }
                _fluidGrid.SetPixel(pixelCoordinate_.x + x, pixelCoordinate_.y + y, Color.red);
            }
        }
        _fluidGrid.Apply();
    }

    private void DrawRectangle((int x, int y) pixelCoordinate_)
    {
        if (pixelCoordinate_.x == _gridSize || pixelCoordinate_.x == _gridSize - 1 || pixelCoordinate_.y == 1)
        {
            throw new NotPaintableException();
        }

        for (int x = 0; x < 3; ++x)
        {
            for (int y = 0; y > -2; --y)
            {
                if (_previousMousePosition != (0, 0))
                {
                    Color pixelColor = CalculatePixelColor(_previousMousePosition.x + x, _previousMousePosition.y + y);
                    _fluidGrid.SetPixel(_previousMousePosition.x + x, _previousMousePosition.y + y, pixelColor);
                }
                _fluidGrid.SetPixel(pixelCoordinate_.x + x, pixelCoordinate_.y + y, Color.red);
            }
        }
        _fluidGrid.Apply();
    }

    private void ClearLastToolPositions()
    {
        switch (_selectedTool)
        {
            case Tool.POINT:
                ClearPoint();
                break;
            case Tool.SQUARE:
                ClearSquare();
                break;
            case Tool.RECTANGLE:
                ClearRectangle();
                break;
            default:
                break;
        }
    }

    private void ClearPoint()
    {
        Color pixelColor = CalculatePixelColor(_previousMousePosition.x, _previousMousePosition.y);
        _fluidGrid.SetPixel(_previousMousePosition.x, _previousMousePosition.y, pixelColor);
        _fluidGrid.Apply();
    }

    private void ClearSquare()
    {
        for (int x = 0; x < 2; ++x)
        {
            for (int y = 0; y > -2; --y)
            {
                Color pixelColor = CalculatePixelColor(_previousMousePosition.x + x, _previousMousePosition.y + y);
                _fluidGrid.SetPixel(_previousMousePosition.x + x, _previousMousePosition.y + y, pixelColor);
            }
        }
        _fluidGrid.Apply();
    }

    private void ClearRectangle()
    {
        for (int x = 0; x < 3; ++x)
        {
            for (int y = 0; y > -2; --y)
            {
                Color pixelColor = CalculatePixelColor(_previousMousePosition.x + x, _previousMousePosition.y + y);
                _fluidGrid.SetPixel(_previousMousePosition.x + x, _previousMousePosition.y + y, pixelColor);
            }
        }
        _fluidGrid.Apply();
    }

    private void UpdateToolPositions((int x, int y) pixelCoordinates_)
    {
        int xLength = 0;
        int yLength = 0;

        switch (_selectedTool)
        {
            case Tool.POINT:
                xLength = 1;
                yLength = 1;
                break;
            case Tool.SQUARE:
                xLength = 2;
                yLength = 2;
                break;
            case Tool.RECTANGLE:
                xLength = 3;
                yLength = 2;
                break;
            default:
                break;
        }

        int i = 0;
        for (int x = 0; x < xLength; ++x)
        {
            for (int y = 0; y > -yLength; --y)
            {
                _toolPositions[i] = (pixelCoordinates_.x + x, pixelCoordinates_.y + y);
                ++i;
            }
        }
    }

    #endregion

}