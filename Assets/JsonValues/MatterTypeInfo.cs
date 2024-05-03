using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class MatterTypeInfo
{

    #region Fields

    private static MatterTypeInfo _instance;

    private Dictionary<string, Dictionary<string, string>> _matterTypeValues = null!;

    #endregion

    #region Properties

    public static MatterTypeInfo Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new MatterTypeInfo();
            }
            return _instance;
        }
    }

    #endregion

    #region Constructor

    private MatterTypeInfo()
    {
        _matterTypeValues = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(@"Assets/JsonValues/MatterTypeValues.json"));
    }


    #endregion

    #region Public methods

    public float TimeStep(MatterType matterType_)
    {
        switch (matterType_)
        {
            case MatterType.WATER:
                return float.Parse(_matterTypeValues["Water"]["timestep"]);
            case MatterType.HONEY:
                return float.Parse(_matterTypeValues["Honey"]["timestep"]);
            case MatterType.HIDROGEN:
                return float.Parse(_matterTypeValues["Hidrogen"]["timestep"]);
            case MatterType.SMOKE:
                return float.Parse(_matterTypeValues["Smoke"]["timestep"]);
            default:
                return 0;
        }
    }

    public float Viscosity(MatterType matterType_)
    {
        switch (matterType_)
        {
            case MatterType.WATER:
                return float.Parse(_matterTypeValues["Water"]["viscosity"]);
            case MatterType.HONEY:
                return float.Parse(_matterTypeValues["Honey"]["viscosity"]);
            case MatterType.HIDROGEN:
                return float.Parse(_matterTypeValues["Hidrogen"]["viscosity"]);
            case MatterType.SMOKE:
                return float.Parse(_matterTypeValues["Smoke"]["viscosity"]);
            default:
                return 0;
        }
    }

    public float Gravity(MatterType matterType_)
    {
        switch (matterType_)
        {
            case MatterType.WATER:
                return float.Parse(_matterTypeValues["Water"]["gravity"]);
            case MatterType.HONEY:
                return float.Parse(_matterTypeValues["Honey"]["gravity"]);
            case MatterType.HIDROGEN:
                return float.Parse(_matterTypeValues["Hidrogen"]["gravity"]);
            case MatterType.SMOKE:
                return float.Parse(_matterTypeValues["Smoke"]["gravity"]);
            default:
                return 0;
        }
    }

    public int StepCount(MatterType matterType_)
    {
        switch (matterType_)
        {
            case MatterType.WATER:
                return int.Parse(_matterTypeValues["Water"]["stepcount"]);
            case MatterType.HONEY:
                return int.Parse(_matterTypeValues["Honey"]["stepcount"]);
            case MatterType.HIDROGEN:
                return int.Parse(_matterTypeValues["Hidrogen"]["stepcount"]);
            case MatterType.SMOKE:
                return int.Parse(_matterTypeValues["Smoke"]["stepcount"]);
            default:
                return 0;
        }
    }

    public Color Color(MatterType matterType_)
    {
        float r, g, b;

        switch (matterType_)
        {
            case MatterType.WATER:
                r = float.Parse(_matterTypeValues["Water"]["color"].Split(',')[0]) / 255;
                g = float.Parse(_matterTypeValues["Water"]["color"].Split(',')[1]) / 255;
                b = float.Parse(_matterTypeValues["Water"]["color"].Split(',')[2]) / 255;
                return new Color(r, g, b);
            case MatterType.HONEY:
                r = float.Parse(_matterTypeValues["Honey"]["color"].Split(',')[0]) / 255;
                g = float.Parse(_matterTypeValues["Honey"]["color"].Split(',')[1]) / 255;
                b = float.Parse(_matterTypeValues["Honey"]["color"].Split(',')[2]) / 255;
                return new Color(r, g, b);
            case MatterType.HIDROGEN:
                r = float.Parse(_matterTypeValues["Hidrogen"]["color"].Split(',')[0]) / 255;
                g = float.Parse(_matterTypeValues["Hidrogen"]["color"].Split(',')[1]) / 255;
                b = float.Parse(_matterTypeValues["Hidrogen"]["color"].Split(',')[2]) / 255;
                return new Color(r, g, b);
            case MatterType.SMOKE:
                r = float.Parse(_matterTypeValues["Smoke"]["color"].Split(',')[0]) / 255;
                g = float.Parse(_matterTypeValues["Smoke"]["color"].Split(',')[1]) / 255;
                b = float.Parse(_matterTypeValues["Smoke"]["color"].Split(',')[2]) / 255;
                return new Color(r, g, b);
            default:
                return new Color(0, 0, 0);
        }
    }

    public MatterState MatterState(MatterType matterType_)
    {
        switch (matterType_)
        {
            case MatterType.WATER:
                return (MatterState)int.Parse(_matterTypeValues["Water"]["matterstate"]);
            case MatterType.HONEY:
                return (MatterState)int.Parse(_matterTypeValues["Honey"]["matterstate"]);
            case MatterType.HIDROGEN:
                return (MatterState)int.Parse(_matterTypeValues["Hidrogen"]["matterstate"]);
            case MatterType.SMOKE:
                return (MatterState)int.Parse(_matterTypeValues["Smoke"]["matterstate"]);
            default:
                return 0;
        }
    }

    #endregion

}