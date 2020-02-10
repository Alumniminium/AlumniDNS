using System.Runtime.InteropServices;
using System.Text;
using Packets.Enums;

namespace Packets
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MsgTransaction
    {
        public int Length;
        public ushort Id;
        public uint CustomerId;
        public MsgTransactionType Type;
        public fixed byte Subdomain[16];

        public static MsgTransaction Create(string domain, MsgTransactionType type)
        {
            var msg = stackalloc MsgTransaction[1];
            msg->Length = sizeof(MsgTransaction);
            msg->Id = 1002;
            msg->Type = type;
            msg->SetSubdomain(domain);
            return *msg;
        }

        public string GetSubdomain()
        {
            fixed (byte* p = Subdomain)
                return Encoding.UTF8.GetString(p, 16).Trim('\0');
        }

        public void SetSubdomain(string subdomain)
        {
            fixed (byte* p = Subdomain)
            {
                for (var i = 0; i < subdomain.Length; i++)
                    p[i] = (byte)subdomain[i];
            }
        }

        public static implicit operator byte[] (MsgTransaction msg)
        {
            var buffer = new byte[sizeof(MsgTransaction)];
            fixed (byte* p = buffer)
                *(MsgTransaction*)p = *&msg;
            return buffer;
        }
        public static implicit operator MsgTransaction(byte[] msg)
        {
            fixed (byte* p = msg)
                return *(MsgTransaction*)p;
        }
    }
}
