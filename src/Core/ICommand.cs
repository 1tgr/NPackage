using System.Collections.Generic;

namespace NPackage.Core
{
    public interface ICommand
    {
        void ParseOptions(IEnumerable<string> arguments);
        int Run();
    }
}