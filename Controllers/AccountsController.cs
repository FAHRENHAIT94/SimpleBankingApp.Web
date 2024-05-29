using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using SımpleBankingApp.Web.Data;
using SımpleBankingApp.Web.Models;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using RabbitMQ.Client;
using SımpleBankingApp.Web.Services;
using Polly;
using Polly.CircuitBreaker;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;


namespace SımpleBankingApp.Web.Controllers
{
    [Route("api/accounts")]
    [ApiController]

    public class AccountsController : ControllerBase
    {
        private readonly BankContext _context;
        private readonly RabbitMQService _rabbitMQService;


        public AccountsController(BankContext context)//, RabbitMQService rabbitMQService)
        {
            _context = context;
            //_rabbitMQService = rabbitMQService;
        }


        [HttpPost("account")]
        public async Task<IActionResult> CreateAccount([FromBody] Account account)
        {
            var accountCreate = new Account { UserId = account.UserId, Balance = account.Balance };
            _context.Accounts.Add(accountCreate);
            await _context.SaveChangesAsync(); // Asenkron olarak değişiklikleri kaydet

            // Kuyruğa mesaj gönder
            _rabbitMQService.PublishToQueue("create-account-queue", accountCreate);

            return Ok(accountCreate);
        }

        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> Deposit(int id, [FromBody] int amount)
        {
            // Circuit Breaker politikasını oluştur
            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
                    onBreak: (ex, breakDuration) =>
                    {
                        // Circuit Breaker devreye girdiğinde yapılacak işlemler
                        Console.WriteLine($"Circuit Breaker: API çağrısı başarısız oldu. Hata: {ex.Message}. Devre dışı bırakılma süresi: {breakDuration.TotalSeconds} saniye.");
                    },
                    onReset: () =>
                    {
                        // Circuit Breaker resetlendiğinde yapılacak işlemler
                        Console.WriteLine("Circuit Breaker: API çağrısı tekrar başarılı oldu. Devre dışı bırakılma sona erdi.");
                    },
                    onHalfOpen: () =>
                    {
                        // Circuit Breaker yarı açık durumdayken yapılacak işlemler
                        Console.WriteLine("Circuit Breaker: API çağrısı yarı açık durumda. Yeniden deneme yapılıyor.");
                    }
                );

            var result = await circuitBreakerPolicy.ExecuteAndCaptureAsync(async () =>
            {
                var account = await _context.Accounts.FindAsync(id);
                if (account == null)
                {
                    NotFound();
                }

                account.Balance += amount;
                var transaction = new Transaction { AccountId = id, Amount = amount, Date = DateTime.UtcNow, Type = "Deposit" };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Kuyruğa mesaj gönder
                //_rabbitMQService.PublishToQueue("deposit-queue", new { AccountId = id, Amount = amount });

                Ok(account);
            });

            if (result.Outcome == OutcomeType.Failure)
            {
                return StatusCode(503, "Servis geçici olarak kullanılamıyor.");
            }

            return Ok();
        }
        [HttpPost("{id}/withdraw")]
        public async Task<IActionResult> Withdraw(int id, [FromBody] int amount)
        {
            try
            {
                var circuitBreakerPolicy = Policy
                    .Handle<DbUpdateException>()//DbUpdateException 
                    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
                        onBreak: (ex, breakDuration) =>
                        {
                            // Circuit Breaker devreye girdiğinde yapılacak işlemler
                            Console.WriteLine($"Circuit Breaker: API çağrısı başarısız oldu. Hata: {ex.Message}. Devre dışı bırakılma süresi: {breakDuration.TotalSeconds} saniye.");
                        },
                        onReset: () =>
                        {
                            // Circuit Breaker resetlendiğinde yapılacak işlemler
                            Console.WriteLine("Circuit Breaker: API çağrısı tekrar başarılı oldu. Devre dışı bırakılma sona erdi.");
                        },
                        onHalfOpen: () =>
                        {
                            // Circuit Breaker yarı açık durumdayken yapılacak işlemler
                            Console.WriteLine("Circuit Breaker: API çağrısı yarı açık durumda. Yeniden deneme yapılıyor.");
                        }
                    );

                var account = await _context.Accounts.FindAsync(id);
                if (account == null || account.Balance < amount)
                {
                    // Hesap bakiyesi yetersiz olduğunda BadRequest döndür
                    throw new Exception("Hesap bakiyesi yetersiz veya geçersiz işlem.");
                }

                account.Balance -= amount;
                var transaction = new Transaction { AccountId = id, Amount = amount, Date = DateTime.UtcNow, Type = "Withdraw" };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Kuyruğa mesaj gönder
                //_rabbitMQService.PublishToQueue("withdraw-queue", new { AccountId = id, Amount = amount });

                // İşlem başarılı olduğunda OK döndür
                return Ok(account);
            }
            catch (BrokenCircuitException)
            {
                // Circuit Breaker devreye girdiğinde servis geçici olarak kullanılamıyor mesajı dön
                return StatusCode(503, "Service temporarily unavailable");
            }
        }

        [HttpGet("{id}/balance")]
        public async Task<IActionResult> GetBalance(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)//|| account.UserId != int.Parse(User.Identity.Name))
            {
                return NotFound();
            }

            // Kuyruğa mesaj gönder
            //_rabbitMQService.PublishToQueue("balance-queue", new { AccountId = id, Balance = account.Balance });

            return Ok(account.Balance);
        }


    }
}
