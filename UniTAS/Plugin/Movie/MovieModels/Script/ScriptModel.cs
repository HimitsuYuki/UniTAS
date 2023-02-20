﻿using System.Collections.Generic;
using System.Linq;

namespace UniTAS.Plugin.Movie.MovieModels.Script;

public class ScriptModel
{
    public ScriptMethodModel MainMethod { get; }
    public ScriptMethodModel[] Methods { get; }

    public ScriptModel(ScriptMethodModel mainMethod, IEnumerable<ScriptMethodModel> methods)
    {
        MainMethod = mainMethod;
        Methods = methods.ToArray();
    }
}