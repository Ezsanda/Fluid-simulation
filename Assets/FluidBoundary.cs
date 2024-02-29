using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidBoundary
{

    #region Fields

    private List<(int,int)> _boundary;

    private int _gridSize;

    #endregion

    #region Constructor

    public FluidBoundary(int gridSize_)
    {
        _gridSize = gridSize_;
        _boundary = new List<(int,int)>();
    }

    #endregion

    #region Private methods

    private void SetDefaultBoundary(BoundaryCondition boundary_, float[,] vectorField_)
    {
        for(int i = 1; i < _gridSize + 1; ++i)
        {
            switch (boundary_)
            {
                case BoundaryCondition.NO_SLIP_X:
                    vectorField_[0, i] = -vectorField_[1, i];
                    vectorField_[_gridSize + 1, i] = -vectorField_[_gridSize, i];
                    vectorField_[i, 0] = vectorField_[i, 1];
                    vectorField_[i, _gridSize + 1] = vectorField_[i, _gridSize];
                    break;
                case BoundaryCondition.NO_SLIP_Y:
                    vectorField_[0, i] = vectorField_[1, i];
                    vectorField_[_gridSize + 1, i] = vectorField_[_gridSize, i];
                    vectorField_[i, 0] = -vectorField_[i, 1];
                    vectorField_[i, _gridSize + 1] = -vectorField_[i, _gridSize];
                    break;
                case BoundaryCondition.NEUMANN:
                    vectorField_[0, i] = vectorField_[1, i];
                    vectorField_[_gridSize + 1, i] = vectorField_[_gridSize, i];
                    vectorField_[i, 0] = vectorField_[i, 1];
                    vectorField_[i, _gridSize + 1] = vectorField_[i, _gridSize];
                    break;
                default:
                    break;
            }
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

    #endregion

    #region Public methods

    public void SetBoundary(BoundaryCondition boundary_, float[,] vectorField_)
    {
        SetDefaultBoundary(boundary_, vectorField_);

        //TODO modify in order to handle dinamic wall changing
        foreach((int x,int y) indexes in _boundary)
        {
            switch (boundary_)
            {
                case BoundaryCondition.NO_SLIP_X:
                    vectorField_[indexes.x, indexes.y] = -vectorField_[indexes.x, indexes.y];
                    break;
                case BoundaryCondition.NO_SLIP_Y:
                    vectorField_[indexes.x, indexes.y] = vectorField_[indexes.x, indexes.y];
                    break;
                case BoundaryCondition.NEUMANN:
                    vectorField_[indexes.x, indexes.y] = vectorField_[indexes.x, indexes.y];
                    break;
                default:
                    break;
            }
        }
    }

    //TODO implement
    public void AddWall((int,int) position_)
    {
        _boundary.Add(position_);
    }

    #endregion

}
