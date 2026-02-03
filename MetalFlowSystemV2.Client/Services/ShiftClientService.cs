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
                return await _http.GetFromJsonAsync<UserAssignmentDto>("api/shifts/my-assignment");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<CheckInResult> CheckInAsync(int branchId)
        {
            var response = await _http.PostAsJsonAsync("api/shifts/check-in", new CheckInRequest { BranchId = branchId });
            return await response.Content.ReadFromJsonAsync<CheckInResult>() ?? new CheckInResult { Success = false, Message = "Parsing error" };
        }
    }
}
