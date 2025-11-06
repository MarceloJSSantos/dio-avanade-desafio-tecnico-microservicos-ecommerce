using SalesManager.API.Application.DTOs;
using SalesManager.API.Application.Interfaces;

namespace SalesManager.API.Infrastructure.HttpClients
{
    public class StockManagerClient : IStockManagerClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _stockManagerBaseUrl; // Vem do appsettings.json

        public StockManagerClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _stockManagerBaseUrl = configuration["ServiceUrls:StockManager"]
                                        ?? throw new InvalidOperationException("A URL base do StockManager não foi configurada.");
        }

        public async Task<ProductStockInfoDTO?> GetProductStockAsync(int productId)
        {
            // Ex: GET http://stockmanager.api/api/products/{productId}/stock
            var endPointFull = $"{_stockManagerBaseUrl}api/products/{productId}";
            var response = await _httpClient.GetAsync(endPointFull);

            if (!response.IsSuccessStatusCode)
            {
                return null; // ou lançar exceção
            }

            return await response.Content.ReadFromJsonAsync<ProductStockInfoDTO>();
        }

        public async Task<bool> DecreaseStockAsync(int productId, int quantity)
        {
            quantity = quantity * -1; //(Inverte o sinal para decrementar)

            // Ex: PUT http://stockmanager.api/api/products/{productId}/stock
            var endPointFull = $"{_stockManagerBaseUrl}api/products/{productId}/stock";
            var content = new { transactionAmount = quantity };

            var request = new HttpRequestMessage(HttpMethod.Patch, endPointFull)
            {
                Content = JsonContent.Create(content)
            };

            var response = await _httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> IncreaseStockAsync(int productId, int quantity)
        {
            // Ex: PUT http://stockmanager.api/api/products/{productId}/stock
            var endPointFull = $"{_stockManagerBaseUrl}api/products/{productId}/stock";
            var content = new { transactionAmount = quantity };

            var request = new HttpRequestMessage(HttpMethod.Patch, endPointFull)
            {
                Content = JsonContent.Create(content)
            };

            var response = await _httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }

    }
}