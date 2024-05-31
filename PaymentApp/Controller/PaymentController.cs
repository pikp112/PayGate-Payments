using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
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
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState.Values.SelectMany(err => err.Errors[0].ErrorMessage));
                }

                var respnse = await payment.AddNewCard(card);
                return Ok(respnse);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("{vaultId}", Name = nameof(GetVaultedCard))]
        public async Task<IActionResult> GetVaultedCard([FromRoute] string vaultId)
        {
            try
            {
                var result = await payment.GetVaultedCard(vaultId);
                return Ok(result?.ToString());
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("query/{payRequestId}", Name = nameof(QueryTransaction))]
        public async Task<IActionResult> QueryTransaction([FromRoute] string payRequestId)
        {
            try
            {
                var result = await payment.QueryTransaction(payRequestId);
                return Ok(result?.ToString());
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}