{ "Packages": [
{
  "Name": "fsharp.core",
  "Version": "2.0.0.0",
  "Description": "F# redistributable",
  "Author": "Microsoft",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "fsharp.core-2.0.0.0/" ],
  "Libraries": {
    "FSharp.Core.dll": {
      "Binary": "FSharp.Core.zip#FSharp.Core.dll"
    },
    "FSharp.Core.optdata": {
      "Binary": "FSharp.Core.zip#FSharp.Core.optdata"
    },
    "FSharp.Core.sigdata": {
      "Binary": "FSharp.Core.zip#FSharp.Core.sigdata"
    },
    "FSharp.Core.xml": {
      "Binary": "FSharp.Core.zip#FSharp.Core.xml"
    }
  }
}
,
{
  "Name": "json",
  "Version": "7",
  "Description": "Makes working with JSON formatted data in .NET simple",
  "Author": "James Newton-King",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "json-7/" ],
  "Libraries": {
    "Newtonsoft.Json.dll": {
      "Binary": "Json35r7.zip#Bin/DotNet/Newtonsoft.Json.dll"
    },
    "Newtonsoft.Json.pdb": {
      "Binary": "Json35r7.zip#Bin/DotNet/Newtonsoft.Json.pdb"
    },
    "Newtonsoft.Json.xml": {
      "Binary": "Json35r7.zip#Bin/DotNet/Newtonsoft.Json.xml"
    }
  }
}
,
{
  "Name": "log4net",
  "Version": "1.2.10",
  "Description": "Tool to help the programmer output log statements to a variety of output targets",
  "Author": "Apache",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://archive.apache.org/dist/incubator/log4net/1.2.10/" ],
  "Libraries": {
    "log4net.dll": {
      "Binary": "incubating-log4net-1.2.10.zip#log4net-1.2.10/bin/net/2.0/release/log4net.dll"
    },
    "log4net.xml": {
      "Binary": "incubating-log4net-1.2.10.zip#log4net-1.2.10/bin/net/2.0/release/log4net.xml"
    }
  }
},
{
  "Name": "mono.cecil",
  "Version": "0.6",
  "Description": "Library to generate and inspect programs and libraries in the ECMA CIL form",
  "Author": "Jb Evain",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://mono.ximian.com/daily/" ],
  "Libraries": {
    "Mono.Cecil.dll": {
      "Binary": "monocharge-20100503.tar.gz#monocharge-20100503/2.0/Mono.Cecil.dll"
    },
    "Mono.Cecil.Mdb.dll": {
      "Binary": "monocharge-20100503.tar.gz#monocharge-20100503/2.0/Mono.Cecil.Mdb.dll"
    }
  }
},
{
  "Name": "mono.options",
  "Version": "0.2.1",
  "Description": "Callback-based program option parser for C#",
  "Author": "Jonathan Pryor",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://mono.ximian.com/daily/" ],
  "Libraries": {
    "Mono.Options.dll": {
      "Binary": "monocharge-20100503.tar.gz#monocharge-20100503/2.0/Mono.Options.dll"
    }
  }
},
{
  "Name": "nhibernate",
  "Version": "2.1.2",
  "Description": ".NET port of the excellent Java Hibernate which provides Object/Relational mapping to persist objects in a relational database",
  "Author": "Ayende Rahien",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://downloads.sourceforge.net/project/nhibernate/NHibernate/2.1.2GA/" ],
  "Requires": [ "log4net" ],
  "Libraries": {
    "NHibernate.dll": {
      "Binary": "NHibernate-2.1.2.GA-bin.zip#Required_Bins/NHibernate.dll"
    },
    "NHibernate.xml": {
      "Binary": "NHibernate-2.1.2.GA-bin.zip#Required_Bins/NHibernate.xml"
    }
  }
},
{
  "Name": "npackage",
  "Version": "daily",
  "Description": "Packaging and distribution system for .NET",
  "Author": "Tim Robinson",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://build.partario.com/guestAuth/repository/download/bt2/.lastSuccessful/" ],
  "Requires": [ "fsharp.core", "sharpziplib", "mono.options", "json" ],
  "CopyLocal": true,
  "Libraries": {
    "NPackage.exe": {
      "Binary": "NPackage.zip#NPackage.exe"
    },
    "NPackage.exe.config": {
      "Binary": "NPackage.zip#NPackage.exe.config"
    },
    "NPackage.pdb": {
      "Binary": "NPackage.zip#NPackage.pdb"
    },
    "NPackage.Core.dll": {
      "Binary": "NPackage.zip#NPackage.Core.dll"
    },
    "NPackage.Core.pdb": {
      "Binary": "NPackage.zip#NPackage.Core.pdb"
    },
    "NPackage.Core.FSharp.dll": {
      "Binary": "NPackage.zip#NPackage.Core.FSharp.dll"
    },
    "NPackage.Core.FSharp.pdb": {
      "Binary": "NPackage.zip#NPackage.Core.FSharp.pdb"
    },
    "NPackage.Core.dll": {
      "Binary": "NPackage.zip#NPackage.Core.dll"
    },
    "NPackage.Core.pdb": {
      "Binary": "NPackage.zip#NPackage.Core.pdb"
    },
  }
}
,
{
  "Name": "nunit",
  "Version": "2.5.5.10112",
  "Description": "Test framework for all .Net languages, running on Microsoft .NET and Mono",
  "Author": "Charlie Poole",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://launchpad.net/nunitv2/2.5/2.5.5/+download/" ],
  "Libraries": {
    "nunit.framework.dll": {
      "Binary": "NUnit-2.5.5.10112.zip#NUnit-2.5.5.10112/bin/net-2.0/framework/nunit.framework.dll"
    },
    "nunit.framework.xml": {
      "Binary": "NUnit-2.5.5.10112.zip#NUnit-2.5.5.10112/bin/net-2.0/framework/nunit.framework.xml"
    }
  }
},
{
  "Name": "quickgraph",
  "Version": "3.3.40824.00",
  "Description": "Generic Graph Data Structures and Algorithms for .Net",
  "Author": "Jonathan de Halleux",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://download.codeplex.com/Project/Download/" ],
  "Libraries": {
    "QuickGraph.dll": {
      "Binary": "FileDownload.aspx?ProjectName=quickgraph&DownloadId=50945&FileTime=128956893024170000&Build=16586#QuickGraph.dll"
    },
    "QuickGraph.xml": {
      "Binary": "FileDownload.aspx?ProjectName=quickgraph&DownloadId=50945&FileTime=128956893024170000&Build=16586#QuickGraph.xml"
    },
    "QuickGraph.Data.dll": {
      "Binary": "FileDownload.aspx?ProjectName=quickgraph&DownloadId=50945&FileTime=128956893024170000&Build=16586#QuickGraph.Data.dll"
    },
    "QuickGraph.Data.xml": {
      "Binary": "FileDownload.aspx?ProjectName=quickgraph&DownloadId=50945&FileTime=128956893024170000&Build=16586#QuickGraph.Data.xml"
    },
    "QuickGraph.Glee.dll": {
      "Binary": "FileDownload.aspx?ProjectName=quickgraph&DownloadId=50945&FileTime=128956893024170000&Build=16586#QuickGraph.Glee.dll"
    },
    "QuickGraph.Glee.xml": {
      "Binary": "FileDownload.aspx?ProjectName=quickgraph&DownloadId=50945&FileTime=128956893024170000&Build=16586#QuickGraph.Glee.xml"
    },
    "QuickGraph.Graphviz.dll": {
      "Binary": "FileDownload.aspx?ProjectName=quickgraph&DownloadId=50945&FileTime=128956893024170000&Build=16586#QuickGraph.Graphviz.dll"
    },
    "QuickGraph.Graphviz.xml": {
      "Binary": "FileDownload.aspx?ProjectName=quickgraph&DownloadId=50945&FileTime=128956893024170000&Build=16586#QuickGraph.Graphviz.xml"
    }
  }
},
{
  "Name": "ravendb.client",
  "Version": "0.63",
  "Description": "Document database for the .NET/Windows platform",
  "Author": "Ayende Rahien",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://s3.amazonaws.com/daily-builds/" ],
  "Libraries": {
    "Raven.Client-3.5.dll": {
      "Binary": "Raven-Build-63.zip#Client-3.5/Raven.Client-3.5.dll"
    },
  }
},
{
  "Name": "ravendb.server",
  "Version": "0.63",
  "Description": "Document database for the .NET/Windows platform",
  "Author": "Ayende Rahien",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://s3.amazonaws.com/daily-builds/" ],
  "Libraries": {
    "RavenDb.exe": {
      "Binary": "Raven-Build-63.zip#Server/RavenDb.exe"
    },
    "RavenDb.exe.config": {
      "Binary": "Raven-Build-63.zip#Server/RavenDb.exe.config"
    },
  }
},
{
  "Name": "rhino.mocks",
  "Version": "3.6",
  "Description": "Dynamic mock object framework for the .Net platform",
  "Author": "Ayende Rahien",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://www.ayende.com/20/section.aspx/download/" ],
  "Libraries": {
    "Rhino.Mocks.dll": {
      "Binary": "234#Rhino.Mocks.dll"
    },
    "Rhino.Mocks.xml": {
      "Binary": "234#Rhino.Mocks.xml"
    }
  }
},
{
  "Name": "sharpziplib",
  "Version": "0.85.5",
  "Description": "Zip, GZip, Tar and BZip2 library written entirely in C# for the .NET platform.",
  "Author": "IC# Code",
  "Maintainer": "tim.g.robinson@gmail.com",
  "MasterSites": [ "http://downloads.sourceforge.net/project/sharpdevelop/SharpZipLib/0.85.5/" ],
  "Libraries": {
    "ICSharpCode.SharpZipLib.dll": {
      "Binary": "SharpZipLib_0855_Bin.zip#net-20/ICSharpCode.SharpZipLib.dll"
    }
  }
},
] }
