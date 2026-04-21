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
    public async Task ExecuteInTransactionIfSupportedAsync_RetriesOperationAndReturnsResult()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new BroadcastingContext(options);
        var executionStrategy = new RetryingExecutionStrategy(context);
        var firstTransaction = new Mock<IDbContextTransaction>();
        var secondTransaction = new Mock<IDbContextTransaction>();
        var transactions = new Queue<IDbContextTransaction>([firstTransaction.Object, secondTransaction.Object]);
        var operationAttempts = 0;

        var result = await DbContextTransactionExtensions.ExecuteInTransactionIfSupportedAsync(
            executionStrategy,
            _ => Task.FromResult(transactions.Dequeue()),
            _ =>
            {
                operationAttempts++;

                if (operationAttempts == 1)
                {
                    throw new InvalidOperationException("Transient failure");
                }

                return Task.FromResult(42);
            },
            CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(2, operationAttempts);
        Assert.Equal(2, executionStrategy.Attempts);
        firstTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        secondTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
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

    private sealed class RetryingExecutionStrategy(DbContext dbContext) : IExecutionStrategy
    {
        public int Attempts { get; private set; }

        public bool RetriesOnFailure => true;

        public TResult Execute<TState, TResult>(
            TState state,
            Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
            => throw new NotSupportedException();

        public async Task<TResult> ExecuteAsync<TState, TResult>(
            TState state,
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
            CancellationToken cancellationToken = default)
        {
            Attempts++;

            try
            {
                return await operation(dbContext, state, cancellationToken);
            }
            catch (InvalidOperationException) when (Attempts == 1)
            {
                Attempts++;
                return await operation(dbContext, state, cancellationToken);
            }
        }
    }
}
