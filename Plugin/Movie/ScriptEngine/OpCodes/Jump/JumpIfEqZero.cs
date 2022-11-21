﻿namespace UniTASPlugin.Movie.ScriptEngine.OpCodes.Jump;

public class JumpIfEqZero : JumpBase
{
    public RegisterType Register { get; }

    public JumpIfEqZero(int offset, RegisterType register) : base(offset)
    {
        Register = register;
    }
}