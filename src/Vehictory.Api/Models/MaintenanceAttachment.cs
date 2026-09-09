namespace Vehictory.Api.Models;

public class MaintenanceAttachment
{
    public int Id { get; set; }
    public required int MaintenanceEntryId { get; set; }
    public MaintenanceEntry? MaintenanceEntry { get; set; }

    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Content { get; set; }
    // Alleen gevuld voor afbeeldingen (niet voor PDF's); zie MaintenanceEntriesController.ReadAttachment.
    public byte[]? Thumbnail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
