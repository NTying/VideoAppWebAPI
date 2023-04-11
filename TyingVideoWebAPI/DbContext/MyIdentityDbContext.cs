using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TyingVideoWebAPI.Model;

namespace TyingVideoWebAPI.DbContext
{
    public class MyIdentityDbContext : IdentityDbContext<MyUser, MyRole, long>
    {
        public MyIdentityDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}
