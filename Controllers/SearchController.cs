using Microsoft.AspNetCore.Mvc;
using FreePLM.Office.Integration.Services;

namespace FreePLM.Office.Integration.Controllers
{
    /// <summary>
    /// Controller for document search operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ILogger<SearchController> _logger;
        private readonly IDialogService _dialogService;

        public SearchController(
            ILogger<SearchController> logger,
            IDialogService dialogService)
        {
            _logger = logger;
            _dialogService = dialogService;
        }

        /// <summary>
        /// Show search dialog UI and return selected document
        /// </summary>
        [HttpPost("search-ui")]
        public async Task<IActionResult> ShowSearchUI()
        {
            _logger.LogInformation("Showing Search UI");

            try
            {
                var dialogResult = await _dialogService.ShowSearchDialogAsync();

                if (dialogResult == null || !dialogResult.Success)
                {
                    _logger.LogInformation("Search cancelled by user");
                    return Ok(new { success = false, message = "Search cancelled by user" });
                }

                return Ok(new
                {
                    success = true,
                    objectId = dialogResult.ObjectId,
                    fileName = dialogResult.FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during search with UI");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
    }
}
