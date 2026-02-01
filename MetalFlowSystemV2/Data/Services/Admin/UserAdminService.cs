using MetalFlowSystemV2.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MetalFlowSystemV2.Data.Services.Admin
{
    public class UserBranchDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class UserAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserAdminService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<ApplicationUser?> GetUserWithBranchesAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.UserBranches)
                .ThenInclude(ub => ub.Branch)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<List<IdentityRole>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }

        public async Task<(IdentityResult Result, ApplicationUser? User)> CreateUserAsync(ApplicationUser user, string password, List<UserBranchDto> branches)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return (result, null);
            }

            // Assign Branches
            if (branches != null && branches.Any())
            {
                foreach (var branchDto in branches)
                {
                    var userBranch = new UserBranch
                    {
                        UserId = user.Id,
                        BranchId = branchDto.BranchId,
                        RoleId = branchDto.RoleId
                    };
                    _context.UserBranches.Add(userBranch);
                }
                await _context.SaveChangesAsync();
            }

            return (IdentityResult.Success, user);
        }

        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user, List<UserBranchDto> branches)
        {
            var existingUser = await _userManager.FindByIdAsync(user.Id);
            if (existingUser == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            existingUser.FullName = user.FullName;
            existingUser.Email = user.Email;
            existingUser.UserName = user.Email; // Keep UserName synced with Email

            var result = await _userManager.UpdateAsync(existingUser);
            if (!result.Succeeded)
            {
                return result;
            }

            // Update Branches
            var currentBranches = await _context.UserBranches
                .Where(ub => ub.UserId == user.Id)
                .ToListAsync();

            // Remove branches not in new list
            var branchesToRemove = currentBranches
                .Where(cb => !branches.Any(nb => nb.BranchId == cb.BranchId))
                .ToList();

            if (branchesToRemove.Any())
            {
                _context.UserBranches.RemoveRange(branchesToRemove);
            }

            // Add or Update branches
            foreach (var branchDto in branches)
            {
                var existingBranch = currentBranches.FirstOrDefault(cb => cb.BranchId == branchDto.BranchId);
                if (existingBranch != null)
                {
                    // Update Role if changed
                    if (existingBranch.RoleId != branchDto.RoleId)
                    {
                        existingBranch.RoleId = branchDto.RoleId;
                        _context.UserBranches.Update(existingBranch);
                    }
                }
                else
                {
                    // Add new
                    var newBranch = new UserBranch
                    {
                        UserId = user.Id,
                        BranchId = branchDto.BranchId,
                        RoleId = branchDto.RoleId
                    };
                    _context.UserBranches.Add(newBranch);
                }
            }

            await _context.SaveChangesAsync();
            return IdentityResult.Success;
        }

        // Method to get branch assignments as DTOs for the UI
        public async Task<List<UserBranchDto>> GetUserBranchDtosAsync(string userId)
        {
             var userBranches = await _context.UserBranches
                .Include(ub => ub.Branch)
                .Where(ub => ub.UserId == userId)
                .ToListAsync();

            // We need role names.
            // Since we stored RoleId, we can fetch roles.
            var roles = await _roleManager.Roles.ToDictionaryAsync(r => r.Id, r => r.Name);

            return userBranches.Select(ub => new UserBranchDto
            {
                BranchId = ub.BranchId,
                BranchName = ub.Branch.Name,
                RoleId = ub.RoleId,
                RoleName = roles.ContainsKey(ub.RoleId) ? roles[ub.RoleId]! : "Unknown"
            }).ToList();
        }
    }
}
