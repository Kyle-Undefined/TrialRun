namespace Console
{
    internal class ConsoleCommand
    {
        public string ClientCode { get; set; } = string.Empty;
        public Commands CommandName { get; set; } = Commands.empty;
        public string TrialName { get; set; } = string.Empty;
    }
}