namespace Packets.Enums
{
    public enum MsgTransactionType : byte
    {
        Update = 0,
        Add = 1,
        Remove =2,
        Fail_Exists=3,
        Fail_OutOfSlots=4,
        Fail_TooLong = 5,
        Fail_IllegalCharacters = 6,
        Fail_NotOwner = 7,
        Success = 100,
    }
}
