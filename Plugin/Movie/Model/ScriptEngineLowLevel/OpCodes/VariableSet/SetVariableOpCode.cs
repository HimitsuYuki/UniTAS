﻿namespace UniTASPlugin.Movie.Model.ScriptEngineLowLevel.OpCodes.VariableSet;

public class SetVariableOpCode : OpCodeBase
{
    public string Name { get; }
    public RegisterType Register { get; }
}