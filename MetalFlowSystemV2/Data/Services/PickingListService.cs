using MetalFlowSystemV2.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MetalFlowSystemV2.Data.Services
{
    public class PickingListImportResult
    {
        public PickingListImportDto Dto { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public int ResolvedBranchId { get; set; }
        public string ResolvedBranchName { get; set; }
    }

    public class PickingListService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly PickingListParser _parser;

        public PickingListService(IDbContextFactory<ApplicationDbContext> contextFactory, PickingListParser parser)
        {
            _contextFactory = contextFactory;
            _parser = parser;
        }

        public async Task<int?> ResolveBranchAsync(string userId)
        {
            using var context = _contextFactory.CreateDbContext();

            // Check UserWorkAssignment (Active Shift)
            var activeAssignment = await context.UserWorkAssignments
                .Include(a => a.Branch)
                .Where(a => a.UserId == userId && a.IsActive)
                .FirstOrDefaultAsync();

            if (activeAssignment != null)
            {
                return activeAssignment.BranchId;
            }

            // Check UserBranches
            var userBranches = await context.UserBranches
                .Include(ub => ub.Branch)
                .Where(ub => ub.UserId == userId)
                .ToListAsync();

            if (userBranches.Count == 1)
            {
                return userBranches.First().BranchId;
            }

            // Check for Default Branch
            var defaultBranch = userBranches.FirstOrDefault(ub => ub.IsDefault);
            if (defaultBranch != null)
            {
                return defaultBranch.BranchId;
            }

            return null;
        }

        public async Task<PickingListImportResult> ValidateAndParseAsync(string text, string userId)
        {
            var result = new PickingListImportResult();

            // 1. Branch Resolution
            var branchId = await ResolveBranchAsync(userId);
            if (branchId == null)
            {
                result.Errors.Add("Could not resolve a single active branch. Please select a branch or clock in.");
                result.IsValid = false;
                return result;
            }

            using var context = _contextFactory.CreateDbContext();
            var branch = await context.Branches.FindAsync(branchId);
            result.ResolvedBranchId = branchId.Value;
            result.ResolvedBranchName = branch.Name;

            // 2. Parse
            try
            {
                result.Dto = _parser.Parse(text);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Parsing failed: {ex.Message}");
                result.IsValid = false;
                return result;
            }

            if (result.Dto == null || result.Dto.Lines.Count == 0)
            {
                result.Errors.Add("No lines found in the import text.");
                result.IsValid = false;
                return result;
            }

            // 3. Validation
            // Check Items
            var itemCodes = result.Dto.Lines.Select(l => l.ItemCode).Distinct().ToList();
            var existingItems = await context.Items
                .Where(i => itemCodes.Contains(i.ItemCode))
                .Select(i => i.ItemCode)
                .ToListAsync();

            var missingItems = itemCodes.Except(existingItems).ToList();
            foreach (var missing in missingItems)
            {
                result.Errors.Add($"Item Code '{missing}' not found in Item Master.");
            }

            // Check LINE_WEIGHT_LBS presence
            foreach (var line in result.Dto.Lines)
            {
                if (!line.LineWeightPresent)
                {
                    result.Errors.Add($"Line {line.LineNumber}: Missing required field 'LINE_WEIGHT_LBS'.");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        public async Task ImportAsync(PickingListImportDto dto, int branchId, Dictionary<int, int> lineProductionAreaMap)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 1. Get or Create PickingList
                var pickingList = await context.PickingLists
                    .Include(pl => pl.Lines)
                    .ThenInclude(l => l.ReservedMaterials)
                    .FirstOrDefaultAsync(pl => pl.BranchId == branchId && pl.PickingListNumber == dto.PickingListNumber);

                if (pickingList == null)
                {
                    pickingList = new PickingList
                    {
                        BranchId = branchId,
                        PickingListNumber = dto.PickingListNumber
                    };
                    context.PickingLists.Add(pickingList);
                }

                // Update Header
                pickingList.PrintDate = dto.PrintDate;
                pickingList.ShipDate = dto.ShipDate;
                pickingList.Buyer = dto.Buyer;
                pickingList.SalesRep = dto.SalesRep;
                pickingList.ShipVia = dto.ShipVia;
                pickingList.SoldTo = dto.SoldTo;
                pickingList.ShipTo = dto.ShipTo;
                pickingList.OrderInstructions = dto.OrderInstructions;
                pickingList.TotalWeightLbs = dto.TotalWeightLbs;

                // Save header to get ID if new
                await context.SaveChangesAsync();

                // 2. Process Lines
                var itemCodes = dto.Lines.Select(l => l.ItemCode).Distinct().ToList();
                var items = await context.Items.Where(i => itemCodes.Contains(i.ItemCode)).ToDictionaryAsync(i => i.ItemCode, i => i.Id);

                foreach (var lineDto in dto.Lines)
                {
                    var line = pickingList.Lines.FirstOrDefault(l => l.LineNumber == lineDto.LineNumber);
                    if (line == null)
                    {
                        line = new PickingListLine
                        {
                            PickingListId = pickingList.Id,
                            LineNumber = lineDto.LineNumber
                        };
                        context.PickingListLines.Add(line);
                        pickingList.Lines.Add(line); // Ensure it's in the collection for next iteration if needed
                    }

                    // Update Line Fields
                    if (items.TryGetValue(lineDto.ItemCode, out int itemId))
                    {
                        line.ItemId = itemId;
                    }
                    line.ItemCode = lineDto.ItemCode;
                    line.Description = lineDto.Description;
                    line.OrderQuantity = lineDto.OrderQtyValue;
                    line.OrderUnit = lineDto.OrderQtyUnit;
                    line.WidthIn = lineDto.WidthIn;
                    line.LengthIn = lineDto.LengthIn;
                    line.LineWeightLbs = lineDto.LineWeightLbs;
                    line.LineInstructions = lineDto.LineInstructions;

                    // Production Area
                    if (lineProductionAreaMap.TryGetValue(lineDto.LineNumber, out int paId))
                    {
                        line.ProductionAreaId = paId;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Production Area not assigned for Line {lineDto.LineNumber}");
                    }

                    // Determine Type (Coil/Sheet) based on Unit
                    // LBS -> Coil, PCS -> Sheet
                    line.LineType = lineDto.OrderQtyUnit.ToUpper() == "PCS" ? PickingListLineType.Sheet : PickingListLineType.Coil;

                    // 3. Reserved Materials
                    if (line.ReservedMaterials == null) line.ReservedMaterials = new List<PickingListLineReservedMaterial>();

                    line.ReservedMaterials.Clear(); // Delete existing

                    foreach (var rmDto in lineDto.ReservedMaterials)
                    {
                        line.ReservedMaterials.Add(new PickingListLineReservedMaterial
                        {
                            TagNumber = rmDto.TagNumber,
                            MillRef = rmDto.MillRef,
                            Quantity = rmDto.Quantity,
                            Unit = rmDto.Unit,
                            Size = rmDto.Size,
                            Location = rmDto.Location
                        });
                    }
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public IQueryable<PickingList> GetPickingLists(IDbContextFactory<ApplicationDbContext> factory, int branchId)
        {
            var context = factory.CreateDbContext();
            return context.PickingLists
                .Where(pl => pl.BranchId == branchId)
                .OrderByDescending(pl => pl.CreatedAt);
        }
    }
}
