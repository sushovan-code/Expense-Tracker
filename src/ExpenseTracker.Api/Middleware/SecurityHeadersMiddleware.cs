namespace ExpenseTracker.Api.Middleware;

/// <summary>
/// Adds the baseline security headers to every response. The API only returns JSON, CSV and
/// problem details, so its content security policy allows nothing. The API reference pages
/// (Scalar and the OpenAPI document) are skipped: they load scripts and styles, and they're only
/// served where <c>Features:ApiDocs</c> is on. HSTS is added separately with <c>UseHsts</c>.
/// </summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
    private const string ReferrerPolicy = "no-referrer";
    private const string NoSniff = "nosniff";
    private const string DenyFraming = "DENY";

    private static readonly string[] ApiReferencePaths = ["/scalar", "/openapi"];

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = NoSniff;
        headers.XFrameOptions = DenyFraming;
        headers["Referrer-Policy"] = ReferrerPolicy;

        if (!IsApiReference(context.Request.Path))
        {
            headers.ContentSecurityPolicy = ContentSecurityPolicy;
        }

        return next(context);
    }

    private static bool IsApiReference(PathString path) =>
        ApiReferencePaths.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
}
