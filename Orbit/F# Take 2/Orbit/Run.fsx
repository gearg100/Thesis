
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

let rec runAndProcessResult nOfReruns (psi:ProcessStartInfo) M G input =
    printf "Running for input (%d, %d)... " M G
    let stdout, stderr = 
        use proc = Process.Start(psi)
        proc.StandardInput.AutoFlush <- true
        proc.StandardInput.WriteLine(input+"\n")
        proc.WaitForExit()
        proc.StandardOutput.ReadToEnd().Trim(), proc.StandardError.ReadToEnd().Trim()
    if stderr <> "" then
        if nOfReruns< 5 then 
            runAndProcessResult (nOfReruns + 1) psi M G input
        else
            printfn "error"
            ("<error>","<error>")
    else
        let resultLine =  stdout |> split '\n' |> last
        let (TimedResult(result, timeElapsed)) = resultLine
        let resStr = sprintf "%s,%s" result timeElapsed
        printfn "%s" resultLine
        result, timeElapsed

Console.Write("nOfTimes each test will run: ")
let times = int <| Console.ReadLine()

let int64ResultPath =
    directory + @"/timesInt64_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
let bigintResultPath =
    directory + @"/timesBigInt_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
Console.WriteLine("int64 results file: " + int64ResultPath)
Console.WriteLine("bigInt results file: " + bigintResultPath)
Console.ReadLine()

let MList = [1;2;4;8;16;32;64]
let GList = [500;1000; 5000;10000;50000]

let run choice =
    use stream = File.Create(if choice = 1 then int64ResultPath else bigintResultPath)
    use writer = new StreamWriter(stream)
    writer.AutoFlush <- true
    writer.WriteLine("Implementation,Number of Workers,Chunk Size,Result,Time Elapsed")
    //Sequential
    do
        let result, timeElapsed = runAndProcessResult 0 psi -1 -1 <| sprintf "%d\n%d\n%d\n%d\n" choice -1 -1 1
        writer.WriteLine(sprintf "%s,%d,%d,%s,%s" " Sequential" -1 -1 result timeElapsed)
    for G in GList do
        let result, timeElapsed = runAndProcessResult 0 psi -1 G <| sprintf "%d\n%d\n%d\n%d\n" choice -1 G 3
        writer.WriteLine(sprintf "%s,%d,%d,%s,%s" "Async Workflows" -1 G result timeElapsed)
    for i, name in [4, "Tasks"; 5, "Agents"] do
    for M in MList do
    for G in GList do
        let result, timeElapsed = runAndProcessResult 0 psi M G (sprintf "%d\n%d\n%d\n%d\n" choice M G i)
        writer.WriteLine(sprintf "%s,%d,%d,%s,%s" name M G result timeElapsed)

do
    run 1
    run 2
            



