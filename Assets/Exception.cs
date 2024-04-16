using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NotHitException : Exception
{
    public NotHitException() : base() {}
}

public class InValidCoordinateException : Exception
{
    public InValidCoordinateException() : base() { }
}

public class NotPaintableException : Exception
{
    public NotPaintableException() : base() {}
}