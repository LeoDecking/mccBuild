using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mccBuild
{
    public enum CommandBlockType
    {
        Impulse,
        ImpulseConditional,
        Repeat,
        RepeatConditional,
        Chain,
        ChainConditional
    }
    public enum CommandBlockOrientation
    {
        Down = 0,
        Up = 1,
        North=2,
        South =3,
        West = 4,
        East = 5
    }
    public class CommandBlock
    {
        public string Command;
        public string Name
        {
            get
            {

                if (CommandBlockType == CommandBlockType.Chain | CommandBlockType == CommandBlockType.ChainConditional) return "chain_command_block";
                if (CommandBlockType == CommandBlockType.Impulse | CommandBlockType == CommandBlockType.ImpulseConditional) return "command_block";
                return "repeating_command_block";
            }
        }
        public CommandBlockType CommandBlockType;
        public CommandBlockOrientation CommandBlockOrientation;
        public int Meta => CommandBlockType == CommandBlockType.ChainConditional || CommandBlockType == CommandBlockType.RepeatConditional || CommandBlockType == CommandBlockType.ImpulseConditional ? (int)CommandBlockOrientation + 8 : (int)CommandBlockOrientation;
        public bool Auto;

        public CommandBlock(string command, CommandBlockType commandBlockType, CommandBlockOrientation commandBlockOrientation, bool auto = true)
        {
            Command = command;
            CommandBlockType = commandBlockType;
            CommandBlockOrientation = commandBlockOrientation;
            Auto = auto;
        }
    }
}
