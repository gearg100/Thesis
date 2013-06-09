
open System
open System.Diagnostics
open System.IO
open System.Linq

[<AutoOpen>]
module Helpers =
    let inline split (c:char) (str:string) = str.Split(c)
    let inline trim (str:string) = str.Trim()
    let inline startsWith (start:string) (str:string) = str.StartsWith(start)

let (|Result|) (stdout:string) = //result and time
    let lines = 
        stdout
        |> split '\n'
        |> Seq.map trim
        |> Seq.skip 1
        |> Seq.filter (not<<(startsWith "Press"))
        |> Seq.toArray
    int64 (lines.[0].Split(' ').[1]), int (lines.[1].Split(' ').[2])


let makePSI fileName arguments =
    ProcessStartInfo( 
        FileName = fileName,
        Arguments = arguments,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    )

let psi = 
    if Environment.OSVersion.Platform = PlatformID.Unix then
        makePSI @"mono" @"--gc=sgen --runtime=v4.0 ./target/OrbitBigInt.exe"
    else
        makePSI @"./target/OrbitBigInt.exe" @""

let rec runAndProcessResult nOfReruns (psi:ProcessStartInfo) M N G (input:string) =
    printf "Running for input (%d, %d, %d)... " M N G
    let stdout, stderr = 
        use proc = Process.Start(psi)
        proc.StandardInput.AutoFlush <- true
        proc.StandardInput.WriteLine(input+"\n")
        proc.WaitForExit()
        proc.StandardOutput.ReadToEnd().Trim(), proc.StandardError.ReadToEnd().Trim()
    if stderr <> "" then
        if nOfReruns< 5 then 
            runAndProcessResult (nOfReruns + 1) psi M N G input
        else
            printfn "error"
            None
    else
        let (Result(result, timeElapsed)) = stdout
        let resStr = sprintf "Result: %d in %d ms" result timeElapsed
        printfn "%s" resStr
        Some(resStr)

Console.Write("nOfTimes each test will run: ")
let times = int <| Console.ReadLine()
do 
    use writer = new StreamWriter(@"timesBigInt",true)
    writer.AutoFlush <- true
    for M in [1;2;4;8;16;32;64] do
      for N in [for i in [1;2;4;8;16] do if i <= M then yield i] do
        for G in [1000; 5000; 10000; 20000; 50000] do
            match runAndProcessResult 0 psi M N G (sprintf "%d\n%d\n%d\naa\n" M N G) with
            |Some(resultString) ->
                writer.WriteLine(sprintf "(%d, %d, %d): %s" M N G resultString)
            |None ->
                writer.WriteLine("(%d, %d, %d): error")
            



