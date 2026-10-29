using System.Buffers.Binary;

namespace DeviceEmulator.Model
{
    /// <summary>
    /// 摆轮分拣命令和结果报文，所有多字节字段均使用小端格式。
    /// </summary>
    public sealed class BalanceWheelProtocol
    {
        private readonly object sync = new();
        private uint? latestMessageId;
        private bool autoReplyEnabled;
        private byte autoReplyResultType;

        public bool AutoReplyEnabled
        {
            get { lock (sync) return autoReplyEnabled; }
            set { lock (sync) autoReplyEnabled = value; }
        }

        public byte AutoReplyResultType
        {
            get { lock (sync) return autoReplyResultType; }
            set
            {
                if (value > 2) throw new ArgumentOutOfRangeException(nameof(value));
                lock (sync) autoReplyResultType = value;
            }
        }

        public void ClearLatestCommand()
        {
            lock (sync) latestMessageId = null;
        }

        public bool TryHandleSortCommand(byte[] data, out byte[] acknowledgement, out byte[]? result)
        {
            acknowledgement = Array.Empty<byte>();
            result = null;
            // 普通版为18字节，自动供包版还包含2字节photo_trigger_time。
            // 现场上位机将校验字段固定为00，表示未填写校验值；非零值仍按协议校验。
            if ((data.Length != 18 && data.Length != 20) || !IsFrame(data, 0x1B01)
                || (data[8] != 0 && data[8] != CalculateChecksum(data)))
            {
                return false;
            }

            uint messageId = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(11, 4));
            uint sequence = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(2, 4));
            lock (sync)
            {
                latestMessageId = messageId;
                acknowledgement = CreateFrame(11, 0x9B01, sequence);
                acknowledgement[8] = CalculateChecksum(acknowledgement);
                if (autoReplyEnabled)
                {
                    result = CreateResult(autoReplyResultType, data[15], messageId, sequence);
                }
            }
            return true;
        }

        /// <summary>
        /// 发送前更新每条结果的package_id，保留快捷命令模板及其他字段。
        /// </summary>
        public byte[] PrepareResult(byte[] data)
        {
            lock (sync)
            {
                if (!autoReplyEnabled || !latestMessageId.HasValue || !IsFrame(data, 0x1B06)
                    || data.Length < 12 || data.Length != 12 + data[11] * 7)
                {
                    return data;
                }

                byte[] result = (byte[])data.Clone();
                for (int i = 0; i < result[11]; i++)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(15 + i * 7, 4), latestMessageId.Value);
                }
                result[8] = CalculateChecksum(result);
                return result;
            }
        }

        public static byte[] CreateResult(byte resultType, ushort sortPort, uint packageId = 0x0ACB, uint sequence = 0)
        {
            byte[] result = CreateFrame(19, 0x1B06, sequence);
            result[11] = 1;
            result[12] = resultType;
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(13, 2), sortPort);
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(15, 4), packageId);
            result[8] = CalculateChecksum(result);
            return result;
        }

        private static bool IsFrame(byte[] data, ushort command)
        {
            return data.Length >= 11 && data[0] == 0xAA && data[1] == 0xAA
                && BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(6, 2)) == data.Length
                && BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(9, 2)) == command;
        }

        private static byte[] CreateFrame(ushort length, ushort command, uint sequence)
        {
            byte[] data = new byte[length];
            data[0] = data[1] = 0xAA;
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(2, 4), sequence);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6, 2), length);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(9, 2), command);
            return data;
        }

        private static byte CalculateChecksum(byte[] data)
        {
            byte checksum = 0;
            for (int i = 9; i < data.Length; i++)
            {
                checksum ^= data[i];
            }
            return checksum;
        }
    }
}
