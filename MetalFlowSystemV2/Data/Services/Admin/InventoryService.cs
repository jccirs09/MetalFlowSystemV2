using Microsoft.EntityFrameworkCore;
using MetalFlowSystemV2.Data.Entities;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class InventoryService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public InventoryService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<InventoryStock>> GetAllActiveAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.InventoryStocks
                .Include(s => s.Branch)
                .Include(s => s.Item)
                .ThenInclude(i => i!.ParentItem)
                .Where(s => s.IsActive)
                .AsNoTracking()
                .OrderBy(s => s.Branch!.Code)
                .ThenBy(s => s.Item!.ItemCode)
                .ToListAsync();
        }

        public async Task<List<InventoryStock>> GetActiveStockByBranchAsync(int branchId)
        {
            using var context = _contextFactory.CreateDbContext();
            return await context.InventoryStocks
                .Include(s => s.Item)
                .ThenInclude(i => i!.ParentItem)
                .Where(s => s.BranchId == branchId && s.IsActive)
                .AsNoTracking()
                .OrderBy(s => s.Item!.ItemCode)
                .ToListAsync();
        }
    }
}
