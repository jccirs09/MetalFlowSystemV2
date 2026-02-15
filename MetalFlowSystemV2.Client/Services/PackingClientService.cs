using System.Net.Http.Json;
using MetalFlowSystemV2.Client.Dtos;

namespace MetalFlowSystemV2.Client.Services
{
    public class PackingClientService
    {
        private readonly HttpClient _http;

        public PackingClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<int> RecordPackingEventAsync(PackingEventDto dto)
        {
            var response = await _http.PostAsJsonAsync("api/packing", dto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<int>();
        }

        public async Task<List<PackingEventDto>> GetEventsForStationShiftAsync(int stationShiftId)
        {
            return await _http.GetFromJsonAsync<List<PackingEventDto>>($"api/packing/shift/{stationShiftId}")
                   ?? new List<PackingEventDto>();
        }
    }
}
