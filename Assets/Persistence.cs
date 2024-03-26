using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class Persistence
{

	#region Fields

	private static int _gridSize;

	private static bool _interpolate;

	private static MatterType _type;

	private static Texture2D _grid;

	private static List<(int, int)> _boundary = new List<(int, int)>();

	#endregion

	#region Properties

	public static int GridSize { get { return _gridSize; } set { _gridSize = value; } }

    public static bool Interpolate { get { return _interpolate; } set { _interpolate = value; } }

	public static MatterType MatterType { get { return _type; } set { _type = value; } }

    public static Texture2D Grid { get { return _grid; } set { _grid = value; } }

	public static List<(int, int)> Boundary { get { return _boundary; } }

	#endregion

	#region Public methods

	public static void SaveCoordinates()
	{
		_boundary.Clear();
		for (int i = 1; i < _gridSize + 1; ++i)
		{
			for (int j = 1; j < _gridSize + 1; ++j)
			{
				if(_grid.GetPixel(i,j) == Color.black)
				{
					_boundary.Add((i, j));
				}
			}
		}
    }

	#endregion

}
