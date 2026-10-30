using System.Text.Json.Serialization;

namespace APIHandler.Models.GroupReservation;

public class SubmitGroupPreOrderRequest
{
    [JsonPropertyName("reservationId")]
    public required string ReservationId { get; set; }
}

public class SubmitGroupPreOrderResponseItem
{
    [JsonPropertyName("dishId")]
    public required string DishId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}