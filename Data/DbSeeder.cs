using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectroScanAI.Models.Entities;
using ElectroScanAI.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Electroscann_ai.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ElectroscannDbContext context, IPasswordHasher<User> passwordHasher)
        {
            if (context.Database.IsSqlite())
            {
                await context.Database.EnsureCreatedAsync();
            }
            else
            {
                try
                {
                    await context.Database.MigrateAsync();
                }
                catch
                {
                    await context.Database.EnsureCreatedAsync();
                }
            }

            // Seed Admin
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@electroscann.ai");
            if (adminUser == null)
            {
                adminUser = new User
                {
                    FullName = "System Administrator",
                    Email = "admin@electroscann.ai",
                    PhoneNumber = "+92 300 1234567",
                    City = "Lahore",
                    Role = UserRole.Admin,
                    IsActive = true,
                    EmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                };
                adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");
                context.Users.Add(adminUser);
                await context.SaveChangesAsync();
            }

            // Seed Electrician User
            var elecUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "electrician@electroscann.ai");
            if (elecUser == null)
            {
                elecUser = new User
                {
                    FullName = "Muhammad Ali",
                    Email = "electrician@electroscann.ai",
                    PhoneNumber = "+92 321 9876543",
                    City = "Lahore",
                    Role = UserRole.Electrician,
                    IsActive = true,
                    EmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                };
                elecUser.PasswordHash = passwordHasher.HashPassword(elecUser, "Pass123!");
                context.Users.Add(elecUser);
                await context.SaveChangesAsync();

                var elecProfile = new Electrician
                {
                    UserId = elecUser.Id,
                    LicenseNumber = "ELEC-PK-98231",
                    Specialization = "Industrial & Residential Wiring",
                    YearsOfExperience = 8,
                    IsVerified = true,
                    Rating = 4.9,
                    TotalReviews = 14,
                    CompletedJobs = 22,
                    CreatedAt = DateTime.UtcNow
                };
                context.Electricians.Add(elecProfile);
                await context.SaveChangesAsync();
            }

            // Seed Company User
            var compUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "company@electroscann.ai");
            if (compUser == null)
            {
                compUser = new User
                {
                    FullName = "Apex Electrical Solutions",
                    Email = "company@electroscann.ai",
                    PhoneNumber = "+92 42 35789000",
                    City = "Karachi",
                    Role = UserRole.Company,
                    IsActive = true,
                    EmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                };
                compUser.PasswordHash = passwordHasher.HashPassword(compUser, "Pass123!");
                context.Users.Add(compUser);
                await context.SaveChangesAsync();

                var compProfile = new Company
                {
                    UserId = compUser.Id,
                    CompanyName = "Apex Electrical Engineering",
                    NTNNumber = "7492018-3",
                    Industry = "Commercial Electrical Contracting",
                    IsVerified = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Companies.Add(compProfile);
                await context.SaveChangesAsync();

                // Seed sample job
                var sampleJob = new Job
                {
                    CompanyId = compProfile.Id,
                    Title = "High Voltage Panel Upgrade & Wiring",
                    Description = "Require licensed electrician for upgrading main distribution panel (400A) and inspecting cable tray distribution.",
                    Budget = 85000,
                    Location = "Karachi Industrial Area",
                    Status = JobStatus.Open,
                    CreatedAt = DateTime.UtcNow
                };
                context.Jobs.Add(sampleJob);
                await context.SaveChangesAsync();
            }

            // Seed Client User
            var clientUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "client@electroscann.ai");
            if (clientUser == null)
            {
                clientUser = new User
                {
                    FullName = "Tariq Mahmood",
                    Email = "client@electroscann.ai",
                    PhoneNumber = "+92 333 4567890",
                    City = "Islamabad",
                    Role = UserRole.Client,
                    IsActive = true,
                    EmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                };
                clientUser.PasswordHash = passwordHasher.HashPassword(clientUser, "Pass123!");
                context.Users.Add(clientUser);
                await context.SaveChangesAsync();

                // Seed sample scan
                var scan = new AIScan
                {
                    ScanName = "Home DB Box Thermal Assessment",
                    UserId = clientUser.Id,
                    ImagePath = "/Image/about.jpg",
                    RiskLevel = ScanRiskLevel.Medium,
                    Status = "Completed",
                    ResultSummary = "Overheating detected on MCB #4 terminal. Insulation degradation likely due to loose clamping screw.",
                    CreatedAt = DateTime.UtcNow
                };
                context.AIScans.Add(scan);
                await context.SaveChangesAsync();

                context.ScanResults.Add(new ScanResult
                {
                    AIScanId = scan.Id,
                    DetectedIssue = "Terminal Overheating (68°C)",
                    Severity = "Medium",
                    Recommendation = "Tighten connection terminal screw immediately to prevent arching or melt-down."
                });

                // Seed sample subscription & payment
                var sub = new Subscription
                {
                    UserId = clientUser.Id,
                    Plan = SubscriptionPlan.Pro,
                    StartDate = DateTime.UtcNow.AddDays(-10),
                    EndDate = DateTime.UtcNow.AddDays(20),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                };
                context.Subscriptions.Add(sub);
                await context.SaveChangesAsync();

                context.Payments.Add(new Payment
                {
                    UserId = clientUser.Id,
                    Amount = 2999,
                    TransactionId = "TXN-2026-98124",
                    Status = PaymentStatus.Paid,
                    PaymentDate = DateTime.UtcNow.AddDays(-10),
                    PaymentMethod = "Credit Card / Visa",
                    SubscriptionId = sub.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                });

                await context.SaveChangesAsync();
            }

            // Seed Market Rates if empty (Task 3.6 & Part 1.4)
            if (!await context.MarketRates.AnyAsync())
            {
                var rates = new List<MarketRate>
                {
                    new MarketRate { ItemName = "Complete House Wiring (per sq ft)", Category = "Wiring", Price = 45, Unit = "sq ft", City = "Lahore" },
                    new MarketRate { ItemName = "Single Phase DB Box Installation", Category = "Distribution", Price = 3500, Unit = "job", City = "Lahore" },
                    new MarketRate { ItemName = "Three Phase DB Box Installation", Category = "Distribution", Price = 8500, Unit = "job", City = "Karachi" },
                    new MarketRate { ItemName = "AC Point Wiring (15A/20A)", Category = "Wiring", Price = 2200, Unit = "point", City = "Islamabad" },
                    new MarketRate { ItemName = "Ceiling Fan Installation & Regulator", Category = "Fixtures", Price = 600, Unit = "unit", City = "Lahore" },
                    new MarketRate { ItemName = "SMD / LED Downlight Fitting", Category = "Fixtures", Price = 250, Unit = "unit", City = "Rawalpindi" },
                    new MarketRate { ItemName = "Solar Inverter Wiring & Earthing", Category = "Solar", Price = 18000, Unit = "job", City = "Multan" },
                    new MarketRate { ItemName = "Short Circuit Troubleshooting & Repair", Category = "Repairs", Price = 2500, Unit = "visit", City = "Karachi" },
                    new MarketRate { ItemName = "Earthing Pit Installation (Copper Rod)", Category = "Earthing", Price = 12000, Unit = "pit", City = "Faisalabad" },
                    new MarketRate { ItemName = "Generator Changeover Switch Installation", Category = "Distribution", Price = 3000, Unit = "job", City = "Lahore" }
                };
                context.MarketRates.AddRange(rates);
                await context.SaveChangesAsync();
            }

            // Seed Contact Messages if empty
            if (!await context.ContactMessages.AnyAsync())
            {
                context.ContactMessages.Add(new ContactMessage
                {
                    Name = "Zain Ul Abideen",
                    Email = "zain@example.com",
                    Phone = "+92 312 0000000",
                    Subject = "Corporate Subscription Inquiry",
                    Message = "We would like to register 15 commercial electricians under our company subscription tier. Please contact us with group pricing.",
                    IsResolved = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
