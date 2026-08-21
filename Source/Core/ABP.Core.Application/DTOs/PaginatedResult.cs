using System.Text.Json.Serialization;

namespace ABP.Core.Application.DTOs
{
    public class PaginatedResult<T>
    {
        [JsonPropertyName("data")]
        public IEnumerable<T> Items { get; set; } = [];
        
        [JsonPropertyName("totalRecords")]
        public int TotalCount { get; set; }
        
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        
        [JsonIgnore]
        public bool HasPreviousPage => Page > 1;
        
        [JsonIgnore]
        public bool HasNextPage => Page < TotalPages;
    }
}
