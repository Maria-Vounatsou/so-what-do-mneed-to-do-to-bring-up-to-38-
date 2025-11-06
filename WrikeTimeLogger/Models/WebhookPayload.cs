using System.ComponentModel.DataAnnotations.Schema;

namespace WrikeTimeLogger.Models;

public class WebhookPayload
{
    public int id { get; set; }
    public string idempotencyKey { get; set; }
    public string? oldStatus { get; set; }
    public string? status { get; set; }
    public string? oldCustomStatusId { get; set; }
    public string? customStatusId { get; set; }
    public string? taskId { get; set; }
    public string? webhookId { get; set; }
    public string? eventAuthorId { get; set; }
    public string eventType { get; set; }
    public string? lastUpdatedDate { get; set; }
    [NotMapped]
    public string[]? addedResponsibles { get; set; }
    [NotMapped]
    public string? timeTrackerId { get; set; }
    [NotMapped]
    public string? type { get; set; }
    [NotMapped]
    public string? hours { get; set; }
    [NotMapped]
    public string[]? removedResponsibles { get; set; }
}

