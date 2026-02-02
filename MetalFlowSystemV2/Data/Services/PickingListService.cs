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
        private readonly ApplicationDbContext _context;
        private readonly PickingListParser _parser;

        public PickingListService(ApplicationDbContext context, PickingListParser parser)
        {
            _context = context;
            _parser = parser;
        }

        public async Task<int?> ResolveBranchAsync(string userId)
        {
            // 1. Check for Active Branch (In a real app, this might be in a SessionService)
            // For now, we will rely on UserWorkAssignment if active, or UserBranch if single.

            // Check UserWorkAssignment (Active Shift)
            var activeAssignment = await _context.UserWorkAssignments
                .Include(a => a.Branch)
                .Where(a => a.UserId == userId && a.IsActive)
                .FirstOrDefaultAsync();

            if (activeAssignment != null)
            {
                return activeAssignment.BranchId;
            }

            // Check UserBranches
            var userBranches = await _context.UserBranches
                .Include(ub => ub.Branch)
                .Where(ub => ub.UserId == userId)
                .ToListAsync();

            if (userBranches.Count == 1)
            {
                return userBranches.First().BranchId;
            }

            // Multiple branches and no active assignment -> Block (Return null)
            // No branches -> Block
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

            var branch = await _context.Branches.FindAsync(branchId);
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
            var existingItems = await _context.Items
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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Get or Create PickingList
                var pickingList = await _context.PickingLists
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
                    _context.PickingLists.Add(pickingList);
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
                await _context.SaveChangesAsync();

                // 2. Process Lines
                var itemCodes = dto.Lines.Select(l => l.ItemCode).Distinct().ToList();
                var items = await _context.Items.Where(i => itemCodes.Contains(i.ItemCode)).ToDictionaryAsync(i => i.ItemCode, i => i.Id);

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
                        _context.PickingListLines.Add(line);
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
                    // Replace strategy: Remove all existing for this line, insert new
                    // Since we might have just created the line, check if it has ID.
                    // If it's new (Id=0), ReservedMaterials is empty.
                    // If existing, we need to clear.

                    // Need to save lines first to get Line IDs?
                    // EF Core handles graph updates if we modify the collection.

                    // If line is tracked, we can modify collection.
                    // line.ReservedMaterials might be null if not included, but we included it in query.
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

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public IQueryable<PickingList> GetPickingLists(int branchId)
        {
            return _context.PickingLists
                .Where(pl => pl.BranchId == branchId)
                .OrderByDescending(pl => pl.CreatedAt);
        }
    }
}
