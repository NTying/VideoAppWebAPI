namespace TyingVideoWebAPI.DTO
{
    public class JWTSettings
    {
        public string? SecretKey { get; set; }
        public int ExpireSecond { get; set; }
    }
}
