using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
/// <summary>
/// This example only support WRQ/ACK/ERROR request.
/// </summary>

namespace Tftp
{
    public class TftpServer: IDisposable
    {
        public string Root { get; set; }
        public string HostName { get; set; }
        public string HostIp { get; set; }
        private const int hostPort = 69;
        public bool IsTftpRunning { get; set; }
        public IPEndPoint HostEndPoint
        {
            get
            {
                return new IPEndPoint(IPAddress.Parse(HostIp), hostPort);
            }
        }

        private Socket tftpSocket { get; set; }
        private byte[] buffer = new byte[1024];
        private Stream dataStream = null;
        private Status currentStatus = Status.IDLE;

        public void StartTftp()
        {
            tftpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                SendBufferSize = 65536,
                ReceiveBufferSize = 65536
            };
            tftpSocket.Bind(HostEndPoint);
            IsTftpRunning = true;
            Init();

            Console.WriteLine("TftpServer is running......");
            int receiveLength;
            while (IsTftpRunning)
            {
                EndPoint ep = new IPEndPoint(IPAddress.Parse("192.168.2.10"), 0);
                receiveLength = tftpSocket.ReceiveFrom(buffer, SocketFlags.None, ref ep);
                if (receiveLength > 0)
                {
                    Console.WriteLine("Received {0} byte.", receiveLength);
                    OnReceive(buffer, receiveLength, ep);
                    Console.WriteLine("==================================================\n");
                }
            }
        }

        public void StopTftp()
        {
            IsTftpRunning = false;
            Dispose();
        }

        private void Init()
        {
            try
            {
                dataStream.Flush();
            }
            catch { return; }
        }

        public void OnReceive(byte[] buffer, int buffersize, EndPoint ep)
        {
            byte[] byteRes = new byte[buffersize];
            Array.ConstrainedCopy(buffer, 0, byteRes, 0, buffersize);

            OpCode REQ = (OpCode)BitConverter.ToInt16(byteRes.Take(2).Reverse().ToArray(), 0);


            switch(REQ)
            {
                case OpCode.WRQ:                        //Write Request
                    OnWrite(byteRes, ep);
                    break;
                case OpCode.RRQ:                        //Read Request
                    OnRead(byteRes, ep);
                    break;
                case OpCode.DATA:                       //Data Transfer
                    switch (currentStatus)              //Get TftpServer Current Working Status
                    {                        
                        case Status.WRITE:              //TftpServer in Write Process
                            BeginWrite(byteRes, ep);
                            break;
                        case Status.READ:               //TftpServer in Read Process
                            BeginRead(byteRes, ep);
                            break;
                        default:                        //Unkonw Status, Initialize
                            Init();
                            break;
                    }
                    break;
                case OpCode.ERROR:                      //Error Occurred
                    OnError(byteRes, ep);
                    break;
                default:                                //Initialize
                    Init();
                    break;
            }
        }

        //TODO: 
        private void OnRead(byte[] byteRes, EndPoint ep)
        {

        }

        private void BeginRead(byte[] byteRes, EndPoint ep)
        {

        }

        private void OnWrite(byte[] byteRes, EndPoint ep)
        {
            ReqPacket wrq = new ReqPacket(byteRes);
            Console.WriteLine("From: {3}  --OpCode={0}, FileName={1}, Mode={2}", wrq.opCode, wrq.fileName, wrq.mode, ((IPEndPoint)ep).Address.ToString());
            currentStatus = Status.WRITE;

            string localPath = Path.Combine(Root, wrq.fileName);
            try
            {
                dataStream = new FileStream(localPath, FileMode.Create, FileAccess.Write);
                AckPacket ack = new AckPacket(0);
                tftpSocket.SendTo(ack.packet, 4, SocketFlags.None, ep);
                Console.WriteLine("Send: WRQ ACK");
            }
            catch(Exception ex)
            {
                ErrorPacket error = new ErrorPacket(ErrorCode.AccessViolation, ex.ToString());
                tftpSocket.SendTo(error.packet, 4, SocketFlags.None, ep);
            }
        }

        private void BeginWrite(byte[] byteRes, EndPoint ep)
        {
            DataPacket data = new DataPacket(byteRes);

            if (data.data.Length > 512)
            {
                Init();
                Console.WriteLine("Receive Error.Data length beyond 512 bytes.");
                ErrorPacket error = new ErrorPacket(ErrorCode.IllegalOperation, "Data length beyond 512 bytes");
                tftpSocket.SendTo(error.packet, SocketFlags.None, ep);
                return;
            }

            if (data.data.Length == 512)
            {
                Console.WriteLine("From: {3}  --OpCode={0}, BlockNumber={1}\nData={2}", data.opCode, data.iblock, Encoding.Default.GetString(data.data), ((IPEndPoint)ep).Address.ToString());
                foreach (byte x in data.data) { dataStream.WriteByte(x); }
                currentStatus = Status.WRITE;
            }
            else
            {
                Console.WriteLine("OpCode={0}, BlockNumber={1}\nData={2}", data.opCode, data.iblock, Encoding.Default.GetString(data.data));
                Console.WriteLine("Receive Done.");
                foreach (byte x in data.data) { dataStream.WriteByte(x); }
                dataStream.Close();
                currentStatus = Status.IDLE;
            }

            AckPacket ack = new AckPacket(data.iblock);
            tftpSocket.SendTo(ack.packet, SocketFlags.None, ep);
            Console.WriteLine("Send: ACK, BlockNumber={0}", data.iblock);
        }

        private void OnError(byte[] byteRes, EndPoint ep)
        {
            ErrorPacket error = new ErrorPacket(byteRes);
            Console.WriteLine("From: {3}  --OpCode={0}, ErrorCode={1}, ErrMessage={2}", error.opCode, error.errorCode, error.errorMessage, ((IPEndPoint)ep).Address.ToString());
            try
            {
                dataStream.Close();
            }
            catch {; }
            finally
            {
                currentStatus = Status.IDLE;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    dataStream.Dispose();
                    tftpSocket.Dispose();
                }
                catch { return; }
            }
        }

        ~TftpServer()
        {
            Dispose(false);
        }
    }
}
