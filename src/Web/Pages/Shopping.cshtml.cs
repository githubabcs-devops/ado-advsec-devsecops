using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.Web.Pages
{
    public class ShoppingModel : PageModel
    {
        private readonly ILogger<ShoppingModel> _logger;

        string adminUserName = "demouser@example.com";

	    // TODO: Don't use this in production
    	public const string DEFAULT_PASSWORD_NEW = "Pass@word1";

        public ShoppingModel(ILogger<ShoppingModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            string drive = Request.Query.ContainsKey("drive") ? Request.Query["drive"] : "C";
            var str = $"/C fsutil volume diskfree {drive}:";

            _logger.LogInformation($"Executing command: {str}");
            _logger.LogInformation($"User: {User.Identity?.Name}");  
            _logger.LogInformation($"Admin: {User.IsInRole("Admin")}");
            _logger.LogInformation("Admin" + adminUserName);

        }
    }
}