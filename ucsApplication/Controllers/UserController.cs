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

        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] AttendanceRequest request)
        {
            try
            {
                // Validate input: UserId or FingerPrintData must be provided
                if (string.IsNullOrEmpty(request.FingerPrintData) && !request.UserId.HasValue)
                {
                    return BadRequest("Either UserId or FingerPrintData must be provided.");
                }

                // Step 1: Find the user in MasterTable
                MasterTable user = null;

                if (string.IsNullOrEmpty(request.FingerPrintData))
                {
                    // If FingerPrintData is provided, search using it
                    user = await _context.MasterTable
                        .FirstOrDefaultAsync(u => u.FingerPrintData == request.FingerPrintData);
                }
                else if (request.UserId.HasValue)
                {
                    // If UserId is provided, search using it
                    user = await _context.MasterTable
                        .FirstOrDefaultAsync(u => u.UserId == request.UserId.Value);
                }
                else
                {
                    // If neither FingerPrintData nor UserId is provided, return BadRequest
                    return BadRequest("Either UserId or FingerPrintData must be provided.");
                }


                //if (!string.IsNullOrEmpty(request.FingerPrintData))
                //{
                //    // Search for user by FingerPrintData
                //    user = await _context.MasterTable
                //        .FirstOrDefaultAsync(u => u.FingerPrintData == request.FingerPrintData);
                //}
                //else
                //{
                //    // Search for user by UserId (assuming validation ensures UserId is provided if FingerPrintData is null/empty)
                //    if (request.UserId.HasValue)
                //    {
                //        user = await _context.MasterTable
                //            .FirstOrDefaultAsync(u => u.UserId == request.UserId.Value);
                //    }
                //}

                // If user not found
                if (user == null)
                {
                    return NotFound("UserId or FingerPrintData not found in MasterTable.");
                }

                // Step 2: Check if the user is already checked in
                var existingTransaction = await _context.TransactionTable
                    .FirstOrDefaultAsync(t => t.UserId == user.MasterId && t.CheckoutDateTime == null);

                if (existingTransaction != null)
                {
                    return BadRequest("User is already checked in.");
                }

                // Step 3: Create a new transaction
                var transaction = new TransactionTable
                {
                    UserId = user.MasterId, // Use MasterId as foreign key
                    CheckinDateTime = DateTime.UtcNow,
                    CheckoutDateTime = null,
                    CheckInMethod = !string.IsNullOrEmpty(request.FingerPrintData) ? "FingerPrint" : "UserId"
                };

                // Update user's LastTransactionDate
                user.LastTransactionDate = DateTime.UtcNow;

                // Save changes
                _context.TransactionTable.Add(transaction);
                await _context.SaveChangesAsync();

                // Step 4: Return success response
                return Ok(new
                {
                    message = "Check-in successful",
                    userId = user.UserId,
                    username = user.Username,
                    checkInTime = transaction.CheckinDateTime,
                    checkInMethod = transaction.CheckInMethod
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut([FromBody] AttendanceRequest request)
        {
            try
            {
                // Validate input: UserId or FingerPrintData must be provided
                if (string.IsNullOrEmpty(request.FingerPrintData) && !request.UserId.HasValue)
                {
                    return BadRequest("Either UserId or FingerPrintData must be provided.");
                }

                // Step 1: Find the user in MasterTable
                MasterTable user = null;

                if (string.IsNullOrEmpty(request.FingerPrintData))
                {
                    user = await _context.MasterTable
                        .FirstOrDefaultAsync(u => u.FingerPrintData == request.FingerPrintData);
                }
                else if (request.UserId.HasValue)
                {
                    // Convert UserId to string for comparison if needed
                    user = await _context.MasterTable
                        .FirstOrDefaultAsync(u => u.UserId == request.UserId.Value);
                }

                // If user not found
                if (user == null)
                {
                    return NotFound("UserId or FingerPrintData not found in MasterTable.");
                }

                // Step 2: Find the active check-in transaction
                var transaction = await _context.TransactionTable
                    .FirstOrDefaultAsync(t => t.UserId == user.MasterId && t.CheckoutDateTime == null);

                if (transaction == null)
                {
                    return BadRequest("No active check-in found for the user.");
                }

                // Step 3: Update checkout time and calculate duration
                transaction.CheckoutDateTime = DateTime.UtcNow;

                // Update user's LastTransactionDate
                user.LastTransactionDate = DateTime.UtcNow;

                // Calculate total duration
                var totalDuration = transaction.CheckoutDateTime - transaction.CheckinDateTime;

                // Save changes
                await _context.SaveChangesAsync();

                // Step 4: Return success response
                return Ok(new
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
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        public class AttendanceRequest
        {
            public int? UserId { get; set; }
            public string FingerPrintData { get; set; }
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

    }
}
