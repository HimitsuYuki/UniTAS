﻿namespace UniTASPlugin.Movie.ScriptEngine.OpCodes.Logic;

public class NotEqualOpCode : LogicComparisonBase
{
    public NotEqualOpCode(RegisterType dest, RegisterType left, RegisterType right) : base(dest, left, right)
    {
    }
}