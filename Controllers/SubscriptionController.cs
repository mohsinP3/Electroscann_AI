using System;
using System.Threading.Tasks;
using ElectroScanAI.Models.Entities;
using ElectroScanAI.Models.Enums;
using Electroscann_ai.Data;
using Electroscann_ai.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Electroscann_ai.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly ElectroscannDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.Extensions.Logging.ILogger<SubscriptionController> _logger;

        public SubscriptionController(ElectroscannDbContext context, INotificationService notificationService, Microsoft.Extensions.Logging.ILogger<SubscriptionController> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubscriptionPlan plan)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId) || userId <= 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var config = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
            var secretKey = config?["PaymentGateway:Stripe:SecretKey"];
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(StripeConfiguration.ApiKey))
            {
                _logger.LogWarning("Stripe Secret Key is missing. Subscription checkout aborted.");
                TempData["Error"] = "Payments are temporarily unavailable — Stripe configuration key is missing. Please contact administrator.";
                return RedirectToAction("Payments", "ClientDashboard");
            }

            decimal price = plan switch
            {
                SubscriptionPlan.Pro => 79,
                SubscriptionPlan.Enterprise => 199,
                _ => 29
            };

            ElectroScanAI.Models.Entities.Subscription? subscription = null;
            Payment? payment = null;

            try
            {
                subscription = new ElectroScanAI.Models.Entities.Subscription
                {
                    UserId = userId,
                    Plan = plan,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                // Create a Payment record with Pending status
                payment = new Payment
                {
                    UserId = userId,
                    Amount = price,
                    TransactionId = "PENDING-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                    Status = PaymentStatus.Pending,
                    PaymentMethod = "Stripe Checkout",
                    SubscriptionId = subscription.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Create Stripe Checkout session
                var domain = $"{Request.Scheme}://{Request.Host}";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmountDecimal = price * 100, // amount in cents
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"{plan} Plan Subscription"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = domain + Url.Action("Success", "Subscription") + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = domain + Url.Action("Cancel", "Subscription") + $"?subscriptionId={subscription.Id}"
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                // Save the Stripe session id to the payment transaction for later verification
                payment.TransactionId = session.Id;
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();

                // Redirect user to a small redirector that uses Stripe.js with the session id
                return RedirectToAction("CheckoutRedirect", new { sessionId = session.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe Checkout Session Creation Error.");
                if (payment != null && _context.Payments.Local.Contains(payment))
                {
                    _context.Payments.Remove(payment);
                }
                if (subscription != null && _context.Subscriptions.Local.Contains(subscription))
                {
                    _context.Subscriptions.Remove(subscription);
                }
                try { await _context.SaveChangesAsync(); } catch { }

                TempData["Error"] = "Payment processing failed — Stripe checkout is currently unavailable. Please contact support.";
                return RedirectToAction("Payments", "ClientDashboard");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult CheckoutRedirect(string sessionId)
        {
            var config = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
            var publishable = config?["PaymentGateway:Stripe:PublishableKey"] ?? "";
            ViewBag.StripePublishableKey = publishable;
            ViewBag.SessionId = sessionId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetryPayment(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return NotFound();
            if (payment.Status == PaymentStatus.Paid) return RedirectToAction("Payments", "ClientDashboard");

            var subscription = await _context.Subscriptions.FindAsync(payment.SubscriptionId);
            if (subscription == null) return NotFound();

            var config = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
            var secretKey = config?["PaymentGateway:Stripe:SecretKey"];
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(StripeConfiguration.ApiKey))
            {
                _logger.LogWarning("Stripe Secret Key is missing. Payment retry aborted.");
                TempData["Error"] = "Payments are temporarily unavailable — Stripe configuration key is missing. Please contact administrator.";
                return RedirectToAction("Payments", "ClientDashboard");
            }

            var price = payment.Amount;

            try
            {
                var domain = $"{Request.Scheme}://{Request.Host}";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmountDecimal = price * 100,
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"{subscription.Plan} Plan Subscription"
                                }
                            },
                            Quantity = 1
                        }
                    },
                    Mode = "payment",
                    SuccessUrl = domain + Url.Action("Success", "Subscription") + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = domain + Url.Action("Cancel", "Subscription") + $"?subscriptionId={subscription.Id}"
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                payment.TransactionId = session.Id;
                payment.Status = PaymentStatus.Pending;
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();

                return RedirectToAction("CheckoutRedirect", new { sessionId = session.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe Checkout Session Retry Creation Error.");
                TempData["Error"] = "Payment retry failed — Stripe checkout is currently unavailable. Please contact support.";
                return RedirectToAction("Payments", "ClientDashboard");
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Success(string session_id)
        {
            TempData["Success"] = "Payment submitted. It will be confirmed once the gateway notifies us.";
            return RedirectToAction("Payments", "ClientDashboard");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Cancel(int subscriptionId)
        {
            TempData["Error"] = "Payment was cancelled or not completed.";
            return RedirectToAction("Payments", "ClientDashboard");
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Webhook()
        {
            // Verify webhook signature if configured
            var json = string.Empty;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                json = await reader.ReadToEndAsync();
            }

            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
            var webhookSecret = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
            var secret = webhookSecret?["PaymentGateway:Stripe:WebhookSecret"];

            Event? stripeEvent = null;
            try
            {
                if (!string.IsNullOrEmpty(secret) && !string.IsNullOrEmpty(stripeSignature))
                {
                    stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, secret);
                }
                else
                {
                    stripeEvent = EventUtility.ParseEvent(json);
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }

            if (stripeEvent == null)
            {
                return BadRequest();
            }

            if (stripeEvent.Type == Events.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                if (session != null)
                {
                    // Lookup by TransactionId = session.Id
                    var dbPayment = await _context.Payments.FirstOrDefaultAsync(p => p.TransactionId == session.Id);
                    if (dbPayment != null)
                    {
                        dbPayment.Status = PaymentStatus.Paid;
                        dbPayment.PaymentDate = DateTime.UtcNow;
                        _context.Payments.Update(dbPayment);
                        await _context.SaveChangesAsync();

                        // Notify the user
                        await _notificationService.NotifyAsync(dbPayment.UserId, "Subscription activated", "Your subscription payment was successful.");
                    }
                }
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ElectroScanAI.Models.Entities.Subscription? activeSub = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdClaim, out int userId) && userId > 0)
                {
                    activeSub = await _context.Subscriptions
                        .Where(s => s.UserId == userId && s.IsActive)
                        .OrderByDescending(s => s.CreatedAt)
                        .FirstOrDefaultAsync();
                }
            }

            ViewBag.ActiveSubscription = activeSub;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSubscription()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out int userId) || userId <= 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var sub = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (sub != null)
            {
                sub.IsActive = false;
                _context.Subscriptions.Update(sub);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Your subscription has been cancelled. You are now on the Free tier.";
            }
            else
            {
                TempData["Info"] = "You do not have an active paid subscription to cancel.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
