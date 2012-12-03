
open System
open System.Diagnostics
open System.IO
open System.Linq

let (|Result|) (str : string) = str.Split(' ').[1]
let (|TimeElapsed|) (str : string) = str.Split(' ').[2]

let run (psi:ProcessStartInfo) (args:string) = 
    use proc = Process.Start(psi)
    proc.StandardInput.AutoFlush <- true
    proc.StandardInput.WriteLine(args+"\n")
    proc.WaitForExit()
    proc.StandardOutput.ReadToEnd()

let psi = 
    ProcessStartInfo( 
        FileName = @"./target/Orbit.exe",
        UseShellExecute = false,
        RedirectStandardInput = true,
        RedirectStandardOutput = true
    )

Console.WriteLine("nOfTimes each test will run: ")
let times = int <| Console.ReadLine()
do 
    use writer = new StreamWriter(@"times",true)
    for M in [1;2;4;8;16;32;64] do
      for N in [for i in [1;2;4;8;16] do if i <= M then yield i] do
        for G in [1000; 5000; 10000; 20000] do
            let mutable totalTimeElapsed = 0
            let mutable res = -1L
            for t = 1 to times do
                let output = run psi (sprintf "%d\n%d\n%d\naa\n" M N G)
                let lines = 
                    query {
                        for line in output.Trim().Split('\n') do
                        let line = line.Trim()
                        skip 1
                        where (not <| line.StartsWith("Press"))
                        select line
                    } |> Seq.toArray
                let (Result result) = lines.[0]
                let (TimeElapsed timeElapsed) = lines.[1]
                totalTimeElapsed <- totalTimeElapsed + int timeElapsed
                res <- int64 result
            sprintf "%d, %d, %d, %d, %d" M N G res (totalTimeElapsed/times)
            |> writer.WriteLine



