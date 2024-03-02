#define DIFFUSION

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

public class FluidModel
{

    #region Fields

    private float _gravity;

    private int _gridSize;

    private float _gridSpacing;

    private float _timeStep;

    private int _stepCount;

    private float _viscosity;

    private float[,] _previousDensity;

    private float[,] _density;

    private float[,] _previousVelocityX;

    private float[,] _previousVelocityY;

    private float[,] _velocityX;

    private float[,] _velocityY;

    private float[,] _pressure;

    private float[,] _velocityDivergence;

    private FluidBoundary _wall;

    #endregion

    #region Properties

    public float[,] Densities { get { return _density; } }

    #endregion

    #region Constructor

    public FluidModel(int gridSize_, float timeStep_, float viscosity_, int stepCount_, float gravity_)
    {
        _gridSize = gridSize_;
        _timeStep = timeStep_;
        _viscosity = viscosity_;
        _stepCount = stepCount_;
        _gravity = gravity_;
        _gridSpacing = 1.0F / _gridSize;

        _previousDensity = new float[_gridSize + 2, _gridSize + 2];
        _density = new float[_gridSize + 2, _gridSize + 2];

        _previousVelocityX = new float[_gridSize + 2, _gridSize + 2];
        _previousVelocityY = new float[_gridSize + 2, _gridSize + 2];

        _velocityX = new float[_gridSize + 2, _gridSize + 2];
        _velocityY = new float[_gridSize + 2, _gridSize + 2];

        _pressure = new float[_gridSize + 2, _gridSize + 2];
        _velocityDivergence = new float[_gridSize + 2, _gridSize + 2];

        _wall = new FluidBoundary(_gridSize);

        /*for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; y++)
            {
                _velocityY[x, y] = -_timeStep * _gravity;
            }
        }*/
    }

    #endregion

    #region Private helper methods

    private void Swap(ref float[,] previousVectorField_, ref float[,] newVectorField_)
    {
        (previousVectorField_, newVectorField_) = (newVectorField_, previousVectorField_);
    }

    private void GaussSeidel(BoundaryCondition boundary_, float[,] aMatrix_, float[,] bVector_, float alpha_, float beta_)
    {
        for (int k = 0; k < _stepCount; ++k)
        {
            for (int x = 1; x < _gridSize + 1; ++x)
            {
                for (int y = 1; y < _gridSize + 1; ++y)
                {
                    aMatrix_[x, y] = (alpha_ * 
                                     (aMatrix_[x - 1, y] + aMatrix_[x + 1, y] +
                                      aMatrix_[x, y - 1] + aMatrix_[x, y + 1]) +
                                      bVector_[x, y]) /
                                      beta_;
                }
            }
            _wall.SetBoundary(boundary_,aMatrix_);
        }
    }

