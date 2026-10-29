using DeviceEmulator.Model;
using System.Buffers.Binary;

void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}

byte Checksum(byte[] data)
{
    byte checksum = 0;
    foreach (byte value in data.AsSpan(9)) checksum ^= value;
    return checksum;
}

byte[] SortCommand(uint id, uint sequence = 0x89ABCDEF, byte port = 7, bool automaticFeeder = false)
{
    byte[] data = new byte[automaticFeeder ? 20 : 18];
    data[0] = data[1] = 0xAA;
    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(2), sequence);
    BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6), (ushort)data.Length);
    data[9] = 0x01;
    data[10] = 0x1B;
    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(11), id);
    data[15] = port;
    data[16] = 0x23;
    data[17] = 0x01;
    if (automaticFeeder) { data[18] = 0x45; data[19] = 0x03; }
    data[8] = Checksum(data);
    return data;
}

void AssertFrame(byte[] data, ushort command)
{
    Assert(data[0] == 0xAA && data[1] == 0xAA
        && BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(6)) == data.Length
        && BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(9)) == command
        && data[8] == Checksum(data), $"valid {command:X4} header, length and XOR checksum");
}

var protocol = new BalanceWheelProtocol();
byte[] template = BalanceWheelProtocol.CreateResult(2, 0x1234);
AssertFrame(template, 0x1B06);
Assert(template.Length == 19 && template[11] == 1 && template[12] == 2
    && BinaryPrimitives.ReadUInt16LittleEndian(template.AsSpan(13)) == 0x1234
    && BinaryPrimitives.ReadUInt32LittleEndian(template.AsSpan(15)) == 0x0ACB,
    "result uses 16-bit port and full 32-bit package ID");
Assert(!protocol.AutoReplyEnabled && ReferenceEquals(template, protocol.PrepareResult(template)),
    "automatic replies default to off and preserve manual data");
Assert(protocol.TryHandleSortCommand(SortCommand(0xFEDCBA98), out byte[] ack, out byte[]? reply)
    && reply == null, "disabled mode acknowledges without automatically sending a result");
AssertFrame(ack, 0x9B01);
Assert(BinaryPrimitives.ReadUInt32LittleEndian(ack.AsSpan(2)) == 0x89ABCDEF,
    "acknowledgement echoes the request frame sequence");

protocol.AutoReplyEnabled = true;
byte[] updated = protocol.PrepareResult(template);
Assert(BinaryPrimitives.ReadUInt32LittleEndian(updated.AsSpan(15)) == 0xFEDCBA98
    && updated[12] == 2 && updated[13] == 0x34 && updated[14] == 0x12,
    "manual/batch result uses the latest message ID while preserving configured fields");
Assert(BinaryPrimitives.ReadUInt32LittleEndian(template.AsSpan(15)) == 0x0ACB,
    "sending leaves the reusable command template unchanged");
AssertFrame(updated, 0x1B06);

Assert(protocol.TryHandleSortCommand(SortCommand(0x87654321, port: 255, automaticFeeder: true), out ack, out reply)
    && reply != null, "automatic feeder request generates a result when enabled");
AssertFrame(reply!, 0x1B06);
Assert(reply![12] == 0 && BinaryPrimitives.ReadUInt16LittleEndian(reply.AsSpan(13)) == 255
    && BinaryPrimitives.ReadUInt32LittleEndian(reply.AsSpan(15)) == 0x87654321
    && BinaryPrimitives.ReadUInt32LittleEndian(reply.AsSpan(2)) == 0x89ABCDEF,
    "automatic result echoes message ID, sort port and frame sequence");
Assert(protocol.TryHandleSortCommand(SortCommand(0, sequence: 42), out ack, out reply)
    && BinaryPrimitives.ReadUInt32LittleEndian(reply!.AsSpan(15)) == 0,
    "standard request and zero message ID are supported");
Assert(BinaryPrimitives.ReadUInt32LittleEndian(protocol.PrepareResult(template).AsSpan(15)) == 0,
    "successive requests replace the previous message ID");

byte[] invalid = SortCommand(123);
invalid[8] ^= 1;
Assert(!protocol.TryHandleSortCommand(invalid, out _, out reply) && reply == null,
    "bad checksum is rejected");
