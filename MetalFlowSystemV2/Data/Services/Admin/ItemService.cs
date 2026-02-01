using Microsoft.EntityFrameworkCore;
using MetalFlowSystemV2.Data.Entities;
using ExcelDataReader;
using System.Data;
using System.Text;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class ItemService
    {
        private readonly ApplicationDbContext _context;

        public ItemService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Item>> GetAllAsync()
        {
            return await _context.Items
                .Include(i => i.ParentItem)
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items
                .Include(i => i.ParentItem)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task CreateAsync(Item item)
        {
            _context.Items.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Item item)
        {
            _context.Items.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Item>> GetCoilItemsAsync()
        {
            return await _context.Items
                .Where(i => i.Type == ItemType.Coil)
                .OrderBy(i => i.ItemCode)
                .ToListAsync();
        }

        public async Task ImportItemsAsync(Stream fileStream)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var reader = ExcelReaderFactory.CreateReader(fileStream);
            var conf = new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            };

            var dataSet = reader.AsDataSet(conf);
            var dataTable = dataSet.Tables[0];

            if (!dataTable.Columns.Contains("ItemCode") ||
                !dataTable.Columns.Contains("Description") ||
                !dataTable.Columns.Contains("CoilRelationship"))
            {
                throw new Exception("Invalid Excel format. Expected columns: ItemCode, Description, CoilRelationship.");
            }

            var importDtos = new List<ItemImportDto>();

            foreach (DataRow row in dataTable.Rows)
            {
                var itemCode = row["ItemCode"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(itemCode)) continue;

                var desc = row["Description"]?.ToString()?.Trim() ?? "";
                var parentCode = row["CoilRelationship"]?.ToString()?.Trim();

                importDtos.Add(new ItemImportDto
                {
                    ItemCode = itemCode,
                    Description = desc,
                    ParentCode = parentCode
                });
            }

            // Transactional Upsert
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Load existing items to memory for lookup (assuming manageable size)
                // In production with huge datasets, we'd batch this or use checking per item.
                // Here we fetch all to optimize lookups.
                var existingItems = await _context.Items.ToDictionaryAsync(i => i.ItemCode, StringComparer.OrdinalIgnoreCase);

                // Pass 1: Upsert "Coils" (Items with NO parent relationship)
                // These are treated as Roots.
                var rootItems = importDtos.Where(d => string.IsNullOrEmpty(d.ParentCode)).ToList();

                foreach (var dto in rootItems)
                {
                    if (existingItems.TryGetValue(dto.ItemCode, out var existingItem))
                    {
                        existingItem.Description = dto.Description;
                        existingItem.Type = ItemType.Coil; // Enforce type
                        existingItem.ParentItemId = null; // Enforce no parent
                        // _context.Update(existingItem); // Tracked by default
                    }
                    else
                    {
                        var newItem = new Item
                        {
                            ItemCode = dto.ItemCode,
                            Description = dto.Description,
                            Type = ItemType.Coil,
                            IsActive = true
                        };
                        _context.Items.Add(newItem);
                        existingItems[dto.ItemCode] = newItem; // Add to dictionary for subsequent lookups (though ID is 0)
                    }
                }

                // Save Coils first so we have IDs for new ones (if EF Core generates IDs on Insert)
                // Note: EF Core Fixup might handle relationships without IDs if objects are linked,
                // but here we look up by Code. Dictionary has reference to the object.
                // We need IDs if we are setting ParentItemId int?. If we set ParentItem object, we don't need IDs yet.
                // Let's rely on setting the object reference if possible, or save to get IDs.
                await _context.SaveChangesAsync();

                // Re-fetch dictionary? Or trust the objects in `existingItems` now have IDs?
                // EF Core populates IDs on the entities in the context after SaveChanges.
                // So `existingItems[code].Id` should be valid now.

                // Pass 2: Upsert "Sheets" (Items WITH parent relationship)
                var childItems = importDtos.Where(d => !string.IsNullOrEmpty(d.ParentCode)).ToList();

                foreach (var dto in childItems)
                {
                    // Find Parent
                    if (!existingItems.TryGetValue(dto.ParentCode!, out var parentItem))
                    {
                        // Parent not found in DB or in this import batch.
                        // Decision: Fail or Skip?
                        // User requirement: "Sheet items must have a Coil parent".
                        // We throw to enforce integrity.
                        throw new Exception($"Parent Item '{dto.ParentCode}' not found for Item '{dto.ItemCode}'. Ensure parent is defined in the file or exists in Master.");
                    }

                    if (existingItems.TryGetValue(dto.ItemCode, out var existingItem))
                    {
                        existingItem.Description = dto.Description;
                        existingItem.Type = ItemType.Sheet;
                        existingItem.ParentItem = parentItem; // Link object
                    }
                    else
                    {
                        var newItem = new Item
                        {
                            ItemCode = dto.ItemCode,
                            Description = dto.Description,
                            Type = ItemType.Sheet,
                            ParentItem = parentItem,
                            IsActive = true
                        };
                        _context.Items.Add(newItem);
                        existingItems[dto.ItemCode] = newItem;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private class ItemImportDto
        {
            public required string ItemCode { get; set; }
            public required string Description { get; set; }
            public string? ParentCode { get; set; }
        }
    }
}
