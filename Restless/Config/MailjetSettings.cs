namespace Restless.Config
{
    public class MailjetSettings
    {
        public string ApiKey { get; set; } = default!;
        public string ApiSecret { get; set; } = default!;
        public string FromEmail { get; set; } = default!;
        public string FromName { get; set; } = default!;
        public long TemplateId { get; set; }
    }
}
