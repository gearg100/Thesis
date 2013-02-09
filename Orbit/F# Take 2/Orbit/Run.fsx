
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
    int m.Groups.[1].Value, int64 m.Groups.[2].Value

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

let path = __SOURCE_DIRECTORY__ + "/bin/Release/Orbit.exe"

let psi = 
    if Environment.OSVersion.Platform = PlatformID.Unix then
        makePSI @"mono" <| "--gc=sgen --runtime=v4.0 " + path
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
            None
    else
        let resultLine =  stdout |> split '\n' |> last
        let (TimedResult(result, timeElapsed)) = resultLine
        let resStr = sprintf "%d, %d" result timeElapsed
        printfn "%s" resultLine
        Some(resStr)

Console.Write("nOfTimes each test will run: ")
let times = int <| Console.ReadLine()

let int64ResultPath =
    __SOURCE_DIRECTORY__ + @"\timesInt64_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
let bigintResultPath =
    __SOURCE_DIRECTORY__ + @"\timesBigInt_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".txt"
Console.WriteLine("int64 results file" + int64ResultPath)
Console.WriteLine("int64 results file" + bigintResultPath)
Console.ReadLine()
do 
    use stream = File.Create(int64ResultPath)
    use writer = new StreamWriter(stream)
    writer.AutoFlush <- true
    for M in [1;2;4;8;16;32;64] do
    for G in [500;1000; 5000;10000;50000] do
        match runAndProcessResult 0 psi M G (sprintf "1\n%d\n%d\n2\n" M G) with
        |Some(resultString) ->
            writer.WriteLine(sprintf "(%d, %d): %s" M G resultString)
        |None ->
            writer.WriteLine("(%d, %d, %d): error")
do 
    use stream = File.Create(bigintResultPath)
    use writer = new StreamWriter(stream)
    writer.AutoFlush <- true
    for M in [1;2;4;8;16;32;64] do
    for G in [500;1000; 5000;10000;50000] do
        match runAndProcessResult 0 psi M G (sprintf "2\n%d\n%d\n2\n" M G) with
        |Some(resultString) ->
            writer.WriteLine(sprintf "(%d, %d): %s" M G resultString)
        |None ->
            writer.WriteLine("(%d, %d, %d): error")
            



