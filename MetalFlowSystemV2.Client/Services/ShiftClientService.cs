using System.Net.Http.Json;
using MetalFlowSystemV2.Client.Models;

namespace MetalFlowSystemV2.Client.Services
{
    public class ShiftClientService
    {
        private readonly HttpClient _http;

        public ShiftClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<UserAssignmentDto?> GetMyAssignmentAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<UserAssignmentDto>("api/shifts/assignment");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<bool> CheckInAsync()
        {
            var response = await _http.PostAsync("api/shifts/checkin", null);
            return response.IsSuccessStatusCode;
        }
    }
}
