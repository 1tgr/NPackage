using System.Collections.Generic;

namespace NPackage.Core
{
    public class Package
    {
        private readonly Dictionary<string, Library> library = new Dictionary<string, Library>();

        /*
            Name:	nunit
            Version:	2.5.5.10112
            Description:	Test framework for all .Net languages, running on Microsoft .NET and Mono
            Author:	Charlie Poole
            Maintainer:	tim.g.robinson@gmail.com
            Master-Sites:	http://launchpad.net/nunitv2/2.5/2.5.5/+download/

            Library: nunit.framework.dll
                Binary: NUnit-2.5.5.10112.zip#bin/net-2.0/nunit.framework.dll
         */

        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Maintainer { get; set; }
        public string MasterSites { get; set; }

        public IDictionary<string, Library> Library
        {
            get { return library; }
        }
    }
}