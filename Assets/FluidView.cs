using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Toolbars;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{

    #region Fields

    [SerializeField]
    private int _size;

    [SerializeField]
    private float _densityMagnitude;

    [SerializeField]
    private float _timeStep;

    [SerializeField]
    private float _viscosity;

    [SerializeField]
    private int _stepCount;

    [SerializeField]
    private float _gravity;

    [SerializeField]
    private MatterType _type;

    [SerializeField]
    private bool _useInterpolation;

    private GameObject _quad;

    private Texture2D _grid;

    private PDESolver _solver;

    private Gradient _colorRange;

    #endregion

    #region Game methods

    void Start()
    {
        InitializeEntities();
        GenerateGrid();
    }

    void FixedUpdate()
    {
        _solver.UpdateVelocity();

        if (Input.GetMouseButtonDown(0))
        {
            (int x, int y) pixelHitCoordinates = CalculatePixelCoordinates();

            if (!ValidCoordinate(pixelHitCoordinates))
            {
                return;
            }

            _solver.UpdateDensity(_densityMagnitude,pixelHitCoordinates.x,pixelHitCoordinates.y);

        }
        else
        {
            _solver.UpdateDensity();
        }

        UpdateColors();
    }

    #endregion

    #region Private methods

    private void InitializeEntities()
    {
        _grid = new Texture2D(_size + 2, _size + 2, TextureFormat.ARGB32, false);
        _grid.filterMode = _useInterpolation ? FilterMode.Bilinear : FilterMode.Point;
        _solver = new PDESolver(_size, _timeStep, _viscosity, _stepCount,_gravity,_type);
        _quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _colorRange = new Gradient();

        //quad scaling
        float quadHeight = Camera.main.orthographicSize * 2;
        float quadWidth = quadHeight * Screen.width / Screen.height;
        _quad.transform.localScale = new Vector3(quadWidth, quadHeight, 1);

        //material setting
        Material fluidMaterial = (Material)Resources.Load("FluidMaterial");
        _quad.GetComponent<Renderer>().material = fluidMaterial;
        _quad.GetComponent<Renderer>().material.mainTexture = _grid;
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < _size + 2; ++x)
        {
            for (int y = 0; y < _size + 2; ++y)
            {
                Color pixelColor = _solver.Boundary.Walls[x,y] ? Color.black : Color.white;
                _grid.SetPixel(x, y, pixelColor);
            }
        }
        _grid.Apply();
    }

    private void UpdateColors()
    {
        for (int x = 1; x < _size + 1; ++x)
        {
            for (int y = 1; y < _size + 1; ++y)
            {
                if (!_solver.Boundary.Walls[x,y])
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
        (float minDensity, float maxDensity) = (0F, 0.0001F);

        float pixelIntensity = (_solver.Grid.Density[x_, y_] - minDensity) / (maxDensity - minDensity);
        pixelIntensity = pixelIntensity < 0 ? 0 : pixelIntensity > 1 ? 1 : pixelIntensity;

        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = _type == MatterType.FLUID ? new GradientColorKey(Color.blue, 1.0F) : new GradientColorKey(Color.yellow, 1.0F);
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
        return pixelCoordinate_.x != 0 && pixelCoordinate_.x != _size + 1 &&
               pixelCoordinate_.y != 0 && pixelCoordinate_.y != _size + 1 &&
               !_solver.Boundary.Walls[pixelCoordinate_.x, pixelCoordinate_.y];
    }

    #endregion

}
