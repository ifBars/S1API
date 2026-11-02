using System;

namespace S1API.Internal.Utils
{
    /// <summary>
    /// INTERNAL: Runs an action on request, or once at the end of a batch when requested inside one.
    /// </summary>
    internal sealed class BatchedPass
    {
        private readonly Action _run;
        private int _depth;
        private bool _pending;

        internal BatchedPass(Action run)
        {
            _run = run;
        }

        internal void Begin() =>
            _depth++;

        internal void End()
        {
            if (_depth == 0 || --_depth > 0 || !_pending)
                return;

            _pending = false;
            _run();
        }

        internal void Request()
        {
            if (_depth > 0)
            {
                _pending = true;
                return;
            }

            _run();
        }
    }
}
