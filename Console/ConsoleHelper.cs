using System.Text.RegularExpressions;

namespace Console
{
    internal enum Commands
    {
        clear,
        create,
        delete,
        exit,
        help,
        list,
        quit,
        qqq,
        empty
    }

    internal static partial class ConsoleHelper
    {
        public static bool TryParse(string input, out ConsoleCommand command)
        {
            command = new();

            var segments = ParseInput(input);

            if (segments.Length > 0 && Enum.TryParse(segments[0].ToLower(), out Commands parsed))
            {
                command.CommandName = parsed;
            }

            if (segments.Length > 1 && ValidateTrialName(segments[1]))
            {
                command.TrialName = segments[1];
            }

            if (segments.Length > 2 && ValidateClientCode(segments[2]))
            {
                command.ClientCode = segments[2].ToLower();
            }

            return command.CommandName is not Commands.empty;
        }

        [GeneratedRegex(@"\w+|""[\w\s]*""")]
        private static partial Regex InputTextRegex();

        private static string[] ParseInput(string input) => InputTextRegex().Matches(input).Cast<Match>().Select(x => x.Value.Replace("\"", string.Empty)).ToArray();

        private static bool ValidateClientCode(string clientCode)
        {
            if (clientCode.Length > 4)
            {
                throw new ArgumentException("ConsoleHelper: ValidateClientCode - Length > 4");
            }
            return !string.IsNullOrWhiteSpace(clientCode);
        }

        private static bool ValidateTrialName(string trialName)
        {
            return !string.IsNullOrWhiteSpace(trialName);
        }
    }
}