using MetalFlowSystemV2.Data.Entities;
using MetalFlowSystemV2.Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class InventorySnapshotImportService
    {
        private readonly ApplicationDbContext _context;

        public InventorySnapshotImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ImportSnapshotAsync(int branchId, Stream csvStream)
        {
            var rows = await ParseCsvAsync(csvStream);

            if (!rows.Any())
            {
                throw new Exception("The snapshot file is empty or invalid.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Soft-deactivate all existing active inventory for this branch
                var existingStocks = await _context.InventoryStocks
                    .Where(s => s.BranchId == branchId && s.IsActive)
                    .ToListAsync();

                foreach (var stock in existingStocks)
                {
                    stock.IsActive = false;
                    stock.LastUpdatedAt = DateTime.UtcNow;
                }
                // Save changes to apply deactivation (needed before inserting new ones due to Unique Index if filtered)
                // If using Filtered Index WHERE IsActive=1, we MUST save here so the index sees them as inactive.
                await _context.SaveChangesAsync();

                // 2. Process Rows
                var itemCache = new Dictionary<string, Item>();

                // Pre-load existing items to minimize DB hits
                var distinctItemCodes = rows.Select(r => r.ItemCode).Distinct().ToList();
                var existingItems = await _context.Items
                    .Where(i => distinctItemCodes.Contains(i.ItemCode))
                    .ToListAsync();

                foreach (var item in existingItems)
                {
                    itemCache[item.ItemCode] = item;
                }

                foreach (var row in rows)
                {
                    if (!itemCache.TryGetValue(row.ItemCode, out var item))
                    {
                        // Auto-create Item
                        item = new Item
                        {
                            ItemCode = row.ItemCode,
                            Description = string.IsNullOrWhiteSpace(row.Description) ? "Imported Item" : row.Description,
                            ItemType = ItemType.Other,
                            Category = "Unclassified",
                            IsActive = false,
                            Uom = "Unknown" // Default
                        };
                        _context.Items.Add(item);
                        // We must save to get the Id for the FK
                        await _context.SaveChangesAsync();
                        itemCache[row.ItemCode] = item;
                    }

                    // Determine Qty vs Weight
                    decimal qty = 0;
                    decimal? weight = null;

                    if (item.ItemType == ItemType.Coil)
                    {
                        weight = row.SnapshotValue;
                        qty = 0;
                    }
                    else if (item.ItemType == ItemType.Sheet)
                    {
                        qty = row.SnapshotValue;
                        weight = null;
                    }
                    else // Other
                    {
                         // Treat as Qty by default for Other
                         qty = row.SnapshotValue;
                         weight = null;
                    }

                    // Check for duplicate (Branch, Item, Location) in THIS import batch?
                    // If the CSV has duplicates, we need to aggregate or fail.
                    // "Snapshot" usually implies distinct lines. But if CSV has 2 lines for same Item+Loc?
                    // "Constraint: Unique index: (BranchId, ItemId, LocationCode)".
                    // If CSV has duplicates, we will crash.
                    // We should aggregate.
                    // But wait, "Snapshot" might list packs individually at same location?
                    // If so, we must aggregate.
                    // I will Aggregate in memory first? Or just check Local tracker.
                    // Let's assume we need to handle it.
                    // Since I am inserting, I can't easily check against DB because I haven't committed (well I saved items).
                    // But I haven't saved stocks.
                    // I'll check `_context.InventoryStocks.Local`? No, too complex.
                    // Better to Aggregate the `rows` first.

                    // Actually, let's Aggregate rows before processing loop.
                }

                // Process Aggregated Rows
                var aggregatedRows = rows
                    .GroupBy(r => new { r.ItemCode, r.SnapshotLocation })
                    .Select(g => new
                    {
                        ItemCode = g.Key.ItemCode,
                        Description = g.First().Description, // Take first
                        Location = g.Key.SnapshotLocation,
                        TotalValue = g.Sum(x => x.SnapshotValue)
                    })
                    .ToList();

                foreach (var row in aggregatedRows)
                {
                     if (!itemCache.TryGetValue(row.ItemCode, out var item))
                     {
                        // Should be in cache from previous pass, but if we aggregated...
                        // Re-check logic: We loaded items based on distinct codes.
                        // If we created new ones, they are in cache.
                        // So it should be there.
                        // Wait, I put creation inside the loop above. I should move creation before or handle it here.
                        // I'll handle creation here.

                        // Copy-paste creation logic (or ensure it's done).
                         item = new Item
                        {
                            ItemCode = row.ItemCode,
                            Description = string.IsNullOrWhiteSpace(row.Description) ? "Imported Item" : row.Description,
                            ItemType = ItemType.Other,
                            Category = "Unclassified",
                            IsActive = false,
                            Uom = "Unknown"
                        };
                        _context.Items.Add(item);
                        await _context.SaveChangesAsync();
                        itemCache[row.ItemCode] = item;
                     }

                    decimal qty = 0;
                    decimal? weight = null;

                    if (item.ItemType == ItemType.Coil)
                    {
                        weight = row.TotalValue;
                        qty = 0;
                    }
                    else
                    {
                        qty = row.TotalValue;
                        weight = null;
                    }

                    var newStock = new InventoryStock
                    {
                        BranchId = branchId,
                        ItemId = item.Id,
                        LocationCode = row.Location,
                        QuantityOnHand = qty,
                        WeightOnHand = weight,
                        LastUpdatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.InventoryStocks.Add(newStock);
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

        private async Task<List<SnapshotRowDto>> ParseCsvAsync(Stream stream)
        {
            var results = new List<SnapshotRowDto>();
            using var reader = new StreamReader(stream);

            // Read Header
            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine)) return results;

            var headers = headerLine.Split(',').Select(h => h.Trim().ToLower()).ToList();

            // Map headers
            // Supported: "Item ID", "Description", "Size", "Snapshot Loc", "Snapshot"
            int itemCodeIdx = -1, descIdx = -1, locIdx = -1, valIdx = -1;

            for (int i = 0; i < headers.Count; i++)
            {
                if (headers[i].Contains("item id") || headers[i] == "item code") itemCodeIdx = i;
                else if (headers[i].Contains("description")) descIdx = i;
                else if (headers[i].Contains("snapshot loc")) locIdx = i;
                else if (headers[i] == "snapshot" || headers[i].Contains("qty") || headers[i].Contains("weight")) valIdx = i;
            }

            if (itemCodeIdx == -1 || valIdx == -1)
            {
                throw new Exception("CSV must contain 'Item ID' and 'Snapshot' columns.");
            }

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Simple split - assuming no commas in values or handled by quotes?
                // For a robust solution, need Regex or parser.
                // Assuming simple CSV for Phase 1 as requested "CSV/XLSX" but "Admin only" often implies controlled input.
                // I will use a regex split for basic CSV support (handles quotes).
                var parts = ParseCsvLine(line);

                if (parts.Count < headers.Count) continue; // Skip malformed?

                var row = new SnapshotRowDto
                {
                    ItemCode = parts.Count > itemCodeIdx ? parts[itemCodeIdx].Trim() : "",
                    Description = (descIdx != -1 && parts.Count > descIdx) ? parts[descIdx].Trim() : "",
                    SnapshotLocation = (locIdx != -1 && parts.Count > locIdx) ? parts[locIdx].Trim() : null,
                };

                // Parse Value
                if (parts.Count > valIdx)
                {
                    var valStr = parts[valIdx].Trim();
                    // Fail on empty/whitespace or non-numeric
                    if (string.IsNullOrWhiteSpace(valStr))
                    {
                         throw new Exception($"Invalid snapshot value (empty) for Item '{row.ItemCode}'.");
                    }

                    if (decimal.TryParse(valStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
                    {
                        row.SnapshotValue = val;
                    }
                    else
                    {
                        throw new Exception($"Invalid numeric snapshot value '{valStr}' for Item '{row.ItemCode}'.");
                    }
                }
                else
                {
                     throw new Exception($"Missing snapshot value column for Item '{row.ItemCode}'.");
                }

                if (!string.IsNullOrEmpty(row.ItemCode))
                {
                    results.Add(row);
                }
            }

            return results;
        }

        private List<string> ParseCsvLine(string line)
        {
            // Basic CSV parser handling quotes
            var result = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current);
            return result;
        }

        private class SnapshotRowDto
        {
            public string ItemCode { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string? SnapshotLocation { get; set; }
            public decimal SnapshotValue { get; set; }
        }
    }
}
