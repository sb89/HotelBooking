using System.Text.Json.Serialization;
using Domain.Entities;

namespace Web.DTOs.HotelRooms;

public class SearchAvailableResult
{
    public required int HotelId { get; set; }

    public required int RoomId { get; set; }

    public required int RoomNumber { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required HotelRoomType HotelRoomType { get; set; }
}