using System.Threading;

namespace ConsoleApp
{
    public delegate JobStatus JobDelegate(Job job, CancellationToken cancellationToken);
}