using System.Net.Http.Json;
using MetalFlowSystemV2.Client.Models;

namespace MetalFlowSystemV2.Client.Services
{
    public class PickingListClientService
    {
        private readonly HttpClient _http;

        public PickingListClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<PickingListSummaryDto>> GetPickingListsAsync(int branchId, string? status = null)
        {
            var url = $"api/pickinglists?branchId={branchId}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }
            return await _http.GetFromJsonAsync<List<PickingListSummaryDto>>(url) ?? new List<PickingListSummaryDto>();
        }

        public async Task<PickingListDetailDto?> GetPickingListDetailAsync(int id)
        {
             try
            {
                return await _http.GetFromJsonAsync<PickingListDetailDto>($"api/pickinglists/{id}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
