using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentApp.Models;
using PaymentApp.Services;

namespace PaymentApp.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController(IPayment payment) : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("Payment API");
        }

        [HttpPost(Name = nameof(AddNewCard))]
        public async Task<IActionResult> AddNewCard([FromBody] Card card)
        {
            var result = await payment.AddNewCard(card);
            return Ok(result);
        }
    }
}