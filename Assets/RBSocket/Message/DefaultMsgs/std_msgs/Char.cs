using System;

namespace RBS.Messages.std_msgs
{
    [System.Serializable]
    public class Char : ExtendMessage
    {
        public char data;
        public override string Type() { return "std_msgs/Char"; }
        public Char()
        {
            data = ' ';
        }
    }
}