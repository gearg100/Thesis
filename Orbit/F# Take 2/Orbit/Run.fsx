
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
    m.Groups.[1].Value, m.Groups.[2].Value

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

let directory = __SOURCE_DIRECTORY__

let path = __SOURCE_DIRECTORY__ + "/bin/Release/Orbit.exe"

let psi = 
    if Environment.OSVersion.Platform = PlatformID.Unix then
        makePSI @"mono" <| "--gc=sgen --runtime=v4.0 " + "\"" + path + "\""
    else
        makePSI path @""

let rec runAndProcessResult nOfReruns (psi:ProcessStartInfo) (precision, M, G, i as inputParams) =
    printf "Running for input (%d, %d)... " M G
    let stdout, stderr = 
        use proc = Process.Start(psi)
        proc.StandardInput.AutoFlush <- true
        proc.StandardInput.WriteLine(sprintf "%d\n%d\n%d\n%d\n\n" precision M G i)
        proc.WaitForExit()
        proc.StandardOutput.ReadToEnd().Trim(), proc.StandardError.ReadToEnd().Trim()
    if stderr <> "" then
        if nOfReruns< 5 then 
            printfn "retry"
            runAndProcessResult (nOfReruns + 1) psi inputParams
        else
            printfn "error"
            sprintf "%d,%d,%s,%s" M G "<error>" "<error>"
    else
        let resultLine =  stdout |> split '\n' |> last
        let (TimedResult(result, timeElapsed)) = resultLine
        printfn "%s" resultLine
        sprintf "%d,%d,%s,%s" M G result timeElapsed

Console.Write("nOfTimes each test will run: ")
let times = int <| Console.ReadLine()

let int64ResultPath =
    directory + @"/timesInt64_" 
        + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
let bigintResultPath =
    directory + @"/timesBigInt_" 
        + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
Console.WriteLine("int64 results file: " + int64ResultPath)
Console.WriteLine("bigint results file: " + bigintResultPath)

let MList = [1;2;4;8;16;32;64]
let GList = [500;1000; 5000;10000;50000]

let run choice =
    use stream = File.Create(if choice = 1 then int64ResultPath else bigintResultPath)
    use writer = new StreamWriter(stream)
    writer.AutoFlush <- true
    fprintfn writer "Implementation,Number of Workers,Chunk Size,Result,Time Elapsed"
    //Sequential
    do
        fprintfn writer "%s,%s" "Sequential" 
            <| runAndProcessResult 0 psi (choice, 1, 1, 1)
    for G in GList do
        fprintfn writer "%s,%s" "Async Workflows" 
            <| runAndProcessResult 0 psi (choice, 1, G, 3)
    for i, implementation in [4, "Tasks"; 5, "Agents"] do
    for M in MList do
    for G in GList do
        fprintfn writer "%s,%s" implementation 
            <| runAndProcessResult 0 psi (choice, M, G, i)

do
    run 1
    run 2
            



