using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TyingVideoWebAPI.DTO;
using TyingVideoWebAPI.Model;

namespace TyingVideoWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class RegisterController : ControllerBase
    {
        private readonly RoleManager<MyRole> _roleManager;
        private readonly UserManager<MyUser> _userManager;

        public RegisterController(RoleManager<MyRole> roleManager, UserManager<MyUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<ActionResult<string>> Register(RegisterRequest registerRequest)
        {
            string userName = registerRequest.UserName;
            string email = registerRequest.EMail;
            string password = registerRequest.Password;

            if (await _roleManager.RoleExistsAsync("subscriptor") == false)
            {
                MyRole role = new MyRole() { Name = "subscriptor" };
                var result = await _roleManager.CreateAsync(role);

                // RoleManger、UserManager 等里面的方法执行异常不会抛出，里面有 Succeed 字段（指示执行结果）、Errors（记录错误信息）
                if (!result.Succeeded)
                    return BadRequest("插入角色失败");
            }

            MyUser user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                user = new MyUser { UserName = userName, Email = email };
                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                    return BadRequest("创建用户失败");
            }

            if (await _userManager.IsInRoleAsync(user, "subscriptor") == false)
            {
                var result = await _userManager.AddToRoleAsync(user, "subscriptor");
                if (!result.Succeeded)
                    return BadRequest("分配角色失败");
            }
            return "Ok";
        }
    }
}
