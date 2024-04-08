using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class PDESolver
{
    #region Fields

    private int _gridSize;

    private float _gravity;

    private float _gridSpacing;

    private float _timeStep;

    private int _stepCount;

    private float _viscosity;

    private FluidGrid _grid;

    private FluidBoundary _boundary;

    private MatrixSolver _solver;

    private MatterType _matterType;

    #endregion

    #region Properties

    public FluidBoundary Boundary { get { return _boundary; } }

    public FluidGrid Grid { get { return _grid; } }

    #endregion

    #region Constructor

    public PDESolver(int gridSize_, float timeStep_, MatterType matterType_, float viscosity_, int stepCount_, float gravity_, WallType[,] wallTypes_)
    {
        _gridSize = gridSize_;
        _timeStep = timeStep_;
        _viscosity = viscosity_;
        _stepCount = stepCount_;
        _gravity = gravity_;
        _gridSpacing = 1.0F / _gridSize;
        _matterType = matterType_;

        _grid = new FluidGrid(_gridSize);
        _boundary = new FluidBoundary(_gridSize, wallTypes_);
        _solver = new MatrixSolver(_boundary, _gridSize, _stepCount);
    }

    #endregion

    #region Private helper methods

    private void Swap(ref float[,] previousVectorField_, ref float[,] newVectorField_)
    {
        (previousVectorField_, newVectorField_) = (newVectorField_, previousVectorField_);
    }

    private void CalculateVelocityDivergence()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                if (_boundary.WallTypes[x, y] == WallType.NONE)
                {
                    _grid.VelocityDivergence[x, y] = _gridSpacing * 
                                                     (_grid.VelocityX[x + 1, y] - _grid.VelocityX[x - 1, y] +
                                                     _grid.VelocityY[x, y + 1] - _grid.VelocityY[x, y - 1])
                                                     / -2;
                    _grid.Pressure[x, y] = 0;
                }
            }
        }
        _boundary.SetBoundary(BoundaryCondition.NEUMANN,_grid.VelocityDivergence);
    }

    private void CalculatePressureGradientField()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                if (_boundary.WallTypes[x, y] == WallType.NONE)
                {
                    _grid.VelocityX[x, y] -= (_grid.Pressure[x + 1, y] - _grid.Pressure[x - 1, y]) / (2 * _gridSpacing);
                    _grid.VelocityY[x, y] -= (_grid.Pressure[x, y + 1] - _grid.Pressure[x, y - 1]) / (2 * _gridSpacing);
                }
            }
        }
        _boundary.SetBoundary(BoundaryCondition.NO_SLIP_X,_grid.VelocityX);
        _boundary.SetBoundary(BoundaryCondition.NO_SLIP_Y,_grid.VelocityY);
    }

    #endregion

    #region Private PDE solver methods

    private void AddDensity(float densityValue_, int x_, int y_)
    {
        _grid.PreviousDensity[x_,y_] += _timeStep * densityValue_;
    }

    private void AddVelocity(float velocityValueX, float velocityValueY, int x_, int y_)
    {
        _grid.VelocityX[x_, y_] += _timeStep * _grid.Density[x_, y_] * velocityValueX;
        _grid.VelocityY[x_, y_] += _timeStep * _grid.Density[x_, y_] * velocityValueY;
    }

    private void ApplyGravity()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                if (_boundary.WallTypes[x, y] == WallType.NONE)
                {
                    _grid.PreviousVelocityY[x, y] += (int)_matterType * _timeStep * _gravity * _grid.Density[x, y];
                }
            }
        }
    }

    private void Diffuse(BoundaryCondition boundary_, float[,] previousVectorField_, float[,] vectorField_)
    {
        float alpha = _timeStep * _viscosity * Mathf.Pow(_gridSize, 2);
        float beta = 1 + 4 * alpha;

        _solver.GaussSeidel(boundary_, vectorField_, previousVectorField_, alpha, beta);
    }

    private void Advect(BoundaryCondition boundary_, float[,] velocityFieldX_, float[,] velocityFieldY_,
                                      float[,] previousVectorField_, float[,] vectorField_)
    {
        float totalTimeSteps = _timeStep * _gridSize;

        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                if (_boundary.WallTypes[x, y] == WallType.NONE)
                {
                    //tracking the current pixel value backwards throught the velocity field

                    //x coordinate of the previous value
                    float previousXIndex = x - totalTimeSteps * velocityFieldX_[x, y];
                    //y coordinate of the previous value
                    float previousYIndex = y - totalTimeSteps * velocityFieldY_[x, y];

                    //checking under/overindexing
                    previousXIndex = previousXIndex < 1.0F ? 1.0F : previousXIndex > _gridSize + 1.0F ? _gridSize - 1.0F : previousXIndex;
                    previousYIndex = previousYIndex < 1.0F ? 1.0F : previousYIndex > _gridSize + 1.0F ? _gridSize - 1.0F : previousYIndex;

                    //calculating the 4 neighboring pixels
                    int previousNeighborXIndex = (int)previousXIndex;
                    int nextNeighborXIndex = previousNeighborXIndex + 1;
                    int previousNeighborYIndex = (int)previousYIndex;
                    int nextNeighborYIndex = previousNeighborYIndex + 1;

                    float xDifference = previousXIndex - previousNeighborXIndex;
                    float secondXDifference = 1 - xDifference;
                    float yDifference = previousYIndex - previousNeighborYIndex;
                    float secondYDifference = 1 - yDifference;

                    //biliear interpolation of the 4 neightbors
                    vectorField_[x, y] = secondXDifference *
                                         (secondYDifference * previousVectorField_[previousNeighborXIndex, previousNeighborYIndex] +
                                         yDifference * previousVectorField_[previousNeighborXIndex, nextNeighborYIndex]) +
                                         xDifference *
                                         (secondYDifference * previousVectorField_[nextNeighborXIndex, previousNeighborYIndex] +
                                         yDifference * previousVectorField_[nextNeighborXIndex, nextNeighborYIndex]);
                }
            }
        }
        _boundary.SetBoundary(boundary_,vectorField_);
    }

    private void Project()
    {
        CalculateVelocityDivergence();
        _solver.GaussSeidel(BoundaryCondition.NEUMANN, _grid.Pressure, _grid.VelocityDivergence, 1, 4);
        CalculatePressureGradientField();
    }

    #endregion

    #region Public methods

    public void UpdateDensity(float densityValue_, int x_, int y_)
    {
        AddDensity(densityValue_,x_,y_);

        if(_matterType == MatterType.FLUID)
        {
            Diffuse(BoundaryCondition.NEUMANN, _grid.PreviousDensity, _grid.Density);
            Swap(ref _grid.PreviousDensity, ref _grid.Density);
        }

        Advect(BoundaryCondition.NEUMANN,_grid.VelocityX,_grid.VelocityY,_grid.PreviousDensity,_grid.Density);
        Swap(ref _grid.PreviousDensity, ref _grid.Density);
    }

    public void UpdateDensity()
    {
        if(_matterType == MatterType.FLUID)
        {
            Diffuse(BoundaryCondition.NEUMANN, _grid.PreviousDensity, _grid.Density);
            Swap(ref _grid.PreviousDensity, ref _grid.Density);
        }

        Advect(BoundaryCondition.NEUMANN, _grid.VelocityX, _grid.VelocityY, _grid.PreviousDensity, _grid.Density);
        Swap(ref _grid.PreviousDensity, ref _grid.Density);
    }

    public void UpdateVelocity(float velocityValueX_, float velocityValueY_, int x_, int y_)
    {
        ApplyGravity();
        AddVelocity(velocityValueX_, velocityValueY_, x_, y_);
        Diffuse(BoundaryCondition.NO_SLIP_X,_grid.PreviousVelocityX,_grid.VelocityX);
        Diffuse(BoundaryCondition.NO_SLIP_Y,_grid.PreviousVelocityY,_grid.VelocityY);
        Project();
        Swap(ref _grid.PreviousVelocityX, ref _grid.VelocityX);
        Swap(ref _grid.PreviousVelocityY, ref _grid.VelocityY);
        Advect(BoundaryCondition.NO_SLIP_X,_grid.PreviousVelocityX,_grid.PreviousVelocityY,_grid.PreviousVelocityX,_grid.VelocityX);
        Advect(BoundaryCondition.NO_SLIP_Y,_grid.PreviousVelocityX,_grid.PreviousVelocityY,_grid.PreviousVelocityY,_grid.VelocityY);
        Swap(ref _grid.PreviousVelocityX, ref _grid.VelocityX);
        Swap(ref _grid.PreviousVelocityY, ref _grid.VelocityY);
        Project();
    }

    public void UpdateVelocity()
    {
        ApplyGravity();
        Diffuse(BoundaryCondition.NO_SLIP_X, _grid.PreviousVelocityX, _grid.VelocityX);
        Diffuse(BoundaryCondition.NO_SLIP_Y, _grid.PreviousVelocityY, _grid.VelocityY);
        Project();
        Swap(ref _grid.PreviousVelocityX, ref _grid.VelocityX);
        Swap(ref _grid.PreviousVelocityY, ref _grid.VelocityY);
        Advect(BoundaryCondition.NO_SLIP_X, _grid.PreviousVelocityX, _grid.PreviousVelocityY, _grid.PreviousVelocityX, _grid.VelocityX);
        Advect(BoundaryCondition.NO_SLIP_Y, _grid.PreviousVelocityX, _grid.PreviousVelocityY, _grid.PreviousVelocityY, _grid.VelocityY);
        Swap(ref _grid.PreviousVelocityX, ref _grid.VelocityX);
        Swap(ref _grid.PreviousVelocityY, ref _grid.VelocityY);
        Project();
    }

    #endregion

}
