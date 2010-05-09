using System;
using System.Collections.Generic;

namespace NPackage.Core
{
    public class HelpCommand : ICommand
    {
        private readonly IDictionary<string, Func<string, ICommand>> commands;
        private readonly List<string> extraArguments = new List<string>();

        public HelpCommand(IDictionary<string, Func<string, ICommand>> commands)
        {
            this.commands = commands;
        }

        public void ParseOptions(IEnumerable<string> arguments)
        {
            extraArguments.AddRange(arguments);
        }

        public int Run()
        {
            if (extraArguments.Count == 0)
            {
                Console.WriteLine("Usage:");

                foreach (string name in commands.Keys)
                    Console.WriteLine("        {0}", name);
            }
            else
            {
                foreach (string name in extraArguments)
                {
                    Func<string, ICommand> func;
                    if (commands.TryGetValue(name, out func))
                    {
                        ICommand command = func(name);
                        CommandBase commandBase = command as CommandBase;
                        if (commandBase != null)
                        {
                            commandBase.ShowHelp = true;
                            commandBase.Run();
                        }
                    }
                }
            }

            return 0;
        }
    }
}