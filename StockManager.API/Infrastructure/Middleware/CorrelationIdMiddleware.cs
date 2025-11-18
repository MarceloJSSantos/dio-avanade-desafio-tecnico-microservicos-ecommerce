namespace StockManager.API.Infrastructure.Middleware
{
    // middleware simples para garantir um CorrelationId por request via header X-Correlation-Id
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string HeaderName = "X-Correlation-Id";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
                context.Request.Headers[HeaderName] = correlationId;
            }

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(HeaderName))
                    context.Response.Headers.Append(HeaderName, correlationId.ToString());
                return Task.CompletedTask;
            });

            using (context.RequestServices.GetService(typeof(Microsoft.Extensions.Logging.ILoggerFactory)) is Microsoft.Extensions.Logging.ILoggerFactory lf
                   ? lf.CreateLogger("CorrelationScope").BeginScope(new System.Collections.Generic.Dictionary<string, object> { ["CorrelationId"] = correlationId })
                   : null)
            {
                await _next(context);
            }
        }
    }
}