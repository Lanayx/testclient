open System
open System.Net
open System.Text
open Pipelines.Sockets.Unofficial
open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Buffers

let port = 3000
let address = "127.0.0.1"

//let mutable efSent = false
//let TcpPack (b: byte[]) =
//    let len = (byte) ((float)b.Length / 4.0)
//    seq {
//        if (efSent |> not)
//        then
//            efSent <- true
//            yield 239uy
//        if len > 127uy
//        then
//            yield 127uy
//            yield len
//        else
//            yield len
//        yield! b
//    }

let hexStringToByteArray (hex : string) =
    [| 0 .. hex.Length-1 |]
    |> Array.filter (fun x -> x % 2 = 0)
    |> Array.map (fun x -> Convert.ToByte(hex.Substring(x, 2), 16))

let doWork() =
    printfn "Starting..."

    let endpoint = IPEndPoint(IPAddress.Parse(address), port)
    let encoding = UTF32Encoding(false, false, false);

    // should work from https://stackoverflow.com/a/41663225/1780648
    let data =
        "EF0A"+
        "0000000000000000"+
        "00203DC050937B58"+
        "14000000"+
        "78974660"+
        "00BB27062BF84D9BBE9C7BB192559FE5"
        |> hexStringToByteArray
        |> ReadOnlyMemory<byte>

    task {
        try
            use! conn = SocketConnection.ConnectAsync(endpoint)
            let! flushResult = conn.Output.WriteAsync(data)
            conn.Output.Complete()
            let mutable continueLooping = true
            while continueLooping do
                let! result = conn.Input.ReadAsync()
                let buffer = result.Buffer
                printfn "Received %i bytes" buffer.Length
                if result.IsCompleted
                then
                    let array = result.Buffer.ToArray()
                    let received = encoding.GetString(array)
                    printfn "Message received: %s" received
                    continueLooping <- false
                else
                    printfn "Waiting..."
                    conn.Input.AdvanceTo(buffer.Start, buffer.End)
        with
        | ex -> printfn "Connection failed: %A" ex
    }

[<EntryPoint>]
let main argv =

    doWork().Wait()

    0

