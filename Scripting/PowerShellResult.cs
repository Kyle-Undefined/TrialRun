namespace Scripting
{
    public struct PowerShellResult(string output, bool error, string errorMessage)
    {
        public bool Error { get; set; } = error;
        public string ErrorMessage { get; set; } = errorMessage;
        public string Output { get; set; } = output;
    }
}