using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProjectDefense.Core.Common;
using ProjectDefense.Core.Entities;

namespace ProjectDefense.Web.Data;

public class ApplicationUserManager : UserManager<ApplicationUser>
{
    public ApplicationUserManager(
        IUserStore<ApplicationUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IEnumerable<IUserValidator<ApplicationUser>> userValidators,
        IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<ApplicationUser>> logger)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
    }

    public override async Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
    {
        if (string.IsNullOrWhiteSpace(user.FullName))
        {
            user.FullName = user.Email ?? user.UserName ?? string.Empty;
        }

        var result = await base.CreateAsync(user, password);
        if (result.Succeeded && !await IsInRoleAsync(user, Roles.Lecturer))
        {
            await AddToRoleAsync(user, Roles.Student);
        }

        return result;
    }
}
