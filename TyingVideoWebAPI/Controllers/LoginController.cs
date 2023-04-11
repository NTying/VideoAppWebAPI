using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TyingVideoWebAPI.DTO;
using TyingVideoWebAPI.Model;
using TyingVideoWebAPI.Utils;

namespace TyingVideoWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class LoginController : ControllerBase
    {
        private readonly RoleManager<MyRole> _roleManager;
        private readonly UserManager<MyUser> _userManager;
        private readonly IOptionsSnapshot<JWTSettings> _jwtSettings;
        private readonly RedisHelper<MyUser> _redisHelper;

        public LoginController(RoleManager<MyRole> roleManager, UserManager<MyUser> userManager, IOptionsSnapshot<JWTSettings> jwtSettings, RedisHelper<MyUser> redisHelper)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _jwtSettings = jwtSettings;
            _redisHelper = redisHelper;
        }

        [HttpPost]
        public async Task<ActionResult<string>> Login(LoginRequest loginRequest)
        {
            string userName = loginRequest.UserName;
            string password = loginRequest.Password;
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null)
            {
                return BadRequest("用户名或者密码错误");
            }
            if (await _userManager.IsLockedOutAsync(user))
            {
                return BadRequest($"账户被锁定，锁定结束时间{user.LockoutEnabled}");
            }
            if (await _userManager.CheckPasswordAsync(user, password))
            {
                await _userManager.ResetAccessFailedCountAsync(user);

                var roles = await _userManager.GetRolesAsync(user);
                List<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.Name, userName));
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                string key = _jwtSettings.Value.SecretKey;
                DateTime expire = DateTime.Now.AddSeconds(_jwtSettings.Value.ExpireSecond);

                var keyBytes = Encoding.UTF8.GetBytes(key);
                var secretKey = new SymmetricSecurityKey(keyBytes);

                var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256Signature);
                var tokenDescriptor = new JwtSecurityToken(claims: claims, expires: expire, signingCredentials: credentials);
                string jwt = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

                _redisHelper.Set(userName, jwt);
                return Ok(jwt);
            }
            else
            {
                await _userManager.AccessFailedAsync(user);
                return BadRequest("用户名或者密码错误");
            }

        }
    }
}
