using System.Security.Cryptography;
using System.Text;

namespace ProductCatalogApi.Middleware
{
    public class CsrfProtectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Dictionary<string, string> _csrfTokens = new();

        public CsrfProtectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // OPTIONS isteklerinde CSRF kontrolü yapmadan devam et
            if (context.Request.Method == "OPTIONS")
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value;

            // CSRF koruması sadece POST/PUT/DELETE istekleri için
            if (ShouldApplyCsrfProtection(context.Request.Method, path))
            {
                if (!await ValidateCsrfToken(context))
                {
                    context.Response.StatusCode = 403; // Forbidden
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\": \"CSRF token validation failed.\"}");
                    return;
                }
            }

            // CSRF token oluştur ve response header'a ekle
            if (context.Request.Method == "GET" && path?.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) == true)
            {
                var token = GenerateCsrfToken();
                var sessionId = GetSessionId(context);
                _csrfTokens[sessionId] = token;
                
                context.Response.Headers["X-CSRF-Token"] = token;
            }

            await _next(context);
        }

        private bool ShouldApplyCsrfProtection(string method, string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // POST/PUT/DELETE istekleri için CSRF koruması
            return (method == "POST" || method == "PUT" || method == "DELETE") &&
                   path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> ValidateCsrfToken(HttpContext context)
        {
            var sessionId = GetSessionId(context);
            var tokenFromHeader = context.Request.Headers["X-CSRF-Token"].FirstOrDefault();
            string? tokenFromForm = null;
            var contentType = context.Request.ContentType ?? "";
            if (contentType.Contains("application/x-www-form-urlencoded") || contentType.Contains("multipart/form-data"))
            {
                var form = await context.Request.ReadFormAsync();
                tokenFromForm = form["csrf_token"].FirstOrDefault();
            }
            var tokenFromQuery = context.Request.Query["csrf_token"].FirstOrDefault();

            var providedToken = tokenFromHeader ?? tokenFromForm ?? tokenFromQuery;

            if (string.IsNullOrEmpty(providedToken))
            {
                return false;
            }

            if (_csrfTokens.TryGetValue(sessionId, out var storedToken))
            {
                return providedToken == storedToken;
            }

            return false;
        }

        private string GetSessionId(HttpContext context)
        {
            // Geliştirme/test için sabit session id
            return "dev-session";
        }

        private string GenerateCsrfToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }
    }
} 