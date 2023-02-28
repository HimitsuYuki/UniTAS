using MoonSharp.Interpreter;

namespace UniTAS.Plugin.Movie.Engine;

public partial class MovieEngine
{
    /// <summary>
    /// A wrapper for a coroutine that will run indefinitely
    /// </summary>
    private class CoroutineHolder
    {
        private readonly IMovieEngine _engine;
        private readonly DynValue _method;
        private DynValue _coroutine;

        public CoroutineHolder(IMovieEngine engine, DynValue method)
        {
            _engine = engine;
            _method = method;
            InitCoroutine();
        }

        public void Resume()
        {
            if (_coroutine.Coroutine.State == CoroutineState.Dead)
            {
                InitCoroutine();
            }

            _coroutine.Coroutine.Resume();
        }

        private void InitCoroutine()
        {
            _coroutine = _engine.Script.CreateCoroutine(_method);
        }
    }
}