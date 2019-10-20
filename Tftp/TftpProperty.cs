using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tftp
{
    //数据包类型
    public enum OpCode
    {
        RRQ = 1,
        WRQ,
        DATA,
        ACK,
        ERROR
    }

    //传输模式
    public struct Mode
    {
        public const string NETASCII = "netascii";
        public const string OCTET = "octet";
        public const string MAIL = "mail";
        public const string UNDEFINED = "undefined";
    }

    //错误码定义
    public enum ErrorCode
    {
        NotDefined,
        FileNotFound,
        AccessViolation,
        DiskFull,
        IllegalOperation,
        UnknowTransferID,
        FileAlreadyExists,
        NoSuchUser
    }

    public enum Status
    {
        IDLE,
        READ,
        WRITE
    }

    //读写请求包
    public struct ReqPacket
    {
        public OpCode opCode { get; set; }
        public string fileName { get; set; }
        public string mode { get; set; }
        public byte[] packet { get; set; }

        public ReqPacket(OpCode opCode, string fileName, string mode)
        {
            this.opCode = opCode;
            this.fileName = fileName;
            this.mode = mode;
            List<byte> byteList = new List<byte>() { 0, (byte)this.opCode };
            byteList.AddRange(Encoding.Default.GetBytes(fileName));
            byteList.Add(0);
            byteList.AddRange(Encoding.Default.GetBytes(mode.ToString()));
            byteList.Add(0);
            packet = byteList.ToArray();
        }

        public ReqPacket(byte[] packet)
        {
            this.packet = packet;
            opCode = (OpCode)BitConverter.ToInt16(this.packet.Take(2).Reverse().ToArray(), 0);
            string[] content = Encoding.Default.GetString(this.packet.Skip(2).ToArray()).Split('\0');
            fileName = content[0];
            mode = content[1];
        }
    }

    public struct DataPacket
    {
        public OpCode opCode { get; set; }
        public byte[] block { get; set; }
        public int iblock { get; set; }
        public byte[] data { get; set; }
        public byte[] packet { get; set; }

        public DataPacket(int iblock, byte[] data)
        {
            opCode = OpCode.DATA;
            this.iblock = iblock;
            this.data = data;
            block = BitConverter.GetBytes(this.iblock).Skip(2).Reverse().ToArray();
            List<byte> byteList = new List<byte>() { 0, (byte)opCode };
            byteList.AddRange(block);
            byteList.AddRange(data);
            packet = byteList.ToArray();
        }

        public DataPacket(byte[] packet)
        {
            opCode = OpCode.DATA;
            this.packet = packet;
            block = packet.Skip(2).Take(2).ToArray();
            iblock = block[0] * (byte.MaxValue + 1) + block[1];
            data = packet.Skip(4).ToArray();
        }
    }

    public struct AckPacket
    {
        public OpCode opCode { get; set; }
        public int block { get; set; }
        public byte[] packet { get; set; }

        public AckPacket(int block)
        {
            opCode = OpCode.ACK;
            this.block = block;
            byte[] arrBlock = BitConverter.GetBytes(this.block).Take(2).Reverse().ToArray();
            packet = new byte[4] {0, (byte)opCode, arrBlock[0], arrBlock[1] };
        }

        public AckPacket(byte[] packet)
        {
            opCode = OpCode.ACK;
            this.packet = packet;
            block = this.packet[2] * (byte.MaxValue + 1) + this.packet[3];
        }
    }

    public struct ErrorPacket
    {
        public OpCode opCode { get; set; }
        public ErrorCode errorCode { get; set; }
        public string errorMessage { get; set; }
        public byte[] packet { get; set; }

        public ErrorPacket(ErrorCode errorCode, string errorMessage)
        {
            opCode = OpCode.ERROR;
            this.errorCode = errorCode;
            this.errorMessage = errorMessage;
            List<byte> byteList = new List<byte>() { 0, (byte)opCode, 0, (byte)this.errorCode };
            byteList.AddRange(Encoding.Default.GetBytes(this.errorMessage));
            byteList.Add(0);
            packet = byteList.ToArray();
        }

        public ErrorPacket(byte[] packet)
        {
            opCode = OpCode.ERROR;
            this.packet = packet;
            errorCode = (ErrorCode)BitConverter.ToInt16(this.packet.Skip(2).Take(2).ToArray(), 0);
            errorMessage = Encoding.Default.GetString(this.packet.Skip(4).ToArray()).Trim('\0');
        }
    }
}
