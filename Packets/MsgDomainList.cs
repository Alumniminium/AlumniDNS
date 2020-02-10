using System.Runtime.InteropServices;
using System.Text;

namespace Packets
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MsgDomainList
    {
        public int Length;
        public ushort Id;
        public uint CustomerId;
        public fixed byte Subdomains[80];

        public static MsgDomainList Create(string domains)
        {
            var msg = stackalloc MsgDomainList[1];
            msg->Length = sizeof(MsgDomainList);
            msg->Id = 1001;

            msg->SetSubdomains(domains);

            return *msg;
        }

        public string[] GetSubdomains()
        {
            fixed (byte* p = Subdomains)
                return Encoding.UTF8.GetString(p, 80).Trim('\0').Split('#');
        }

        public void SetSubdomains(string subdomains)
        {
            fixed (byte* p = Subdomains)
            {
                for (var i = 0; i < subdomains.Length; i++)
                    p[i] = (byte)subdomains[i];
            }
        }

        public static implicit operator byte[] (MsgDomainList msg)
        {
            var buffer = new byte[sizeof(MsgDomainList)];
            fixed (byte* p = buffer)
                *(MsgDomainList*)p = *&msg;
            return buffer;
        }
        public static implicit operator MsgDomainList(byte[] msg)
        {
            fixed (byte* p = msg)
                return *(MsgDomainList*)p;
        }
    }
}
