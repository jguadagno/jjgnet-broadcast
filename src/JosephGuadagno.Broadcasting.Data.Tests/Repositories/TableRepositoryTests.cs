using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.AzureHelpers.Cosmos;
using Microsoft.Azure.Cosmos.Table;
using Moq;
using Moq.Protected;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Tests.Repositories;

public class TableRepositoryTests
{
    public class TestTableEntity : TableEntity { }

    public class TestTableRepository : TableRepository<TestTableEntity>
    {
        public TestTableRepository(string connectionString, string tableName) : base(connectionString, tableName) { }

        public Mock<Func<string, string, Task<TestTableEntity>>> GetTableEntityMock { get; } = new();
        protected override Task<TEntity> GetTableEntityAsync<TEntity>(string partitionKey, string rowKey)
        {
            return (Task<TEntity>)(object)GetTableEntityMock.Object(partitionKey, rowKey);
        }

        public Mock<Func<TestTableEntity, Task<TableOperationResult>>> InsertOrReplaceEntityMock { get; } = new();
        protected override Task<TableOperationResult> InsertOrReplaceEntityAsync(TestTableEntity entity)
        {
            return InsertOrReplaceEntityMock.Object(entity);
        }

        public Mock<Func<string, Task<List<TestTableEntity>>>> GetPartitionMock { get; } = new();
        protected override Task<List<TEntity>> GetPartitionAsync<TEntity>(string partitionKey)
        {
            return (Task<List<TEntity>>)(object)GetPartitionMock.Object(partitionKey);
        }
    }

    [Fact]
    public async Task GetAsync_CallsTable()
    {
        var repo = new TestTableRepository("UseDevelopmentStorage=true", "TestTable");
        var expected = new TestTableEntity();
        repo.GetTableEntityMock.Setup(m => m("pk", "rk")).ReturnsAsync(expected);

        var result = await repo.GetAsync("pk", "rk");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task SaveAsync_CallsTable()
    {
        var repo = new TestTableRepository("UseDevelopmentStorage=true", "TestTable");
        var entity = new TestTableEntity();
        var tableResult = new TableResult { HttpStatusCode = 200 };
        repo.InsertOrReplaceEntityMock.Setup(m => m(entity))
            .ReturnsAsync(new TableOperationResult(tableResult));

        var result = await repo.SaveAsync(entity);

        // We assert that it returns the value from TableOperationResult.WasSuccessful
        Assert.Equal(new TableOperationResult(tableResult).WasSuccessful, result);
    }

    [Fact]
    public async Task AddAllAsync_NullEntities_ReturnsFalse()
    {
        var repo = new TestTableRepository("UseDevelopmentStorage=true", "TestTable");
        var result = await repo.AddAllAsync(null!);
        Assert.False(result);
    }

    [Fact]
    public async Task AddAllAsync_CallsSaveAsyncForEachEntity()
    {
        var repo = new Mock<TestTableRepository>("UseDevelopmentStorage=true", "TestTable") { CallBase = true };
        repo.Setup(r => r.SaveAsync(It.IsAny<TestTableEntity>())).ReturnsAsync(true);
        var entities = new List<TestTableEntity> { new(), new() };

        var result = await repo.Object.AddAllAsync(entities);

        Assert.True(result);
        repo.Verify(r => r.SaveAsync(It.IsAny<TestTableEntity>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddAllAsync_Failure_ReturnsFalse()
    {
        var repo = new Mock<TestTableRepository>("UseDevelopmentStorage=true", "TestTable") { CallBase = true };
        repo.SetupSequence(r => r.SaveAsync(It.IsAny<TestTableEntity>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        var entities = new List<TestTableEntity> { new(), new() };

        var result = await repo.Object.AddAllAsync(entities);

        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_CallsTable()
    {
        var repo = new TestTableRepository("UseDevelopmentStorage=true", "TestTable");
        var expected = new List<TestTableEntity> { new() };
        repo.GetPartitionMock.Setup(m => m("pk")).ReturnsAsync(expected);

        var result = await repo.GetAllAsync("pk");

        Assert.Equal(expected, result);
    }
}