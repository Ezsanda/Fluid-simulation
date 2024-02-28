using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class FluidModel
{

    #region Enums

    private enum Boundary
    {
        NO_SLIP_X,
        NO_SLIP_Y,
        NEUMANN
    }

    #endregion

    #region Fields

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

    #endregion

    #region Properties

    public float[,] Densities { get { return _density; } }

    #endregion

    #region Constructor

    public FluidModel(int gridSize_, float timeStep_, float viscosity_, int stepCount_)
    {
        _gridSize = gridSize_;
        _timeStep = timeStep_;
        _viscosity = viscosity_;
        _stepCount = stepCount_;
        _gridSpacing = 1.0F / _gridSize;

        _previousDensity = new float[_gridSize + 2, _gridSize + 2];
        _density = new float[_gridSize + 2, _gridSize + 2];

        _previousVelocityX = new float[_gridSize + 2, _gridSize + 2];
        _previousVelocityY = new float[_gridSize + 2, _gridSize + 2];
        _velocityX = new float[_gridSize + 2, _gridSize + 2];
        _velocityY = new float[_gridSize + 2, _gridSize + 2];

        _pressure = new float[_gridSize + 2, _gridSize + 2];

        _velocityDivergence = new float[_gridSize + 2, _gridSize + 2];

        //FlowTest();
    }

    #endregion

    #region Private helper methods

    private void Swap(ref float[,] previousVectorField_, ref float[,] newVectorField_)
    {
        (previousVectorField_, newVectorField_) = (newVectorField_, previousVectorField_);
    }

    private void GaussSeidel(Boundary boundary_, float[,] aMatrix_, float[,] bVector_, float alpha_, float beta_)
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
            //TODO check
            SetBoundary(boundary_,aMatrix_);
        }
    }

    private void CalculateVelocityDivergence()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                //TODO double check divergence calculation
                _velocityDivergence[x, y] = (_velocityX[x + 1, y] - _velocityX[x - 1, y] + _velocityY[x, y + 1] - _velocityY[x, y - 1]) /
                                            (-2 * _gridSpacing);
                _pressure[x, y] = 0;
            }
        }
        SetBoundary(Boundary.NEUMANN,_velocityDivergence);
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
        SetBoundary(Boundary.NO_SLIP_X,_velocityX);
        SetBoundary(Boundary.NO_SLIP_Y,_velocityY);
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

    //TODO stabilize
    private void ApplyGravity()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                _velocityY[x, y] += _timeStep * _density[x,y] * 9.81F;
            }
        }
    }

    //TODO modify in order to handle dynamic wall changing
    private void SetBoundary(Boundary boundary_, float[,] vectorField_)
    {
        for (int i = 1; i < _gridSize + 1; ++i)
        {
            switch (boundary_)
            {
                case Boundary.NO_SLIP_X:
                    vectorField_[0, i] = -vectorField_[1, i];
                    vectorField_[_gridSize + 1, i] = -vectorField_[_gridSize, i];
                    vectorField_[i, 0] = vectorField_[i, 1];
                    vectorField_[i, _gridSize + 1] = vectorField_[i, _gridSize];
                    break;
                case Boundary.NO_SLIP_Y:
                    vectorField_[0, i] = vectorField_[1, i];
                    vectorField_[_gridSize + 1, i] = vectorField_[_gridSize, i];
                    vectorField_[i, 0] = -vectorField_[i, 1];
                    vectorField_[i, _gridSize + 1] = -vectorField_[i, _gridSize];
                    break;
                case Boundary.NEUMANN:
                    vectorField_[0, i] = vectorField_[1, i];
                    vectorField_[_gridSize + 1, i] = vectorField_[_gridSize, i];
                    vectorField_[i, 0] = vectorField_[i, 1];
                    vectorField_[i, _gridSize + 1] = vectorField_[i, _gridSize];
                    break;
                default:
                    break;
            }
            //bottom left corner
            vectorField_[0, 0] = (vectorField_[1, 0] + vectorField_[0, 1]) / 2.0F;
            //top left corner
            vectorField_[0, _gridSize + 1] = (vectorField_[1, _gridSize + 1] + vectorField_[0, _gridSize]) / 2.0F;
            //bottom right corner
            vectorField_[_gridSize + 1, 0] = (vectorField_[_gridSize, 0] + vectorField_[_gridSize + 1, 1]) / 2.0F;
            //top right corner
            vectorField_[_gridSize + 1, _gridSize + 1] = (vectorField_[_gridSize, _gridSize + 1] + vectorField_[_gridSize + 1, _gridSize]) / 2.0F;
        }
    }

    private void Diffuse(Boundary boundary_, float[,] previousVectorField_, float[,] vectorField_)
    {
        float alpha = _timeStep * _viscosity * Mathf.Pow(_gridSize, 2);
        float beta = 1 + 4 * alpha;

        GaussSeidel(boundary_, vectorField_, previousVectorField_, alpha, beta);
    }

    private void Advect(Boundary boundary_, float[,] velocityFieldX_, float[,] velocityFieldY_,
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
        SetBoundary(boundary_,vectorField_);
    }

    private void Project()
    {
        CalculateVelocityDivergence();
        GaussSeidel(Boundary.NEUMANN, _pressure, _velocityDivergence, 1, 4);
        CalculatePressureGradientField();
    }

    #endregion

    #region Public methods

    public void UpdateDensity(float densityValue_, int x_, int y_)
    {
        AddDensity(densityValue_,x_,y_);
        Diffuse(Boundary.NEUMANN,_previousDensity,_density);
        Swap(ref _previousDensity, ref _density);
        Advect(Boundary.NEUMANN,_velocityX,_velocityY,_previousDensity,_density);
    }

    public void UpdateDensity()
    {
        Diffuse(Boundary.NEUMANN, _previousDensity, _density);
        Swap(ref _previousDensity, ref _density);
        Advect(Boundary.NEUMANN, _velocityX, _velocityY, _previousDensity, _density);
    }

    public void UpdateVelocity(float velocityValueX_, float velocityValueY_, int x_, int y_)
    {
        ApplyGravity();
        AddVelocity(velocityValueX_, velocityValueY_, x_, y_);
        Diffuse(Boundary.NO_SLIP_X,_previousVelocityX,_velocityX);
        Swap(ref _previousVelocityY, ref _velocityY);
        Diffuse(Boundary.NO_SLIP_Y,_previousVelocityY,_velocityY);
        Project();
        Swap(ref _previousVelocityX, ref _velocityX);
        Swap(ref _previousVelocityY, ref _velocityY);
        Advect(Boundary.NO_SLIP_X,_previousVelocityX,_previousVelocityY,_previousVelocityX,_velocityX);
        Advect(Boundary.NO_SLIP_Y,_previousVelocityX,_previousVelocityY,_previousVelocityY,_velocityY);
        Project();
    }

    public void UpdateVelocity()
    {
        ApplyGravity();
        Diffuse(Boundary.NO_SLIP_X, _previousVelocityX, _velocityX);
        Swap(ref _previousVelocityY, ref _velocityY);
        Diffuse(Boundary.NO_SLIP_Y, _previousVelocityY, _velocityY);
        Project();
        Swap(ref _previousVelocityX, ref _velocityX);
        Swap(ref _previousVelocityY, ref _velocityY);
        Advect(Boundary.NO_SLIP_X, _previousVelocityX, _previousVelocityY, _previousVelocityX, _velocityX);
        Advect(Boundary.NO_SLIP_Y, _previousVelocityX, _previousVelocityY, _previousVelocityY, _velocityY);
        Project();
    }

    //TODO think
    /*public (float min, float max) CalculateMinMaxDensities()
    {
        float minDensity = 0;
        float maxDensity = 0;

        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                if (_density[x, y] < minDensity)
                {
                    minDensity = _density[x, y];
                }
                else if (_density[x, y] > maxDensity)
                {
                    maxDensity = _density[x, y];
                }
            }
        }

        return (minDensity, maxDensity);
    }*/

    #endregion

    #region Test

    private void FlowTest()
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                float xx = _gridSize / 2F + x;
                float yy = _gridSize / 2F + y;

                _velocityX[x, y] = (float)Math.Pow(xx, 2) - MathF.Pow(yy, 2) - 4;
                _velocityY[x, y] = 2 * xx * yy;
            }
        }
    }

    #endregion

}
