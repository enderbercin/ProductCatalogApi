using System.Collections.Concurrent;

namespace ProductCatalogApi.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();
        private readonly int _maxRequests;
        private readonly TimeSpan _window;

        public RateLimitingMiddleware(RequestDelegate next, int maxRequests = 100, int windowMinutes = 1)
        {
            _next = next;
            _maxRequests = maxRequests;
            _window = TimeSpan.FromMinutes(windowMinutes);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientId(context);
            var endpoint = context.Request.Path.Value ?? string.Empty;

            // Rate limiting
            if (ShouldApplyRateLimit(endpoint))
            {
                if (!IsRequestAllowed(clientId, endpoint))
                {
                    context.Response.StatusCode = 429;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\": \"Rate limit exceeded. Please try again later.\"}");
                    return;
                }
            }

            await _next(context);
        }

        private string GetClientId(HttpContext context)
        {
            // IP adresi veya API key kullanarak client'ı tanımla
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool ShouldApplyRateLimit(string? endpoint)
        {
            if (string.IsNullOrEmpty(endpoint))
                return false;

            // POST endpoint'leri için rate limiti
            return endpoint.StartsWith("/api/products", StringComparison.OrdinalIgnoreCase) ||
                   endpoint.StartsWith("/api/orders", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsRequestAllowed(string clientId, string endpoint)
        {
            var key = $"{clientId}:{endpoint}";
            var now = DateTime.UtcNow;

            if (_rateLimitStore.TryGetValue(key, out var info))
            {
                // Window dışındaki istekleri temizle
                if (now - info.WindowStart > _window)
                {
                    info.RequestCount = 0;
                    info.WindowStart = now;
                }

                if (info.RequestCount >= _maxRequests)
                {
                    return false;
                }

                info.RequestCount++;
                return true;
            }

            // İlk istek
            _rateLimitStore.TryAdd(key, new RateLimitInfo
            {
                RequestCount = 1,
                WindowStart = now
            });

            return true;
        }
    }

    public class RateLimitInfo
    {
        public int RequestCount { get; set; }
        public DateTime WindowStart { get; set; }
    }
} 