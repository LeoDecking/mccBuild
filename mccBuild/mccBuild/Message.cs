using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mccBuild
{
    public enum MessageType
    {
        Error,
        InfinityLoop
    }
    public class Message
    {
        public string Text;
        public MessageType MessageType;
        
        public string File;
        public int Line;
        public int Column;
        public int EndColumn;

        public Message(string text, MessageType messageType, int column = 0, int endColumn = 0, string file ="", int line =0)
        {
            Text = text;
            MessageType = messageType;
            File = file;
            Line = line;
            Column = column;
            EndColumn = endColumn;
        }
    }
}
