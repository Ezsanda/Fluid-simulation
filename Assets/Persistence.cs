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

	private MatterType _matterType;

	private float _timeStep;

	private float _viscosity;

	private float _gravity;

    private int _stepCount;

	private Texture2D _grid;

    private WallType[,] _wallTypes;

	#endregion

	#region Properties

	public int GridSize { get { return _gridSize; } }

    public bool Interpolate { get { return _interpolate; } }

    public MatterType MatterType { get { return _matterType; } }

    public float TimeStep { get { return _timeStep; } }

    public float Viscosity { get { return _viscosity; } }

    public float Gravity { get { return _gravity; } }

    public int StepCount { get { return _stepCount; } }

    public Texture2D Grid { get { return _grid; } }

    public WallType[,] WallTypes { get { return _wallTypes; } }

    #endregion

    #region Private methods

    //TODO szebben
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

    public static Persistence GetInstance()
    {
        if(_instance == null)
        {
            _instance = new Persistence();
        }
        return _instance;
    }

	public void SaveSettings(int gridSize_, Texture2D grid_, bool interpolate_, MatterType matterType_, float timeStep_, float viscosity_, float gravity_, float stepCount_)
	{
		StreamWriter sr = new StreamWriter("settings.txt", false);
        sr.WriteLine(gridSize_);
        sr.WriteLine(interpolate_);
        sr.WriteLine(matterType_);
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
		sr.Close();
    }

	public void LoadSettings()
    {
        StreamReader sr = new StreamReader("settings.txt");
		_gridSize = int.Parse(sr.ReadLine());
        _interpolate = bool.Parse(sr.ReadLine());
        _matterType = sr.ReadLine() == "FLUID" ? MatterType.FLUID : MatterType.GAS;
        _timeStep = float.Parse(sr.ReadLine());
        _viscosity = float.Parse(sr.ReadLine());
        _gravity = float.Parse(sr.ReadLine());
        _stepCount = int.Parse(sr.ReadLine());
        _grid = new Texture2D(_gridSize + 2, _gridSize + 2, TextureFormat.ARGB32, false);
        _grid.filterMode = _interpolate ? FilterMode.Bilinear : FilterMode.Point;
        _wallTypes = new WallType[_gridSize + 2, _gridSize + 2];

        for (int x = _gridSize; x > 0; --x)
		{
            _grid.SetPixel(x, 0, Color.black);
            _grid.SetPixel(x, _gridSize + 1, Color.black);
            _grid.SetPixel(0, x, Color.black);
            _grid.SetPixel(_gridSize + 1, x, Color.black);

			string line = sr.ReadLine();
			for (int y = 1; y < _gridSize + 1; ++y)
			{
                WallType currentPixel = (WallType)int.Parse(line[y - 1].ToString());
                if (currentPixel != WallType.NONE)
                {
                    _grid.SetPixel(y, x, Color.black);
                }
                _wallTypes[y, x] = currentPixel;
            }
		}
        _grid.SetPixel(0, 0, Color.black);
        _grid.SetPixel(0, _gridSize + 1, Color.black);
        _grid.SetPixel(_gridSize + 1, 0, Color.black);
        _grid.SetPixel(_gridSize + 1, _gridSize + 1, Color.black);
        _grid.Apply();

		sr.Close();
    }

	#endregion

}
