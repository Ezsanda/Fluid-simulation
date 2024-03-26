using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidGrid
{

	#region Fields

	private float[,] _previousDensity;

    private float[,] _density;

    private float[,] _previousVelocityX;

    private float[,] _previousVelocityY;

    private float[,] _velocityX;

    private float[,] _velocityY;

    private float[,] _pressure;

    private float[,] _velocityDivergence;

    private int _gridSize;

    #endregion

    #region Constructor

    public FluidGrid()
    {
        _gridSize = Persistence.GridSize;

        _previousDensity = new float[_gridSize + 2, _gridSize + 2];
        _density = new float[_gridSize + 2, _gridSize + 2];

        _previousVelocityX = new float[_gridSize + 2, _gridSize + 2];
        _previousVelocityY = new float[_gridSize + 2, _gridSize + 2];

        _velocityX = new float[_gridSize + 2, _gridSize + 2];
        _velocityY = new float[_gridSize + 2, _gridSize + 2];

        _pressure = new float[_gridSize + 2, _gridSize + 2];
        _velocityDivergence = new float[_gridSize + 2, _gridSize + 2];

    }

    #endregion

    #region Properties

    public int GridSize { get { return _gridSize; } }

    public ref float[,] PreviousDensity { get { return ref _previousDensity; } }

    public ref float[,] Density { get { return ref _density; } }

    public ref float[,] PreviousVelocityX { get { return ref _previousVelocityX; } }

    public ref float[,] PreviousVelocityY { get { return ref _previousVelocityY; } }

    public ref float[,] VelocityX { get { return ref _velocityX; } }

    public ref float[,] VelocityY { get { return ref _velocityY; } }

    public float[,] Pressure { get { return _pressure; } }

    public float[,] VelocityDivergence { get { return _velocityDivergence; } }

    #endregion

}
