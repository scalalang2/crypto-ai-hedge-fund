using CommandLine;

Parser.Default.ParseArguments<Options>(args)
    .WithParsed<Options>(o =>
    {
        string Config(string what)
        {
            switch (what)
            {
                case "api_id": return o.ApiId;
                case "api_hash": return o.ApiHash;
                case "phone_number": return o.PhoneNumber;
                case "verification_code": Console.Write("Code: "); return Console.ReadLine();
                default: return null;
            }
        }
        
        using var client = new WTelegram.Client(Config);
        client.LoginUserIfNeeded().Wait();
    });

public class Options
{
    [Option('a', "api-id", Required = true, HelpText = "Telegram API ID")]
    public string ApiId { get; set; }
    
    [Option('h', "api-hash", Required = true, HelpText = "Telegram API Hash")]
    public string ApiHash { get; set; } = string.Empty;
    
    [Option('p', "phone-number", Required = true, HelpText = "Phone number for Telegram account")]
    public string PhoneNumber { get; set; } = string.Empty;
}