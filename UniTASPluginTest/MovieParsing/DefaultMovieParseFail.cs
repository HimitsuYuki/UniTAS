﻿using Antlr4.Runtime.Misc;
using FluentAssertions;
using UniTASPlugin.Movie.ScriptEngine.EngineMethods;
using UniTASPlugin.Movie.ScriptEngine.MovieModels.Script;
using UniTASPlugin.Movie.ScriptEngine.Parsers.MovieScriptParser;

namespace UniTASPluginTest.MovieParsing;

public class DefaultMovieParseFail
{
    private static ScriptModel Setup(string input)
    {
        var parser = new DefaultMovieScriptParser(new[] { new PrintExternalMethod() });
        var methods = parser.Parse(input).ToList();
        var mainMethod = methods.First(x => x.Name == null);
        var definedMethods = methods.Where(x => x.Name != null);
        return new ScriptModel(mainMethod, definedMethods);
    }

    [Fact]
    public void ParseError()
    {
        var parser = () => Setup(@"$value = 10
fn method(arg) { }
method(no)");

        parser.Should().ThrowExactly<ParseCanceledException>();
    }
}