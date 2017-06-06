using System;
using JetBrains.Annotations;

namespace ConsoleApp
{
    [PublicAPI]
    public sealed class Job
    {
        public Guid ID { get; }

        public JobStatus? Status { get; }

        public DateTimeOffset? EnqueuedAt { get; }

        [NotNull]
        internal JobDelegate Delegate { get; }

        public bool IsRetryDesired { get; }

        private Job(Guid jobID, JobDelegate jobDelegate, DateTimeOffset? enqueuedAt = null)
        {
            ID = jobID;
            Delegate = jobDelegate;
        }

        private Job(
            [NotNull] Job source, 
            DateTimeOffset? enqueuedAt = null)
        {
            ID = source.ID;
            Delegate = source.Delegate;
            EnqueuedAt = enqueuedAt ?? source.EnqueuedAt;
        }


        public static Job Create(JobDelegate jobDelegate) 
            => Create(Guid.NewGuid(), jobDelegate);

        public static Job Create(Guid jobID, JobDelegate jobDelegate) 
            => new Job(jobID, jobDelegate);

        [Pure]
        internal Job WithEnqueuedAt(DateTimeOffset timestamp)
        {
            return new Job(this, enqueuedAt: timestamp);
        }

        [Pure]
        internal Job WithFirstProcessedAt(DateTimeOffset timestamp) 
            => throw new NotImplementedException();

        [Pure]
        internal Job WithLastProcessedAt(DateTimeOffset timestamp)
            => throw new NotImplementedException();

        [Pure]
        internal Job AttemptedAt(DateTimeOffset timestamp)
            => throw new NotImplementedException();

        [Pure]
        internal Job SucceededAt(DateTimeOffset timestamp)
            => throw new NotImplementedException();

        [Pure]
        internal Job FaultedAt(DateTimeOffset timestamp)
            => throw new NotImplementedException();

        [Pure]
        internal Job CancelledAt(DateTimeOffset timestamp)
            => throw new NotImplementedException();

        [Pure]
        internal Job WithProcessedAt(DateTimeOffset timestamp)
            => throw new NotImplementedException();

        public Job WithException(Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}