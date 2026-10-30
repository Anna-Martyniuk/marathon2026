using APIHandler.Entities;
using System.Collections.Generic;

namespace APIHandler.Models.GroupReservation;

public class GetGroupSessionsSnapshotResponse
{
    public List<ParticipantDto> Participants { get; set; } = [];
    public decimal Total { get; set; }
}

public class ParticipantDto
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required string Name { get; set; }
    public List<ParticipantItemDto> Items { get; set; } = [];
    public decimal Total { get; set; }
    public bool IsSubmitted { get; set; }
}

public class ParticipantItemDto
{
    public required DishDto Dish { get; set; }
    public int Quantity { get; set; }
}

public class DishDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
}