using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class DbContextTransactionExtensionsTests
{
    [Fact]
    public async Task ExecuteInTransactionIfSupportedAsync_UsesExecutionStrategyToOwnTransaction()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new BroadcastingContext(options);
        var executionStrategy = new TrackingExecutionStrategy(context);
        var transaction = new Mock<IDbContextTransaction>();
        var transactionStartedInsideStrategy = false;
        var operationExecutedInsideStrategy = false;

        await DbContextTransactionExtensions.ExecuteInTransactionIfSupportedAsync(
            executionStrategy,
            ct =>
            {
                transactionStartedInsideStrategy = executionStrategy.IsExecuting;
                return Task.FromResult(transaction.Object);
            },
            ct =>
            {
                operationExecutedInsideStrategy = executionStrategy.IsExecuting;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.True(transactionStartedInsideStrategy);
        Assert.True(operationExecutedInsideStrategy);
        Assert.Equal(1, executionStrategy.ExecuteAsyncCallCount);
        transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteInTransactionIfSupportedAsync_InMemoryProvider_RunsWithoutTransaction()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new BroadcastingContext(options);
        var operationCalled = false;

        await context.ExecuteInTransactionIfSupportedAsync(
            () =>
            {
                operationCalled = true;
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.True(operationCalled);
    }

    private sealed class TrackingExecutionStrategy(DbContext dbContext) : IExecutionStrategy
    {
        public int ExecuteAsyncCallCount { get; private set; }

        public bool IsExecuting { get; private set; }

        public bool RetriesOnFailure => true;

        public TResult Execute<TState, TResult>(
            TState state,
            Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
        {
            IsExecuting = true;
            try
            {
                return operation(dbContext, state);
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public async Task<TResult> ExecuteAsync<TState, TResult>(
            TState state,
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
            CancellationToken cancellationToken = default)
        {
            ExecuteAsyncCallCount++;
            IsExecuting = true;
            try
            {
                return await operation(dbContext, state, cancellationToken);
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }
}
