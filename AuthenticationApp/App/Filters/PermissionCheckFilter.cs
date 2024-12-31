using DataAccessLayer.Data;
using LMSApi.App.Atrributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace LMSApi.App.Filters
{
    public class PermissionCheckFilter(ILogger<PermissionCheckFilter> logger, AppDbContext appDbContext) : IAuthorizationFilter
    {
        public async void OnAuthorization(AuthorizationFilterContext context)
        {
            logger.LogInformation($"Endpoint metadata: {context.ActionDescriptor.EndpointMetadata}");
            var atrribute = context.ActionDescriptor.EndpointMetadata
                .FirstOrDefault(x => x is CheckPermissionAttribute) as CheckPermissionAttribute;

            if (atrribute != null)
            {
                var claimIdentity = context.HttpContext.User.Identity as ClaimsIdentity;
                if (claimIdentity == null || !claimIdentity.IsAuthenticated)
                {
                    context.Result = new ForbidResult();
                    return;
                }

                // Get the permission(s) required for the action
                var requiredPermissions = atrribute.permissionRouteName.Split(',');

                // Get the user's permissions from the claims
                var userPermissions = claimIdentity.Claims
                    .Where(c => c.Type == "permissions")
                    .Select(c => c.Value)
                    .SelectMany(permission => permission.Split(','))
                    .Distinct()
                    .ToList();

                // Check if any required permission is present in user's permissions
                if (!requiredPermissions.Any(permission => userPermissions.Contains(permission)))
                {
                    context.Result = new ForbidResult();
                }
            }
        }
    }
}
