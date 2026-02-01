using Microsoft.EntityFrameworkCore;
using MetalFlowSystemV2.Data.Entities;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class InventoryService
    {
        private readonly ApplicationDbContext _context;

        public InventoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryStock>> GetAllActiveAsync()
        {
            return await _context.InventoryStocks
                .Include(s => s.Branch)
                .Include(s => s.Item)
                .ThenInclude(i => i!.ParentItem)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Branch!.Code)
                .ThenBy(s => s.Item!.ItemCode)
                .ToListAsync();
        }

        public async Task<List<InventoryStock>> GetActiveStockByBranchAsync(int branchId)
        {
            return await _context.InventoryStocks
                .Include(s => s.Item)
                .ThenInclude(i => i!.ParentItem)
                .Where(s => s.BranchId == branchId && s.IsActive)
                .OrderBy(s => s.Item!.ItemCode)
                .ToListAsync();
        }
    }
}
