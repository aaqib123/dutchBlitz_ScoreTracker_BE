using Microsoft.AspNetCore.Mvc;

namespace DutchBlitzBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DutchBlitzController : ControllerBase
    {
        private readonly ILogger<DutchBlitzController> _logger;

        public DutchBlitzController(ILogger<DutchBlitzController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public String Get()
        {
            return "Multi";
        }
    }
}
