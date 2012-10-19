namespace Concordance

open Agents
open Helpers
open Execution 

module Program = 
    [<EntryPoint>]
    let main (argv:string array)=            
        print("Give the times for the execution to be repeated and press <Enter>: [default = 1] ")
        let T = readLine() |> tryParseInt |> getOrElse 1

        print("Give maximum length of word sequences and press <Enter>: [default = 3] ")
        let S = readLine() |> tryParseInt |> getOrElse 3
        print("Give grainsize and press <Enter>: [default = 3000] ")
        let G = readLine() |> tryParseInt |> getOrElse 3000
        print("Give number of senders and press <Enter>: [default = 2] ")
        let M = readLine() |> tryParseInt |> getOrElse 2 
        print("Give number of hashtables(power of 2) and press <Enter>: [default = availableProcessors/2] ")
        let N = readLine() |> tryParseInt |> getOrElse (System.Environment.ProcessorCount/2)
        print("Give input filename: [default = ./bible.txt] ")
        let file = 
            let filename = readLine()
            if filename = "" then """bible.txt""" else filename
        run T file S G M N
        0