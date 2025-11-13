using System.Net;
using System.Text.Json;
using System.Data.Common;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SalesManager.API.Infrastructure.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (KeyNotFoundException knf)
            {
                await HandleExceptionAsync(httpContext, knf, "Not Found", knf.Message, StatusCodes.Status404NotFound, LogLevel.Warning);
            }
            catch (InvalidOperationException ioe)
            {
                await HandleExceptionAsync(httpContext, ioe, "Bad Request", ioe.Message, StatusCodes.Status400BadRequest, LogLevel.Warning);
            }
            catch (DbUpdateConcurrencyException concEx)
            {
                await HandleExceptionAsync(httpContext, concEx, "Conflict", "Conflito de concorrência: recurso foi alterado por outro processo.", StatusCodes.Status409Conflict, LogLevel.Warning);
            }
            catch (DbException dbEx)
            {
                await HandleExceptionAsync(httpContext, dbEx, "Service Unavailable", "Erro ao acessar o banco de dados. Tente novamente mais tarde.", StatusCodes.Status503ServiceUnavailable, LogLevel.Error, addRetryAfter: true);
            }
            catch (SocketException sockEx)
            {
                await HandleExceptionAsync(httpContext, sockEx, "Service Unavailable", "Erro de conectividade de rede. Tente novamente mais tarde.", StatusCodes.Status503ServiceUnavailable, LogLevel.Error, addRetryAfter: true);
            }
            catch (TimeoutException tex)
            {
                await HandleExceptionAsync(httpContext, tex, "Service Unavailable", "Tempo esgotado ao acessar dependência externa. Tente novamente mais tarde.", StatusCodes.Status503ServiceUnavailable, LogLevel.Error, addRetryAfter: true);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex, "Internal Server Error", "Ocorreu um erro interno.", StatusCodes.Status500InternalServerError, LogLevel.Error);
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext httpContext,
            Exception ex,
            string title,
            string detail,
            int statusCode,
            LogLevel logLevel,
            bool addRetryAfter = false)
        {
            _logger.Log(logLevel, ex, "{Title}: {Message}", title, ex.Message);

            if (httpContext.Response.HasStarted)
                return;

            var pd = new ProblemDetails
            {
                Title = title,
                Detail = detail ?? ex.Message,
                Status = statusCode
            };

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            if (addRetryAfter)
            {
                httpContext.Response.Headers["Retry-After"] = "30";
            }

            var json = JsonSerializer.Serialize(pd);
            await httpContext.Response.WriteAsync(json);
        }
    }
}