using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mccBuild
{
    public class Function
    {
        public string[] commands;
        public bool loop;
        public string name;
        public int priority;

        public Function(string[] commands, bool loop, string name, int priority)
        {
            this.priority = priority;
            this.commands = commands;
            this.loop = loop;
            this.name = name;
        }
    }
}
