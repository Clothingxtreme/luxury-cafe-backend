using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MaisonGlace.API.Models;

public class Booking
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    // ── Step 1: Booking Details ──────────────────────────────────────────
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Guests { get; set; } = string.Empty;
    public string SeatType { get; set; } = string.Empty;

    // ── Step 2: Allergies ────────────────────────────────────────────────
    public List<string> Allergens { get; set; } = [];
    public string DietaryPreference { get; set; } = string.Empty;
    public string AllergyNotes { get; set; } = string.Empty;

    // ── Step 3: Menu ─────────────────────────────────────────────────────
    public List<string> Appetizers { get; set; } = [];
    public List<string> MainCourse { get; set; } = [];
    public List<string> Desserts { get; set; } = [];
    public List<string> NonAlcoholic { get; set; } = [];
    public List<string> Alcoholic { get; set; } = [];
    public string ComplimentaryDish { get; set; } = string.Empty;
    public decimal PreOrderTotal { get; set; } = 0;

    // ── Step 4: Final Details ─────────────────────────────────────────────
    public string ReserveCar { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string SpecialRequests { get; set; } = string.Empty;

    // ── Admin-managed fields ──────────────────────────────────────────────
    public string Status { get; set; } = "confirmed";
    public bool IsDeleted { get; set; } = false;
    public string AdminNotes { get; set; } = string.Empty;
    public List<ReceiptItem> ReceiptItems { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ReceiptItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
