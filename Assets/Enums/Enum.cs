using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BoundaryCondition
{
    NO_SLIP_X,
    NO_SLIP_Y,
    NEUMANN
}

public enum MatterType
{
    CUSTOM,
    WATER,
    HONEY,
    HIDROGEN
}

public enum PaintHelper
{
    BOUNDARY,
    NONE,
    TOPLEFT,
    TOPRIGHT,
    BOTTOMLEFT,
    BOTTOMRIGHT
}

public enum Tool
{
    POINT,
    SQUARE,
    RECTANGLE
}

public enum WallType
{
    NONE,
    INNER,
    TOPLEFT,
    TOP,
    TOPRIGHT,
    RIGHT,
    BOTTOMRIGHT,
    BOTTOM,
    BOTTOMLEFT,
    LEFT
}

public enum MatterState
{
    FLUID,
    GAS
}