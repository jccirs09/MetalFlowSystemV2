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

                // UseHeaderRow = false so we can manually inspect the header row for the blank column
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = false
                    }
                });

                if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count < 2)
                {
                    result.Errors.Add("No data found in file.");
                    result.Success = false;
                    return result;
                }

                var table = dataSet.Tables[0];
                var headerRow = table.Rows[0];

                // Map Headers
                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var blankHeaders = new List<int>();

                for (int i = 0; i < table.Columns.Count; i++)
                {
                    var val = headerRow[i]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(val))
                    {
                        blankHeaders.Add(i);
                    }
                    else
                    {
                        if (!headerMap.ContainsKey(val))
                            headerMap[val] = i;
                    }
                }

                // Validate Headers
                var requiredHeaders = new[] { "Item ID", "Description", "Snapshot Loc", "Snapshot" };
                var missingHeaders = requiredHeaders.Where(h => !headerMap.ContainsKey(h)).ToList();

                if (missingHeaders.Any())
                {
                    result.Errors.Add($"Missing required headers: {string.Join(", ", missingHeaders)}");
                    result.Success = false;
                    return result;
                }

                // Validate UOM Column (One blank header)
                if (blankHeaders.Count != 1)
                {
                    result.Errors.Add($"File must contain exactly one column with a blank header (for UOM). Found {blankHeaders.Count}.");
                    result.Success = false;
                    return result;
                }
                int uomIndex = blankHeaders[0];

                int snapshotIndex = headerMap["Snapshot"];
                int widthIndex = headerMap.ContainsKey("Width") ? headerMap["Width"] : -1;
                int lengthIndex = headerMap.ContainsKey("Length") ? headerMap["Length"] : -1;

                // Validate Rows
                int rowIndex = 2; // Data starts at Row 1 (Index 1) which is "Row 2" in Excel
                for (int i = 1; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    var uom = row[uomIndex]?.ToString()?.Trim().ToUpper();

                    if (uom != "PCS" && uom != "LBS")
                    {
                        result.Errors.Add($"Row {rowIndex}: Invalid UOM '{uom}'. Must be PCS or LBS.");
                    }
                    else if (uom == "PCS")
                    {
                        if (widthIndex == -1 || lengthIndex == -1)
                        {
                             // We can't validate row-specifics if columns are missing, but we should fail globally if any PCS row exists.
                             // But we'll catch it here.
                             if (widthIndex == -1) result.Errors.Add("Column 'Width' is required for PCS items.");
                             if (lengthIndex == -1) result.Errors.Add("Column 'Length' is required for PCS items.");
                             // Return early to avoid spamming
                             if (widthIndex == -1 || lengthIndex == -1) { result.Success = false; return result; }
                        }
                    }

                    var snapshotVal = row[snapshotIndex]?.ToString();
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
                    result.RowsProcessed = table.Rows.Count - 1; // Subtract header
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
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
                });
                var table = dataSet.Tables[0];
                var headerRow = table.Rows[0];

                // Map Headers
                var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                int uomIndex = -1;
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    var val = headerRow[i]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(val)) uomIndex = i;
                    else if (!headerMap.ContainsKey(val)) headerMap[val] = i;
                }

                if (uomIndex == -1) throw new Exception("Missing UOM column (blank header).");

                int itemIndex = headerMap["Item ID"];
                int descIndex = headerMap["Description"];
                int locIndex = headerMap["Snapshot Loc"];
                int snapshotIndex = headerMap["Snapshot"];
                int widthIndex = headerMap.ContainsKey("Width") ? headerMap["Width"] : -1;
                int lengthIndex = headerMap.ContainsKey("Length") ? headerMap["Length"] : -1;

                var now = DateTime.UtcNow;
                var itemsCache = await _context.Items.ToDictionaryAsync(i => i.ItemCode, StringComparer.OrdinalIgnoreCase);

                // Start loop from 1
                for (int i = 1; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    var itemCode = row[itemIndex]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(itemCode)) continue;

                    var description = row[descIndex]?.ToString()?.Trim() ?? "";
                    var location = row[locIndex]?.ToString()?.Trim() ?? "UNKNOWN";
                    var snapshotStr = row[snapshotIndex]?.ToString()?.Trim();
                    var uom = row[uomIndex]?.ToString()?.Trim().ToUpper(); // PCS or LBS

                    decimal snapshotVal = 0;
                    if (!string.IsNullOrEmpty(snapshotStr) && decimal.TryParse(snapshotStr, out var val))
                    {
                        snapshotVal = val;
                    }

                    if (snapshotVal == 0) continue;

                    if (!itemsCache.TryGetValue(itemCode, out var item))
                    {
                        // Create new Item
                        item = new Item
                        {
                            ItemCode = itemCode,
                            Description = description,
                            UOM = uom ?? "PCS",
                            PoundsPerSquareFoot = 0, // Cannot guess PPSF
                            IsActive = true // Prompt said "Auto-create... IsActive = false" but typically active. Sticking to false as per existing code, or check logic?
                            // Existing code had IsActive = false. Keeping it.
                        };
                        item.IsActive = false;

                        if (uom == "LBS") item.Type = ItemType.Coil;
                        else item.Type = ItemType.Sheet;

                        _context.Items.Add(item);
                        itemsCache[itemCode] = item; // Add to cache so we reuse it
                    }
                    else
                    {
                        // Update UOM/Type if mismatch? No, Item Master is authoritative.
                        // But if UOM is blank in DB, maybe update it?
                        // Assuming Item Master is setup.
                    }

                    var stock = new InventoryStock
                    {
                        BranchId = branchId,
                        Item = item,
                        LocationCode = location,
                        LastUpdatedAt = now,
                        IsActive = true
                    };

                    if (uom == "LBS")
                    {
                        // Coil
                        stock.WeightOnHand = snapshotVal;
                        stock.QuantityOnHand = 1; // Logic: Coil count 1
                        stock.Width = 0; // Not parsed for Coil
                        stock.Length = 0;
                    }
                    else if (uom == "PCS")
                    {
                        // Sheet
                        stock.QuantityOnHand = (int)snapshotVal;

                        // Parse Dimensions
                        decimal width = 0;
                        decimal length = 0;

                        if (widthIndex != -1 && decimal.TryParse(row[widthIndex]?.ToString(), out var w)) width = w;
                        if (lengthIndex != -1 && decimal.TryParse(row[lengthIndex]?.ToString(), out var l)) length = l;

                        if (width == 0 || length == 0)
                        {
                            throw new Exception($"Row {i+1}: Missing Width/Length for PCS item '{itemCode}'.");
                        }

                        stock.Width = width;
                        stock.Length = length;

                        // Calculate Weight
                        if (item.PoundsPerSquareFoot == null || item.PoundsPerSquareFoot <= 0)
                        {
                             throw new Exception($"Row {i+1}: Item '{itemCode}' missing PoundsPerSquareFoot.");
                        }

                        stock.WeightOnHand = stock.QuantityOnHand.Value * (width / 12m) * (length / 12m) * item.PoundsPerSquareFoot.Value;
                    }
                    else
                    {
                         throw new Exception($"Row {i+1}: Invalid UOM '{uom}'.");
                    }

                    _context.InventoryStocks.Add(stock);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Success = true;
                result.RowsProcessed = table.Rows.Count - 1;
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