    private void CalculateVelocityDivergence()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                //TODO which one is correct???
                //_velocityDivergence[x, y] = (_velocityX[x + 1, y] - _velocityX[x - 1, y] + _velocityY[x, y + 1] - _velocityY[x, y - 1]) / (-2 * _gridSpacing);
                _velocityDivergence[x, y] = _gridSpacing * (_velocityX[x + 1, y] - _velocityX[x - 1, y] + _velocityY[x, y + 1] - _velocityY[x, y - 1]) / -2;
                _pressure[x, y] = 0;
            }
        }
        _wall.SetBoundary(BoundaryCondition.NEUMANN,_velocityDivergence);
    }

    private void CalculatePressureGradientField()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                _velocityX[x, y] -= (_pressure[x + 1, y] - _pressure[x - 1, y]) / (2 * _gridSpacing);
                _velocityY[x, y] -= (_pressure[x, y + 1] - _pressure[x, y - 1]) / (2 * _gridSpacing);
            }
        }
        _wall.SetBoundary(BoundaryCondition.NO_SLIP_X,_velocityX);
        _wall.SetBoundary(BoundaryCondition.NO_SLIP_Y,_velocityY);
    }

    #endregion

    #region Private PDE solver methods

    private void AddDensity(float densityValue_, int x_, int y_)
    {
        _previousDensity[x_,y_] += _timeStep * densityValue_;
    }

    private void AddVelocity(float velocityValueX, float velocityValueY, int x_, int y_)
    {
        _velocityX[x_, y_] += _timeStep * _density[x_, y_] * velocityValueX;
        _velocityY[x_, y_] += _timeStep * _density[x_, y_] * velocityValueY;
    }

    //TODO make more significant than viscosity
    private void ApplyGravity()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                _previousVelocityY[x, y] -= _timeStep * _density[x, y] * _gravity;
            }
        }
    }

    private void Diffuse(BoundaryCondition boundary_, float[,] previousVectorField_, float[,] vectorField_)
    {
        float alpha = _timeStep * _viscosity * Mathf.Pow(_gridSize, 2);
        float beta = 1 + 4 * alpha;

        GaussSeidel(boundary_, vectorField_, previousVectorField_, alpha, beta);
    }

    private void Advect(BoundaryCondition boundary_, float[,] velocityFieldX_, float[,] velocityFieldY_,
                                      float[,] previousVectorField_, float[,] vectorField_)
    {
        float totalTimeSteps = _timeStep * _gridSize;

        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                //tracking the current pixel value backwards throught the velocity field

                //x coordinate of the previous value
                float previousXIndex = x - totalTimeSteps * velocityFieldX_[x,y];
                //y coordinate of the previous value
                float previousYIndex = y - totalTimeSteps * velocityFieldY_[x,y];

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
        _wall.SetBoundary(boundary_,vectorField_);
    }

    private void Project()
    {
        CalculateVelocityDivergence();
        GaussSeidel(BoundaryCondition.NEUMANN, _pressure, _velocityDivergence, 1, 4);
        CalculatePressureGradientField();
    }

    #endregion

    #region Public methods

    public void UpdateDensity(float densityValue_, int x_, int y_)
    {
        AddDensity(densityValue_,x_,y_);
        #if DIFFUSION
            Diffuse(BoundaryCondition.NEUMANN,_previousDensity,_density);
            Swap(ref _previousDensity, ref _density);
        #endif
        Advect(BoundaryCondition.NEUMANN,_velocityX,_velocityY,_previousDensity,_density);
        Swap(ref _previousDensity, ref _density);
    }

    public void UpdateDensity()
    {
        #if DIFFUSION
            Diffuse(BoundaryCondition.NEUMANN, _previousDensity, _density);
            Swap(ref _previousDensity, ref _density);
        #endif
        Advect(BoundaryCondition.NEUMANN, _velocityX, _velocityY, _previousDensity, _density);
        Swap(ref _previousDensity, ref _density);
    }

    public void UpdateVelocity(float velocityValueX_, float velocityValueY_, int x_, int y_)
    {
        ApplyGravity();
        AddVelocity(velocityValueX_, velocityValueY_, x_, y_);
        Diffuse(BoundaryCondition.NO_SLIP_X,_previousVelocityX,_velocityX);
        Diffuse(BoundaryCondition.NO_SLIP_Y,_previousVelocityY,_velocityY);
        Project();
        Swap(ref _previousVelocityX, ref _velocityX);
        Swap(ref _previousVelocityY, ref _velocityY);
        Advect(BoundaryCondition.NO_SLIP_X,_previousVelocityX,_previousVelocityY,_previousVelocityX,_velocityX);
        Advect(BoundaryCondition.NO_SLIP_Y,_previousVelocityX,_previousVelocityY,_previousVelocityY,_velocityY);
        Swap(ref _previousVelocityX, ref _velocityX); //are those 2 swaps needed?
        Swap(ref _previousVelocityY, ref _velocityY);
        Project();
    }

    public void UpdateVelocity(bool added)
    {
        ApplyGravity();
        Diffuse(BoundaryCondition.NO_SLIP_X, _previousVelocityX, _velocityX);
        Diffuse(BoundaryCondition.NO_SLIP_Y, _previousVelocityY, _velocityY);
        Project();
        Swap(ref _previousVelocityX, ref _velocityX);
        Swap(ref _previousVelocityY, ref _velocityY);
        Advect(BoundaryCondition.NO_SLIP_X, _previousVelocityX, _previousVelocityY, _previousVelocityX, _velocityX);
        Advect(BoundaryCondition.NO_SLIP_Y, _previousVelocityX, _previousVelocityY, _previousVelocityY, _velocityY);
        Swap(ref _previousVelocityX, ref _velocityX); //are those 2 swaps needed?
        Swap(ref _previousVelocityY, ref _velocityY);
        Project();
        if(added)
        {
            Debug.Log("fasz");
        }
    }

    #endregion

}
