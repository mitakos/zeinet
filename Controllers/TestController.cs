using Microsoft.AspNetCore.Mvc;
using ZEIage.Models;
using ZEIage.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZEIage.Controllers
{
    /// <summary>
    /// Controller for testing voice call functionality
    /// Provides endpoints for testing call initiation and webhook handling
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly CallController _callController;
        private readonly ILogger<TestController> _logger;

        public TestController(CallController callController, ILogger<TestController> logger)
        {
            _callController = callController;
            _logger = logger;
        }

        /// <summary>
        /// Test endpoint for initiating a call with predefined parameters
        /// Uses a test phone number and default conversation variables
        /// </summary>
        [HttpPost("call")]
        public async Task<IActionResult> TestCall()
        {
            try
            {
                _logger.LogInformation("Starting test call");
                var request = new InitiateCallRequest
                {
                    PhoneNumber = "+385989821434",
                    Variables = new Dictionary<string, string>
                    {
                        { "name", "Test User" },
                        { "purpose", "Testing voice agent" },
                        { "language", "Croatian" }
                    }
                };

                return await _callController.InitiateCall(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test call");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
} 