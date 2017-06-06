using System;
using JetBrains.Annotations;

namespace ConsoleApp
{
    [PublicAPI]
    public sealed class JobEventArgs : EventArgs
    {
        public JobEventArgs(Job job)
        {
            Job = job;
        }

        public Job Job { get; }
    }
}