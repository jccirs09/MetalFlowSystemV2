using MetalFlowSystemV2.Data.Entities;
using MetalFlowSystemV2.Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class ItemAdminService
    {
        private readonly ApplicationDbContext _context;

        public ItemAdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Item>> GetAllAsync(string? search = null, ItemType? type = null, bool? active = null)
        {
            var query = _context.Items
                .Include(i => i.ParentItem)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.ItemCode.Contains(search) || i.Description.Contains(search));
            }

            if (type.HasValue)
            {
                query = query.Where(i => i.ItemType == type.Value);
            }

            if (active.HasValue)
            {
                query = query.Where(i => i.IsActive == active.Value);
            }

            return await query.OrderBy(i => i.ItemCode).ToListAsync();
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items
                .Include(i => i.ParentItem)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Item> CreateAsync(Item item)
        {
            await ValidateItemAsync(item);
            _context.Items.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<Item> UpdateAsync(Item item)
        {
            await ValidateItemAsync(item);
            _context.Items.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task DeactivateAsync(int id)
        {
            var item = await _context.Items
                .Include(i => i.ChildItems)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null) throw new Exception("Item not found");

            // Prevent deleting/deactivating Coil if it has active Sheet children
            if (item.ItemType == ItemType.Coil && item.ChildItems.Any(c => c.IsActive))
            {
                throw new Exception("Cannot deactivate Coil with active Sheet children.");
            }

            item.IsActive = false;
            await _context.SaveChangesAsync();
        }

        private async Task ValidateItemAsync(Item item)
        {
            // Unique ItemCode check
            var existing = await _context.Items
                .Where(i => i.ItemCode == item.ItemCode && i.Id != item.Id)
                .FirstOrDefaultAsync();
            if (existing != null)
            {
                throw new Exception($"Item Code '{item.ItemCode}' already exists.");
            }

            // Hierarchy Rules
            if (item.ItemType == ItemType.Coil)
            {
                if (item.ParentItemId != null)
                {
                    throw new Exception("Coils cannot have a Parent Item.");
                }
            }
            else if (item.ItemType == ItemType.Sheet)
            {
                if (item.ParentItemId == null)
                {
                    throw new Exception("Sheets must have a Parent Coil.");
                }

                // Verify Parent is a Coil
                var parent = await _context.Items.FindAsync(item.ParentItemId);
                if (parent == null)
                {
                    throw new Exception("Parent Item not found.");
                }
                if (parent.ItemType != ItemType.Coil)
                {
                    throw new Exception("Parent of a Sheet must be a Coil.");
                }
            }
            // Other ItemType rules: No strict hierarchy enforced in prompt, but implied "No multi-level nesting".
            // "Sheet cannot be parent of another item" -> This means if I am a Parent, I must be Coil?
            // "Coil cannot have a parent".
            // If ItemType == Other, can it have parent? Prompt says "Hierarchy Rules (Strict)" only mentions Coil and Sheet.
            // But "No multi-level nesting" applies generally?
            // "Sheet cannot be parent of another item".
            // If I am updating an item to be a Parent of someone, I must check if I am a Sheet.
            // But here I am validating 'item'. If 'item' is a Sheet, it cannot be a parent.
            // EF Core check: does anyone have 'item.Id' as ParentItemId?
            // Only relevant on Update (if type changes) or if adding children (which is done via updating child).
            // So if I change 'item' to Sheet, I must check if it has children.

            if (item.Id > 0 && item.ItemType == ItemType.Sheet)
            {
                var hasChildren = await _context.Items.AnyAsync(i => i.ParentItemId == item.Id);
                if (hasChildren)
                {
                    throw new Exception("A Sheet cannot be a parent to other items.");
                }
            }
        }

        public async Task<List<Item>> GetCoilsAsync()
        {
             return await _context.Items
                .Where(i => i.ItemType == ItemType.Coil && i.IsActive)
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }

        public async Task<bool> HasActiveChildrenAsync(int itemId)
        {
            return await _context.Items.AnyAsync(i => i.ParentItemId == itemId && i.IsActive);
        }
    }
}
