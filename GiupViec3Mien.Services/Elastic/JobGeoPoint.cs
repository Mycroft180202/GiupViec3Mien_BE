using System.Text.Json.Serialization;

namespace GiupViec3Mien.Services.Elastic;

public record JobGeoPoint(
    [property: JsonPropertyName("lat")] double Lat, 
    [property: JsonPropertyName("lon")] double Lon
);

