using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ucsApplication.Data;
using ucsApplication.Models;

namespace ucsApplication.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }


        public class CheckInRequest
        {
            public int UserId { get; set; }
            public DateTime? Checkin { get; set; }
            public string FingerPrintData { get; set; }
        }

        public class CheckOutRequest
        {
            public int UserId { get; set; }
            public DateTime? Checkout { get; set; }
            public string FingerPrintData { get; set; }
        }

        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] List<CheckInRequest> requests)
        {
            try
            {
                if (requests == null || requests.Count == 0)
                {
                    return BadRequest("Request list cannot be empty.");
                }

                var responses = new List<object>();

                foreach (var request in requests)
                {
                    // Validate input: Either UserId or FingerPrintData must be provided
                    if (request.UserId == 0 && string.IsNullOrEmpty(request.FingerPrintData))
                    {
                        responses.Add(new
                        {
                            message = "Validation failed: Either UserId or FingerPrintData must be provided.",
                            status = "Error"
                        });
                        continue;
                    }

                    // Find the user in MasterTable
                    MasterTable user = null;
                    if (request.UserId != 0)
                    {
                        user = await _context.MasterTable
                            .FirstOrDefaultAsync(u => u.UserId == request.UserId);
                    }

                    // If not found by UserId, try by FingerPrintData
                    if (user == null && !string.IsNullOrEmpty(request.FingerPrintData))
                    {
                        user = await _context.MasterTable
                            .FirstOrDefaultAsync(u => u.FingerPrintData == request.FingerPrintData);
                    }

                    if (user == null)
                    {
                        responses.Add(new
                        {
                            message = "User not found in MasterTable.",
                            userId = request.UserId,
                            fingerPrintData = request.FingerPrintData,
                            status = "Error"
                        });
                        continue;
                    }

                    // Check if the user is already checked in
                    var existingTransaction = await _context.TransactionTable
                        .FirstOrDefaultAsync(t => t.UserId == user.MasterId && t.CheckoutDateTime == null);

                    if (existingTransaction != null)
                    {
                        responses.Add(new
                        {
                            message = "User is already checked in.",
                            userId = user.UserId,
                            username = user.Username,
                            status = "Error"
                        });
                        continue;
                    }

                    // Determine check-in method
                    string checkInMethod = "None";
                    if (request.UserId != 0 && !string.IsNullOrEmpty(request.FingerPrintData))
                        checkInMethod = "UserIdAndFingerPrint";
                    else if (request.UserId != 0)
                        checkInMethod = "UserId";
                    else if (!string.IsNullOrEmpty(request.FingerPrintData))
                        checkInMethod = "FingerPrint";

                    // Create a new transaction
                    var transaction = new TransactionTable
                    {
                        UserId = user.MasterId,
                        CheckinDateTime = request.Checkin ?? DateTime.UtcNow,
                        CheckoutDateTime = null,
                        CheckInMethod = checkInMethod
                    };

                    // Update user's LastTransactionDate
                    user.LastTransactionDate = transaction.CheckinDateTime;

                    // Add transaction to the context
                    _context.TransactionTable.Add(transaction);

                    // Prepare successful response
                    responses.Add(new
                    {
                        message = "Check-in successful",
                        userId = user.UserId,
                        username = user.Username,
                        checkInTime = transaction.CheckinDateTime,
                        checkInMethod = transaction.CheckInMethod,
                        status = "Success"
                    });
                }

                // Save all changes in one batch
                await _context.SaveChangesAsync();

                return Ok(responses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut([FromBody] List<CheckOutRequest> requests)
        {
            try
            {
                if (requests == null || requests.Count == 0)
                {
                    return BadRequest("Request list cannot be empty.");
                }

                var responses = new List<object>();

                foreach (var request in requests)
                {
                    // Validate input: Either UserId or FingerPrintData must be provided
                    if (request.UserId == 0 && string.IsNullOrEmpty(request.FingerPrintData))
                    {
                        responses.Add(new
                        {
                            message = "Validation failed: Either UserId or FingerPrintData must be provided.",
                            status = "Error"
                        });
                        continue;
                    }

                    // Find the user in MasterTable
                    MasterTable user = null;
                    if (request.UserId != 0)
                    {
                        user = await _context.MasterTable
                            .FirstOrDefaultAsync(u => u.UserId == request.UserId);
                    }

                    // If not found by UserId, try by FingerPrintData
                    if (user == null && !string.IsNullOrEmpty(request.FingerPrintData))
                    {
                        user = await _context.MasterTable
                            .FirstOrDefaultAsync(u => u.FingerPrintData == request.FingerPrintData);
                    }

                    // If user not found
                    if (user == null)
                    {
                        responses.Add(new
                        {
                            message = "User not found in MasterTable.",
                            userId = request.UserId,
                            fingerPrintData = request.FingerPrintData,
                            status = "Error"
                        });
                        continue;
                    }

                    // Find the active check-in transaction
                    var transaction = await _context.TransactionTable
                        .FirstOrDefaultAsync(t => t.UserId == user.MasterId && t.CheckoutDateTime == null);

                    if (transaction == null)
                    {
                        responses.Add(new
                        {
                            message = "No active check-in found for the user.",
                            userId = request.UserId,
                            fingerPrintData = request.FingerPrintData,
                            status = "Error"
                        });
                        continue;
                    }

                    // Determine check-in method
                    string checkInMethod = "None";
                    if (request.UserId != 0 && !string.IsNullOrEmpty(request.FingerPrintData))
                        checkInMethod = "UserIdAndFingerPrint";
                    else if (request.UserId != 0)
                        checkInMethod = "UserId";
                    else if (!string.IsNullOrEmpty(request.FingerPrintData))
                        checkInMethod = "FingerPrint";

                    // Update checkout time
                    transaction.CheckoutDateTime = request.Checkout ?? DateTime.UtcNow;

                    // Update user's LastTransactionDate
                    user.LastTransactionDate = (DateTime)transaction.CheckoutDateTime;

                    // Calculate total duration
                    var totalDuration = transaction.CheckoutDateTime - transaction.CheckinDateTime;

                    // Prepare successful response
                    responses.Add(new
                    {
                        message = "Check-out successful",
                        userId = user.UserId,
                        username = user.Username,
                        checkOutTime = transaction.CheckoutDateTime,
                        totalDuration = totalDuration?.ToString(@"hh\:mm\:ss"),
                        checkInMethod = transaction.CheckInMethod,
                        lastTransactionDate = user.LastTransactionDate
                    });
                }

                // Save changes
                await _context.SaveChangesAsync();

                return Ok(responses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("active-checkins")]
        public async Task<IActionResult> ListActiveCheckIns()
        {
            try
            {
                // Step 1: Query active check-ins where CheckoutDateTime is null
                var activeCheckIns = await _context.TransactionTable
                    .Where(t => t.CheckoutDateTime == null) // Filter only active check-ins
                    .Include(t => t.Master) // Include user data from MasterTable
                    .Select(t => new
                    {
                        t.Id,
                        UserId = t.Master.UserId,
                        Username = t.Master.Username,
                        CheckinDateTime = t.CheckinDateTime,
                        CheckInMethod = t.CheckInMethod
                    })
                    .ToListAsync();

                // Step 2: Check if there are any active check-ins
                if (!activeCheckIns.Any())
                {
                    return NotFound("No active check-ins found");
                }

                // Step 3: Return the list of active check-ins
                return Ok(activeCheckIns);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("master")]
        public async Task<IActionResult> GetMasterData()
        {
            try
            {
                var masterData = await _context.MasterTable.OrderByDescending(m=> m.LastTransactionDate)
                    .Select(m => new
                    {
                        m.MasterId,
                        m.UserId,
                        m.Username,
                        m.FingerPrintData,
                        m.LastTransactionDate
                    })
                    .ToListAsync();

                return Ok(masterData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
