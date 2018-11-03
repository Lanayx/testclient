module MTProto


open System.Buffers
open System
open System
open Microsoft.FSharp


type UnencryptedMessage =
    {
        AuthId: int64
        MsgId: int64
        Bytes: byte[]
    }

let encode (msg: UnencryptedMessage) =
    let resultBuffer = Array.zeroCreate<byte>(24 + msg.Bytes.Length)
    let bufferSpan = resultBuffer.AsSpan()
    BitConverter.TryWriteBytes(bufferSpan.Slice(0, 8), msg.AuthId) |> ignore
    BitConverter.TryWriteBytes(bufferSpan.Slice(8, 8), msg.MsgId) |> ignore
    BitConverter.TryWriteBytes(bufferSpan.Slice(16, 4), msg.Bytes.Length) |> ignore
    BitConverter.TryWriteBytes(bufferSpan.Slice(20, 4), 0x60469778) |> ignore
    Buffer.BlockCopy(msg.Bytes, 0, resultBuffer, 24, msg.Bytes.Length)
    resultBuffer

let decode (msg: byte[]) =
    let authId = BitConverter.ToInt64(msg, 0)
    let msgId = BitConverter.ToInt64(msg, 8)
    let bytesLength = BitConverter.ToInt32(msg, 16)
    let bytes = Array.zeroCreate<byte>(bytesLength)
    Buffer.BlockCopy(msg, 24, bytes, 0, bytes.Length)
    { AuthId = authId; MsgId = msgId; Bytes = bytes }

