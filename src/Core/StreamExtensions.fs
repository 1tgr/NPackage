module NPackage.Core.StreamExtensions

open System.IO

let rec copyWith (buffer : array<byte>) (inputStream : Stream) (outputStream : Stream) =
    match inputStream.Read(buffer, 0, buffer.Length) with
    | count when count > 0 ->
        outputStream.Write(buffer, 0, count)
        copyWith buffer inputStream outputStream

    | _ -> ()

let copy inputStream outputStream =
    copyWith (Array.zeroCreate 4096) inputStream outputStream
