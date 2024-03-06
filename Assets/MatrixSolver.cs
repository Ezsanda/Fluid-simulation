using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatrixSolver
{

    #region Fields

    private int _gridSize;

    private int _stepCount;

    private FluidBoundary _wall;

    #endregion

    #region Constructor

    public MatrixSolver(FluidBoundary wall_, int gridSize_, int stepCount_)
    {
        _gridSize = gridSize_;
        _stepCount = stepCount_;
        _wall = wall_;
    }

    #endregion

    #region Public methods

    public void GaussSeidel(BoundaryCondition boundary_, float[,] aMatrix_, float[,] bVector_, float alpha_, float beta_)
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
            _wall.SetBoundary(boundary_, aMatrix_);
        }
    }

    public void Jacobi(BoundaryCondition boundary_, float[,] aMatrix_, float[,] bVector_)
    {
        //TODO
    }

    #endregion

}
