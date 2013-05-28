
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

let java =
    if Environment.OSVersion.Platform = PlatformID.Unix then
        "java"
    else
        @"C:\Program Files\Java\jdk1.7.0_17\bin\java.exe" //To be replaced with java.exe
let directory = __SOURCE_DIRECTORY__

let path = Path.Combine("target","scala-2.10","orbit.jar")
let processors = Environment.ProcessorCount

Console.Write("Choose element type [1 -> int64; 2 -> bigint] (default = 1): ")
let t = try Console.ReadLine() |> int with _ -> 1

let nOfReruns = 10

let runAndProcessResult implementationName (processorsToUse, precision, M, G, implementation) =
    let affinity = makeAffinityNum processors processorsToUse
    let affinityString = toHexString affinity
    let psi = 
        makePSI <||
            if Environment.OSVersion.Platform = PlatformID.Unix then
                @"taskset", 
                sprintf """%s java -jar %s""" affinityString path
            else 
                java,
                sprintf """-Xmx4g -XX:MaxPermSize=256m -jar %s""" path
            <| processorsToUse
    let rec runAndProcessResultHelper nOfErrors r t =        
        let stdout, stderr = 
            use proc = Process.Start(psi, ProcessorAffinity = nativeint affinity)
            proc.StandardInput.AutoFlush <- true
            proc.StandardInput.WriteLine(sprintf "%d\n%d\n%d\n%d\n\n%d\n\n" nOfReruns precision M G implementation)
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
let processorsToUseList = [
    for i = 0 to powerOf2 processors do 
        yield 2.0 ** (float i) |> int
]

let run choice =
    let path = 
        match choice with
        | 1 -> 
            let p = 
                Path.Combine(
                    directory, 
                    "timesInt64_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
                )
            Console.WriteLine("int64 results file: " + p)
            p
        | 2 ->
            let p = 
                Path.Combine(
                    directory,
                    "timesBigInt_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
                )
            Console.WriteLine("bigint results file: " + p)
            p
        | _ -> failwith "Invalid"
    use stream = File.Create(path)
    use writer = new StreamWriter(stream)
    writer.AutoFlush <- true
    fprintfn writer "Implementation,Number of Workers,Chunk Size,Processors to Use,Result,Time Elapsed"
    for i, implementation in [1, "Sequential with ImmutableSet"; 2, "Sequential with MutableSet"] do
        for n in processorsToUseList do
        runAndProcessResult implementation (n, choice, 1, -1, i)
        |> Seq.iter (fprintfn writer "%s")
    for i, implementation in [3, "Parallel Collections"; 4, "Parallel Collections with ConcurrentMap"] do
        for n in processorsToUseList do
        for M in MList |> Seq.filter ((>=) n) do
        runAndProcessResult implementation (n, choice, M, -1, i)
        |> Seq.iter (fprintfn writer "%s")
    for i, implementation in [ 5, "Futures with ConcurrentMap" ] do
        for n in processorsToUseList do
        for M in MList |> Seq.filter ((>=) n) do 
        for G in GList do
        runAndProcessResult implementation (n, choice, M, G, i)
        |> Seq.iter (fprintfn writer "%s")
    for i, implementation in [6, "Akka with ImmutableSet"; 7, "Akka with MutableSet"; 8, "Akka with ConcurrentMap" ] do
        for n in processorsToUseList do
        for G in GList do
        runAndProcessResult implementation (n, choice, 1, G, i)
        |> Seq.iter (fprintfn writer "%s")
    for i, implementation in [9, "Akka System with Actor Workers"] do
        for n in processorsToUseList do
        for M in MList |> Seq.filter ((>=) n) do 
        for G in GList do
        runAndProcessResult implementation (n, choice, M, G, i)
        |> Seq.iter (fprintfn writer "%s")

do
    run t
