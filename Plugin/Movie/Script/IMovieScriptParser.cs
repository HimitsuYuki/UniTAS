﻿using System.Collections.Generic;
using UniTASPlugin.MovieEngine.OpCodes;

namespace UniTASPlugin.Movie.Script;

public interface IMovieScriptParser
{
    /// <summary>
    /// Parses the given input into a list of OpCode list, where each list represents a method.
    /// If not defined in method (basically the main method), the method name is null.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    IEnumerable<KeyValuePair<string, IEnumerable<OpCodeBase>>> Parse(string input);
}