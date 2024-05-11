using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Android;

public class FluidBoundary
{

    #region Fields

    private WallType[,] _wallTypes;

    private int _gridSize;

    #endregion

    #region Constructor

    public FluidBoundary(int gridSize_, WallType[,] wallTypes_)
    {
        _gridSize = gridSize_;
        _wallTypes = wallTypes_;
    }

    #endregion

    #region Properties

    public WallType[,] WallTypes { get { return _wallTypes; } }

    #endregion

    #region Private methods

    private void SetDefaultBoundary(BoundaryCondition boundary_, float[,] vectorField_, int x_)
    {
        switch (boundary_)
        {
            case BoundaryCondition.NO_SLIP_X:
                vectorField_[0, x_] = -vectorField_[1, x_]; //left wall
                vectorField_[_gridSize + 1, x_] = -vectorField_[_gridSize, x_]; //right wall
                vectorField_[x_, 0] = vectorField_[x_, 1]; //lower wall
                vectorField_[x_, _gridSize + 1] = vectorField_[x_, _gridSize]; //upper wall
                break;
            case BoundaryCondition.NO_SLIP_Y:
                vectorField_[0, x_] = vectorField_[1, x_]; //left wall
                vectorField_[_gridSize + 1, x_] = vectorField_[_gridSize, x_]; //right wall
                vectorField_[x_, 0] = -vectorField_[x_, 1]; //lower wall
                vectorField_[x_, _gridSize + 1] = -vectorField_[x_, _gridSize]; //upper wall
                break;
            case BoundaryCondition.NEUMANN:
                vectorField_[0, x_] = vectorField_[1, x_]; //left wall
                vectorField_[_gridSize + 1, x_] = vectorField_[_gridSize, x_]; //right wall
                vectorField_[x_, 0] = vectorField_[x_, 1]; //lower wall
                vectorField_[x_, _gridSize + 1] = vectorField_[x_, _gridSize]; //upper wall
                break;
            default:
                break;
        }

        vectorField_[0, 0] = (vectorField_[1, 0] + vectorField_[0, 1]) / 2.0F;
        vectorField_[0, _gridSize + 1] = (vectorField_[1, _gridSize + 1] + vectorField_[0, _gridSize]) / 2.0F;
        vectorField_[_gridSize + 1, 0] = (vectorField_[_gridSize, 0] + vectorField_[_gridSize + 1, 1]) / 2.0F;
        vectorField_[_gridSize + 1, _gridSize + 1] = (vectorField_[_gridSize, _gridSize + 1] + vectorField_[_gridSize + 1, _gridSize]) / 2.0F;
    }

    private void SetInnerBoundary(BoundaryCondition boundary_, float[,] vectorField_, int x_, int y_)
    {
        switch (_wallTypes[x_, y_])
        {
            case WallType.TOPLEFT:
                vectorField_[x_, y_] = (vectorField_[x_ - 1, y_] + vectorField_[x_, y_ + 1]) / 2.0F;
                break;
            case WallType.TOP:
                switch (boundary_)
                {
                    case BoundaryCondition.NO_SLIP_X:
                        vectorField_[x_, y_] = vectorField_[x_, y_ + 1];
                        break;
                    case BoundaryCondition.NO_SLIP_Y:
                        vectorField_[x_, y_] = -vectorField_[x_, y_ + 1];
                        break;
                    case BoundaryCondition.NEUMANN:
                        vectorField_[x_, y_] = vectorField_[x_, y_ + 1];
                        break;
                    default:
                        break;
                }
                break;
            case WallType.TOPRIGHT:
                vectorField_[x_, y_] = (vectorField_[x_ + 1, y_] + vectorField_[x_, y_ + 1]) / 2.0F;
                break;
            case WallType.RIGHT:
                switch (boundary_)
                {
                    case BoundaryCondition.NO_SLIP_X:
                        vectorField_[x_, y_] = -vectorField_[x_ + 1, y_];
                        break;
                    case BoundaryCondition.NO_SLIP_Y:
                        vectorField_[x_, y_] = vectorField_[x_ + 1, y_];
                        break;
                    case BoundaryCondition.NEUMANN:
                        vectorField_[x_, y_] = vectorField_[x_ + 1, y_];
                        break;
                    default:
                        break;
                }
                break;
            case WallType.BOTTOMRIGHT:
                vectorField_[x_, y_] = (vectorField_[x_ + 1, y_] + vectorField_[x_, y_ - 1]) / 2.0F;
                break;
            case WallType.BOTTOM:
                switch (boundary_)
                {
                    case BoundaryCondition.NO_SLIP_X:
                        vectorField_[x_, y_] = vectorField_[x_, y_ - 1];
                        break;
                    case BoundaryCondition.NO_SLIP_Y:
                        vectorField_[x_, y_] = -vectorField_[x_, y_ - 1];
                        break;
                    case BoundaryCondition.NEUMANN:
                        vectorField_[x_, y_] = vectorField_[x_, y_ - 1];
                        break;
                    default:
                        break;
                }
                break;
            case WallType.BOTTOMLEFT:
                vectorField_[x_, y_] = (vectorField_[x_ - 1, y_] + vectorField_[x_, y_ - 1]) / 2.0F;
                break;
            case WallType.LEFT:
                switch (boundary_)
                {
                    case BoundaryCondition.NO_SLIP_X:
                        vectorField_[x_, y_] = -vectorField_[x_ - 1, y_];
                        break;
                    case BoundaryCondition.NO_SLIP_Y:
                        vectorField_[x_, y_] = vectorField_[x_ - 1, y_];
                        break;
                    case BoundaryCondition.NEUMANN:
                        vectorField_[x_, y_] = vectorField_[x_ - 1, y_];
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
    }

    #endregion

    #region Public methods

    public void SetBoundary(BoundaryCondition boundary_, float[,] vectorField_)
    {
        for (int x = 1; x < _gridSize + 1; ++x)
        {
            SetDefaultBoundary(boundary_, vectorField_, x);
            for (int y = 1; y < _gridSize + 1; ++y)
            {
                SetInnerBoundary(boundary_, vectorField_, x, y);
            }
        }
    }

    #endregion

}
