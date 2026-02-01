using System.IO;
using System.Data;
using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;
using ExcelDataReader;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class InventorySnapshotImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ItemService _itemService;

        public InventorySnapshotImportService(ApplicationDbContext context, ItemService itemService)
        {
            _context = context;
            _itemService = itemService;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public async Task<ImportResult> ValidateSnapshotAsync(Stream fileStream, string fileName)
        {
            var result = new ImportResult();

            try
            {
                using var reader = CreateReader(fileStream, fileName);
                if (reader == null)
                {
                    result.Errors.Add("Unsupported file format.");
                    result.Success = false;
                    return result;
                }

                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true
                    }
                });

                if (dataSet.Tables.Count == 0)
                {
                    result.Errors.Add("No data found in file.");
                    result.Success = false;
                    return result;
                }

                var table = dataSet.Tables[0];
                var headers = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

                var requiredHeaders = new[] { "Item ID", "Description", "Snapshot Loc", "Snapshot" };
                var missingHeaders = requiredHeaders.Where(h => !headers.Contains(h, StringComparer.OrdinalIgnoreCase)).ToList();

                if (missingHeaders.Any())
                {
                    result.Errors.Add($"Missing required headers: {string.Join(", ", missingHeaders)}");
                    result.Success = false;
                    return result;
                }

                // Validate Rows
                int rowIndex = 2; // Row 1 is header
                foreach (DataRow row in table.Rows)
                {
                    var snapshotVal = row["Snapshot"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(snapshotVal) && !decimal.TryParse(snapshotVal, out _))
                    {
                        result.Errors.Add($"Row {rowIndex}: Invalid numeric value for Snapshot '{snapshotVal}'");
                    }
                    rowIndex++;
                }

                if (result.Errors.Any())
                {
                    result.Success = false;
                }
                else
                {
                    result.Success = true;
                    result.RowsProcessed = table.Rows.Count;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error reading file: {ex.Message}");
                result.Success = false;
            }

            return result;
        }

        public async Task<ImportResult> ImportSnapshotAsync(int branchId, Stream fileStream, string fileName)
        {
            var result = new ImportResult();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Deactivate all existing stock for this branch
                var existingStock = await _context.InventoryStocks
                    .Where(s => s.BranchId == branchId && s.IsActive)
                    .ToListAsync();

                foreach (var stock in existingStock)
                {
                    stock.IsActive = false;
                }

                // Read File
                using var reader = CreateReader(fileStream, fileName);
                if (reader == null) throw new Exception("Invalid file format");

                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                });
                var table = dataSet.Tables[0];

                var now = DateTime.UtcNow;
                var itemsCache = await _context.Items.ToDictionaryAsync(i => i.ItemCode, StringComparer.OrdinalIgnoreCase);

                foreach (DataRow row in table.Rows)
                {
                    var itemCode = row["Item ID"]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(itemCode)) continue;

                    var description = row["Description"]?.ToString()?.Trim() ?? "";
                    var location = row["Snapshot Loc"]?.ToString()?.Trim() ?? "UNKNOWN";
                    var snapshotStr = row["Snapshot"]?.ToString()?.Trim();

                    decimal snapshotVal = 0;
                    if (!string.IsNullOrEmpty(snapshotStr) && decimal.TryParse(snapshotStr, out var val))
                    {
                        snapshotVal = val;
                    }

                    if (snapshotVal == 0) continue; // Skip zero quantity/weight? Prompt says "Snapshot values differ...". If 0, assume no stock? Or keep record? Usually keep if it's in snapshot. But let's assume > 0 or keep 0. Let's keep 0 if it's explicitly there. But `if (snapshotVal == 0)` check above. The prompt is silent on zero. I'll include it.
                    // Wait, if !TryParse, it defaults to 0.

                    if (!itemsCache.TryGetValue(itemCode, out var item))
                    {
                        // Create new Item (Option A)
                        item = new Item
                        {
                            ItemCode = itemCode,
                            Description = description,
                            Type = ItemType.Other, // Unclassified
                            IsActive = false // "Auto-create Item as... IsActive = false"
                        };
                        _context.Items.Add(item);
                        // We need to save to get ID? Or just add to context and EF handles it.
                        // EF Core handles generic additions, but dictionary cache needs it.
                        // We can't easily add to dictionary until saved if we need ID for FK?
                        // Actually, we can add to context, and EF fixes up FKs if we use the object.
                        // But for next rows, we need the object.
                        itemsCache[itemCode] = item;
                    }

                    var stock = new InventoryStock
                    {
                        BranchId = branchId,
                        Item = item, // Use navigation property
                        LocationCode = location,
                        LastUpdatedAt = now,
                        IsActive = true
                    };

                    if (item.Type == ItemType.Sheet)
                    {
                        stock.QuantityOnHand = (int)snapshotVal; // Truncate decimal? Snapshot = PCS.
                    }
                    else if (item.Type == ItemType.Coil)
                    {
                        stock.WeightOnHand = snapshotVal;
                    }
                    else // Other
                    {
                        // Fallback logic
                        if (snapshotVal % 1 == 0)
                            stock.QuantityOnHand = (int)snapshotVal;
                        else
                            stock.WeightOnHand = snapshotVal;
                    }

                    _context.InventoryStocks.Add(stock);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Success = true;
                result.RowsProcessed = table.Rows.Count;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.Success = false;
                result.Errors.Add($"Import failed: {ex.Message}");
            }

            return result;
        }

        private IExcelDataReader? CreateReader(Stream fileStream, string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            if (extension == ".csv")
            {
                return ExcelReaderFactory.CreateCsvReader(fileStream);
            }
            else if (extension == ".xlsx" || extension == ".xls")
            {
                return ExcelReaderFactory.CreateReader(fileStream);
            }
            return null;
        }
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new();
        public int RowsProcessed { get; set; }
    }
}
