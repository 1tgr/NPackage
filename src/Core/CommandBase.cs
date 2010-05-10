using System;
using System.Collections.Generic;
using Mono.Options;

namespace NPackage.Core
{
    public abstract class CommandBase : ICommand
    {
        private readonly List<string> extraArguments = new List<string>();

        protected virtual OptionSet CreateOptionSet()
        {
            return new OptionSet().Add("h|help", "show this message and exit", v => ShowHelp = v != null);
        }

        public void ParseOptions(IEnumerable<string> arguments)
        {
            OptionSet set = CreateOptionSet();
            extraArguments.AddRange(set.Parse(arguments));
        }

        protected abstract int RunCore();

        public int Run()
        {
            if (ShowHelp)
            {
                Console.WriteLine("Usage:");
                CreateOptionSet().WriteOptionDescriptions(Console.Out);
                return 0;
            }
            else
                return RunCore();
        }

        protected IList<string> ExtraArguments
        {
            get { return extraArguments; }
        }

        public bool ShowHelp { get; set; }
    }
}