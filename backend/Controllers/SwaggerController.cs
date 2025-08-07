using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("swagger-ui")]
public class SwaggerController : ControllerBase
{
    [HttpGet("custom.css")]
    public IActionResult GetCustomCss()
    {
        var css = @"
            .swagger-ui .topbar { background-color: #2c3e50; }
            .swagger-ui .topbar .download-url-wrapper .select-label { color: #ecf0f1; }
            .swagger-ui .info .title { color: #2c3e50; }
            .swagger-ui .scheme-container { background-color: #ecf0f1; }
            .swagger-ui .auth-wrapper { background-color: #3498db; }
            .swagger-ui .auth-wrapper .authorize { background-color: #2980b9; }
            .swagger-ui .opblock.opblock-get .opblock-summary-method { background-color: #61affe; }
            .swagger-ui .opblock.opblock-post .opblock-summary-method { background-color: #49cc90; }
            .swagger-ui .opblock.opblock-put .opblock-summary-method { background-color: #fca130; }
            .swagger-ui .opblock.opblock-delete .opblock-summary-method { background-color: #f93e3e; }
            .swagger-ui .btn.authorize { border-color: #2980b9; color: #2980b9; }
            .swagger-ui .btn.authorize:hover { background-color: #2980b9; color: white; }
            .swagger-ui .btn.execute { background-color: #4990e2; border-color: #4990e2; }
            .swagger-ui .btn.execute:hover { background-color: #357abd; border-color: #357abd; }
        ";
        
        return Content(css, "text/css");
    }

    [HttpGet("custom.js")]
    public IActionResult GetCustomJs()
    {
        var js = @"
            // Auto-focus on the authorize button when page loads
            document.addEventListener('DOMContentLoaded', function() {
                const authorizeBtn = document.querySelector('.btn.authorize');
                if (authorizeBtn) {
                    authorizeBtn.focus();
                }
                
                // Add helpful tooltip to authorize button
                if (authorizeBtn) {
                    authorizeBtn.title = 'Click here to add your JWT token (without Bearer prefix)';
                }
                
                // Update the authorize modal description
                setTimeout(function() {
                    const authModal = document.querySelector('.auth-wrapper');
                    if (authModal) {
                        const description = authModal.querySelector('.auth-container .auth-description');
                        if (description) {
                            description.innerHTML = 'Enter your JWT token below (without Bearer prefix):';
                        }
                    }
                }, 1000);
            });
            
            // Add success message when token is set
            const originalAuthorize = window.ui.authActions.authorize;
            window.ui.authActions.authorize = function(payload) {
                originalAuthorize(payload);
                if (payload.Bearer) {
                    console.log('JWT Token set successfully!');
                    // Show a success message
                    const successMsg = document.createElement('div');
                    successMsg.style.cssText = 'position: fixed; top: 20px; right: 20px; background: #4CAF50; color: white; padding: 10px; border-radius: 5px; z-index: 9999;';
                    successMsg.textContent = '✅ JWT Token set successfully!';
                    document.body.appendChild(successMsg);
                    setTimeout(() => successMsg.remove(), 3000);
                }
            };
        ";
        
        return Content(js, "application/javascript");
    }
} 