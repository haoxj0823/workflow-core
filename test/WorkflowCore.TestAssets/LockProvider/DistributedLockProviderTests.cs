using FluentAssertions;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using WorkflowCore.Services;

namespace WorkflowCore.TestAssets.LockProvider;

public abstract class DistributedLockProviderTests
{
    protected IDistributedLockProvider Subject;

    protected abstract IDistributedLockProvider CreateProvider();

    [Test]
    public async Task AcquiresLock()
    {
        const string lock1 = "lock1";
        const string lock2 = "lock2";
        await Subject.AcquireLockAsync(lock2, new CancellationToken());

        var acquired = await Subject.AcquireLockAsync(lock1, new CancellationToken());

        acquired.Should().Be(true);
    }

    [Test]
    public async Task DoesNotAcquireWhenLocked()
    {
        const string lock1 = "lock1";
        await Subject.AcquireLockAsync(lock1, new CancellationToken());

        var acquired = await Subject.AcquireLockAsync(lock1, new CancellationToken());

        acquired.Should().Be(false);
    }

    [Test]
    public async Task ReleasesLock()
    {
        const string lock1 = "lock1";
        await Subject.AcquireLockAsync(lock1, new CancellationToken());

        await Subject.ReleaseLockAsync(lock1, new CancellationToken());

        var available = await Subject.AcquireLockAsync(lock1, new CancellationToken());
        available.Should().Be(true);
    }
}
