module NPackage.Core.Archive

open System
open System.IO
open ICSharpCode.SharpZipLib.GZip
open ICSharpCode.SharpZipLib.Tar
open ICSharpCode.SharpZipLib.Zip

let extract (archiveFilename : string) entryName filename =
    let notFoundInArchive archiveFilename entryName =
        let message = String.Format("There is no {0} in {1}.", entryName, archiveFilename)
        new InvalidOperationException(message)

    let rec findTarEntry (stream : TarInputStream) entryName =
        match stream.GetNextEntry() with
        | null -> None
        | entry ->
            if String.Equals(entry.Name, entryName, StringComparison.InvariantCultureIgnoreCase) then
                Some entry
            else
                findTarEntry stream entryName

    if archiveFilename.EndsWith(".zip", StringComparison.InvariantCultureIgnoreCase) then
        use file = new ZipFile(archiveFilename)
        match file.FindEntry(entryName, true) with
        | index when index >= 0 ->
            use inputStream = file.GetInputStream(int64 index)
            use outputStream = File.Create(filename)
            StreamExtensions.copy inputStream outputStream

        | _ ->
            raise (notFoundInArchive archiveFilename entryName)

    else if archiveFilename.EndsWith(".tar.gz", StringComparison.InvariantCultureIgnoreCase) || archiveFilename.EndsWith(".tgz", StringComparison.InvariantCultureIgnoreCase) then
        use fileStream = File.Open(archiveFilename, FileMode.Open, FileAccess.Read)
        use gzipStream = new GZipInputStream(fileStream)
        use tarStream = new TarInputStream(gzipStream)
        match findTarEntry tarStream entryName with
        | Some entry ->
            use outputStream = File.Create(filename)
            tarStream.CopyEntryContents(outputStream)

        | None ->
            raise (notFoundInArchive archiveFilename entryName)
    else
        raise (new NotSupportedException(archiveFilename + " is not a recognised archive."))
