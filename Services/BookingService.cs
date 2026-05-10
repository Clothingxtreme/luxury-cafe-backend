using MongoDB.Driver;
using MaisonGlace.API.Models;

namespace MaisonGlace.API.Services;

public class BookingService
{
    private readonly IMongoCollection<Booking> _bookings;

    public BookingService(DatabaseContext db)
    {
        _bookings = db.GetCollection<Booking>("bookings");
    }

    public async Task<List<Booking>> GetAllAsync() =>
        await _bookings
            .Find(b => !b.IsDeleted)
            .SortByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<Booking?> GetByIdAsync(string id) =>
        await _bookings.Find(b => b.Id == id && !b.IsDeleted).FirstOrDefaultAsync();

    public async Task<Booking> CreateAsync(Booking booking)
    {
        booking.ReferenceNumber = GenerateReference();
        booking.CreatedAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookings.InsertOneAsync(booking);
        return booking;
    }

    public async Task<bool> UpdateAsync(string id, Booking updated)
    {
        updated.Id = id;
        updated.UpdatedAt = DateTime.UtcNow;
        var result = await _bookings.ReplaceOneAsync(b => b.Id == id && !b.IsDeleted, updated);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> SoftDeleteAsync(string id)
    {
        var update = Builders<Booking>.Update
            .Set(b => b.IsDeleted, true)
            .Set(b => b.Status, "cancelled")
            .Set(b => b.UpdatedAt, DateTime.UtcNow);
        var result = await _bookings.UpdateOneAsync(b => b.Id == id, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> AddReceiptItemAsync(string id, ReceiptItem item)
    {
        item.AddedAt = DateTime.UtcNow;
        var update = Builders<Booking>.Update
            .Push(b => b.ReceiptItems, item)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);
        var result = await _bookings.UpdateOneAsync(b => b.Id == id && !b.IsDeleted, update);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    private static string GenerateReference() =>
        $"MG-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
}
