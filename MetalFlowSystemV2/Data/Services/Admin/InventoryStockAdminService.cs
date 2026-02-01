using MetalFlowSystemV2.Data.Entities;
using MetalFlowSystemV2.Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class InventoryStockAdminService
    {
        private readonly ApplicationDbContext _context;

        public InventoryStockAdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryStock>> GetByBranchAsync(int branchId, string? search = null)
        {
            var query = _context.InventoryStocks
                .Include(s => s.Item)
                .Where(s => s.BranchId == branchId && s.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.Item!.ItemCode.Contains(search) || s.Item.Description.Contains(search) || (s.LocationCode != null && s.LocationCode.Contains(search)));
            }

            return await query
                .OrderBy(s => s.Item!.ItemCode)
                .ThenBy(s => s.LocationCode)
                .ToListAsync();
        }

        public async Task UpsertAsync(int branchId, int itemId, string? locationCode, decimal qty, decimal? weight, int id = 0)
        {
            // Validate Logic based on Item Type
            var item = await _context.Items.FindAsync(itemId);
            if (item == null) throw new Exception("Item not found");

            if (item.ItemType == ItemType.Coil)
            {
                // Enforce Coil Rules
                // Snapshot/Input maps to Weight. Qty should be 0 or 1?
                // Policy: Coils are tracked by Weight. Qty = 0.
                if (weight == null) throw new Exception("Weight is required for Coils.");
                qty = 0;
            }
            else if (item.ItemType == ItemType.Sheet)
            {
                // Enforce Sheet Rules
                // Snapshot/Input maps to Qty. Weight should be null.
                weight = null;
            }

            if (qty < 0 || (weight.HasValue && weight.Value < 0))
            {
                throw new Exception("Negative stock not allowed.");
            }

            InventoryStock? targetStock = null;

            if (id > 0)
            {
                // Updating specific record
                targetStock = await _context.InventoryStocks.FindAsync(id);
                if (targetStock == null) throw new Exception("Stock record not found.");

                // Check for conflict: Is there another Active record with same Key but different ID?
                var conflict = await _context.InventoryStocks
                    .AnyAsync(s => s.BranchId == branchId
                                   && s.ItemId == itemId
                                   && s.LocationCode == locationCode
                                   && s.IsActive
                                   && s.Id != id);

                if (conflict)
                {
                    throw new Exception($"Stock for this item already exists at location '{locationCode}'.");
                }
            }
            else
            {
                // New or Merge
                targetStock = await _context.InventoryStocks
                    .FirstOrDefaultAsync(s => s.BranchId == branchId
                                              && s.ItemId == itemId
                                              && s.LocationCode == locationCode
                                              && s.IsActive);
            }

            if (targetStock != null)
            {
                // Update existing
                targetStock.BranchId = branchId; // Theoretically unchanged but ensures consistency
                targetStock.ItemId = itemId;
                targetStock.LocationCode = locationCode;
                targetStock.QuantityOnHand = qty;
                targetStock.WeightOnHand = weight;
                targetStock.LastUpdatedAt = DateTime.UtcNow;
                _context.InventoryStocks.Update(targetStock);
            }
            else
            {
                // Create new
                var newStock = new InventoryStock
                {
                    BranchId = branchId,
                    ItemId = itemId,
                    LocationCode = locationCode,
                    QuantityOnHand = qty,
                    WeightOnHand = weight,
                    LastUpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.InventoryStocks.Add(newStock);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeactivateAsync(int id)
        {
             var stock = await _context.InventoryStocks.FindAsync(id);
             if (stock != null)
             {
                 stock.IsActive = false;
                 stock.LastUpdatedAt = DateTime.UtcNow;
                 await _context.SaveChangesAsync();
             }
        }
    }
}
