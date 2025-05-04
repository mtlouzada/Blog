namespace Blog;

public static class Configuration
{
    public static string JwtKey { get; private set; }
    public static string ApiKeyName { get; private set; }
    public static string ApiKey { get; private set; }
    public static SmtpConfiguration Smtp { get; private set; }

    public static void Load(IConfiguration configuration)
    {
        JwtKey = configuration["JwtKey"];
        ApiKeyName = configuration["ApiKeyName"];
        ApiKey = configuration["ApiKey"];

        var smtp = new SmtpConfiguration();
        configuration.GetSection("SmtpConfiguration").Bind(smtp);
        Smtp = smtp;
    }

    public class SmtpConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; } = 25;
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
