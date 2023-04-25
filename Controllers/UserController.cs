using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using saleapp.DTO;
using saleapp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace saleapp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;

        public UserController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<UserController> logger,
            IConfiguration configuration
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _configuration = configuration;
        }

        // Add your controller methods here
        [HttpPost]
        [AllowAnonymous]
        [Route("register")]
        public async Task<IActionResult> Register([FromForm] RegisterUserDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var fileName = "";
            if (registerDto.Avatar != null)
            {
                fileName = Guid.NewGuid().ToString() + Path.GetExtension(registerDto.Avatar.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "images/avatar", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await registerDto.Avatar.CopyToAsync(stream);
                }

            }
            var user = new User { UserName = registerDto.Email, Email = registerDto.Email, FirstName = registerDto.FirstName, LastName = registerDto.LastName, DateOfBirth = registerDto.DateOfBirth, AvatarUrl = fileName };
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");
                await _userManager.AddToRoleAsync(user, "User");
                //await _userManager.AddToRolesAsync(user, new[] { "User", "Admin" });

                //await _userManager.AddToRoleAsync(user, "Admin");
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                // TODO: Send email confirmation link to user

                return Ok(new { message = "User created successfully" });
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }
        }
        

        [HttpPost]
        [AllowAnonymous]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("JwtSettings:Secret").Value);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
            new Claim(ClaimTypes.Name, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Aud, "your_audience"),
            new Claim(JwtRegisteredClaimNames.Iss, "your_issuer"),

                    // Add additional claims as needed
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { access_token = tokenString, name = user.FirstName +" "+ user.LastName });
        }

        [HttpGet]
        [Authorize]
        [Route("current-user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            //var user = await _userManager.GetUserAsync(User);
            var user = await _userManager.FindByEmailAsync(User.FindFirst(ClaimTypes.Email).Value);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                Avatar = "https://localhost:7097/images/avatar/" + user.AvatarUrl,
                Roles = roles,
            });
        }

        [HttpPost]
        [Authorize]
        [Route("register-shipper")]
        public async Task<IActionResult> RegisterShipper([FromForm] RegisterUserDto registerDto)
        {
            var userAdmin = await _userManager.FindByEmailAsync(User.FindFirst(ClaimTypes.Email).Value);
            var roles = await _userManager.GetRolesAsync(userAdmin);
            if (!roles.Contains("Admin"))
            {
                return Forbid("Only Admin can create shipper");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var fileName = "";
            if (registerDto.Avatar != null)
            {
                fileName = Guid.NewGuid().ToString() + Path.GetExtension(registerDto.Avatar.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "images/avatar", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await registerDto.Avatar.CopyToAsync(stream);
                }

            }
            var user = new User { UserName = registerDto.Email, Email = registerDto.Email, FirstName = registerDto.FirstName, LastName = registerDto.LastName, DateOfBirth = registerDto.DateOfBirth, AvatarUrl = fileName };
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");
                await _userManager.AddToRoleAsync(user, "User");
                //await _userManager.AddToRolesAsync(user, new[] { "User", "Admin" });

                //await _userManager.AddToRoleAsync(user, "Admin");
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token = token }, Request.Scheme);

                // TODO: Send email confirmation link to user

                return Ok(new { message = "User created successfully" });
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }
        }

    }

}

