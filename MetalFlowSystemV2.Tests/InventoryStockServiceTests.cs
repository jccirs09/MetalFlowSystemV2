using MetalFlowSystemV2.Data;
using MetalFlowSystemV2.Data.Entities;
using MetalFlowSystemV2.Data.Entities.Enums;
using MetalFlowSystemV2.Data.Services.Admin;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MetalFlowSystemV2.Tests
{
    public class InventoryStockServiceTests
    {
        private ApplicationDbContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task UpsertAsync_CreatesNewRecord_WhenIdIsZeroAndNoMatch()
        {
            var context = GetInMemoryContext("NewStockDb");
            var service = new InventoryStockAdminService(context);

            var item = new Item { Id = 1, ItemCode = "C1", Description = "Coil 1", ItemType = ItemType.Coil, IsActive = true };
            context.Items.Add(item);
            await context.SaveChangesAsync();

            await service.UpsertAsync(1, 1, "LOC-A", 0, 1000, 0);

            var stock = await context.InventoryStocks.FirstOrDefaultAsync();
            Assert.NotNull(stock);
            Assert.Equal("LOC-A", stock.LocationCode);
            Assert.Equal(1000, stock.WeightOnHand);
        }

        [Fact]
        public async Task UpsertAsync_UpdatesExistingByMatch_WhenIdIsZero()
        {
            var context = GetInMemoryContext("MergeStockDb");
            var service = new InventoryStockAdminService(context);

            var item = new Item { Id = 1, ItemCode = "S1", Description = "Sheet 1", ItemType = ItemType.Sheet, IsActive = true };
            context.Items.Add(item);
            var existing = new InventoryStock { BranchId = 1, ItemId = 1, LocationCode = "LOC-B", QuantityOnHand = 10, IsActive = true };
            context.InventoryStocks.Add(existing);
            await context.SaveChangesAsync();

            await service.UpsertAsync(1, 1, "LOC-B", 20, null, 0);

            var stock = await context.InventoryStocks.SingleAsync();
            Assert.Equal(20, stock.QuantityOnHand);
        }

        [Fact]
        public async Task UpsertAsync_UpdatesRecordById_EvenIfLocationChanges()
        {
            var context = GetInMemoryContext("UpdateStockDb");
            var service = new InventoryStockAdminService(context);

            var item = new Item { Id = 1, ItemCode = "S1", Description = "Sheet 1", ItemType = ItemType.Sheet, IsActive = true };
            context.Items.Add(item);
            var existing = new InventoryStock { Id = 5, BranchId = 1, ItemId = 1, LocationCode = "OLD-LOC", QuantityOnHand = 10, IsActive = true };
            context.InventoryStocks.Add(existing);
            await context.SaveChangesAsync();

            // Edit location
            await service.UpsertAsync(1, 1, "NEW-LOC", 10, null, 5);

            var count = await context.InventoryStocks.CountAsync();
            var stock = await context.InventoryStocks.SingleAsync();

            Assert.Equal(1, count);
            Assert.Equal(5, stock.Id);
            Assert.Equal("NEW-LOC", stock.LocationCode);
        }

        [Fact]
        public async Task UpsertAsync_Throws_WhenDuplicateExists()
        {
            var context = GetInMemoryContext("ConflictStockDb");
            var service = new InventoryStockAdminService(context);

            var item = new Item { Id = 1, ItemCode = "S1", Description = "Sheet 1", ItemType = ItemType.Sheet, IsActive = true };
            context.Items.Add(item);

            // Two records
            var stock1 = new InventoryStock { Id = 1, BranchId = 1, ItemId = 1, LocationCode = "LOC-1", QuantityOnHand = 10, IsActive = true };
            var stock2 = new InventoryStock { Id = 2, BranchId = 1, ItemId = 1, LocationCode = "LOC-2", QuantityOnHand = 10, IsActive = true };

            context.InventoryStocks.AddRange(stock1, stock2);
            await context.SaveChangesAsync();

            // Try to move stock2 to LOC-1 (conflict with stock1)
            await Assert.ThrowsAsync<Exception>(async () =>
                await service.UpsertAsync(1, 1, "LOC-1", 10, null, 2));
        }

        [Fact]
        public async Task ImportSnapshotAsync_ThrowsOnInvalidNumeric()
        {
            var context = GetInMemoryContext("ImportDb");
            var service = new InventorySnapshotImportService(context);

            var item = new Item { Id = 1, ItemCode = "I1", Description = "Item 1", ItemType = ItemType.Sheet, IsActive = true };
            context.Items.Add(item);
            await context.SaveChangesAsync();

            var csv = "Item ID, Description, Snapshot Loc, Snapshot\nI1, Desc, L1, INVALID";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            await Assert.ThrowsAsync<Exception>(async () =>
                await service.ImportSnapshotAsync(1, stream));
        }

        [Fact]
        public async Task ImportSnapshotAsync_ThrowsOnEmptyNumeric()
        {
            var context = GetInMemoryContext("ImportEmptyDb");
            var service = new InventorySnapshotImportService(context);

             var item = new Item { Id = 1, ItemCode = "I1", Description = "Item 1", ItemType = ItemType.Sheet, IsActive = true };
            context.Items.Add(item);
            await context.SaveChangesAsync();

            var csv = "Item ID, Description, Snapshot Loc, Snapshot\nI1, Desc, L1,   ";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            var ex = await Assert.ThrowsAsync<Exception>(async () =>
                await service.ImportSnapshotAsync(1, stream));

            Assert.Contains("empty", ex.Message);
        }
    }
}
