using LogisticsApp.Api.Data;
using LogisticsApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LogisticsApp.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class SprintOneController : ControllerBase
    {
        private readonly LogisticsContext _context;
        private readonly IConfiguration _configuration;

        public SprintOneController(LogisticsContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Endpoint 1: Customer Shipment Tracking
        [HttpGet("tracking/{trackingId}")]
        public async Task<ActionResult<Shipment>> GetShipment(string trackingId)
        {
            var shipment = await _context.Shipments.FindAsync(trackingId);
            if (shipment == null) return NotFound(new { message = "Invalid Tracking ID" });
            return Ok(shipment);
        }

        // Endpoint 2: Dispatcher Shipment Logs (Audit Trail)
        [Authorize(Roles = "Dispatcher")]
        [HttpGet("dispatcher/shipment-logs/{trackingId}")]
        public async Task<ActionResult<IEnumerable<ShipmentLog>>> GetShipmentLogs(string trackingId)
        {
            var logs = await _context.ShipmentLogs
                .Where(log => log.TrackingId == trackingId)
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync();

            return Ok(logs);
        }

        // Endpoint 3: Universal Auth Login
        [HttpPost("auth/login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginData)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginData.Email && u.Password == loginData.Password);

            if (user == null) return Unauthorized(new { message = "Invalid credentials." });

            
            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.Email),
        new Claim("UserId", user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Role, user.Role)
    };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds
            );

            var jwtString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new { Token = jwtString, user.FullName, user.Role });
        }

        // Endpoint 4: Dispatcher Dashboard Data
        [Authorize(Roles = "Dispatcher,Driver")]
        [HttpGet("dispatcher/active-shipments")]
        public async Task<ActionResult<IEnumerable<Shipment>>> GetActiveShipments()
        {
          
            return await _context.Shipments
                .Where(s => s.CurrentStatus != "Delivered")
                .ToListAsync();
        }

        // Endpoint 5: Driver updates the shipment status
        [Authorize(Roles = "Dispatcher,Driver")]
        [HttpPut("driver/update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.TrackingId == request.TrackingId);
            if (shipment == null) return NotFound(new { message = "Shipment not found." });

            string oldStatus = shipment.CurrentStatus;
            shipment.CurrentStatus = request.NewStatus;

            
            _context.ShipmentLogs.Add(new ShipmentLog
            {
                TrackingId = shipment.TrackingId,
                Action = "Status Updated",
                Details = $"Status changed from '{oldStatus}' to '{request.NewStatus}'",

                //PerformedBy = User.Identity.Name ?? "Driver",
                PerformedBy = "Driver",

                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Status updated successfully." });
        }

        public class UpdateStatusRequest
        {
            public string TrackingId { get; set; }
            public string NewStatus { get; set; }
        }



        // Endpoint 6: Generate Performance Report
        [Authorize(Roles = "Manager,Dispatcher")]
        [HttpGet("reports/performance")]
        public async Task<IActionResult> GetPerformanceReport()
        {
            var totalShipments = await _context.Shipments.CountAsync();
            var deliveredCount = await _context.Shipments.CountAsync(s => s.CurrentStatus == "Delivered");
            var delayedCount = await _context.Shipments.CountAsync(s => s.CurrentStatus.Contains("Delayed"));
            var inTransitCount = await _context.Shipments.CountAsync(s => s.CurrentStatus == "In Transit" || s.CurrentStatus == "Out for Delivery");

            double successRate = totalShipments > 0 ? ((double)deliveredCount / totalShipments) * 100 : 0;

            var report = new
            {
                TotalProcessed = totalShipments,
                SuccessfullyDelivered = deliveredCount,
                CurrentlyDelayed = delayedCount,
                ActiveInTransit = inTransitCount,
                DeliverySuccessRate = Math.Round(successRate, 1) 
            };

            return Ok(report);
        }

        // Endpoint 7: QuickBooks Sync Trigger (Mock Integration)
        [Authorize(Roles = "Manager")]
        [HttpPost("integrations/quickbooks/sync")]
        public async Task<IActionResult> SyncWithAccounting()
        {
            
            var pendingInvoices = await _context.Shipments
                .Where(s => s.CurrentStatus == "Delivered")
                .ToListAsync();

            
            await Task.Delay(1500);

            return Ok(new { message = $"Successfully synced {pendingInvoices.Count} records to QuickBooks." });
        }

        // Endpoint 8: Dispatcher assigns a driver to a shipment
        [Authorize(Roles = "Dispatcher")]
        [HttpPut("dispatcher/assign-driver")]
        public async Task<IActionResult> AssignDriver([FromBody] AssignDriverRequest request)
        {
            var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.TrackingId == request.TrackingId);
            if (shipment == null) return NotFound();

            string oldDriver = shipment.AssignedDriverName ?? "Unassigned";
            shipment.AssignedDriverName = request.DriverName;
            shipment.CurrentStatus = "Assigned to Driver";

            _context.ShipmentLogs.Add(new ShipmentLog
            {
                TrackingId = shipment.TrackingId,
                Action = "Driver Assigned",
                Details = $"Reassigned from {oldDriver} to {request.DriverName}",
                PerformedBy = User.Identity.Name,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        // Endpoint 9: Create a new shipment (Order Entry)
        [Authorize(Roles = "Dispatcher")]
        [HttpPost("dispatcher/add-shipment")]
        public async Task<IActionResult> AddShipment([FromBody] CreateShipmentRequest request)
        {
            
            string uniqueId = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            string newTrackingId = $"TRK-{uniqueId}";

            var newShipment = new Shipment
            {
                TrackingId = newTrackingId,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                DeliveryAddress = request.DeliveryAddress,
                CurrentStatus = "Processing", 
                //AssignedDriverName = ""
            };

            _context.Shipments.Add(newShipment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Shipment created successfully.",
                trackingId = newTrackingId,
                shipment = newShipment
            });
        }
        // Endpoint 10: Dispatcher reschedules a delivery
        [Authorize(Roles = "Dispatcher")]
        [HttpPut("dispatcher/reschedule")]
        public async Task<IActionResult> RescheduleDelivery([FromBody] RescheduleRequest request)
        {
            var shipment = await _context.Shipments.FindAsync(request.TrackingId);
            if (shipment == null)
            {
                return NotFound(new { message = "Shipment not found." });
            }

            shipment.EstimatedDelivery = request.NewDate;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Delivery date updated successfully.", shipment });
        }
        [Authorize(Roles = "Manager,Dispatcher")]
        [HttpGet("manager/all-shipments")]

        public async Task<ActionResult<IEnumerable<Shipment>>> GetAllShipmentsForReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.Shipments.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(s => s.EstimatedDelivery >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.EstimatedDelivery <= endOfDay);
            }

            var shipments = await query.OrderByDescending(s => s.TrackingId).ToListAsync();

            return Ok(shipments);
        }

        public class CreateShipmentRequest
        {
            public string CustomerName { get; set; }
            public string DeliveryAddress { get; set; }
            public string CustomerPhone { get; set; }
        }

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
        }
        public class AssignDriverRequest
        {
            public string TrackingId { get; set; }
            public string DriverName { get; set; }
        }

        public class StatusUpdateRequest
        {
            public string TrackingId { get; set; }
            public string NewStatus { get; set; }
        }

        public class InstructionRequest
        {
            public string Instructions { get; set; }
        }
        public class RescheduleRequest
        {
            public string TrackingId { get; set; }
            public DateTime NewDate { get; set; }
        }
    }
}
