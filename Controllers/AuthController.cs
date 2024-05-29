using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SımpleBankingApp.Web.Data;
using SımpleBankingApp.Web.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SımpleBankingApp.Web.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly BankContext _context;

        public AuthController(BankContext context, IConfiguration configuration)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var existingUser = _context.Users.FirstOrDefault(u => u.Username == user.Username);
            if (existingUser != null)
                return Conflict("User already exists");

            // Create new user
            var newUser = new User
            {
                Username = user.Username,
                Password = BCrypt.Net.BCrypt.HashPassword(user.Password)
            };
            _context.Users.Add(newUser);
            //_context.SaveChanges();
            await _context.SaveChangesAsync();

            return Ok("Registration successful");
            //user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            //_context.Users.Add(user);
            //await _context.SaveChangesAsync();
            //return Ok();
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            var userLogin = _context.Users.FirstOrDefault(u => u.Username == user.Username);
            if (userLogin == null || !BCrypt.Net.BCrypt.Verify(user.Password, userLogin.Password))
                return Unauthorized("Invalid username or password");

            // Generate JWT token
            var token = GenerateJwtToken(userLogin);
            return Ok(new { token });
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new byte[32];
            new RNGCryptoServiceProvider().GetBytes(key);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
            new Claim(ClaimTypes.Name, user.Username)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        //[HttpPost("login")]
        //public IActionResult Login([FromBody] User user)
        //{
        //    var userLogin = _context.Users.FirstOrDefault(u => u.Username == user.Username);
        //    if (userLogin == null || !BCrypt.Net.BCrypt.Verify(user.Password, userLogin.Password))
        //        return Unauthorized("Invalid username or password");

        //    // Generate JWT token
        //    var token = GenerateJwtToken(userLogin);
        //    return Ok(new { token });
        //}
        //private string GenerateJwtToken(User user)
        //{
        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    //var key = Encoding.ASCII.GetBytes("salt"); // Replace this with your secret key
        //    var key = new byte[32]; // 32 byte'lık bir anahtar oluşturuluyor
        //    new RNGCryptoServiceProvider().GetBytes(key); // Rastgele veri ile anahtar dolduruluyor
        //    var tokenDescriptor = new SecurityTokenDescriptor
        //    {
        //        Subject = new ClaimsIdentity(new Claim[]
        //        {
        //            new Claim(ClaimTypes.Name, user.Username)
        //        }),
        //        Expires = DateTime.UtcNow.AddDays(7), // Token expiration time
        //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //    };

        //    var token = tokenHandler.CreateToken(tokenDescriptor);
        //    return tokenHandler.WriteToken(token);
        //}
    }
}
