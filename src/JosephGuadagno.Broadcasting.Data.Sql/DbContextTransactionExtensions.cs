using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JosephGuadagno.Broadcasting.Data.Sql;

internal static class DbContextTransactionExtensions
{
    public static Task ExecuteInTransactionIfSupportedAsync(
        this BroadcastingContext context,
        Func<Task> operation,
        CancellationToken cancellationToken)
        => context.ExecuteInTransactionIfSupportedAsync(
            _ => operation(),
            cancellationToken);

    public static Task ExecuteInTransactionIfSupportedAsync(
        this BroadcastingContext context,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
        => context.ExecuteInTransactionIfSupportedAsync(
            async ct =>
            {
                await operation(ct);
                return true;
            },
            cancellationToken);

    public static Task<TResult> ExecuteInTransactionIfSupportedAsync<TResult>(
        this BroadcastingContext context,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return operation(cancellationToken);
        }

        return ExecuteInTransactionIfSupportedAsync(
            context.Database.CreateExecutionStrategy(),
            ct => context.Database.BeginTransactionAsync(ct),
            operation,
            cancellationToken);
    }

    internal static Task ExecuteInTransactionIfSupportedAsync(
        IExecutionStrategy executionStrategy,
        Func<CancellationToken, Task<IDbContextTransaction>> beginTransactionAsync,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
        => ExecuteInTransactionIfSupportedAsync(
            executionStrategy,
            beginTransactionAsync,
            async ct =>
            {
                await operation(ct);
                return true;
            },
            cancellationToken);

    internal static Task<TResult> ExecuteInTransactionIfSupportedAsync<TResult>(
        IExecutionStrategy executionStrategy,
        Func<CancellationToken, Task<IDbContextTransaction>> beginTransactionAsync,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        return executionStrategy.ExecuteAsync<object?, TResult>(
            null,
            async (_, _, ct) =>
            {
                await using var transaction = await beginTransactionAsync(ct);
                var result = await operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            },
            null,
            cancellationToken);
    }
}
