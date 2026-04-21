using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JosephGuadagno.Broadcasting.Data.Sql;

internal static class DbContextTransactionExtensions
{
    public static Task ExecuteInTransactionIfSupportedAsync(
        this BroadcastingContext context,
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            return operation();
        }

        return ExecuteInTransactionIfSupportedAsync(
            context.Database.CreateExecutionStrategy(),
            ct => context.Database.BeginTransactionAsync(ct),
            ct => operation(),
            cancellationToken);
    }

    internal static Task ExecuteInTransactionIfSupportedAsync(
        IExecutionStrategy executionStrategy,
        Func<CancellationToken, Task<IDbContextTransaction>> beginTransactionAsync,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        return executionStrategy.ExecuteAsync<object?, bool>(
            null,
            async (_, _, ct) =>
            {
                await using var transaction = await beginTransactionAsync(ct);
                await operation(ct);
                await transaction.CommitAsync(ct);
                return true;
            },
            null,
            cancellationToken);
    }
}
