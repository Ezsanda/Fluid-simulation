using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class FluidBoundary
{

    #region Fields

    private List<(int,int)> _boundary;

    private bool[,] _walls;

    private int _gridSize;

    #endregion

    #region Constructor

    public FluidBoundary(int gridSize_, List<(int, int)> boundary_)
    {
        _gridSize = gridSize_;
        _boundary = boundary_;
        _walls = new bool[_gridSize + 2, _gridSize + 2];

        //TODO szebben
        for (int i = 1; i < _gridSize + 1; ++i)
        {
            for (int j = 1; j < _gridSize + 1; ++j)
            {
                _walls[i,j] = _boundary.Contains((i, j));
            }
            _walls[i, 0] = true;
            _walls[i, _gridSize + 1] = true;
            _walls[0, i] = true;
            _walls[_gridSize + 1, i] = true;
        }
        _walls[0, 0] = true;
        _walls[0, _gridSize + 1] = true;
        _walls[_gridSize + 1, 0] = true;
        _walls[_gridSize + 1, _gridSize + 1] = true;
    }

    #endregion

    #region Properties

    public bool[,] Walls { get { return _walls; } }

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

        foreach((int x,int y) indexes in _boundary)
        {
            //TODO
        }
    }

    #endregion

}
