module NPackage.Core.MapExtensions
    let appendWith f = Map.fold (fun map1 key value2 -> 
            match Map.tryFind key map1 with
            | Some value1 -> Map.add key (f value1 value2) map1
            | None -> Map.add key value2 map1)
