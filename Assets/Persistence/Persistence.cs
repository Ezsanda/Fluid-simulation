using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class Persistence
{

	#region Fields

	private static Persistence _instance;

	private int _gridSize;

	private bool _interpolate;

    private MatterState _matterState;

	private MatterType _matterType;

	private float _timeStep;

	private float _viscosity;

	private float _gravity;

    private int _stepCount;

	private Texture2D _fluidGrid;

    private Texture2D _wallGrid;

    private WallType[,] _wallTypes;

    private PaintHelper[,] _paintHelper;

    private Color _fluidColor;

    private bool _firstStart;

    #endregion

    #region Properties

    public static Persistence Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Persistence();
            }
            return _instance;
        }
    }

    public int GridSize { get { return _gridSize; } }

    public bool Interpolate { get { return _interpolate; } }

    public MatterState MatterState { get { return _matterState; } }

    public MatterType MatterType { get { return _matterType; } }

    public float TimeStep { get { return _timeStep; } }

    public float Viscosity { get { return _viscosity; } }

    public float Gravity { get { return _gravity; } }

    public int StepCount { get { return _stepCount; } }

    public Texture2D FluidGrid { get { return _fluidGrid; } }

    public Texture2D WallGrid { get { return _wallGrid; } }

    public WallType[,] WallTypes { get { return _wallTypes; } }

    public PaintHelper[,] PaintHelper { get { return _paintHelper; } }

    public Color FluidColor { get { return _fluidColor; } }

    public bool FirstStart { get { return _firstStart; } }

    #endregion

    #region Constructor

    private Persistence()
    {
        _firstStart = true;
    }

    #endregion

    #region Private methods

    private int CalculateWallType(Texture2D grid_, int x_, int y_)
    {
        Color center = grid_.GetPixel(x_, y_);
        if(center == Color.white)
        {
            return (int)WallType.NONE;
        }

        Color top = grid_.GetPixel(x_, y_ + 1);
        Color right = grid_.GetPixel(x_ + 1, y_);
        Color bottom = grid_.GetPixel(x_, y_ - 1);
        Color left = grid_.GetPixel(x_ - 1, y_);

        if(top == Color.white && left == Color.white)
        {
            return (int)WallType.TOPLEFT;
        }
        else if(left == Color.black && right == Color.black && top == Color.white)
        {
            return (int)WallType.TOP;
        }
        else if(top == Color.white && right == Color.white)
        {
            return (int)WallType.TOPRIGHT;
        }
        else if(top == Color.black && bottom == Color.black && right == Color.white)
        {
            return (int)WallType.RIGHT;
        }
        else if(bottom == Color.white && right == Color.white)
        {
            return (int)WallType.BOTTOMRIGHT;
        }
        else if(left == Color.black && right == Color.black && bottom == Color.white)
        {
            return (int)WallType.BOTTOM;
        }
        else if (bottom == Color.white && left == Color.white)
        {
            return (int)WallType.BOTTOMLEFT;
        }
        else if(top == Color.black && bottom == Color.black && left == Color.white)
        {
            return (int)WallType.LEFT;
        }
        return (int)WallType.INNER;
    }

    #endregion

    #region Public methods

	public void SaveSettings(int gridSize_, Texture2D grid_, Color fluidColor_, bool interpolate_, MatterState matterState_, MatterType matterType_, float timeStep_, float viscosity_, float gravity_, float stepCount_, PaintHelper[,] paintHelper_)
	{
        _firstStart = false;

		StreamWriter sr = new StreamWriter("settings.txt", false);
        sr.WriteLine(gridSize_);
        sr.WriteLine(fluidColor_.r);
        sr.WriteLine(fluidColor_.g);
        sr.WriteLine(fluidColor_.b);
        sr.WriteLine(interpolate_);
        sr.WriteLine((int)matterState_);
        sr.WriteLine((int)matterType_);
        sr.WriteLine(timeStep_);
        sr.WriteLine(viscosity_);
        sr.WriteLine(gravity_);
        sr.WriteLine(stepCount_);

		for (int x = gridSize_; x > 0; --x)
		{
			for (int y = 1; y < gridSize_ + 1; ++y)
			{
                int currentPixel = CalculateWallType(grid_, y, x);
                sr.Write(currentPixel);
			}
			sr.WriteLine();
		}

        for (int x = gridSize_; x > 0; --x)
        {
            for (int y = 1; y < gridSize_ + 1; ++y)
            {
                sr.Write((int)paintHelper_[y, x]);
            }
            sr.WriteLine();
        }

		sr.Close();
    }

	public void LoadSettings()
    {
        StreamReader sr = new StreamReader("settings.txt");
		_gridSize = int.Parse(sr.ReadLine());
        _fluidColor = new Color(float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()), float.Parse(sr.ReadLine()));
        _interpolate = bool.Parse(sr.ReadLine());
        _matterState = (MatterState)int.Parse(sr.ReadLine());
        _matterType = (MatterType)int.Parse(sr.ReadLine());
        _timeStep = float.Parse(sr.ReadLine());
        _viscosity = float.Parse(sr.ReadLine());
        _gravity = float.Parse(sr.ReadLine());
        _stepCount = int.Parse(sr.ReadLine());
        _fluidGrid = new Texture2D(_gridSize + 2, _gridSize + 2, TextureFormat.ARGB32, false);
        _fluidGrid.filterMode = _interpolate ? FilterMode.Bilinear : FilterMode.Point;
        _wallGrid = new Texture2D(_gridSize + 2, _gridSize + 2, TextureFormat.ARGB32, false);
        _wallGrid.filterMode = FilterMode.Point;
        _wallTypes = new WallType[_gridSize + 2, _gridSize + 2];
        _paintHelper = new PaintHelper[_gridSize + 2, _gridSize + 2];

        Color wallColor = new Color(0, 0, 0, 255);
        Color transparentColor = new Color(255, 255, 255, 0);

        for (int x = _gridSize; x > 0; --x)
		{
            _wallGrid.SetPixel(x, 0, wallColor);
            _wallGrid.SetPixel(x, _gridSize + 1, wallColor);
            _wallGrid.SetPixel(0, x, wallColor);
            _wallGrid.SetPixel(_gridSize + 1, x, wallColor);

			string line = sr.ReadLine();
			for (int y = 1; y < _gridSize + 1; ++y)
			{
                WallType currentPixel = (WallType)int.Parse(line[y - 1].ToString());
                if (currentPixel != WallType.NONE)
                {
                    _wallGrid.SetPixel(y, x, wallColor);
                }
                else
                {
                    _wallGrid.SetPixel(y, x, transparentColor);
                }
                _wallTypes[y, x] = currentPixel;
                _fluidGrid.SetPixel(y, x, Color.white);
            }
		}
        _wallGrid.SetPixel(0, 0, wallColor);
        _wallGrid.SetPixel(0, _gridSize + 1, wallColor);
        _wallGrid.SetPixel(_gridSize + 1, 0, wallColor);
        _wallGrid.SetPixel(_gridSize + 1, _gridSize + 1, wallColor);

        _fluidGrid.Apply();
        _wallGrid.Apply();

        for (int x = _gridSize; x > 0; --x)
        {
            string line = sr.ReadLine();
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                PaintHelper currentPixel = (PaintHelper)int.Parse(line[y - 1].ToString());
                _paintHelper[y, x] = currentPixel;
            }
        }

        sr.Close();
    }

	#endregion

}
