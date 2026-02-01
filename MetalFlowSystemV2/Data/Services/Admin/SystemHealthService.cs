using MetalFlowSystemV2.Data;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class SystemHealthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SystemHealthService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<SystemHealthData> GetSystemHealthAsync()
        {
            var branchCount = await _context.Branches.CountAsync();
            var productionAreaCount = await _context.ProductionAreas.CountAsync();
            var shiftCount = await _context.Shifts.CountAsync();
            var truckCount = await _context.Trucks.CountAsync();
            var dbProvider = _context.Database.ProviderName ?? "Unknown";

            return new SystemHealthData
            {
                EnvironmentName = _env.EnvironmentName,
                DatabaseProvider = dbProvider,
                BranchCount = branchCount,
                ProductionAreaCount = productionAreaCount,
                ShiftCount = shiftCount,
                TruckCount = truckCount
            };
        }
    }

    public class SystemHealthData
    {
        public string EnvironmentName { get; set; } = string.Empty;
        public string DatabaseProvider { get; set; } = string.Empty;
        public int BranchCount { get; set; }
        public int ProductionAreaCount { get; set; }
        public int ShiftCount { get; set; }
        public int TruckCount { get; set; }
    }
}
