
open System
open System.Diagnostics
open System.IO
open System.Linq

[<AutoOpen>]
module Helpers =
    let inline split (c:char) (str:string) = str.Split(c)
    let inline trim (str:string) = str.Trim()
    let inline startsWith (start:string) (str:string) = str.StartsWith(start)
    let inline last n ar = Array.sub ar (Array.length ar - n) n

let (|TimedResult|) line =
    let m = 
        System.Text.RegularExpressions.Regex.Match(
            line, @"Result: (\d+) - Time Elapsed: (\d+) ms"
        )
    m.Groups.[1].Value, int64 m.Groups.[2].Value

let powerOf2 n = 
    System.Math.Log(float n, 2.0)
    |> ceil
    |> int

let inline makeAffinityNum total affinity =
    let charArray = Array.create total '0'
    for i in (total-1)..(- total / affinity)..0 do
        charArray.[i] <- '1'
    new System.String(charArray)
    |> fun s -> System.Convert.ToUInt64(s,2)

let inline toHexString n = (^a : (member ToString:string -> string) (n,"X")) 

let makePSI fileName arguments nOfProcessors=
    let psi = 
        ProcessStartInfo( 
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        )
    if psi.EnvironmentVariables.ContainsKey("NUMBER_OF_PROCESSORS") then
        psi.EnvironmentVariables.["NUMBER_OF_PROCESSORS"] <- string nOfProcessors
    else
        psi.EnvironmentVariables.Add("NUMBER_OF_PROCESSORS", string nOfProcessors)
    psi

let directory = __SOURCE_DIRECTORY__

let processors = Environment.ProcessorCount

let nOfReruns = 1

let runAndProcessResult implementationName (processorsToUse, M, G, implementation) =
    let affinity = makeAffinityNum processors processorsToUse
    let affinityString = toHexString affinity
    let psi = 
        makePSI <||
            if Environment.OSVersion.Platform = PlatformID.Unix then
                @"taskset", 
                sprintf """%s escript orbit.erl""" affinityString
            else 
                "escript",
                "orbit.erl"
            <| processorsToUse
    let rec runAndProcessResultHelper nOfErrors r t =        
        let stdout, stderr = 
            use proc = Process.Start(psi, ProcessorAffinity = nativeint affinity)
            proc.StandardInput.AutoFlush <- true
            proc.StandardInput.WriteLine(sprintf "%d\n%d\n%d\n%d\n20000\n10000\n8\n" nOfReruns M G implementation)
            proc.WaitForExit()
            proc.StandardOutput.ReadToEnd().Trim(), proc.StandardError.ReadToEnd().Trim()
        if stderr <> "" then
            Console.WriteLine(stderr)
            if nOfErrors < 5 then 
                printfn "retry"
                runAndProcessResultHelper (nOfErrors + 1) r t
            else
                printfn "error"
                seq ["<error>", -1L ]
        else
            let resultLines =  stdout |> split '\n' |> last nOfReruns
            resultLines |> Seq.iter (Console.WriteLine); Console.WriteLine()
            resultLines |> Seq.map (function TimedResult(result, timeElapsed) -> result, timeElapsed)
    printfn "Running '%s' for input (%d, %d, %d)..." implementationName M G processorsToUse
    runAndProcessResultHelper 0 "" []
    |> Seq.map (fun (result, timeElapsed) -> sprintf "%s,%d,%d,%d,%s,%d" implementationName M G processorsToUse result timeElapsed)

let MList = [1;2;4;8;16;32;64]
let GList = [1; 10; 100; 500;1000;5000;10000]
let processorsToUseList =
    [
        for i = 0 to powerOf2 processors do 
            yield 2.0 ** (float i) |> int
    ]

let run() =
    let path = 
        Path.Combine(
            directory, 
            "timesErlang_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
        )
    use stream = File.Create(path)
    use writer = new StreamWriter(stream)
    writer.AutoFlush <- true
    fprintfn writer "Implementation,Number of Workers,Chunk Size,Processors to Use,Result,Time Elapsed"
    for i, implementation in [1, "Sequential"] do
        for n in processorsToUseList do
        runAndProcessResult implementation (n, 1, -1, i)
        |> Seq.iter (fprintfn writer "%s")
    for i, implementation in [2, "Concurrent Tasks with Sequential Ets"; 3, "Concurrent Tasks with Concurrent Ets"] do
        for n in processorsToUseList do
        for G in GList do
        runAndProcessResult implementation (n, G, -1, i)
        |> Seq.iter (fprintfn writer "%s")
    for i, implementation in [ 4, "Concurrent with Workers and Concurrent Ets" ] do
        for n in processorsToUseList do
        for M in MList do 
        for G in GList do
        runAndProcessResult implementation (n, M, G, i)
        |> Seq.iter (fprintfn writer "%s")
do
    run()
