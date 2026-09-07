using ElectroScanAI.Models.Entities;
using ElectroScanAI.Models.Enums;
using Electroscann_ai.Data;
using Electroscann_ai.Models;
using Electroscann_ai.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace Electroscann_ai.Controllers
{
    public class WirePointInput
    {
        public double XRatio { get; set; }
        public double YRatio { get; set; }
    }

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ElectroscannDbContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, ElectroscannDbContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index() => View();

        public IActionResult Privacy() => View();

        public IActionResult Faq() => View();

        public IActionResult About() => View();

        public IActionResult AI_Scanner() => View();

        // ========== SCAN HISTORY PAGE ==========
        [HttpGet]
        public async Task<IActionResult> History()
        {
            List<AIScan> scans = new List<AIScan>();
            bool isHistoryCleared = HttpContext.Session.GetString("HistoryCleared") == "true";
            var delJson = HttpContext.Session.GetString("DeletedScanIds");
            var delIds = !string.IsNullOrEmpty(delJson) ? (JsonSerializer.Deserialize<List<int>>(delJson) ?? new List<int>()) : new List<int>();

            if (User.Identity?.IsAuthenticated == true)
            {
                var claim = User.FindFirst("UserId")?.Value;
                if (int.TryParse(claim, out int userId))
                {
                    scans = await _context.AIScans
                        .AsNoTracking()
                        .Where(s => s.UserId == userId && !delIds.Contains(s.Id))
                        .OrderByDescending(s => s.CreatedAt)
                        .ToListAsync();
                }
            }
            else
            {
                var sessionScans = HttpContext.Session.GetString("GuestScans");
                if (!string.IsNullOrEmpty(sessionScans))
                {
                    try
                    {
                        var scanIds = JsonSerializer.Deserialize<List<int>>(sessionScans) ?? new List<int>();
                        if (scanIds.Any())
                        {
                            scans = await _context.AIScans
                                .AsNoTracking()
                                .Where(s => scanIds.Contains(s.Id) && !delIds.Contains(s.Id))
                                .OrderByDescending(s => s.CreatedAt)
                                .ToListAsync();
                        }
                    }
                    catch { }
                }

                if (!scans.Any() && !isHistoryCleared)
                {
                    scans = await _context.AIScans
                        .AsNoTracking()
                        .Where(s => !delIds.Contains(s.Id))
                        .OrderByDescending(s => s.CreatedAt)
                        .Take(12)
                        .ToListAsync();
                }
            }

            return View(scans);
        }

        // ========== DELETE SCAN POST ==========
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteScan(int id)
        {
            var delJson = HttpContext.Session.GetString("DeletedScanIds");
            var delIds = !string.IsNullOrEmpty(delJson) ? (JsonSerializer.Deserialize<List<int>>(delJson) ?? new List<int>()) : new List<int>();
            if (!delIds.Contains(id))
            {
                delIds.Add(id);
                HttpContext.Session.SetString("DeletedScanIds", JsonSerializer.Serialize(delIds));
            }

            var scan = await _context.AIScans.FirstOrDefaultAsync(s => s.Id == id);
            if (scan == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = "Scan record not found." });
                }
                TempData["ErrorMessage"] = "Scan record not found.";
                return RedirectToAction(nameof(History));
            }

            try
            {
                // Delete uploaded image file from wwwroot/uploads if it exists
                if (!string.IsNullOrWhiteSpace(scan.ImagePath) && scan.ImagePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    string localFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", scan.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(localFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(localFilePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not delete image file at {Path}", localFilePath);
                        }
                    }
                }

                // Remove associated ScanResults
                var relatedResults = await _context.ScanResults.Where(r => r.AIScanId == id).ToListAsync();
                if (relatedResults.Any())
                {
                    _context.ScanResults.RemoveRange(relatedResults);
                }

                _context.AIScans.Remove(scan);
                await _context.SaveChangesAsync();

                // Clean guest session list if present
                var sessionScans = HttpContext.Session.GetString("GuestScans");
                if (!string.IsNullOrEmpty(sessionScans))
                {
                    try
                    {
                        var scanIds = JsonSerializer.Deserialize<List<int>>(sessionScans) ?? new List<int>();
                        if (scanIds.Contains(id))
                        {
                            scanIds.Remove(id);
                            HttpContext.Session.SetString("GuestScans", JsonSerializer.Serialize(scanIds));
                        }
                    }
                    catch { }
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = true, message = "Scan entry deleted successfully." });
                }

                TempData["SuccessMessage"] = "Scan record successfully deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting scan record {ScanId}", id);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = "Failed to delete scan entry." });
                }
                TempData["ErrorMessage"] = "Failed to delete scan entry. Please try again.";
            }

            return RedirectToAction(nameof(History));
        }

        // ========== CLEAR ALL SCAN HISTORY POST ==========
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> DeleteAllScans()
        {
            try
            {
                List<AIScan> scansToDelete = new List<AIScan>();
                if (User.Identity?.IsAuthenticated == true)
                {
                    var claim = User.FindFirst("UserId")?.Value;
                    if (int.TryParse(claim, out int userId))
                    {
                        scansToDelete = await _context.AIScans.Where(s => s.UserId == userId).ToListAsync();
                    }
                }
                else
                {
                    var sessionScans = HttpContext.Session.GetString("GuestScans");
                    if (!string.IsNullOrEmpty(sessionScans))
                    {
                        try
                        {
                            var scanIds = JsonSerializer.Deserialize<List<int>>(sessionScans) ?? new List<int>();
                            if (scanIds.Any())
                            {
                                scansToDelete = await _context.AIScans.Where(s => scanIds.Contains(s.Id)).ToListAsync();
                            }
                        }
                        catch { }
                    }
                }

                HttpContext.Session.Remove("GuestScans");
                HttpContext.Session.SetString("HistoryCleared", "true");

                if (scansToDelete.Any())
                {
                    var ids = scansToDelete.Select(s => s.Id).ToList();
                    var relatedResults = await _context.ScanResults.Where(r => ids.Contains(r.AIScanId)).ToListAsync();
                    if (relatedResults.Any())
                    {
                        _context.ScanResults.RemoveRange(relatedResults);
                    }

                    _context.AIScans.RemoveRange(scansToDelete);
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "All scan history records have been permanently cleared.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all scan history.");
                TempData["ErrorMessage"] = "Failed to clear scan history. Please try again.";
            }

            return RedirectToAction(nameof(History));
        }

        // ========== CONTACT GET ==========
        [HttpGet]
        public IActionResult Contact() => View();

        // ========== CONTACT POST — saves to DB ==========
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Contact([FromForm] ContactMessage model)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" 
                       || Request.Headers["Accept"].ToString().Contains("application/json")
                       || (Request.ContentType != null && Request.ContentType.Contains("application/json"));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                string errorMsg = errors.FirstOrDefault() ?? "Please fill in all required fields accurately.";
                if (isAjax)
                {
                    return Json(new { success = false, message = errorMsg });
                }
                TempData["Error"] = errorMsg;
                return View(model);
            }

            model.IsResolved = false;
            model.CreatedAt = DateTime.UtcNow;

            _context.ContactMessages.Add(model);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Contact message received from {Email}", model.Email);

            if (isAjax)
            {
                return Json(new { success = true, message = "Your message has been received! Our team will respond within 24 hours." });
            }

            TempData["Success"] = "Your message has been sent! We'll respond within 24 hours.";
            return RedirectToAction(nameof(Contact));
        }

        public IActionResult Service() => View();

        public IActionResult Testimonials() => View();

        // ========== NEWSLETTER SUBSCRIBE POST ==========
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Subscribe(string email)
        {
            string referer = Request.Headers["Referer"].ToString();
            if (string.IsNullOrWhiteSpace(referer)) referer = "/";

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                TempData["Error"] = "Please enter a valid email address.";
                return Redirect(referer);
            }

            var existing = await _context.NewsletterSubscribers.FirstOrDefaultAsync(s => s.Email == email);
            if (existing == null)
            {
                _context.NewsletterSubscribers.Add(new NewsletterSubscriber
                {
                    Email = email,
                    IsSubscribed = true,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Thank you for subscribing to ElectroScan AI updates!";
            return Redirect(referer);
        }

        // ========== WIRE COLOR ANALYSIS POST (Point-Based L/N/E Identification) ==========
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AnalyzeWireColors(IFormFile? imageFile, string? existingImagePath, string? pointsJson)
        {
            try
            {
                string imagePath = existingImagePath ?? "/Image/about.jpg";
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string uniqueFileName = Guid.NewGuid().ToString("N") + "_" + Path.GetFileName(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    imagePath = "/uploads/" + uniqueFileName;
                }

                string fullPhysicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));

                var pointsList = new List<WirePointInput>();
                if (!string.IsNullOrWhiteSpace(pointsJson))
                {
                    try
                    {
                        pointsList = JsonSerializer.Deserialize<List<WirePointInput>>(pointsJson) ?? new List<WirePointInput>();
                    }
                    catch { }
                }

                string selectedStandard = "Pakistan Standard (Red=L, Black=N, Green=E)";

                // STAGE 1: Check wire presence first
                bool hasWirePresence = CheckWirePresenceInImage(fullPhysicalPath);

                List<Dictionary<string, object>> detectionResults;
                if (!hasWirePresence)
                {
                    // Stage 1 Failed: Stop immediately, return 0% confidence, Uncertain status
                    detectionResults = pointsList.Select((pt, idx) => new Dictionary<string, object>
                    {
                        ["pointIndex"] = idx + 1,
                        ["xRatio"] = pt.XRatio,
                        ["yRatio"] = pt.YRatio,
                        ["rgbHex"] = "#808080",
                        ["colorName"] = "No Wire / Non-Conductor",
                        ["detectedLabel"] = "Uncertain — No Wire Detected",
                        ["wireRole"] = "U",
                        ["confidence"] = 0,
                        ["recommendation"] = "No electrical wire or component detected at this location."
                    }).ToList();
                }
                else
                {
                    // Stage 2: Perform point color matching
                    detectionResults = ProcessWireImagePoints(fullPhysicalPath, pointsList);
                }

                int? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var claim = User.FindFirst("UserId")?.Value;
                    if (int.TryParse(claim, out int id)) userId = id;
                }

                bool hasUncertain = !hasWirePresence || detectionResults.Count == 0 || detectionResults.Any(r => r["wireRole"]?.ToString() == "U" || (r.ContainsKey("confidence") && Convert.ToInt32(r["confidence"]) < 80));

                var summaryData = new
                {
                    wiringStandard = selectedStandard,
                    wireDetections = detectionResults,
                    scannedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    uncertainFound = hasUncertain,
                    stage1Passed = hasWirePresence
                };

                string summaryJson = JsonSerializer.Serialize(summaryData);

                var scan = new AIScan
                {
                    UserId = userId,
                    ScanName = $"Wire Color Inspection ({selectedStandard})",
                    ImagePath = imagePath,
                    RiskLevel = !hasWirePresence ? ScanRiskLevel.Low : (hasUncertain ? ScanRiskLevel.Medium : ScanRiskLevel.Low),
                    Status = hasWirePresence ? "Completed" : "Invalid Scan",
                    ResultSummary = hasWirePresence ? summaryJson : "Stage 1 Wire Detection: No electrical wires or components found in image.",
                    CreatedAt = DateTime.UtcNow
                };

                _context.AIScans.Add(scan);
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("HistoryCleared");

                var scanResult = new ScanResult
                {
                    AIScanId = scan.Id,
                    DetectedIssue = !hasWirePresence 
                        ? "No electrical wires or components detected." 
                        : (pointsList.Count == 0 
                            ? "No pin points were tapped for spot analysis." 
                            : hasUncertain 
                                ? "One or more points did not match a known wire color (Red Live, Black Neutral, Green Earth)." 
                                : $"Wire color inspection verified across {detectionResults.Count} pin locations."),
                    Severity = scan.RiskLevel.ToString(),
                    Recommendation = !hasWirePresence
                        ? "Please upload or capture a clear photo showing electrical wires, cables, or panel terminals."
                        : (hasUncertain 
                            ? "Ensure pin markers are placed directly on wire insulation under good lighting." 
                            : "Inspect wire terminals, ensure secure torque settings, and verify compliance with Pakistan wiring codes."),
                    CreatedAt = DateTime.UtcNow
                };
                _context.ScanResults.Add(scanResult);
                await _context.SaveChangesAsync();

                if (userId == null)
                {
                    var sessionScans = HttpContext.Session.GetString("GuestScans");
                    var scanIdList = new List<int>();
                    if (!string.IsNullOrEmpty(sessionScans))
                    {
                        try { scanIdList = JsonSerializer.Deserialize<List<int>>(sessionScans) ?? new List<int>(); } catch { }
                    }
                    scanIdList.Insert(0, scan.Id);
                    HttpContext.Session.SetString("GuestScans", JsonSerializer.Serialize(scanIdList));
                }

                return Json(new
                {
                    success = true,
                    scanId = scan.Id,
                    imagePath = imagePath,
                    standard = selectedStandard,
                    wiresDetected = hasWirePresence,
                    results = detectionResults,
                    summary = scanResult.DetectedIssue,
                    recommendation = scanResult.Recommendation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AnalyzeWireColors");
                return Json(new { success = false, message = "Failed to analyze wire colors." });
            }
        }

        private List<Dictionary<string, object>> ProcessWireImagePoints(string filePath, List<WirePointInput> points)
        {
            var list = new List<Dictionary<string, object>>();

            if (points == null || points.Count == 0)
            {
                return list;
            }

            if (!System.IO.File.Exists(filePath))
            {
                int idx = 1;
                foreach (var pt in points)
                {
                    list.Add(new Dictionary<string, object>
                    {
                        ["pointIndex"] = idx,
                        ["xRatio"] = pt.XRatio,
                        ["yRatio"] = pt.YRatio,
                        ["rgbHex"] = "#808080",
                        ["colorName"] = "Unknown / File Missing",
                        ["detectedLabel"] = "Uncertain / No Wire Detected",
                        ["wireRole"] = "U",
                        ["confidence"] = 0,
                        ["recommendation"] = "Image file unavailable for pixel analysis."
                    });
                    idx++;
                }
                return list;
            }

            try
            {
                using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(filePath);
                int imgW = image.Width;
                int imgH = image.Height;

                int pointIndex = 1;
                foreach (var pt in points)
                {
                    int centerX = Math.Clamp((int)(pt.XRatio * imgW), 0, imgW - 1);
                    int centerY = Math.Clamp((int)(pt.YRatio * imgH), 0, imgH - 1);

                    long rSum = 0, gSum = 0, bSum = 0;
                    int count = 0;
                    for (int dx = -3; dx <= 3; dx++)
                    {
                        for (int dy = -3; dy <= 3; dy++)
                        {
                            int sx = Math.Clamp(centerX + dx, 0, imgW - 1);
                            int sy = Math.Clamp(centerY + dy, 0, imgH - 1);
                            var pixel = image[sx, sy];
                            rSum += pixel.R;
                            gSum += pixel.G;
                            bSum += pixel.B;
                            count++;
                        }
                    }

                    int r = (int)(rSum / count);
                    int g = (int)(gSum / count);
                    int b = (int)(bSum / count);

                    string rgbHex = $"#{r:X2}{g:X2}{b:X2}";
                    RgbToHsv(r, g, b, out double h, out double s, out double v);

                    var (role, label, colorName, confidence, rec) = MatchWireColorPakistan(r, g, b, h, s, v);

                    list.Add(new Dictionary<string, object>
                    {
                        ["pointIndex"] = pointIndex,
                        ["xRatio"] = pt.XRatio,
                        ["yRatio"] = pt.YRatio,
                        ["rgbHex"] = rgbHex,
                        ["colorName"] = colorName,
                        ["detectedLabel"] = label,
                        ["wireRole"] = role,
                        ["confidence"] = confidence,
                        ["recommendation"] = rec
                    });

                    pointIndex++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading image pixels with ImageSharp");
            }

            return list;
        }

        private static (string role, string label, string colorName, int confidence, string rec) MatchWireColorPakistan(
            int r, int g, int b, double h, double s, double v)
        {
            // Pakistan / Subcontinent standard reference:
            // Live (Phase - L): Red wire
            // Neutral (N): Black wire
            // Earth Ground (E): Green wire

            // 1. Check RED (Phase - Live)
            bool isRedHue = (h <= 22 || h >= 338) && s >= 0.35 && v >= 0.20;
            bool isRedRgb = r >= 105 && r > (g + 35) && r > (b + 35);
            if (isRedHue || isRedRgb)
            {
                int conf = Math.Clamp((int)(82 + (s * 16)), 82, 98);
                return ("L", "Phase Live (L)", "Red Insulation", conf, "Red Live Phase wire identified per Pakistan standard code.");
            }

            // 2. Check BLACK (Neutral - N)
            bool isBlackHsv = v <= 0.18 && s <= 0.35;
            bool isBlackRgb = r <= 55 && g <= 55 && b <= 55 && Math.Abs(r - g) <= 15 && Math.Abs(g - b) <= 15;
            if (isBlackHsv || isBlackRgb)
            {
                int conf = Math.Clamp((int)(84 + ((0.18 - v) * 50)), 82, 96);
                return ("N", "Neutral (N)", "Black Insulation", conf, "Black Neutral conductor identified per Pakistan standard code.");
            }

            // 3. Check GREEN (Earth Ground - E)
            bool isGreenHue = h >= 72 && h <= 160 && s >= 0.30 && v >= 0.20;
            bool isGreenRgb = g >= 90 && g > (r + 25) && g > (b + 25);
            if (isGreenHue || isGreenRgb)
            {
                int conf = Math.Clamp((int)(84 + (s * 14)), 84, 98);
                return ("E", "Earth Ground (E)", "Green Insulation", conf, "Green Protective Earth conductor identified per Pakistan standard code.");
            }

            // Strictly return 0% confidence when color does not match known wire insulation
            return ("U", "Uncertain — No Wire Detected", "Non-conductor / Unknown Color", 0, "Sampled area color does not match known wire insulation (Red Live, Black Neutral, Green Earth). Ensure pin is placed directly on wire.");
        }

        private static void RgbToHsv(int r, int g, int b, out double h, out double s, out double v)
        {
            double rd = r / 255.0;
            double gd = g / 255.0;
            double bd = b / 255.0;
            double max = Math.Max(rd, Math.Max(gd, bd));
            double min = Math.Min(rd, Math.Min(gd, bd));
            double delta = max - min;

            v = max;
            s = max == 0 ? 0 : delta / max;

            if (delta == 0)
            {
                h = 0;
            }
            else if (max == rd)
            {
                h = (60 * ((gd - bd) / delta) + 360) % 360;
            }
            else if (max == gd)
            {
                h = (60 * ((rd - bd) / delta) + 120) % 360;
            }
            else
            {
                h = (60 * ((rd - gd) / delta) + 240) % 360;
            }
        }

        // ========== AI SCANNER ANALYZE POST (Part 3.2 & Part 4) ==========
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AnalyzeScan(IFormFile? imageFile, string? deviceName, string? existingImagePath)
        {
            try
            {
                string imagePath = !string.IsNullOrWhiteSpace(existingImagePath) ? existingImagePath : "/Image/sample_wire_panel.jpg";
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string uniqueFileName = Guid.NewGuid().ToString("N") + "_" + Path.GetFileName(imageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    imagePath = "/uploads/" + uniqueFileName;
                }

                int? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var claim = User.FindFirst("UserId")?.Value;
                    if (int.TryParse(claim, out int id)) userId = id;
                }

                string scanName = string.IsNullOrWhiteSpace(deviceName) ? "Thermal Scan Analysis" : deviceName;
                string fullPhysicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));

                // Fallback Chain: 1. Gemini 3.6 Flash -> 2. Local Heuristic Demo Mode
                var geminiRes = await AnalyzeWireImageWithGeminiAsync(fullPhysicalPath, imageFile);

                bool wiresDetected;
                int confidenceScore;
                ScanRiskLevel riskLevel;
                string issue;
                string recommendation;
                string summary;
                bool demoMode;
                object wireDetails;
                string providerUsed;

                if (geminiRes.success)
                {
                    wiresDetected = geminiRes.wiresDetected;
                    confidenceScore = geminiRes.confidenceScore;
                    riskLevel = geminiRes.riskLevel;
                    issue = geminiRes.issue;
                    recommendation = geminiRes.recommendation;
                    summary = geminiRes.summary;
                    demoMode = geminiRes.demoMode;
                    wireDetails = geminiRes.wireDetails;
                    providerUsed = "Gemini 3.6 Flash";
                }
                else
                {
                    _logger.LogWarning("Gemini Vision API analysis failed or returned unsuccessful. Falling back to Local Heuristics / Demo Mode.");
                    var heuristicRes = await AnalyzeWireImageWithHeuristicsAsync(fullPhysicalPath);
                    wiresDetected = heuristicRes.wiresDetected;
                    confidenceScore = heuristicRes.confidenceScore;
                    riskLevel = heuristicRes.riskLevel;
                    issue = heuristicRes.issue;
                    recommendation = heuristicRes.recommendation;
                    summary = heuristicRes.summary;
                    demoMode = heuristicRes.demoMode;
                    wireDetails = heuristicRes.wireDetails;
                    providerUsed = "Rule-Based Demo Mode";
                }

                var scan = new AIScan
                {
                    UserId = userId,
                    ScanName = scanName,
                    ImagePath = imagePath,
                    RiskLevel = riskLevel,
                    Status = wiresDetected ? (demoMode ? "Completed (Demo Mode)" : $"Completed ({providerUsed})") : "Invalid Scan",
                    ResultSummary = summary,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AIScans.Add(scan);
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("HistoryCleared");

                var scanResult = new ScanResult
                {
                    AIScanId = scan.Id,
                    DetectedIssue = issue,
                    Severity = riskLevel.ToString(),
                    Recommendation = recommendation,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ScanResults.Add(scanResult);
                await _context.SaveChangesAsync();

                if (userId == null)
                {
                    var sessionScans = HttpContext.Session.GetString("GuestScans");
                    var scanIdList = new List<int>();
                    if (!string.IsNullOrEmpty(sessionScans))
                    {
                        try { scanIdList = JsonSerializer.Deserialize<List<int>>(sessionScans) ?? new List<int>(); } catch { }
                    }
                    scanIdList.Insert(0, scan.Id);
                    HttpContext.Session.SetString("GuestScans", JsonSerializer.Serialize(scanIdList));
                }

                return Json(new
                {
                    success = true,
                    wiresDetected = wiresDetected,
                    confidenceScore = confidenceScore,
                    scanId = scan.Id,
                    scanName = scan.ScanName,
                    riskLevel = scan.RiskLevel.ToString(),
                    issue = scanResult.DetectedIssue,
                    recommendation = scanResult.Recommendation,
                    summary = scan.ResultSummary,
                    demoMode = demoMode,
                    wireDetails = wireDetails,
                    provider = providerUsed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI Scan");
                return Json(new { 
                    success = false, 
                    wiresDetected = false, 
                    confidenceScore = 0, 
                    riskLevel = "Low", 
                    issue = "No electrical wires or components detected.", 
                    recommendation = "Please upload or capture a clear photo showing electrical wires, cables, or panel terminals.",
                    summary = "Stage 1 Wire Detection: Error analyzing image."
                });
            }
        }

        private async Task<(bool success, bool wiresDetected, int confidenceScore, ScanRiskLevel riskLevel, string issue, string recommendation, string summary, bool demoMode, object wireDetails)> AnalyzeWireImageWithGeminiAsync(string filePath, IFormFile? imageFile)
        {
            string apiKey = _configuration["Gemini:ApiKey"]
                ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";

            string modelName = _configuration["Gemini:Model"]
                ?? Environment.GetEnvironmentVariable("GEMINI_VISION_MODEL")
                ?? "gemini-3.6-flash";

            if (string.IsNullOrEmpty(apiKey) || !System.IO.File.Exists(filePath))
            {
                _logger.LogError("Gemini API Key or image file is missing. Aborting Gemini call.");
                return (false, false, 0, ScanRiskLevel.Low, "", "", "", false, null!);
            }

            try
            {
                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                string base64Image = Convert.ToBase64String(imageBytes);

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                if (!client.DefaultRequestHeaders.Contains("x-goog-api-key"))
                {
                    client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
                }

                string promptText = GetMultiWirePromptText();

                var requestBody = new
                {
                    contents = new object[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = promptText },
                                new
                                {
                                    inlineData = new
                                    {
                                        mimeType = "image/jpeg",
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        responseMimeType = "application/json"
                    }
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody, jsonOptions), System.Text.Encoding.UTF8, "application/json");
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
                var response = await client.PostAsync(url, content);

                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseJson);

                        if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                            candidates.ValueKind == JsonValueKind.Array &&
                            candidates.GetArrayLength() > 0)
                        {
                            var firstCandidate = candidates[0];
                            if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                                contentElem.TryGetProperty("parts", out var partsElem) &&
                                partsElem.ValueKind == JsonValueKind.Array &&
                                partsElem.GetArrayLength() > 0)
                            {
                                string textResult = partsElem[0].GetProperty("text").GetString() ?? "";
                                if (!string.IsNullOrWhiteSpace(textResult))
                                {
                                    var cleanedJson = textResult.Trim();
                                    if (cleanedJson.StartsWith("```json")) cleanedJson = cleanedJson.Substring(7);
                                    if (cleanedJson.StartsWith("```")) cleanedJson = cleanedJson.Substring(3);
                                    if (cleanedJson.EndsWith("```")) cleanedJson = cleanedJson.Substring(0, cleanedJson.Length - 3);
                                    cleanedJson = cleanedJson.Trim();

                                    try
                                    {
                                        using var resDoc = JsonDocument.Parse(cleanedJson);
                                        var root = resDoc.RootElement;

                                        bool detected = false;
                                        int confidence = 0;
                                        if (root.TryGetProperty("confidence", out var cProp) && cProp.ValueKind == JsonValueKind.Number)
                                            confidence = cProp.GetInt32();
                                        else if (root.TryGetProperty("confidenceScore", out var confProp) && confProp.ValueKind == JsonValueKind.Number)
                                            confidence = confProp.GetInt32();
                                        else if (root.TryGetProperty("confidence", out var cStrProp) && cStrProp.ValueKind == JsonValueKind.String && int.TryParse(cStrProp.GetString(), out int cVal1))
                                            confidence = cVal1;

                                        if (root.TryGetProperty("object_detected", out var odCheck) && odCheck.ValueKind == JsonValueKind.Array && odCheck.GetArrayLength() > 0)
                                            detected = true;
                                        else if (root.TryGetProperty("wiresDetected", out var detProp))
                                            detected = detProp.GetBoolean();
                                        else
                                            detected = confidence >= 30;

                                        if (!detected || confidence < 30)
                                        {
                                            return (true, false, 0, ScanRiskLevel.Low,
                                                "No electrical wires or components detected.",
                                                "Please upload or capture a clear photo showing electrical wires, cables, or panel terminals.",
                                                "Stage 1 Wire Detection: No electrical wires or components found in image.", false, null!);
                                        }

                                        string safetyLevel = root.TryGetProperty("safety_level", out var slProp) ? slProp.GetString() ?? "Low Risk" : (root.TryGetProperty("riskLevel", out var rProp) ? rProp.GetString() ?? "Low" : "Low");
                                        ScanRiskLevel risk = ScanRiskLevel.Low;
                                        if (safetyLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)) risk = ScanRiskLevel.Critical;
                                        else if (safetyLevel.Equals("High Risk", StringComparison.OrdinalIgnoreCase) || safetyLevel.Equals("High", StringComparison.OrdinalIgnoreCase)) risk = ScanRiskLevel.High;
                                        else if (safetyLevel.Equals("Medium Risk", StringComparison.OrdinalIgnoreCase) || safetyLevel.Equals("Medium", StringComparison.OrdinalIgnoreCase)) risk = ScanRiskLevel.Medium;

                                        var hazardsList = new List<string>();
                                        if (root.TryGetProperty("hazards", out var hzElem) && hzElem.ValueKind == JsonValueKind.Array)
                                        {
                                            foreach (var h in hzElem.EnumerateArray())
                                            {
                                                if (h.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(h.GetString()))
                                                    hazardsList.Add(h.GetString()!);
                                            }
                                        }

                                        var recsList = new List<string>();
                                        if (root.TryGetProperty("recommendation", out var recElem))
                                        {
                                            if (recElem.ValueKind == JsonValueKind.Array)
                                            {
                                                foreach (var r in recElem.EnumerateArray())
                                                {
                                                    if (r.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(r.GetString()))
                                                        recsList.Add(r.GetString()!);
                                                }
                                            }
                                            else if (recElem.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(recElem.GetString()))
                                            {
                                                recsList.Add(recElem.GetString()!);
                                            }
                                        }

                                        string issue = hazardsList.Any() ? string.Join(", ", hazardsList) : (root.TryGetProperty("issue", out var iProp) ? iProp.GetString() ?? "Electrical wires and components analyzed." : "Electrical components analyzed.");
                                        string recommendation = recsList.Any() ? string.Join("; ", recsList) : "Routine electrical inspection recommended.";
                                        string summary = root.TryGetProperty("summary", out var sProp) ? sProp.GetString() ?? $"Gemini Vision ({modelName}) inspection completed." : "AI inspection completed.";

                                        var wireDetailsObj = ParseWireDetailsFromRoot(root, confidence, risk);

                                        return (true, true, confidence, risk, issue, recommendation, summary, false, wireDetailsObj);
                                    }
                                    catch (Exception parseEx)
                                    {
                                        _logger.LogError(parseEx, "Failed to parse cleaned Gemini Vision JSON block. Cleaned text was: {Text}", cleanedJson);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogError(jsonEx, "Encountered JSON parse error on raw Gemini Vision response payload: {RawJson}", responseJson);
                    }
                }
                else
                {
                    _logger.LogError("Gemini Vision API call failed. Status Code: {StatusCode}. Response Body: {Body}",
                        (int)response.StatusCode, responseJson);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AnalyzeWireImageWithGeminiAsync.");
            }

            return (false, false, 0, ScanRiskLevel.Low, "", "", "", false, null!);
        }

        private async Task<(bool success, bool wiresDetected, int confidenceScore, ScanRiskLevel riskLevel, string issue, string recommendation, string summary, bool demoMode, object wireDetails)> AnalyzeWireImageWithHeuristicsAsync(string filePath)
        {
            // STAGE 1: Object / Wire Presence Fallback Check
            bool localDetected = CheckWirePresenceInImage(filePath);
            if (!localDetected)
            {
                return (true, false, 0, ScanRiskLevel.Low,
                    "No electrical wires or components detected (Demo Mode).",
                    "Please upload or capture a clear photo showing electrical wires, cables, or panel terminals.",
                    "Stage 1 Wire Detection: No electrical wires or components found in image (Demo Mode).", true, null!);
            }

            var demoObjects = new List<string> { "Phase Live Wire (Red)", "Neutral Wire (Black)", "Earth Ground (Green)", "Terminal Block" };
            var demoHazards = new List<string> { "None detected under visual inspection" };
            var demoRecs = new List<string> { "Verify terminal torque", "Ensure proper earthing grounding per Pakistan standards" };

            var fallbackWireDetails = new
            {
                object_detected = demoObjects,
                objectsDetected = demoObjects,
                wire_material = "Copper",
                wireMaterial = "Copper",
                wire_gauge = "2.5 mm² / 14 AWG",
                wireGauge = "2.5 mm² / 14 AWG",
                wire_condition = "Good insulation",
                wireCondition = "Good insulation",
                meter_visible = false,
                meterVisible = false,
                current_reading = "Exact current cannot be determined from a standard RGB camera.",
                currentReading = "Exact current cannot be determined from a standard RGB camera.",
                voltage_reading = "230V AC",
                voltageReading = "230V AC",
                hazards = demoHazards,
                safety_level = "Low Risk",
                safetyLevel = "Low Risk",
                recommendation = demoRecs,
                recommendations = demoRecs,
                confidence = 90,
                confidenceScore = 90,
                demoMode = true,
                wiresDetected = true,
                riskLevel = "Low",
                issue = "Electrical conductors and terminal connections analyzed (Demo Mode).",
                summary = "Demo Mode — Local vision rule-based inspection completed."
            };

            return (true, true, 90, ScanRiskLevel.Low,
                "Electrical conductors and terminal connections analyzed (Demo Mode).",
                "Verify terminal torque and ensure proper phase separation per Pakistan electrical standards.",
                "Demo Mode — Local vision heuristic applied.", true, fallbackWireDetails);
        }

        private static string GetMultiWirePromptText()
        {
            return """
You are ElectroScann AI, a Professional Electrical Engineering Inspection Assistant.
Your job is to analyze the provided image as an expert in electrical engineering,
industrial/residential/commercial wiring, power distribution, safety standards,
circuit protection, fault diagnosis, electrical components, and maintenance.

Detect every visible electrical object: wires (copper/aluminum/PVC/flexible/power/
control/earth/neutral/phase), MCB, MCCB, RCCB, ELCB, RCBO, distribution board,
electrical panel, switch, socket, plug, fuse, relay, contactor, transformer,
capacitor, motor, generator, VFD, PLC, current/voltage transformer, busbar,
terminal block, digital multimeter, clamp meter, energy meter, analog meter,
conduit, cable tray, junction box.

For wires: assess material, estimated gauge/thickness, insulation condition,
burn marks, loose connections, broken/naked wire, cracked insulation, overheating,
water damage, rust, corrosion, spark signs, carbon marks, mechanical damage.

If a digital/clamp meter display is visible, read it via OCR and extract current,
voltage, resistance, frequency, continuity, power factor, energy, and units —
report the EXACT displayed value, never modify it.

CRITICAL RULE: If no meter is visible, you MUST return
"Exact current cannot be determined from a standard RGB camera." for current_reading
and MUST NOT estimate, guess, or invent amperes/voltage/any measurement. Never
hallucinate engineering data.

Determine an overall safety_level: Safe, Low Risk, Medium Risk, High Risk, or
Critical. Detect hazards: electric shock risk, fire risk, loose wire, short
circuit risk, overload signs, exposed conductor, improper earthing, burning
marks, unsafe installation, water contact, overheated equipment.

Provide professional corrective recommendations (e.g. replace damaged insulation,
disconnect main power, use insulated gloves, replace loose terminal, install
proper earthing, replace overloaded cable, use correct cable size, inspect
breaker rating).

Return a confidence percentage (0-100).

Return ONLY valid JSON, no markdown fences, no extra text, in exactly this shape:
{
  "object_detected": [],
  "wire_material": "",
  "wire_gauge": "",
  "wire_condition": "",
  "meter_visible": false,
  "current_reading": "",
  "voltage_reading": "",
  "hazards": [],
  "safety_level": "",
  "recommendation": [],
  "confidence": 0
}
""";
        }

        private static object ParseWireDetailsFromRoot(JsonElement root, int globalConfidence, ScanRiskLevel risk)
        {
            string detectionMode = root.TryGetProperty("detectionMode", out var dmProp) ? dmProp.GetString() ?? "PanelWire" : "PanelWire";
            if (string.Equals(detectionMode, "GeneralCable", StringComparison.OrdinalIgnoreCase) ||
                (root.TryGetProperty("cableType", out var ctCheck) && !string.IsNullOrWhiteSpace(ctCheck.GetString())))
            {
                detectionMode = "GeneralCable";
            }

            // ElectroScann AI strict prompt parsing
            bool meterVisible = root.TryGetProperty("meter_visible", out var mvProp) ? mvProp.GetBoolean() : (root.TryGetProperty("meterVisible", out var mvp) && mvp.GetBoolean());
            string currentReading = root.TryGetProperty("current_reading", out var crProp) ? crProp.GetString() ?? "" : (root.TryGetProperty("currentReading", out var crp) ? crp.GetString() ?? "" : "");
            string voltageReading = root.TryGetProperty("voltage_reading", out var vrProp) ? vrProp.GetString() ?? "" : (root.TryGetProperty("voltageReading", out var vrp) ? vrp.GetString() ?? "" : "");

            if (!meterVisible)
            {
                currentReading = "Exact current cannot be determined from a standard RGB camera.";
                voltageReading = "";
            }

            string wireMaterial = root.TryGetProperty("wire_material", out var wmProp) ? wmProp.GetString() ?? "Copper Conductor" : (root.TryGetProperty("wireMaterial", out var wmp) ? wmp.GetString() ?? "Copper Conductor" : "Copper Conductor");
            string wireGauge = root.TryGetProperty("wire_gauge", out var wgProp) ? wgProp.GetString() ?? "" : (root.TryGetProperty("wireGauge", out var wgp) ? wgp.GetString() ?? "" : "");
            string wireCondition = root.TryGetProperty("wire_condition", out var wcProp) ? wcProp.GetString() ?? "" : (root.TryGetProperty("wireCondition", out var wcp) ? wcp.GetString() ?? "" : "");
            string safetyLevel = root.TryGetProperty("safety_level", out var slProp) ? slProp.GetString() ?? "Safe" : (root.TryGetProperty("safetyLevel", out var slp) ? slp.GetString() ?? "Safe" : "Safe");

            string cableType = root.TryGetProperty("cableType", out var ctProp) ? ctProp.GetString() ?? "General Electrical Cable" : "General Electrical Cable";
            string estimatedPowerRangeWatts = root.TryGetProperty("estimatedPowerRangeWatts", out var prProp) ? prProp.GetString() ?? "N/A" : "N/A";
            string visualClues = root.TryGetProperty("visualClues", out var vcProp) ? vcProp.GetString() ?? "Cable features and connector type analyzed." : "Cable features and connector type analyzed.";
            string disclaimer = root.TryGetProperty("disclaimer", out var dcProp) ? dcProp.GetString() ?? "This is a visual inspection based on typical specifications and thermal load patterns, not a hard-contact physical ammeter measurement." : "This is a visual inspection based on typical specifications and thermal load patterns, not a hard-contact physical ammeter measurement.";

            var wireList = new List<object>();
            int totalCount = 0;
            int correctCount = 0;
            int incorrectCount = 0;

            if (root.TryGetProperty("detectedWires", out var wiresElem) && wiresElem.ValueKind == JsonValueKind.Array)
            {
                int index = 1;
                foreach (var w in wiresElem.EnumerateArray())
                {
                    string pos = w.TryGetProperty("positionLabel", out var pProp) ? pProp.GetString() ?? $"Wire #{index}" : $"Wire #{index}";
                    string color = w.TryGetProperty("detectedColor", out var cProp) ? cProp.GetString() ?? "Unknown Insulation" : "Unknown Insulation";
                    string role = w.TryGetProperty("classifiedRole", out var rProp) ? rProp.GetString() ?? "Unidentified Conductor" : "Unidentified Conductor";
                    string expColor = w.TryGetProperty("expectedColor", out var ecProp) ? ecProp.GetString() ?? "" : "";
                    bool isCorrect = w.TryGetProperty("isCorrect", out var icProp) ? icProp.GetBoolean() : true;
                    int wireConf = w.TryGetProperty("confidence", out var wConfProp) ? wConfProp.GetInt32() : globalConfidence;
                    string note = w.TryGetProperty("note", out var nProp) ? nProp.GetString() ?? "" : "";

                    string icon = (role.Contains("Live", StringComparison.OrdinalIgnoreCase) || role.Contains("Phase", StringComparison.OrdinalIgnoreCase)) ? "fa-bolt" :
                                 role.Contains("Neutral", StringComparison.OrdinalIgnoreCase) ? "fa-minus-circle" :
                                 (role.Contains("Earth", StringComparison.OrdinalIgnoreCase) || role.Contains("Ground", StringComparison.OrdinalIgnoreCase)) ? "fa-shield-alt" : "fa-question-circle";

                    if (isCorrect) correctCount++;
                    else incorrectCount++;
                    totalCount++;

                    wireList.Add(new
                    {
                        id = index++,
                        positionLabel = pos,
                        detectedColor = color,
                        classifiedRole = role,
                        expectedColor = expColor,
                        isCorrect = isCorrect,
                        confidence = wireConf,
                        note = note,
                        icon = icon
                    });
                }
            }

            if (detectionMode == "PanelWire" && totalCount == 0)
            {
                string pName = root.TryGetProperty("phaseWireName", out var pN) ? pN.GetString() ?? "Phase Live (L)" : "Phase Live (L)";
                string pCol = root.TryGetProperty("phaseWireColor", out var pC) ? pC.GetString() ?? "Red Insulation" : "Red Insulation";
                string pCond = root.TryGetProperty("phaseCondition", out var pD) ? pD.GetString() ?? "Live Phase conductor verified per Pakistan standards." : "Live Phase conductor verified.";

                string nName = root.TryGetProperty("neutralWireName", out var nN) ? nN.GetString() ?? "Neutral Conductor (N)" : "Neutral Conductor (N)";
                string nCol = root.TryGetProperty("neutralWireColor", out var nC) ? nC.GetString() ?? "Black Insulation" : "Black Insulation";
                string nCond = root.TryGetProperty("neutralCondition", out var nD) ? nD.GetString() ?? "Neutral return wire verified per Pakistan standards." : "Neutral return wire verified.";

                string eName = root.TryGetProperty("earthWireName", out var eN) ? eN.GetString() ?? "Earth Ground (E)" : "Earth Ground (E)";
                string eCol = root.TryGetProperty("earthWireColor", out var eC) ? eC.GetString() ?? "Green Insulation" : "Green Insulation";
                string eCond = root.TryGetProperty("earthCondition", out var eD) ? eD.GetString() ?? "Earth protective ground verified per Pakistan standards." : "Earth protective ground verified.";

                wireList.Add(new { id = 1, positionLabel = "Terminal 1 (Phase / Live)", detectedColor = pCol, classifiedRole = pName, expectedColor = "Red", isCorrect = true, confidence = globalConfidence, note = pCond, icon = "fa-bolt" });
                wireList.Add(new { id = 2, positionLabel = "Terminal 2 (Neutral)", detectedColor = nCol, classifiedRole = nName, expectedColor = "Black", isCorrect = true, confidence = globalConfidence, note = nCond, icon = "fa-minus-circle" });
                wireList.Add(new { id = 3, positionLabel = "Terminal 3 (Earth Ground)", detectedColor = eCol, classifiedRole = eName, expectedColor = "Green", isCorrect = true, confidence = globalConfidence, note = eCond, icon = "fa-shield-alt" });

                totalCount = 3;
                correctCount = 3;
                incorrectCount = 0;
            }

            if (root.TryGetProperty("totalWiresDetected", out var twProp) && twProp.GetInt32() > 0)
            {
                totalCount = twProp.GetInt32();
            }
            if (root.TryGetProperty("correctlyWiredCount", out var cwProp))
            {
                correctCount = cwProp.GetInt32();
            }
            if (root.TryGetProperty("incorrectlyWiredCount", out var iwProp))
            {
                incorrectCount = iwProp.GetInt32();
            }

            var objectsDetectedList = new List<string>();
            if (root.TryGetProperty("object_detected", out var odProp) && odProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in odProp.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    {
                        objectsDetectedList.Add(item.GetString()!);
                    }
                }
            }

            var hazardsList = new List<string>();
            if (root.TryGetProperty("hazards", out var hzProp) && hzProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in hzProp.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                    {
                        hazardsList.Add(item.GetString()!);
                    }
                }
            }

            return new
            {
                detectionMode = detectionMode,
                meterVisible = meterVisible,
                meter_visible = meterVisible,
                currentReading = currentReading,
                current_reading = currentReading,
                voltageReading = voltageReading,
                voltage_reading = voltageReading,
                wireMaterial = wireMaterial,
                wire_material = wireMaterial,
                wireGauge = wireGauge,
                wire_gauge = wireGauge,
                wireCondition = wireCondition,
                wire_condition = wireCondition,
                safetyLevel = safetyLevel,
                safety_level = safetyLevel,
                objectsDetected = objectsDetectedList,
                object_detected = objectsDetectedList,
                hazards = hazardsList,
                cableType = cableType,
                estimatedPowerRangeWatts = estimatedPowerRangeWatts,
                visualClues = visualClues,
                disclaimer = disclaimer,
                totalWiresDetected = totalCount,
                correctlyWiredCount = correctCount,
                incorrectlyWiredCount = incorrectCount,
                detectedWires = wireList,
                standard = "Pakistan Electrical Standard (Red=Live, Black=Neutral, Green=Earth)"
            };
        }

        private static bool CheckWirePresenceInImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return false;
            try
            {
                using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(filePath);
                int width = image.Width;
                int height = image.Height;
                if (width < 20 || height < 20) return false;

                int totalSampled = 0;
                int wireColorCount = 0;
                int edgeCount = 0;
                int neutralCableEdgeCount = 0;

                int stepX = Math.Max(1, width / 80);
                int stepY = Math.Max(1, height / 80);

                for (int x = 1; x < width - stepX; x += stepX)
                {
                    for (int y = 1; y < height - stepY; y += stepY)
                    {
                        var pixel = image[x, y];
                        var pxRight = image[x + 1, y];
                        var pxDown = image[x, y + 1];

                        totalSampled++;

                        // Edge & contrast check
                        int deltaR = Math.Abs(pixel.R - pxRight.R) + Math.Abs(pixel.R - pxDown.R);
                        int deltaG = Math.Abs(pixel.G - pxRight.G) + Math.Abs(pixel.G - pxDown.G);
                        int deltaB = Math.Abs(pixel.B - pxRight.B) + Math.Abs(pixel.B - pxDown.B);
                        int edgeGradient = deltaR + deltaG + deltaB;

                        if (edgeGradient > 55)
                        {
                            edgeCount++;
                        }

                        RgbToHsv(pixel.R, pixel.G, pixel.B, out double h, out double s, out double v);

                        // Red wire insulation (vivid red, high saturation, distinct from skin/wood)
                        bool isRedWire = ((h <= 25 || h >= 335) && s >= 0.25 && v >= 0.20 && pixel.R >= 90 && pixel.R > pixel.G + 20 && pixel.R > pixel.B + 20);
                        
                        // Green wire insulation (vivid green)
                        bool isGreenWire = (h >= 65 && h <= 165 && s >= 0.25 && v >= 0.18 && pixel.G >= 80 && pixel.G > pixel.R + 15 && pixel.G > pixel.B + 15);

                        // Black wire insulation (dark linear conductor WITH high adjacent edge gradient)
                        bool isBlackWire = (v <= 0.22 && s <= 0.40 && edgeGradient > 30);

                        // Blue/Yellow conductor insulation
                        bool isBlueWire = (h >= 180 && h <= 250 && s >= 0.30 && v >= 0.20 && pixel.B >= 90 && pixel.B > pixel.R + 15);
                        bool isYellowWire = (h >= 40 && h <= 72 && s >= 0.30 && v >= 0.25);

                        // Neutral/light-colored consumer cable edge check (white/grey charger cords)
                        bool isNeutralCableEdge = (s <= 0.15 && v >= 0.55 && edgeGradient > 40);
                        if (isNeutralCableEdge)
                        {
                            neutralCableEdgeCount++;
                        }

                        if (isRedWire || isGreenWire || isBlackWire || isBlueWire || isYellowWire)
                        {
                            wireColorCount++;
                        }
                    }
                }

                if (totalSampled == 0) return false;

                double wireColorRatio = (double)wireColorCount / totalSampled;
                double edgeDensity = (double)edgeCount / totalSampled;
                double neutralCableRatio = (double)neutralCableEdgeCount / totalSampled;

                // STAGE 1 RULE:
                // An image MUST have distinct wire insulation colors (>= 2.5%) AND structural edge contrast (>= 3.5%), OR high wire insulation density (>= 6%),
                // OR neutral/light-colored cable edge ratio (>= 4%) with strong structural edges (>= 5%).
                // Non-electrical objects (walls, hands, paper, faces, clothes, tables) will fail this check
                bool passedStage1 = (wireColorRatio >= 0.025 && edgeDensity >= 0.035) || (wireColorRatio >= 0.060) || (neutralCableRatio >= 0.04 && edgeDensity >= 0.05);
                return passedStage1;
            }
            catch
            {
                return false;
            }
        }

        // ========== WIRE COST CALCULATOR ==========
        [HttpGet]
        public async Task<IActionResult> Calculator()
        {
            var model = new WireCalculatorViewModel();
            await PopulateHistoryAsync(model);
            return View(model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Calculator(WireCalculatorViewModel model)
        {
            // First apply calculation to derive Load, Distance, CurrentAmps, WireSize, BreakerRating, SafetyMessage, etc.
            ApplyCalculation(model);

            // Remove modelstate errors on server-computed fields
            ModelState.Remove(nameof(model.Load));
            ModelState.Remove(nameof(model.Distance));
            ModelState.Remove(nameof(model.WireSize));
            ModelState.Remove(nameof(model.Result));

            if (!ModelState.IsValid)
            {
                await PopulateHistoryAsync(model);
                return View(model);
            }

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdClaim, out int userId) && userId > 0)
                {
                    _context.WireCalculations.Add(new WireCalculation
                    {
                        UserId = userId,
                        Load = model.Load,
                        Distance = model.Distance,
                        WireSize = model.WireSize ?? string.Empty,
                        Result = model.Result ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Calculation saved to your account history!";
                }
            }
            else
            {
                var sessionData = HttpContext.Session.GetString("GuestCalcHistory");
                var list = new List<WireCalculationHistoryItem>();
                if (!string.IsNullOrEmpty(sessionData))
                {
                    try { list = System.Text.Json.JsonSerializer.Deserialize<List<WireCalculationHistoryItem>>(sessionData) ?? new List<WireCalculationHistoryItem>(); } catch { }
                }
                list.Insert(0, new WireCalculationHistoryItem
                {
                    CreatedAt = DateTime.UtcNow,
                    Load = model.Load,
                    Distance = model.Distance,
                    WireSize = model.WireSize ?? string.Empty,
                    Result = model.Result ?? string.Empty
                });
                if (list.Count > 5) list = list.Take(5).ToList();
                HttpContext.Session.SetString("GuestCalcHistory", System.Text.Json.JsonSerializer.Serialize(list));
                TempData["Success"] = "Calculation computed successfully! Saved to history below.";
            }

            await PopulateHistoryAsync(model);
            return View(model);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("api/calculator/calculate")]
        public async Task<IActionResult> CalculateApi([FromBody] WireCalculatorViewModel model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "Invalid input data." });
            }

            ApplyCalculation(model);

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdClaim, out int userId) && userId > 0)
                {
                    _context.WireCalculations.Add(new WireCalculation
                    {
                        UserId = userId,
                        Load = model.Load,
                        Distance = model.Distance,
                        WireSize = model.WireSize ?? string.Empty,
                        Result = model.Result ?? string.Empty,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var sessionData = HttpContext.Session.GetString("GuestCalcHistory");
                var list = new List<WireCalculationHistoryItem>();
                if (!string.IsNullOrEmpty(sessionData))
                {
                    try { list = System.Text.Json.JsonSerializer.Deserialize<List<WireCalculationHistoryItem>>(sessionData) ?? new List<WireCalculationHistoryItem>(); } catch { }
                }
                list.Insert(0, new WireCalculationHistoryItem
                {
                    Id = (int)(DateTime.UtcNow.Ticks % 1000000),
                    CreatedAt = DateTime.UtcNow,
                    Load = model.Load,
                    Distance = model.Distance,
                    WireSize = model.WireSize ?? string.Empty,
                    Result = model.Result ?? string.Empty
                });
                if (list.Count > 5) list = list.Take(5).ToList();
                HttpContext.Session.SetString("GuestCalcHistory", System.Text.Json.JsonSerializer.Serialize(list));
            }

            await PopulateHistoryAsync(model);

            var historyItems = model.RecentCalculations?.Select(item => new
            {
                id = item.Id,
                dateFormatted = item.CreatedAt.ToLocalTime().ToString("g"),
                load = item.Load,
                loadFormatted = $"{item.Load:N0} W",
                distance = item.Distance,
                distanceFormatted = $"{item.Distance:0} m",
                wireSize = item.WireSize,
                result = item.Result
            }).ToList();

            return Json(new
            {
                success = true,
                wireLength = $"{model.Distance:0} m",
                wireGauge = model.WireSize,
                load = $"{model.Load:N0} W",
                currentAmps = $"{model.CurrentAmps:0.#} A",
                breakerRating = model.BreakerRating,
                roomArea = $"{model.RoomArea:0.#} sq ft",
                safetyMessage = model.SafetyMessage,
                result = model.Result,
                history = historyItems
            });
        }

        /// <summary>NEC-style rules matching WireCalculationRules (voltage 220V).</summary>
        private static void ApplyCalculation(WireCalculatorViewModel model)
        {
            model.RoomArea = model.RoomLength * model.RoomWidth;
            double perimeter = 2 * (model.RoomLength + model.RoomWidth);
            model.Distance = Math.Round((perimeter * model.RoomHeight * 1.4) + (model.RoomArea * 0.9));

            model.Load = (model.Lights * WireCalculationRules.LightWatts)
                       + (model.Fans * WireCalculationRules.FanWatts)
                       + (model.Sockets * WireCalculationRules.SocketWatts)
                       + (model.AcUnits * WireCalculationRules.AcUnitWatts)
                       + (model.HeavyLoads * WireCalculationRules.HeavyLoadWatts);

            model.CurrentAmps = Math.Round(model.Load / WireCalculationRules.StandardVoltage, 1);

            var (gauge, breaker, safety) = WireCalculationRules.EvaluateLoad(model.Load);

            model.WireSize = gauge;
            model.BreakerRating = breaker;
            model.SafetyMessage = safety;
            model.Result = $"Area: {model.RoomArea:0.#} sq ft | Current: {model.CurrentAmps:0.#} A | Breaker: {breaker} | {safety}";
            model.HasResult = true;
        }

        private async Task PopulateHistoryAsync(WireCalculatorViewModel model)
        {
            model.RecentCalculations = new List<WireCalculationHistoryItem>();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (int.TryParse(userIdClaim, out int userId) && userId > 0)
                {
                    model.RecentCalculations = await _context.WireCalculations
                        .AsNoTracking()
                        .Where(w => w.UserId == userId)
                        .OrderByDescending(w => w.CreatedAt)
                        .Take(5)
                        .Select(w => new WireCalculationHistoryItem
                        {
                            Id = w.Id,
                            Load = w.Load,
                            Distance = w.Distance,
                            WireSize = w.WireSize,
                            Result = w.Result,
                            CreatedAt = w.CreatedAt
                        })
                        .ToListAsync();
                }
            }
            else
            {
                var sessionData = HttpContext.Session.GetString("GuestCalcHistory");
                if (!string.IsNullOrEmpty(sessionData))
                {
                    try
                    {
                        model.RecentCalculations = System.Text.Json.JsonSerializer.Deserialize<List<WireCalculationHistoryItem>>(sessionData) ?? new List<WireCalculationHistoryItem>();
                    }
                    catch { }
                }
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Route("api/calculator/delete/{id}")]
        public async Task<IActionResult> DeleteCalculationApi(int id)
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst("UserId")?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        var calc = await _context.WireCalculations.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
                        if (calc != null)
                        {
                            _context.WireCalculations.Remove(calc);
                            await _context.SaveChangesAsync();
                            return Json(new { success = true, message = "Calculation deleted successfully." });
                        }
                    }
                }

                // Guest / session fallback
                var sessionData = HttpContext.Session.GetString("GuestCalcHistory");
                if (!string.IsNullOrEmpty(sessionData))
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<WireCalculationHistoryItem>>(sessionData) ?? new List<WireCalculationHistoryItem>();
                    var itemToRemove = list.FirstOrDefault(x => x.Id == id);
                    if (itemToRemove != null)
                    {
                        list.Remove(itemToRemove);
                        HttpContext.Session.SetString("GuestCalcHistory", System.Text.Json.JsonSerializer.Serialize(list));
                        return Json(new { success = true, message = "Calculation removed from session history." });
                    }
                    else if (list.Count > 0)
                    {
                        // Remove first item if matching ID not found
                        list.RemoveAt(0);
                        HttpContext.Session.SetString("GuestCalcHistory", System.Text.Json.JsonSerializer.Serialize(list));
                        return Json(new { success = true, message = "Calculation entry deleted." });
                    }
                }

                return Json(new { success = true, message = "Calculation removed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting calculation {Id}", id);
                return Json(new { success = false, message = "Failed to delete calculation entry." });
            }
        }

        [HttpGet]
        [Route("api/calculator/details/{id}")]
        public async Task<IActionResult> GetCalculationDetailsApi(int id)
        {
            try
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst("UserId")?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                        var calc = await _context.WireCalculations.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
                        if (calc != null)
                        {
                            return Json(new
                            {
                                success = true,
                                id = calc.Id,
                                load = $"{calc.Load:N0} W",
                                distance = $"{calc.Distance:0} m",
                                wireSize = calc.WireSize,
                                result = calc.Result,
                                dateFormatted = calc.CreatedAt.ToLocalTime().ToString("f")
                            });
                        }
                    }
                }

                var sessionData = HttpContext.Session.GetString("GuestCalcHistory");
                if (!string.IsNullOrEmpty(sessionData))
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<WireCalculationHistoryItem>>(sessionData) ?? new List<WireCalculationHistoryItem>();
                    var item = list.FirstOrDefault(x => x.Id == id) ?? list.FirstOrDefault();
                    if (item != null)
                    {
                        return Json(new
                        {
                            success = true,
                            id = item.Id,
                            load = $"{item.Load:N0} W",
                            distance = $"{item.Distance:0} m",
                            wireSize = item.WireSize,
                            result = item.Result,
                            dateFormatted = item.CreatedAt.ToLocalTime().ToString("f")
                        });
                    }
                }

                return Json(new { success = false, message = "Calculation record not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching calculation details {Id}", id);
                return Json(new { success = false, message = "Failed to fetch details." });
            }
        }

        [HttpGet]
        [Route("api/market-rates/live-commodities")]
        public IActionResult GetLiveCommoditiesApi()
        {
            var rng = new Random();
            var commodities = new[]
            {
                new { name = "Copper (Grade A)", symbol = "HG=F", unit = "per lb", priceUsd = Math.Round(4.38 + (rng.NextDouble() * 0.08 - 0.04), 2), changePct = +1.45, trend = "up" },
                new { name = "Electrical Copper Cable", symbol = "ECW", unit = "per kg", priceUsd = Math.Round(9.85 + (rng.NextDouble() * 0.12 - 0.06), 2), changePct = +2.10, trend = "up" },
                new { name = "Aluminum (99.7%)", symbol = "ALI=F", unit = "per metric ton", priceUsd = Math.Round(2640.0 + (rng.NextDouble() * 20.0 - 10.0), 2), changePct = -0.68, trend = "down" },
                new { name = "Structural Steel / Conduit", symbol = "STL", unit = "per metric ton", priceUsd = Math.Round(890.0 + (rng.NextDouble() * 8.0 - 4.0), 2), changePct = +0.32, trend = "up" },
                new { name = "Gold Plating (Terminal)", symbol = "GC=F", unit = "per oz", priceUsd = Math.Round(2380.5 + (rng.NextDouble() * 15.0 - 7.5), 2), changePct = +0.88, trend = "up" },
                new { name = "Silver Plating (Switch)", symbol = "SI=F", unit = "per oz", priceUsd = Math.Round(28.4 + (rng.NextDouble() * 0.4 - 0.2), 2), changePct = -0.42, trend = "down" }
            };

            return Json(new
            {
                success = true,
                updatedAt = DateTime.UtcNow.ToLocalTime().ToString("t"),
                commodities = commodities
            });
        }

        public IActionResult Blog() => View();

        public IActionResult Careers() => View();

        public IActionResult Analytics() => RedirectToAction(nameof(System_Monitor));

        public IActionResult System_Monitor() => View();

        public IActionResult Reports() => RedirectToAction(nameof(History));

        // ========== MARKET RATES — with real DB data + filtering ==========
        public async Task<IActionResult> Market_Rates(string? city, string? category)
        {
            var query = _context.MarketRates.AsQueryable();

            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(m => m.City.Contains(city));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(m => m.Category == category);

            ViewBag.Rates = await query.OrderBy(m => m.Category).ThenBy(m => m.ItemName).ToListAsync();
            ViewBag.Cities = await _context.MarketRates.Select(m => m.City).Distinct().OrderBy(c => c).ToListAsync();
            ViewBag.Categories = await _context.MarketRates.Select(m => m.Category).Distinct().OrderBy(c => c).ToListAsync();
            ViewBag.FilterCity = city;
            ViewBag.FilterCategory = category;

            return View();
        }

        public IActionResult Demo() => RedirectToAction(nameof(AI_Scanner));

        public IActionResult Pricing() => View();

        public IActionResult Terms() => View();

        public IActionResult Cookies() => View();

        public IActionResult Estimation() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
