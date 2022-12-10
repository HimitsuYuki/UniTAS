﻿namespace UniTASPlugin.Movie.ScriptEngine.Exceptions.ParseExceptions;

public class MissingMovieScriptException : MovieParseException
{
    public MissingMovieScriptException() : base("Missing script")
    {
    }
}