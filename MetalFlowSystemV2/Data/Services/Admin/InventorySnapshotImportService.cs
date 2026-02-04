using System.IO;
using System.Data;
using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;
using ExcelDataReader;
using System.Text.RegularExpressions;

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

                // Check for UOM
                // We trust the import logic to find it.
                // We can't validate it strictly here without repeating logic,
                // but we can check if a candidate exists.

                int rowIndex = 2;
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

                using var reader = CreateReader(fileStream, fileName);
                if (reader == null) throw new Exception("Invalid file format");

                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                });
                var table = dataSet.Tables[0];
                var headers = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

                // UOM Detection Strategy
                var knownHeaders = new[] { "Item ID", "Description", "Snapshot Loc", "Snapshot" };
                var candidateCols = headers.Except(knownHeaders, StringComparer.OrdinalIgnoreCase).ToList();

                string uomCol = "";
                foreach(var col in candidateCols)
                {
                    var values = table.Rows.Cast<DataRow>().Select(r => r[col]?.ToString()?.ToUpper().Trim()).Where(v => !string.IsNullOrEmpty(v)).Take(10).ToList();
                    if(values.Any() && values.All(v => v == "PCS" || v == "LBS"))
                    {
                        uomCol = col;
                        break;
                    }
                }

                if(string.IsNullOrEmpty(uomCol))
                {
                     throw new Exception("Could not identify UOM column (Column with blank header containing PCS/LBS).");
                }

                // Dimension Detection Strategy
                // Look for "Width" and "Length"?
                // Or are they also unnamed?
                // Let's assume standard names "Width" and "Length" for now.
                // If they are missing, we default to 0 and fail validation if needed.
                string widthCol = headers.FirstOrDefault(h => h.Contains("Width", StringComparison.OrdinalIgnoreCase)) ?? "";
                string lengthCol = headers.FirstOrDefault(h => h.Contains("Length", StringComparison.OrdinalIgnoreCase)) ?? "";

                var now = DateTime.UtcNow;
                var itemsCache = await _context.Items.ToDictionaryAsync(i => i.ItemCode, StringComparer.OrdinalIgnoreCase);

                int rowIndex = 1;
                foreach (DataRow row in table.Rows)
                {
                    rowIndex++;
                    var itemCode = row["Item ID"]?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(itemCode)) continue;

                    var description = row["Description"]?.ToString()?.Trim() ?? "";
                    var location = row["Snapshot Loc"]?.ToString()?.Trim() ?? "UNKNOWN";
                    var snapshotStr = row["Snapshot"]?.ToString()?.Trim();
                    var uom = row[uomCol]?.ToString()?.ToUpper().Trim();

                    if (uom != "PCS" && uom != "LBS")
                    {
                         throw new Exception($"Row {rowIndex}: Invalid UOM '{uom}'. Must be PCS or LBS.");
                    }

                    decimal snapshotVal = 0;
                    if (!string.IsNullOrEmpty(snapshotStr) && decimal.TryParse(snapshotStr, out var val))
                    {
                        snapshotVal = val;
                    }

                    if (snapshotVal == 0) continue;

                    if (!itemsCache.TryGetValue(itemCode, out var item))
                    {
                        item = new Item
                        {
                            ItemCode = itemCode,
                            Description = description,
                            UOM = uom,
                            Type = (uom == "LBS") ? ItemType.Coil : ItemType.Sheet,
                            IsActive = true
                        };

                        if (uom == "PCS")
                        {
                             // We cannot validate PPSF here as we just created it and default is 0.
                             // But we will check calculation logic below.
                        }

                        _context.Items.Add(item);
                        itemsCache[itemCode] = item;
                    }
                    else
                    {
                        if (uom == "PCS" && item.UOM != "PCS")
                        {
                             throw new Exception($"Row {rowIndex}: Snapshot says PCS but Item '{itemCode}' is defined as {item.UOM}.");
                        }
                    }

                    // Parse Dimensions
                    decimal width = 0;
                    decimal length = 0;

                    if (!string.IsNullOrEmpty(widthCol))
                        decimal.TryParse(row[widthCol]?.ToString(), out width);

                    if (!string.IsNullOrEmpty(lengthCol))
                        decimal.TryParse(row[lengthCol]?.ToString(), out length);

                    var stock = new InventoryStock
                    {
                        BranchId = branchId,
                        Item = item,
                        LocationCode = location,
                        LastUpdatedAt = now,
                        IsActive = true,
                        Width = width,
                        Length = length
                    };

                    if (uom == "LBS")
                    {
                        stock.WeightOnHand = snapshotVal;
                        stock.QuantityOnHand = 1;
                    }
                    else // PCS
                    {
                        stock.QuantityOnHand = (int)snapshotVal;

                        // Calculation Logic
                        if (width <= 0 || length <= 0)
                             throw new Exception($"Row {rowIndex}: Sheet item '{itemCode}' missing valid Dimensions (Width/Length).");

                        if (item.PoundsPerSquareFoot <= 0)
                             throw new Exception($"Row {rowIndex}: Sheet item '{itemCode}' missing PoundsPerSquareFoot.");

                        // Formula: Qty * Width(ft) * Length(ft) * PPSF
                        // Assuming Width/Length in Inches
                        decimal areaSqFt = (width / 12m) * (length / 12m);
                        stock.WeightOnHand = stock.QuantityOnHand * areaSqFt * item.PoundsPerSquareFoot;
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
