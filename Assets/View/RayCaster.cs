using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering.VirtualTexturing;
using TMPro;

public class RayCaster
{

    #region Fields

    private static RayCaster _instance;

    #endregion

    #region Properties

    public static RayCaster Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new RayCaster();
            }
            return _instance;
        }
    }

    #endregion

    #region Private methods

    private bool ValidCoordinate((int x, int y) pixelCoordinate_, int gridSize_)
    {
        return pixelCoordinate_.x != 0 && pixelCoordinate_.x != gridSize_ + 1 &&
               pixelCoordinate_.y != 0 && pixelCoordinate_.y != gridSize_ + 1;
    }

    private bool ValidCoordinate((int x, int y) pixelCoordinate_, int gridSize_, WallType[,] wallTypes_)
    {
        return pixelCoordinate_.x != 0 && pixelCoordinate_.x != gridSize_ + 1 &&
               pixelCoordinate_.y != 0 && pixelCoordinate_.y != gridSize_ + 1 &&
               wallTypes_[pixelCoordinate_.x, pixelCoordinate_.y] == WallType.NONE;
    }

    //TODO szebben
    private bool Paintable((int x, int y) pixelCoordinate_, int gridSize_, PaintHelper[,] paintHelper_, bool leftDown_)
    {
        if (pixelCoordinate_.x == gridSize_ - 2 || pixelCoordinate_.y == 1)
        {
            return false;
        }

        bool paintable = true;

        PaintHelper innerTopLeft = paintHelper_[pixelCoordinate_.x, pixelCoordinate_.y];
        PaintHelper innerTopRight = paintHelper_[pixelCoordinate_.x + 1, pixelCoordinate_.y];
        PaintHelper innerBottomRight = paintHelper_[pixelCoordinate_.x + 1, pixelCoordinate_.y - 1];
        PaintHelper innerBottomLeft = paintHelper_[pixelCoordinate_.x, pixelCoordinate_.y - 1];

        //clockwise
        PaintHelper outerTopLeft = paintHelper_[pixelCoordinate_.x - 1, pixelCoordinate_.y + 1];
        PaintHelper outerTop1 = paintHelper_[pixelCoordinate_.x, pixelCoordinate_.y + 1];
        PaintHelper outerTop2 = paintHelper_[pixelCoordinate_.x + 1, pixelCoordinate_.y + 1];
        PaintHelper outerTopRight = paintHelper_[pixelCoordinate_.x + 2, pixelCoordinate_.y + 1];
        PaintHelper outerRight1 = paintHelper_[pixelCoordinate_.x + 2, pixelCoordinate_.y];
        PaintHelper outerRight2 = paintHelper_[pixelCoordinate_.x + 2, pixelCoordinate_.y - 1];
        PaintHelper outerBottomRight = paintHelper_[pixelCoordinate_.x + 2, pixelCoordinate_.y - 2];
        PaintHelper outerBottom1 = paintHelper_[pixelCoordinate_.x + 1, pixelCoordinate_.y - 2];
        PaintHelper outerBottom2 = paintHelper_[pixelCoordinate_.x, pixelCoordinate_.y - 2];
        PaintHelper outerBottomLeft = paintHelper_[pixelCoordinate_.x - 1, pixelCoordinate_.y - 2];
        PaintHelper outerLeft1 = paintHelper_[pixelCoordinate_.x - 1, pixelCoordinate_.y - 1];
        PaintHelper outerLeft2 = paintHelper_[pixelCoordinate_.x - 1, pixelCoordinate_.y];

        bool skipTopAndRight = outerTop2 != PaintHelper.NONE || outerRight1 != PaintHelper.NONE;
        bool skipRightAndBottom = outerRight2 != PaintHelper.NONE || outerBottom1 != PaintHelper.NONE;
        bool skipBottomAndLeft = outerBottom2 != PaintHelper.NONE || outerLeft1 != PaintHelper.NONE;
        bool skipLeftAndTop = outerLeft2 != PaintHelper.NONE || outerTop1 != PaintHelper.NONE;

        if (leftDown_ && !skipTopAndRight)
        {
            paintable &= outerTopRight == PaintHelper.NONE;
        }
        if (leftDown_ && !skipRightAndBottom)
        {
            paintable &= outerBottomRight == PaintHelper.NONE;
        }
        if (leftDown_ && !skipBottomAndLeft)
        {
            paintable &= outerBottomLeft == PaintHelper.NONE;
        }
        if (leftDown_ && !skipLeftAndTop)
        {
            paintable &= outerTopLeft == PaintHelper.NONE;
        }

        paintable &= (leftDown_ &&
                      innerTopLeft == PaintHelper.NONE && innerTopRight == PaintHelper.NONE &&
                      innerBottomLeft == PaintHelper.NONE && innerBottomRight == PaintHelper.NONE) ||
                      (!leftDown_ &&
                      innerTopLeft == PaintHelper.TOPLEFT);

        return paintable;
    }

    #endregion

    #region Public methods

    public (int, int) CalculatePixelCoordinates(GameObject quad_, Texture2D texture_)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 3))
        {
            Vector2 textCoord = hit.textureCoord;
            textCoord.x *= texture_.width;
            textCoord.y *= texture_.height;

            Vector2 tiling = quad_.GetComponent<Renderer>().material.mainTextureScale;

            int pixelHitX = Mathf.FloorToInt(textCoord.x * tiling.x);
            int pixelHitY = Mathf.FloorToInt(textCoord.y * tiling.y);

            return (pixelHitX, pixelHitY);
        }

        throw new NotHitException();
    }

    public (int, int) CalculatePixelCoordinates(GameObject quad_, Texture2D texture_, WallType[,] wallTypes_)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector2 textCoord = hit.textureCoord;
            textCoord.x *= texture_.width;
            textCoord.y *= texture_.height;

            Vector2 tiling = quad_.GetComponent<Renderer>().material.mainTextureScale;

            int pixelHitX = Mathf.FloorToInt(textCoord.x * tiling.x);
            int pixelHitY = Mathf.FloorToInt(textCoord.y * tiling.y);

            if (!ValidCoordinate((pixelHitX, pixelHitY), texture_.width - 2, wallTypes_))
            {
                throw new InValidCoordinateException();
            }

            return (pixelHitX, pixelHitY);
        }

        throw new NotHitException();
    }

    public (int, int) CalculatePixelCoordinates(GameObject quad_, Texture2D texture_, PaintHelper[,] paintHelper_, bool leftDown_)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector2 textCoord = hit.textureCoord;
            textCoord.x *= texture_.width;
            textCoord.y *= texture_.height;

            Vector2 tiling = quad_.GetComponent<Renderer>().material.mainTextureScale;

            int pixelHitX = Mathf.FloorToInt(textCoord.x * tiling.x);
            int pixelHitY = Mathf.FloorToInt(textCoord.y * tiling.y);

            if (!ValidCoordinate((pixelHitX, pixelHitY), texture_.width - 2))
            {
                throw new InValidCoordinateException();
            }
            if (!Paintable((pixelHitX, pixelHitY), texture_.width, paintHelper_, leftDown_))
            {
                throw new NotPaintableException();
            }

            return (pixelHitX, pixelHitY);
        }

        throw new NotHitException();
    }

    #endregion

}