invalid = SortCommand(123);
invalid[6]--;
Assert(!protocol.TryHandleSortCommand(invalid, out _, out _), "incorrect frame length is rejected");
invalid = SortCommand(123);
invalid[0] = 0;
Assert(!protocol.TryHandleSortCommand(invalid, out _, out _), "incorrect header is rejected");
invalid = SortCommand(123);
invalid[9] = 2;
invalid[8] = Checksum(invalid);
Assert(!protocol.TryHandleSortCommand(invalid, out _, out _), "unrelated commands are rejected");
Assert(!protocol.TryHandleSortCommand(new byte[15], out _, out _), "truncated request is rejected");
Assert(BinaryPrimitives.ReadUInt32LittleEndian(protocol.PrepareResult(template).AsSpan(15)) == 0,
    "invalid requests cannot overwrite the latest ID");

byte[] multiple = new byte[26];
template.CopyTo(multiple, 0);
multiple[6] = 26;
multiple[11] = 2;
multiple[19] = 1;
multiple[20] = 9;
multiple[8] = Checksum(multiple);
updated = protocol.PrepareResult(multiple);
Assert(BinaryPrimitives.ReadUInt32LittleEndian(updated.AsSpan(15)) == 0
    && BinaryPrimitives.ReadUInt32LittleEndian(updated.AsSpan(22)) == 0 && updated[19] == 1 && updated[20] == 9,
    "all entries in a multiple-result frame receive the latest ID");
AssertFrame(updated, 0x1B06);
Assert(ReferenceEquals(ack, protocol.PrepareResult(ack)), "other outgoing commands stay unchanged");
protocol.AutoReplyEnabled = false;
Assert(ReferenceEquals(template, protocol.PrepareResult(template)), "turning the switch off restores manual IDs");
protocol.AutoReplyEnabled = true;
protocol.ClearLatestCommand();
Assert(ReferenceEquals(template, protocol.PrepareResult(template)), "reconnecting clears stale message IDs");

// 来自现场Debug_20261028.0.log；上位机没有填写校验值。
byte[] actualRequest = Convert.FromHexString("AAAA01000000140000011B89E85D4D0133003C00");
Assert(protocol.TryHandleSortCommand(actualRequest, out ack, out reply) && reply != null,
    "actual upper-computer request with zero checksum is accepted");
byte[] actualReply = reply!;
Assert(actualReply.AsSpan(15, 4).SequenceEqual(Convert.FromHexString("89E85D4D"))
    && actualReply[13] == 1 && actualReply[14] == 0,
    "actual automatic reply echoes all four message ID bytes and requested port");
AssertFrame(actualReply, 0x1B06);
updated = protocol.PrepareResult(template);
Assert(updated.AsSpan(15, 4).SequenceEqual(actualRequest.AsSpan(11, 4)),
    "manual result after the actual request uses 89 E8 5D 4D instead of CB 0A 00 00");
protocol.AutoReplyEnabled = false;
Assert(protocol.TryHandleSortCommand(actualRequest, out ack, out reply) && reply == null
    && ReferenceEquals(template, protocol.PrepareResult(template)),
    "actual request respects the disabled automatic reply switch");
protocol.AutoReplyEnabled = true;
byte[] nextActualRequest = Convert.FromHexString("AAAA02000000140000011B785634120233003C00");
Assert(protocol.TryHandleSortCommand(nextActualRequest, out ack, out reply)
    && protocol.PrepareResult(template).AsSpan(15, 4).SequenceEqual(Convert.FromHexString("78563412")),
    "successive zero-checksum requests replace the cached message ID");
Assert(protocol.AutoReplyResultType == 0, "automatic reply defaults to normal sorting");
foreach (byte resultType in new byte[] { 1, 2, 0 })
{
    protocol.AutoReplyResultType = resultType;
    Assert(protocol.TryHandleSortCommand(actualRequest, out ack, out reply) && reply != null
        && reply[12] == resultType && reply.AsSpan(15, 4).SequenceEqual(actualRequest.AsSpan(11, 4))
        && reply[13] == actualRequest[15],
        $"selected automatic result type {resultType} is applied while preserving package ID and port");
    AssertFrame(reply!, 0x1B06);
}
protocol.AutoReplyResultType = 1;
Assert(protocol.PrepareResult(template)[12] == 2, "automatic result selection preserves manual command result types");
protocol.AutoReplyEnabled = false;
Assert(protocol.TryHandleSortCommand(actualRequest, out ack, out reply) && reply == null,
    "selected result does not send automatically while switch is off");
Console.WriteLine("All DeviceEmulator protocol tests passed.");
