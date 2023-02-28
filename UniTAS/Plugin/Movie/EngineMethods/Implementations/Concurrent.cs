using System.Diagnostics.CodeAnalysis;
using MoonSharp.Interpreter;
using UniTAS.Plugin.Movie.Engine;

namespace UniTAS.Plugin.Movie.EngineMethods.Implementations;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class Concurrent : EngineMethodClass
{
    private readonly IMovieEngine _engine;

    public Concurrent(IMovieEngine engine)
    {
        _engine = engine;
    }

    public override string ClassName => "concurrent";

    public void Register(DynValue coroutine, bool preUpdate, params DynValue[] defaultArgs)
    {
        _engine.RegisterConcurrent(coroutine, preUpdate, defaultArgs);
    }
}