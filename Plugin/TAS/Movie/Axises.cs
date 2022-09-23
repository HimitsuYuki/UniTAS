﻿using System.Collections.Generic;

namespace UniTASPlugin.TAS.Movie;

public class Axises
{
    public Dictionary<string, float> AxisMove;

    public Axises() : this(new()) { }

    public Axises(Dictionary<string, float> axisMove)
    {
        AxisMove = axisMove;
    }
}