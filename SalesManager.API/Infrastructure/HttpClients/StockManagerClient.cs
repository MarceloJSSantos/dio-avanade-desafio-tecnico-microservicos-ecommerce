using SalesManager.API.Application.DTOs;
using SalesManager.API.Application.Interfaces;

namespace SalesManager.API.Infrastructure.HttpClients
{
    public class StockManagerClient : IStockManagerClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _stockManagerBaseUrl; // Vem do appsettings.json
        private readonly ILogger<StockManagerClient> _logger;

        public StockManagerClient(HttpClient httpClient, IConfiguration configuration, ILogger<StockManagerClient> logger)
        {
            _httpClient = httpClient;
            _stockManagerBaseUrl = configuration["ServiceUrls:StockManager"]
                                        ?? throw new InvalidOperationException("A URL base do StockManager não foi configurada.");
            _logger = logger;
        }

        public async Task<ProductStockInfoDTO?> GetProductStockAsync(int productId)
        {
            var endPointFull = $"{_stockManagerBaseUrl}api/products/{productId}";
            _logger.LogInformation(">>> Requesting product stock from StockManager. ProductId={ProductId}", productId);
            var response = await _httpClient.GetAsync(endPointFull);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(">>> StockManager returned non-success for ProductId={ProductId} Status={Status}", productId, response.StatusCode);
                return null;
            }

            var content = response.Content.ReadFromJsonAsync<ProductStockInfoDTO>();

            _logger.LogDebug(">>> Received product stock for ProductId={ProductId}: {@StockInfo}", productId, content);
            return await content;
        }

        public async Task<bool> DecreaseStockAsync(int productId, int quantity)
        {
            quantity = quantity * -1; //(Inverte o sinal para decrementar)

            var endPointFull = $"{_stockManagerBaseUrl}api/products/{productId}/stock";
            var content = new { transactionAmount = quantity };

            var request = new HttpRequestMessage(HttpMethod.Patch, endPointFull)
            {
                Content = JsonContent.Create(content)
            };

            _logger.LogInformation(">>> Request to decrease stock ProductId={ProductId} Quantity={Quantity}", productId, quantity);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(">>> Failed to decrease stock ProductId={ProductId} Status={Status}", productId, response.StatusCode);
                return false;
            }

            _logger.LogInformation(">>> Decreased stock successfully ProductId={ProductId} Quantity={Quantity}", productId, quantity);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> IncreaseStockAsync(int productId, int quantity)
        {
            var endPointFull = $"{_stockManagerBaseUrl}api/products/{productId}/stock";
            var content = new { transactionAmount = quantity };

            var request = new HttpRequestMessage(HttpMethod.Patch, endPointFull)
            {
                Content = JsonContent.Create(content)
            };

            _logger.LogInformation(">>> Request to increase stock ProductId={ProductId} Quantity={Quantity}", productId, quantity);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(">>> Failed to increase stock ProductId={ProductId} Status={Status}", productId, response.StatusCode);
                return false;
            }

            _logger.LogInformation(">>> Increased stock successfully ProductId={ProductId} Quantity={Quantity}", productId, quantity);
            return response.IsSuccessStatusCode;
        }

    }
}