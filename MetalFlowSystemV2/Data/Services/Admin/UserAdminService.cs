using MetalFlowSystemV2.Data;
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
        public bool IsDefault { get; set; }
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
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return (IdentityResult.Failed(new IdentityError { Description = "Email is required." }), null);
            }

            user.Email = user.Email.Trim();
            user.UserName = user.Email;

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return (result, null);
            }

            // Assign Branches
            if (branches != null && branches.Any())
            {
                var roleIds = new HashSet<string>();
                foreach (var branchDto in branches)
                {
                    var userBranch = new UserBranch
                    {
                        UserId = user.Id,
                        BranchId = branchDto.BranchId,
                        RoleId = branchDto.RoleId,
                        IsDefault = branchDto.IsDefault
                    };
                    _context.UserBranches.Add(userBranch);
                    roleIds.Add(branchDto.RoleId);
                }
                await _context.SaveChangesAsync();

                // Sync Identity Roles
                var allRoles = await _roleManager.Roles.ToListAsync();
                foreach (var roleId in roleIds)
                {
                    var role = allRoles.FirstOrDefault(r => r.Id == roleId);
                    if (role != null)
                    {
                        await _userManager.AddToRoleAsync(user, role.Name!);
                    }
                }
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
                    // Update fields if changed
                    bool changed = false;
                    if (existingBranch.RoleId != branchDto.RoleId)
                    {
                        existingBranch.RoleId = branchDto.RoleId;
                        changed = true;
                    }
                    if (existingBranch.IsDefault != branchDto.IsDefault)
                    {
                        existingBranch.IsDefault = branchDto.IsDefault;
                        changed = true;
                    }

                    if (changed)
                    {
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
                        RoleId = branchDto.RoleId,
                        IsDefault = branchDto.IsDefault
                    };
                    _context.UserBranches.Add(newBranch);
                }
            }

            await _context.SaveChangesAsync();

            // Sync Identity Roles
            // 1. Get all roles assigned across all branches
            var finalUserBranches = await _context.UserBranches.Where(ub => ub.UserId == user.Id).ToListAsync();
            var targetRoleIds = finalUserBranches.Select(ub => ub.RoleId).Distinct().ToList();

            var allRoles = await _roleManager.Roles.ToListAsync();
            var targetRoleNames = allRoles.Where(r => targetRoleIds.Contains(r.Id)).Select(r => r.Name!).ToList();

            // 2. Get current Identity Roles
            var currentIdentityRoles = await _userManager.GetRolesAsync(existingUser);

            // 3. Add missing
            var toAdd = targetRoleNames.Except(currentIdentityRoles).ToList();
            if (toAdd.Any())
            {
                await _userManager.AddToRolesAsync(existingUser, toAdd);
            }

            // 4. Remove extra (optional? logic dictates if user loses role in all branches, they lose it globally)
            var toRemove = currentIdentityRoles.Except(targetRoleNames).ToList();
            if (toRemove.Any())
            {
                // Be careful not to remove "Admin" if it was manually assigned,
                // but here we assume Roles are driven by Branch Assignments.
                await _userManager.RemoveFromRolesAsync(existingUser, toRemove);
            }

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
                RoleName = roles.ContainsKey(ub.RoleId) ? roles[ub.RoleId]! : "Unknown",
                IsDefault = ub.IsDefault
            }).ToList();
        }
    }
}
