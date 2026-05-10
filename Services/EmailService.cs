using MailKit.Net.Smtp;
using MimeKit;
using MaisonGlace.API.Models;
using MaisonGlace.API.Settings;
using Microsoft.Extensions.Options;

namespace MaisonGlace.API.Services;

public class EmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendBookingConfirmationAsync(Booking booking)
    {
        var html = BuildGuestEmailHtml(booking);
        await SendAsync(booking.Email, booking.Name,
            $"Booking Confirmation — {booking.ReferenceNumber}", html);
    }

    public async Task SendAdminNotificationAsync(Booking booking)
    {
        var html = BuildAdminEmailHtml(booking);
        await SendAsync(_settings.AdminEmail, "Maison Glacé Admin",
            $"New Booking: {booking.ReferenceNumber} — {booking.Name}", html);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.Username));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        // Accept self-signed / VPS certificates
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort,
            MailKit.Security.SecureSocketOptions.Auto);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static string SeatLabel(string seatType) => seatType switch
    {
        "indoor"  => "Indoor Dining",
        "outdoor" => "Outdoor Terrace",
        "private" => "Private Room",
        "bar"     => "Bar Seating",
        _         => seatType
    };

    private static string BuildGuestEmailHtml(Booking b)
    {
        var allPreOrders = b.Appetizers
            .Concat(b.MainCourse)
            .Concat(b.Desserts)
            .Concat(b.NonAlcoholic)
            .Concat(b.Alcoholic)
            .ToList();

        var preOrderRows = allPreOrders.Count > 0
            ? string.Join("", allPreOrders.Select(i =>
                $"<li style='display:flex;justify-content:space-between;padding:5px 0;border-bottom:1px solid rgba(255,255,255,0.05);color:#F8FAFC;font-size:13px;'><span>{i}</span></li>"))
            : "<li style='color:#9CA3AF;font-size:13px;'>No pre-orders selected</li>";

        var complimentaryBlock = !string.IsNullOrEmpty(b.ComplimentaryDish)
            ? $"""
              <div style='margin-top:16px;padding:12px 16px;border:1px solid rgba(74,222,128,0.2);border-radius:6px;background:rgba(74,222,128,0.05);display:flex;justify-content:space-between;align-items:center;'>
                <span style='color:#F8FAFC;font-size:13px;'>{b.ComplimentaryDish}</span>
                <span style='color:#4ade80;font-size:11px;font-weight:600;'>COMPLIMENTARY</span>
              </div>
              """
            : "";

        var totalBlock = b.PreOrderTotal > 0
            ? $"""
              <div style='margin-top:16px;padding-top:12px;border-top:2px solid rgba(212,175,55,0.3);display:flex;justify-content:space-between;align-items:center;'>
                <div>
                  <p style='margin:0;color:#F8FAFC;font-size:13px;font-weight:600;'>Pre-Order Estimate</p>
                  <p style='margin:4px 0 0;color:#9CA3AF;font-size:11px;'>Final bill settled on the day</p>
                </div>
                <span style='color:#D4AF37;font-size:22px;font-weight:300;'>${b.PreOrderTotal}</span>
              </div>
              """
            : "";

        var specialBlock = !string.IsNullOrWhiteSpace(b.SpecialRequests)
            ? $"""
              <div style='margin-top:20px;'>
                <p style='color:#9CA3AF;font-size:11px;letter-spacing:0.2em;text-transform:uppercase;margin:0 0 6px;'>Special Requests</p>
                <p style='color:#F8FAFC;font-size:13px;line-height:1.6;margin:0;'>{b.SpecialRequests}</p>
              </div>
              """
            : "";

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#0F172A;font-family:Georgia,serif;">
              <div style="max-width:600px;margin:40px auto;background:#111827;border:1px solid rgba(255,255,255,0.08);border-radius:12px;overflow:hidden;">

                <!-- Header -->
                <div style="background:#D4AF37;padding:36px;text-align:center;">
                  <h1 style="margin:0;font-size:26px;color:#0F172A;font-weight:300;letter-spacing:0.12em;">Maison Glacé</h1>
                  <p style="margin:6px 0 0;color:#0F172A;opacity:0.75;font-size:11px;letter-spacing:0.25em;text-transform:uppercase;">Booking Confirmation</p>
                </div>

                <!-- Body -->
                <div style="padding:40px;">
                  <p style="color:#9CA3AF;font-size:14px;margin-top:0;">Dear {b.Name},</p>
                  <p style="color:#F8FAFC;font-size:14px;line-height:1.7;">Thank you for choosing Maison Glacé. Your reservation has been confirmed. Please find your booking details below.</p>

                  <!-- Reference pill -->
                  <div style="background:rgba(212,175,55,0.08);border:1px solid rgba(212,175,55,0.25);border-radius:8px;padding:22px 24px;margin:24px 0;text-align:center;">
                    <p style="margin:0 0 4px;color:#D4AF37;font-size:11px;letter-spacing:0.25em;text-transform:uppercase;">Booking Reference</p>
                    <p style="margin:0;color:#F8FAFC;font-size:24px;font-weight:700;letter-spacing:0.1em;">{b.ReferenceNumber}</p>
                  </div>

                  <!-- Details table -->
                  <table style="width:100%;border-collapse:collapse;">
                    <tr><td style="color:#9CA3AF;font-size:13px;padding:9px 0;border-bottom:1px solid rgba(255,255,255,0.06);">Date</td><td style="color:#F8FAFC;font-size:13px;padding:9px 0;border-bottom:1px solid rgba(255,255,255,0.06);text-align:right;">{b.Date}</td></tr>
                    <tr><td style="color:#9CA3AF;font-size:13px;padding:9px 0;border-bottom:1px solid rgba(255,255,255,0.06);">Time</td><td style="color:#F8FAFC;font-size:13px;padding:9px 0;border-bottom:1px solid rgba(255,255,255,0.06);text-align:right;">{b.Time}</td></tr>
                    <tr><td style="color:#9CA3AF;font-size:13px;padding:9px 0;border-bottom:1px solid rgba(255,255,255,0.06);">Guests</td><td style="color:#F8FAFC;font-size:13px;padding:9px 0;border-bottom:1px solid rgba(255,255,255,0.06);text-align:right;">{b.Guests}</td></tr>
                    <tr><td style="color:#9CA3AF;font-size:13px;padding:9px 0;border-bottom:1px solid rgba(255,255,255,0.06);">Seating</td><td style="color:#F8FAFC;font-size:13px;padding:9px 0;border-bottom:1px solid rgba(255,255,255,0.06);text-align:right;">{SeatLabel(b.SeatType)}</td></tr>
                    <tr><td style="color:#9CA3AF;font-size:13px;padding:9px 0;">Parking</td><td style="color:#F8FAFC;font-size:13px;padding:9px 0;text-align:right;">{(b.ReserveCar == "yes" ? "Reserved" : "Not required")}</td></tr>
                  </table>

                  <!-- Pre-orders -->
                  {(allPreOrders.Count > 0 ? $"""
                  <div style="margin-top:24px;">
                    <p style="color:#9CA3AF;font-size:11px;letter-spacing:0.2em;text-transform:uppercase;margin:0 0 10px;">Pre-Ordered Items</p>
                    <ul style="margin:0;padding:0;list-style:none;">{preOrderRows}</ul>
                    {complimentaryBlock}
                    {totalBlock}
                  </div>
                  """ : complimentaryBlock != "" ? $"<div style='margin-top:24px;'><p style='color:#9CA3AF;font-size:11px;letter-spacing:0.2em;text-transform:uppercase;margin:0 0 10px;'>Chef's Compliment</p>{complimentaryBlock}</div>" : "")}

                  {specialBlock}

                  <!-- Footer note -->
                  <div style="margin-top:36px;padding-top:24px;border-top:1px solid rgba(255,255,255,0.07);">
                    <p style="color:#9CA3AF;font-size:13px;line-height:1.7;margin:0;">
                      To modify or cancel your reservation please contact us at least 24 hours in advance.<br><br>
                      We look forward to welcoming you.<br>
                      <strong style="color:#D4AF37;">Maison Glacé</strong>
                    </p>
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private static string BuildAdminEmailHtml(Booking b)
    {
        var allergenText = b.Allergens.Count > 0 ? string.Join(", ", b.Allergens) : "None";
        var preOrders = b.Appetizers.Concat(b.MainCourse).Concat(b.Desserts)
            .Concat(b.NonAlcoholic).Concat(b.Alcoholic).ToList();
        var preOrderText = preOrders.Count > 0 ? string.Join(", ", preOrders) : "None";

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="utf-8"></head>
            <body style="margin:0;padding:40px;background:#0F172A;font-family:sans-serif;">
              <div style="max-width:600px;margin:0 auto;background:#111827;border:1px solid rgba(255,255,255,0.08);border-radius:12px;padding:32px;">
                <div style="display:flex;align-items:center;gap:12px;margin-bottom:24px;">
                  <div style="width:10px;height:10px;border-radius:50%;background:#D4AF37;"></div>
                  <h2 style="margin:0;color:#D4AF37;font-size:18px;font-weight:500;">New Booking — {b.ReferenceNumber}</h2>
                </div>
                <table style="width:100%;border-collapse:collapse;">
                  <tr><td style="color:#9CA3AF;font-size:13px;padding:7px 0;width:140px;">Guest</td><td style="color:#F8FAFC;font-size:13px;padding:7px 0;">{b.Name}</td></tr>
                  <tr><td style="color:#9CA3AF;font-size:13px;padding:7px 0;">Email</td><td style="color:#F8FAFC;font-size:13px;padding:7px 0;">{b.Email}</td></tr>
                  <tr><td style="color:#9CA3AF;font-size:13px;padding:7px 0;">Phone</td><td style="color:#F8FAFC;font-size:13px;padding:7px 0;">{(string.IsNullOrEmpty(b.Phone) ? "—" : b.Phone)}</td></tr>
                  <tr><td style="color:#9CA3AF;font-size:13px;padding:7px 0;">Date & Time</td><td style="color:#F8FAFC;font-size:13px;padding:7px 0;">{b.Date} at {b.Time}</td></tr>
                  <tr><td style="color:#9CA3AF;font-size:13px;padding:7px 0;">Guests</td><td style="color:#F8FAFC;font-size:13px;padding:7px 0;">{b.Guests}</td></tr>
                  <tr><td style="color:#9CA3AF;font-size:13px;padding:7px 0;">Seating</td><td style="color:#F8FAFC;font-size:13px;padding:7px 0;">{b.SeatType}</td></tr>
                  <tr><td style="color:#9CA3AF;font-size:13px;padding:7px 0;">Parking</td><td style="color:#F8FAFC;font-size:13px;padding:7px 0;">{b.ReserveCar}</td></tr>
                  <tr><td style="color:#9CA3AF;font-size:13px;padding:7px 0;">Allergens</td><td style="color:#F8FAFC;font-size:13px;padding:7px 0;">{allergenText}</td></tr>
                  <tr><td style="color:#9CA3AF;font-size:13px;padding:7px 0;">Dietary</td><td style="color:#F8FAFC;font-size:13px;padding:7px 0;">{b.DietaryPreference}</td></tr>
                  <tr><td style="color:#9CA3AF;font-size:13px;padding:7px 0;">Pre-orders</td><td style="color:#F8FAFC;font-size:13px;padding:7px 0;">{preOrderText}</td></tr>
                  {(b.PreOrderTotal > 0 ? $"<tr><td style='color:#9CA3AF;font-size:13px;padding:7px 0;'>Pre-Order Estimate</td><td style='color:#D4AF37;font-size:13px;padding:7px 0;font-weight:600;'>${b.PreOrderTotal}</td></tr>" : "")}
                  {(!string.IsNullOrEmpty(b.ComplimentaryDish) ? $"<tr><td style='color:#9CA3AF;font-size:13px;padding:7px 0;'>Chef's Compliment</td><td style='color:#F8FAFC;font-size:13px;padding:7px 0;'>{b.ComplimentaryDish}</td></tr>" : "")}
                  {(!string.IsNullOrWhiteSpace(b.SpecialRequests) ? $"<tr><td style='color:#9CA3AF;font-size:13px;padding:7px 0;'>Special Requests</td><td style='color:#F8FAFC;font-size:13px;padding:7px 0;'>{b.SpecialRequests}</td></tr>" : "")}
                </table>
                <p style="color:#9CA3AF;font-size:12px;margin-top:24px;padding-top:16px;border-top:1px solid rgba(255,255,255,0.06);">
                  Submitted: {b.CreatedAt:dddd, dd MMMM yyyy HH:mm} UTC<br>
                  Log in to the admin portal to view or manage this booking.
                </p>
              </div>
            </body>
            </html>
            """;
    }
}
