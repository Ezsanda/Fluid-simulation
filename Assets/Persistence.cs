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

	private Texture2D _grid;

	private List<(int, int)> _boundary = new List<(int, int)>();

	#endregion

	#region Constructor

	private Persistence()
    {
		_boundary = new List<(int, int)>();
    }

	#endregion

	#region Properties

	public int GridSize { get { return _gridSize; } }
    public bool Interpolate { get { return _interpolate; } }
    public MatterType MatterType { get { return _matterType; } }
    public float TimeStep { get { return _timeStep; } }
    public float Viscosity { get { return _viscosity; } }
    public float Gravity { get { return _gravity; } }
    public Texture2D Grid { get { return _grid; } }
    public List<(int, int)> Boundary { get { return _boundary; } }

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

	public void SaveSettings(int gridSize_, Texture2D grid_, bool interpolate_, MatterType matterType_, float timeStep_, float viscosity_, float gravity_)
	{
		StreamWriter sr = new StreamWriter("settings.txt", false);
        sr.WriteLine(gridSize_);
        sr.WriteLine(interpolate_);
        sr.WriteLine(matterType_);
        sr.WriteLine(timeStep_);
        sr.WriteLine(viscosity_);
        sr.WriteLine(gravity_);

		_boundary.Clear();
		for (int i = 1; i < gridSize_ + 1; ++i)
		{
			for (int j = 1; j < gridSize_ + 1; ++j)
			{
				if(grid_.GetPixel(i,j) == Color.black)
				{
					sr.Write("1");
				}
                else
				{
					sr.Write("0");
				}
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

        _grid = new Texture2D(_gridSize + 2, _gridSize + 2, TextureFormat.ARGB32, false);

		_boundary.Clear();
        for (int i = 1; i < _gridSize + 1; ++i)
		{
            _grid.SetPixel(i, 0, Color.black);
            _grid.SetPixel(i, _gridSize + 1, Color.black);
            _grid.SetPixel(0, i, Color.black);
            _grid.SetPixel(_gridSize + 1, i, Color.black);

			string line = sr.ReadLine();
			for (int j = 1; j < _gridSize + 1; ++j)
			{
                if (line[j - 1] == '1')
                {
                    _boundary.Add((i, j));
                    _grid.SetPixel(i, j, Color.black);
                }
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
