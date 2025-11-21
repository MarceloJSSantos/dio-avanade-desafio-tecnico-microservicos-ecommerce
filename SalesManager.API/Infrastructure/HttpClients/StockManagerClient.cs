using SalesManager.API.Application.DTOs;
using SalesManager.API.Application.Interfaces;

namespace SalesManager.API.Infrastructure.HttpClients
{
    public class StockManagerClient : IStockManagerClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StockManagerClient> _logger;

        public StockManagerClient(HttpClient httpClient, ILogger<StockManagerClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ProductStockInfoDTO?> GetProductStockAsync(int productId)
        {
            var relativePath = $"api/products/{productId}";

            try
            {
                _logger.LogInformation(">>> Requesting product stock. ProductId={ProductId}", productId);

                var response = await _httpClient.GetAsync(relativePath);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<ProductStockInfoDTO>();
                    return content;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(">>> Product not found in StockManager. ProductId={ProductId}", productId);
                    return null;
                }

                response.EnsureSuccessStatusCode(); // Isso lança HttpRequestException
                return null; // (Nunca chega aqui por causa da linha acima)
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, ">>> Error requesting StockManager. ProductId={ProductId}", productId);
                throw; // Repassa o erro
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> Resilience Error (Circuit Breaker/Timeout). ProductId={ProductId}", productId);
                throw; // Repassa o erro
            }
        }

        public async Task<bool> DecreaseStockAsync(int productId, int quantity)
        {
            quantity = quantity * -1;
            var relativePath = $"api/products/{productId}/stock";
            var content = new { transactionAmount = quantity };

            var request = new HttpRequestMessage(HttpMethod.Patch, relativePath)
            {
                Content = JsonContent.Create(content)
            };

            return await SendRequestWithResilience(request, productId, "Decrease");
        }

        public async Task<bool> IncreaseStockAsync(int productId, int quantity)
        {
            var relativePath = $"api/products/{productId}/stock";
            var content = new { transactionAmount = quantity };

            var request = new HttpRequestMessage(HttpMethod.Patch, relativePath)
            {
                Content = JsonContent.Create(content)
            };

            return await SendRequestWithResilience(request, productId, "Increase");
        }

        private async Task<bool> SendRequestWithResilience(HttpRequestMessage request, int productId, string operation)
        {
            try
            {
                _logger.LogInformation(">>> Requesting {Operation} stock. ProductId={ProductId}", operation, productId);
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(">>> Failed to {Operation} stock. ProductId={ProductId} Status={Status}", operation, productId, response.StatusCode);
                    return false;
                }

                _logger.LogInformation(">>> {Operation} stock success. ProductId={ProductId}", operation, productId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> CRITICAL FAILURE: StockManager is unreachable or failing consistently during {Operation}. ProductId={ProductId}", operation, productId);
                return false;
            }
        }
    }
}