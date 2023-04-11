using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using TyingVideoWebAPI.Model;
using TyingVideoWebAPI.Utils;

namespace TyingVideoWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private readonly RedisHelper<MyUser> redisUtils;
        
        public UserController(RedisHelper<MyUser> redisUtils)
        {
            this.redisUtils = redisUtils;
        }
    }
}
