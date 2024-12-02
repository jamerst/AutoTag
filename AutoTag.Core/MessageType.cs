namespace AutoTag.Core;
[Flags]
public enum MessageType
{
    Information = 1,
    Warning = 2,
    Error = 4,
    Log = 8
}