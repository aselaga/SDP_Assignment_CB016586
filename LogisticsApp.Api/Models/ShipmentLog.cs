namespace LogisticsApp.Api.Models
{
    public class ShipmentLog
    {
        public int Id { get; set; }
        public string TrackingId { get; set; }
        public string Action { get; set; } 
        public string Details { get; set; }  
        public string PerformedBy { get; set; } 
        public DateTime Timestamp { get; set; }
    }
}