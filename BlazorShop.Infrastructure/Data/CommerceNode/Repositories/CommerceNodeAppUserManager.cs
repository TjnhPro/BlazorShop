namespace BlazorShop.Infrastructure.Data.CommerceNode.Repositories
{
    using System.Security.Claims;

    using BlazorShop.Domain.Contracts.Authentication;
    using BlazorShop.Domain.Entities.Identity;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public sealed class CommerceNodeAppUserManager : IAppUserManager
    {
        private readonly CommerceNodeDbContext context;
        private readonly IAppRoleManager roleManager;
        private readonly SignInManager<AppUser> signInManager;
        private readonly UserManager<AppUser> userManager;

        public CommerceNodeAppUserManager(
            IAppRoleManager roleManager,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            CommerceNodeDbContext context)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.context = context;
        }

        public async Task<bool> CreateUserAsync(AppUser user)
        {
            var currentUser = await this.GetUserByEmailAsync(user.Email!);
            if (currentUser is not null)
            {
                return false;
            }

            var result = await this.userManager.CreateAsync(user, user.PasswordHash!);
            return result.Succeeded;
        }

        public async Task<UserLoginResult> LoginUserAsync(AppUser user)
        {
            var currentUser = await this.GetUserByEmailAsync(user.Email!);
            if (currentUser is null)
            {
                return new UserLoginResult(false);
            }

            var roleName = await this.roleManager.GetUserRoleAsync(user.Email!);
            if (string.IsNullOrEmpty(roleName))
            {
                return new UserLoginResult(false);
            }

            var result = await this.signInManager.CheckPasswordSignInAsync(
                currentUser,
                user.PasswordHash!,
                lockoutOnFailure: true);

            return new UserLoginResult(result.Succeeded, result.IsLockedOut, result.IsNotAllowed);
        }

        public async Task<AppUser?> GetUserByEmailAsync(string email)
        {
            return await this.userManager.FindByEmailAsync(email);
        }

        public async Task<AppUser?> GetUserByIdAsync(string id)
        {
            return await this.userManager.FindByIdAsync(id);
        }

        public async Task<IEnumerable<AppUser?>> GetAllUsersAsync()
        {
            return await this.context.Users.ToListAsync();
        }

        public async Task<int> RemoveUserByEmail(string email)
        {
            var user = await this.context.Users.FirstOrDefaultAsync(candidate => candidate.Email == email);
            if (user is null)
            {
                return 0;
            }

            this.context.Users.Remove(user);
            return await this.context.SaveChangesAsync();
        }

        public async Task<List<Claim>> GetUserClaimsAsync(string email)
        {
            var user = await this.GetUserByEmailAsync(email);
            var roleName = await this.roleManager.GetUserRoleAsync(email);

            return
            [
                new Claim("FullName", user!.FullName),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Role, roleName!)
            ];
        }

        public async Task<bool> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword)
        {
            var result = await this.userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result.Succeeded;
        }

        public async Task<bool> CheckPasswordAsync(AppUser user, string password)
        {
            return await this.userManager.CheckPasswordAsync(user, password);
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(AppUser user)
        {
            return await this.userManager.GenerateEmailConfirmationTokenAsync(user);
        }

        public async Task<bool> ConfirmEmailAsync(AppUser user, string token)
        {
            var result = await this.userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(AppUser user)
        {
            return await this.userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task<bool> ResetPasswordAsync(AppUser user, string token, string newPassword)
        {
            var result = await this.userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }

        public async Task<bool> UpdateUserAsync(string userId, string fullName, string email, string? phoneNumber)
        {
            var user = await this.userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return false;
            }

            user.FullName = fullName;
            user.Email = email;
            user.UserName = email;
            user.PhoneNumber = phoneNumber;

            var result = await this.userManager.UpdateAsync(user);
            return result.Succeeded;
        }
    }
}
