using System.Security.Claims;

namespace WebFashion.Middleware
{
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var authorizeAttribute = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>();

            if (authorizeAttribute != null)
            {
                var user = context.User;
                if (!user.Identity?.IsAuthenticated ?? true)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { message = "Unauthorized" });
                    return;
                }

                var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

                // 🔒 Check if admin-only endpoint
                if (context.Request.Path.StartsWithSegments("/api/Admin", StringComparison.OrdinalIgnoreCase))
                {
                    // ✅ Chỉ cấm nếu KHÔNG phải Admin VÀ KHÔNG phải 1
                    if (roleClaim != "Admin" && roleClaim != "1")
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { message = "Forbidden - Admin access only" });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute
    {
    }
}