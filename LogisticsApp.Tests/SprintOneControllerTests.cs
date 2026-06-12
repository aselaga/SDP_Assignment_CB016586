using LogisticsApp.Api.Controllers;
using LogisticsApp.Api.Data;
using LogisticsApp.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static LogisticsApp.Api.Controllers.SprintOneController;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace LogisticsApp.Tests
{
    public class SprintOneControllerTests
    {

        [Fact]
        public async Task UpdateStatus_UpdatesShipment_And_GeneratesAuditLog()
        {
            // --- 1. ARRANGE ---
            // Create a fresh, empty in-memory database for this specific test
            var options = new DbContextOptionsBuilder<LogisticsContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Seed the database with a test shipment
            using (var setupContext = new LogisticsContext(options))
            {
                setupContext.Shipments.Add(new Shipment
                {
                    TrackingId = "TRK-125478",
                    CurrentStatus = "In Transit",
                    CustomerName= "Test Customer",
                    CustomerPhone = "1234567890",
                    DeliveryAddress = "123 Test St,Colombo",
                });
                await setupContext.SaveChangesAsync();
            }

            // --- 2. ACT ---
            using (var context = new LogisticsContext(options))
            {
                // Instantiate the controller (Adjust the name if yours is SprintOneController)
                // Build an empty, dummy configuration just to satisfy the constructor
                var configuration = new ConfigurationBuilder().Build();

                // Instantiate the controller with BOTH the context and the dummy config
                var controller = new SprintOneController(context, configuration);

                // Simulate the payload coming from the Driver Portal
                var request = new UpdateStatusRequest
                {
                    TrackingId = "TRK-125478",
                    NewStatus = "Delivered"
                };

                // Execute the API endpoint
                await controller.UpdateStatus(request);
            }

            // --- 3. ASSERT ---
            using (var assertContext = new LogisticsContext(options))
            {
                // Verify the Shipment's status actually changed in the database
                var updatedShipment = await assertContext.Shipments.FirstOrDefaultAsync(s => s.TrackingId == "TRK-125478");
                Assert.NotNull(updatedShipment);
                Assert.Equal("Delivered", updatedShipment.CurrentStatus);

                // Verify the system automatically generated the Audit Log
                var logEntry = await assertContext.ShipmentLogs.FirstOrDefaultAsync(l => l.TrackingId == "TRK-125478");
                Assert.NotNull(logEntry);
                Assert.Equal("Status Updated", logEntry.Action);
                Assert.Contains("Delivered", logEntry.Details);
            }
        }


    }
}
