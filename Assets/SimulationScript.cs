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

public class SimulationScript : MonoBehaviour
{

    #region Fields

    [SerializeField]
    private GameObject _quad;

    [SerializeField]
    private Slider _densitySlider;

    [SerializeField]
    private TextMeshProUGUI _densityText;

    [SerializeField]
    private Button _editorButton;

    [SerializeField]
    private Button _pauseButton;

    private int _gridSize;

    private bool _isSimulating = true;

    private Texture2D _grid;

    private PDESolver _solver;

    private Gradient _colorRange;

    private Persistence _persistence;

    #endregion

    #region Game methods

    void Start()
    {
        InitializeEntities();
    }

    void FixedUpdate()
    {
        if (_isSimulating)
        {
            _solver.UpdateVelocity();

            if (Input.GetMouseButtonDown(0))
            {
                (int x, int y) pixelHitCoordinates = CalculatePixelCoordinates();

                if (pixelHitCoordinates == (-1, -1) || !ValidCoordinate(pixelHitCoordinates))
                {
                    return;
                }

                _solver.UpdateDensity(_densitySlider.value, pixelHitCoordinates.x, pixelHitCoordinates.y);
            }
            else
            {
                _solver.UpdateDensity();
            }

            UpdateColors();
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

    #endregion

    #region Private methods

    private void InitializeEntities()
    {
        _densitySlider.onValueChanged.AddListener((float value) => OnDensityChanged(value));
        _editorButton.onClick.AddListener(() => OnEditorClick());
        _pauseButton.onClick.AddListener(() => OnPauseClick());

        _persistence = Persistence.GetInstance();
        _persistence.LoadSettings();

        _gridSize = _persistence.GridSize;
        _grid = _persistence.Grid;
        _solver = new PDESolver(_gridSize, _persistence.Diffuse, _persistence.TimeStep, _persistence.MatterType, _persistence.Viscosity, _persistence.StepCount, _persistence.Gravity, _persistence.WallTypes);
        _colorRange = new Gradient();

        _quad.GetComponent<Renderer>().material.mainTexture = _grid;

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
                    _grid.SetPixel(x, y, pixelColor);
                }
            }
        }
        _grid.Apply();
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

    private Color CalculatePixelColor(int x_, int y_)
    {
        //TODO scale min and max via testing
        (float minDensity, float maxDensity) = (0, 0.0001F);

        float pixelIntensity = (_solver.Grid.Density[x_, y_] - minDensity) / (maxDensity - minDensity);

        pixelIntensity = pixelIntensity < 0 ? 0 : pixelIntensity > 1 ? 1 : pixelIntensity;

        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = _persistence.MatterType == MatterType.FLUID ? new GradientColorKey(Color.blue, 1.0F) : new GradientColorKey(Color.yellow, 1.0F);
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

    #endregion

}
