using Newtonsoft.Json.Linq;
using PaymentApp.Models;

namespace PaymentApp.Services
{
    public interface IPayment
    {
        // make a payment and tokenize card for future use
        Task<CardPaymentResponse> AddNewCard(Card card);

        // get the vaulted/tokenized card information
        Task<JToken?> GetVaultedCard(string vaultId);

        // query the status of a transaction using its id
        Task<JToken?> QueryTransaction(string payRequestId);
    }
}