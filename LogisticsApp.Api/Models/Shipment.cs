

using System.ComponentModel.DataAnnotations;



namespace LogisticsApp.Api.Models
{
    public class Shipment
    {
        [Key]
        public string TrackingId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string DeliveryAddress { get; set; }
        public string CurrentStatus { get; set; } // "Processing", "In Transit", "Delivered"
        public double CurrentLatitude { get; set; } 
        public double CurrentLongitude { get; set; }
        public DateTime EstimatedDelivery { get; set; }
        public string? AssignedDriverName { get; set; }
    }
}
