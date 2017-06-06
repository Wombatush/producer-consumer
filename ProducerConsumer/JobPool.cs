using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace ConsoleApp
{
    [PublicAPI]
    public sealed class JobPool
    {
        private readonly object syncRoot = new object();

        private readonly CancellationTokenSource source = new CancellationTokenSource();

        [NotNull, ItemNotNull]
        private List<Job> jobs = new List<Job>();
        private Thread thread;

        public delegate void JobDelegate(CancellationToken cancellationToken);

        private readonly List<Job> unprocessed = new List<Job>();

        public JobPool()
        {
            thread = new Thread(Start);
        }

        public EventHandler<JobEventArgs> OnOutcome;

        public void Enqueue([NotNull] Job job)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            lock (syncRoot)
            {
                VerifyNotCancelled();

                job = job.WithEnqueuedAt(DateTimeOffset.Now);

                jobs.Add(job);

                Monitor.Pulse(syncRoot);
            }
        }

        public void Shutdown()
        {
            lock (syncRoot)
            {
                VerifyNotCancelled();

                source.Cancel();

                Monitor.Pulse(syncRoot);
            }

            thread.Join();
        }

        private void Start()
        {
            var token = source.Token;

            while (!token.IsCancellationRequested)
            {
                List<Job> thingsToDo;
                List<Job> replacement;

                var outcomes = new List<Job>();

                replacement = new List<Job>();

                lock (syncRoot)
                {
                    if (jobs.Count <= 0)
                    {
                        Monitor.Wait(syncRoot);
                    }

                    thingsToDo = jobs;
                    jobs = replacement;
                }

                using (var enumerator = thingsToDo.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        var retryJob = default(Job);

                        while (true)
                        {
                            var job = retryJob ?? enumerator.Current;

                            if (ReferenceEquals(job, null))
                            {
                                // GO FOR NEXT
                                break;
                            }

                            if (token.IsCancellationRequested)
                            {
                                if (job.Status.HasValue)
                                {
                                    outcomes.Add(job);
                                }
                                else
                                {
                                    unprocessed.Add(job);
                                }

                                // GO FOR NEXT
                                break;
                            }

                            job = job.AttemptedAt(DateTimeOffset.Now);

                            JobStatus outcome;

                            try
                            {
                                outcome = job.Delegate(job, token);
                            }
                            catch (Exception exception)
                            {
                                outcome = JobStatus.Faulted;
                                job = job.WithException(exception);
                            }

                            switch (outcome)
                            {
                                case JobStatus.Succeeded:
                                    job = job.SucceededAt(DateTimeOffset.Now);
                                    break;

                                case JobStatus.Cancelled:
                                    job = job.CancelledAt(DateTimeOffset.Now);
                                    break;

                                case JobStatus.Faulted:
                                    job = job.FaultedAt(DateTimeOffset.Now);
                                    break;

                                default:
                                    throw new NotSupportedException();
                            }

                            if (outcome == JobStatus.Faulted && job.IsRetryDesired)
                            {
                                retryJob = job;

                                // RETRY
                                continue;
                            }
                                
                            outcomes.Add(job);

                            // GO FOR NEXT
                            break;
                        }
                    }
                }

                foreach (var outcome in outcomes)
                {
                    OnOutcome?.Invoke(this, new JobEventArgs(outcome));
                }
            }

            lock (syncRoot)
            {
                unprocessed.AddRange(jobs);
            }
        }

        private void VerifyNotCancelled()
        {
            if (source.IsCancellationRequested)
            {
                throw new InvalidOperationException();
            }
        }
    }
}