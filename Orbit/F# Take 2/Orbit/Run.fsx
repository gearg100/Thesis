
open System
open System.Diagnostics
open System.IO
open System.Linq

[<AutoOpen>]
module Helpers =
    let inline split (c:char) (str:string) = str.Split(c)
    let inline trim (str:string) = str.Trim()
    let inline startsWith (start:string) (str:string) = str.StartsWith(start)
    let inline last (ar: _ []) = ar.[ar.Length-1]

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

let path = Path.Combine(directory,"bin","Release","Orbit.exe")

let processors = Environment.ProcessorCount

Console.Write("nOfTimes each test will run: ")
let times = int <| Console.ReadLine()

let runAndProcessResult implementationName (processorsToUse, precision, M, G, implementation) =
    let affinity = makeAffinityNum processors processorsToUse
    let affinityString = toHexString affinity
    let psi = 
        makePSI <||
            if Environment.OSVersion.Platform = PlatformID.Unix then
                @"taskset", 
                sprintf """%s mono --gc=sgen --runtime=v4.0 "%s" """ affinityString path
            else 
                path,
                ""
            <| processorsToUse
    let rec runAndProcessResultHelper nOfReruns nOfErrors r t =        
        let stdout, stderr = 
            use proc = Process.Start(psi, ProcessorAffinity = nativeint affinity)
            proc.StandardInput.AutoFlush <- true
            proc.StandardInput.WriteLine(sprintf "%d\n%d\n%d\n%d\n\n\n" precision M G implementation)
            proc.WaitForExit()
            proc.StandardOutput.ReadToEnd().Trim(), proc.StandardError.ReadToEnd().Trim()
        if stderr <> "" then
            if nOfErrors < 5 then 
                printfn "retry"
                runAndProcessResultHelper nOfReruns (nOfErrors + 1) r t
            else
                printfn "error"
                "<error>", [ -1L ]
        elif nOfReruns < times then
            let resultLine =  stdout |> split '\n' |> last
            let (TimedResult(result, timeElapsed)) = resultLine
            printf "@ %s" resultLine
            runAndProcessResultHelper (nOfReruns + 1) nOfErrors result (timeElapsed :: t)
        else 
            printfn ""
            r, t
    printf "Running '%s' for input (%d, %d, %d)..." implementationName M G processorsToUse
    let (result, timesElapsed) = runAndProcessResultHelper 0 0 "" []
    sprintf "%s,%d,%d,%d,%s,%d" implementationName M G processorsToUse result (timesElapsed |> Seq.averageBy float |> round |> int64)

let int64ResultPath = 
    Path.Combine(
        directory, 
        "timesInt64_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
    )
let bigintResultPath =
    Path.Combine(
        directory,
        "timesBigInt_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
    )

Console.WriteLine("int64 results file: " + int64ResultPath)
Console.WriteLine("bigint results file: " + bigintResultPath)

let MList = [1;2;4]//;8;16;32;64] //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
let GList = [1; 10; 100;]// 500;1000;5000;10000;50000] //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
let processorsToUseList = [
    for i = 0 to powerOf2 processors do 
        yield 2.0 ** (float i) |> int
]

let run choice =
    use stream = File.Create(if choice = 1 then int64ResultPath else bigintResultPath)
    use writer = new StreamWriter(stream)
    writer.AutoFlush <- true
    fprintfn writer "Implementation,Number of Workers,Chunk Size,Processors to Use,Result,Time Elapsed"
//    for n in processorsToUseList do
//        fprintfn writer "%s"  
//            <| runAndProcessResult "Sequential" (n, choice, 1, 1, 1)
    for n in processorsToUseList do
        for M in MList do
            fprintfn writer "%s"  
                <| runAndProcessResult "PLinq" (n, choice, M, -1, 2)
//    for n in processorsToUseList do
//        for M in MList do
//            fprintfn writer "%s"  
//                <| runAndProcessResult "Parallel.ForEach" (n, choice, M, -1, 3)
    for i, implementation in [4, "Async Workflows"; 5, "TPL - Tasks"] do
        for n in processorsToUseList do
        for G in GList do
        fprintfn writer "%s"  
            <| runAndProcessResult implementation (n, choice, 1, G, i)
    for i, implementation in [ 6, "Agents"; 7, "ConcurrentSet"] do
        for n in processorsToUseList do
        for M in MList do 
            if M <= n then
                for G in GList do
                fprintfn writer "%s"  
                    <| runAndProcessResult implementation (n, choice, M, G, i)

do
    run 1
    run 2
