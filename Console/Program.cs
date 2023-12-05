using Application;
using System.Drawing;
using System.Text;
using Window = Colorful.Console;

namespace Console
{
    internal static class Program
    {
        private static readonly Color _appColor = Color.LightSkyBlue;
        private static readonly Color _successColor = Color.LightGreen;
        private static readonly Color _warningColor = Color.LightYellow;
        private static readonly CancellationTokenSource CancellationTokenSource = new();
        private static bool Processing = true;

        public static async Task RunAsync()
        {
            await Task.Run(async () =>
            {
                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var input = Window.ReadLine();

                        if (input is not null && !string.IsNullOrWhiteSpace(input))
                        {
                            if (ConsoleHelper.TryParse(input, out var command))
                            {
                                switch (command.CommandName)
                                {
                                    case Commands.clear:
                                        Window.Clear();
                                        break;

                                    case Commands.quit:
                                    case Commands.exit:
                                    case Commands.qqq:
                                        CloseProgram();
                                        break;

                                    case Commands.create:
                                        if (!string.IsNullOrWhiteSpace(command.ClientCode) && !string.IsNullOrWhiteSpace(command.TrialName))
                                        {
                                            Task<bool> createTask = App.CreateAsync(command.ClientCode, command.TrialName);
                                            Task<bool> completedTask = (Task<bool>)await Task.WhenAny(ShowProcessing(_appColor), createTask);

                                            var createResult = await completedTask;
                                            Processing = false;

                                            if (createResult)
                                            {
                                                Window.WriteLine("Trial Created Successfully", _successColor);
                                            }
                                        }
                                        else
                                        {
                                            Window.WriteLine("Please fill out the command properly.", _warningColor);
                                        }

                                        break;

                                    case Commands.delete:
                                        if (!string.IsNullOrWhiteSpace(command.TrialName))
                                        {
                                            Task removeTask = App.DeleteAsync(command.TrialName);
                                            Task completedTask = await Task.WhenAny(ShowProcessing(_appColor), removeTask);
                                            Processing = false;

                                            if (completedTask.IsCompletedSuccessfully)
                                            {
                                                Window.WriteLine("Trial Removed Successfully", _successColor);
                                            }
                                        }
                                        else
                                        {
                                            Window.WriteLine("Please fill out the command properly.", _warningColor);
                                        }

                                        break;

                                    case Commands.list:
                                        var helpResult = await App.ListTrialsAsync();

                                        Window.Write(helpResult, _appColor);
                                        break;

                                    case Commands.help:
                                        var str = new StringBuilder();
                                        str.AppendLine("Commands:");
                                        str.AppendLine("Clear - No Arguments - Clears the console");
                                        str.AppendLine("Create - 2 Arguments: TrialName, ClientCode - Spins up new VM and Databases using specified name");
                                        str.AppendLine("Delete - 1 Argument: Id - Spins down the specified VM and Databases");
                                        str.AppendLine("List - No Arguments - Lists all Trials");
                                        str.AppendLine("Quit / Exit / QQQ / Ctrl + C - No Arguments - Exits the application");

                                        Window.Write(str.ToString(), _appColor);
                                        break;

                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                Window.WriteLine("Command Not Recognized", _warningColor);
                            }
                        }

                        App.Error = false;
                    }
                    catch (Exception ex)
                    {
                        Window.WriteLine(ex.Message, _warningColor);
                    }
                }
            });
        }

        private static void CloseProgram()
        {
            var task = Task.Run(async delegate
            {
                Window.WriteLine("Closing Program", Color.OrangeRed);

                CancellationTokenSource.Cancel();
                await Task.WhenAny(ShowProcessing(Color.OrangeRed), Task.Delay(3000));
            });

            task.Wait();
        }

        private static void ContinuePrompt()
        {
            Window.WriteLine();
            Window.WriteLine("Press Enter To Continue And Clear The Console", _appColor);
            Window.ReadLine();
            Window.Clear();
        }

        private static async Task Main()
        {
            Window.CancelKeyPress += Window_CancelKeyPress;

            try
            {
                Window.WriteLine("Application Initializing", _appColor);

                var tasks = new List<Task>
                {
                    WaitForInitializationAsync(),
                    App.InitializeAsync()
                };
                await Task.WhenAll(tasks);

                Window.WriteLine("Application Ready", _appColor);

                ContinuePrompt();
                await RunAsync();
            }
            catch
            {
                Window.ReadLine();
            }
        }

        private static async Task ShowProcessing(Color color)
        {
            Processing = true;

            var spinner = new ConsoleSpinner(color, 200);

            while (Processing)
            {
                await Task.Run(() => spinner.Turn());
            }

            int currentLineCursor = Window.CursorTop;
            Window.SetCursorPosition(0, Window.CursorTop);
            Window.Write(new string(' ', Window.WindowWidth));
            Window.SetCursorPosition(0, currentLineCursor);
        }

        private static async Task WaitForInitializationAsync()
        {
            var spinner = new ConsoleSpinner(_appColor, 200);

            while (!App.Initialized && !App.Error && !CancellationTokenSource.IsCancellationRequested)
            {
                await Task.Run(() => spinner.Turn());
            }

            Window.WriteLine();
        }

        private static void Window_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            CloseProgram();
        }
    }
}