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

public class SimulationScript : MonoBehaviour,IPointerDownHandler,IPointerUpHandler
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

    private int _gridSize;

    private bool _isSimulating = true;

    private bool _leftDown;

    private bool _rightDown;

    private Texture2D _fluidGrid;

    private Texture2D _wallGrid;

    private PDESolver _solver;

    private Gradient _colorRange;

    private Persistence _persistence;

    private Color _fluidColor;

    #endregion

    #region Game methods

    void Start()
    {
        AddEventHandlers();
        SetupUI();
    }

    //TODO szebben
    void FixedUpdate()
    {
        if (_isSimulating)
        {
            if(_leftDown || _rightDown)
            {
                (int x, int y) pixelHitCoordinates = CalculatePixelCoordinates();

                if (pixelHitCoordinates == (-1, -1) || !ValidCoordinate(pixelHitCoordinates))
                {
                    return;
                }

                if(_leftDown)
                {
                    _solver.UpdateVelocity();
                    _solver.UpdateDensity(_densitySlider.value, pixelHitCoordinates.x, pixelHitCoordinates.y);
                }
                else
                {
                    (int x, int y) direction = CalculateDirection(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                    _solver.UpdateVelocity(direction.x, direction.y, pixelHitCoordinates.x, pixelHitCoordinates.y);
                    _solver.UpdateDensity();
                }

            }
            else
            {
                _solver.UpdateVelocity();
                _solver.UpdateDensity();
            }

            UpdateColors();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
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
        if(eventData.button == PointerEventData.InputButton.Left)
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
        _solver = new PDESolver(_gridSize, _persistence.TimeStep, _persistence.Diffuse, _persistence.Viscosity, _persistence.StepCount, _persistence.Gravity, _persistence.WallTypes);

        _densitySlider.value = 1;
        _densityText.text = _densitySlider.value.ToString();
    }

    #endregion

    #region Private methods

    private void AddEventHandlers()
    {
        _densitySlider.onValueChanged.AddListener((float value) => OnDensityChanged(value));
        _editorButton.onClick.AddListener(() => OnEditorClick());
        _pauseButton.onClick.AddListener(() => OnPauseClick());
        _restartButton.onClick.AddListener(() => OnRestartClick());
    }

    private void SetupUI()
    {
        _persistence = Persistence.GetInstance();
        _persistence.LoadSettings();

        _gridSize = _persistence.GridSize;
        _fluidGrid = _persistence.FluidGrid;
        _wallGrid = _persistence.WallGrid;
        _fluidColor = _persistence.FluidColor;
        _solver = new PDESolver(_gridSize, _persistence.TimeStep, _persistence.Diffuse, _persistence.Viscosity, _persistence.StepCount, _persistence.Gravity, _persistence.WallTypes);
        _colorRange = new Gradient();

        _fluidQuad.GetComponent<Renderer>().material.mainTexture = _fluidGrid;
        _wallQuad.GetComponent<Renderer>().material.mainTexture = _wallGrid;

        _densitySlider.minValue = 1;
        _densitySlider.maxValue = 10;
        _densitySlider.value = 1;

        _densityText.text = _densitySlider.value.ToString();
    }

    private void UpdateColors()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                if(_solver.Boundary.WallTypes[x, y] == WallType.NONE)
                {
                    Color pixelColor = CalculatePixelColor(x, y);
                    _fluidGrid.SetPixel(x, y, pixelColor);
                }
            }
        }
        _fluidGrid.Apply();
    }

    private (int, int) CalculatePixelCoordinates()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector2 textCoord = hit.textureCoord;
            textCoord.x *= _fluidGrid.width;
            textCoord.y *= _fluidGrid.height;

            Vector2 tiling = _fluidQuad.GetComponent<Renderer>().material.mainTextureScale;

            int pixelHitX = Mathf.FloorToInt(textCoord.x * tiling.x);
            int pixelHitY = Mathf.FloorToInt(textCoord.y * tiling.y);

            return (pixelHitX, pixelHitY);
        }

        return (-1, -1);
    }

    private Color CalculatePixelColor(int x_, int y_)
    {
        //TODO scale min and max via testing
        (float minDensity, float maxDensity) = (0, 0.0001F);

        float pixelIntensity = (_solver.Grid.Density[x_, y_] - minDensity) / (maxDensity - minDensity);

        pixelIntensity = pixelIntensity < 0 ? 0 : pixelIntensity > 1 ? 1 : pixelIntensity;

        GradientColorKey[] colorKeys = new GradientColorKey[2];

        switch (_persistence.MatterType)
        {
            case MatterType.NONE:
                colorKeys[0] = new GradientColorKey(_fluidColor, 1.0F);
                break;
            case MatterType.WATER:
                colorKeys[0] = new GradientColorKey(Color.blue, 1.0F);
                break;
            case MatterType.HONEY:
                break;
            case MatterType.HIDROGEN:
                colorKeys[0] = new GradientColorKey(Color.yellow, 1.0F);
                break;
            default:
                break;
        }

        colorKeys[1] = new GradientColorKey(Color.white, 0.0F);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0F, 1.0F);
        alphaKeys[1] = new GradientAlphaKey(1.0F, 1.0F);

        _colorRange.SetKeys(colorKeys, alphaKeys);
        Color pixelColor = _colorRange.Evaluate(pixelIntensity);

        return pixelColor;
    }

    private bool ValidCoordinate((int x, int y) pixelCoordinate_)
    {
        return pixelCoordinate_.x != 0 && pixelCoordinate_.x != _gridSize + 1 &&
               pixelCoordinate_.y != 0 && pixelCoordinate_.y != _gridSize + 1 &&
               _solver.Boundary.WallTypes[pixelCoordinate_.x, pixelCoordinate_.y] == WallType.NONE;
    }

    private (int,int) CalculateDirection(float horizontal_, float vertical_)
    {
        int outX = horizontal_ < 0 ? -3 : horizontal_ > 0 ? 3 : 0;
        int outY = vertical_ < 0 ? -3 : vertical_ > 0 ? 3 : 0;

        return (outX, outY);
    }

    #endregion

}
