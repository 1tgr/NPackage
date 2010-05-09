using System;
using System.Collections.Generic;
using NPackage.Core;
using SystemConsole = System.Console;

namespace NPackage.Console
{
    internal static class Program
    {
        private static readonly Dictionary<string, Func<string, ICommand>> commands = 
            new Dictionary<string, Func<string, ICommand>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "help", delegate { return CreateHelpCommand(); } },
                { "install", delegate { return new InstallCommand(); } },
            };

        private static HelpCommand CreateHelpCommand()
        {
            return new HelpCommand(commands);
        }

        private static int Main(string[] args)
        {
            try
            {
                ICommand command;
                Func<string, ICommand> func;
                if (args.Length > 0 &&
                    commands.TryGetValue(args[0], out func))
                {
                    command = func(args[0]);
                }
                else
                    command = CreateHelpCommand();

                string[] otherArgs;
                if (args.Length > 0)
                {
                    otherArgs = new string[args.Length - 1];
                    Array.Copy(args, 1, otherArgs, 0, otherArgs.Length);
                }
                else
                    otherArgs = new string[] { };

                command.ParseOptions(otherArgs);
                return command.Run();
            }
            catch (Exception ex)
            {
                SystemConsole.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}