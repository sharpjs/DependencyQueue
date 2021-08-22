using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyQueue
{
    public interface IStep
    {
        /// <summary>
        ///   Gets whether the step should run once on each worker rather than
        ///   once on one worker.
        /// </summary>
        public bool RunOnAllWorkers { get; }

        /// <summary>
        ///   Gets whether the step is enabled.
        /// </summary>
        public bool IsEnabled { get; }
    }
}
