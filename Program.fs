open System
open System.Net
open System.Text
open Pipelines.Sockets.Unofficial
open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Buffers

open MTProto

let port = 3000
let address = "127.0.0.1"


let rand = Random()
let timestamp() =
    (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds

let doWork() =
    printfn "Starting..."

    let endpoint = IPEndPoint(IPAddress.Parse(address), port)
    let nonceBuffer = Array.zeroCreate<byte>(16);
    rand.NextBytes(nonceBuffer)
    let msgId = timestamp() * (2.0 ** 32.0) |> int64
    let data =
        MTProto.encode { AuthId = 0L; MsgId = msgId; Bytes = nonceBuffer }
    let test =
        MTProto.decode data
    printfn "Encode decode success=%b" (test.MsgId = msgId)
    printfn "Sending data [%s]" (BitConverter.ToString(data))
    task {
        try
            use! conn = SocketConnection.ConnectAsync(endpoint)
            let! flushResult = conn.Output.WriteAsync(data |> ReadOnlyMemory)
            conn.Output.Complete()
            let mutable continueLooping = true
            while continueLooping do
                let! result = conn.Input.ReadAsync()
                let buffer = result.Buffer
                printfn "Received %i bytes" buffer.Length
                if result.IsCompleted
                then
                    let array = result.Buffer.ToArray()
                    let received = decode array
                    printfn "Message received: %A" received
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

