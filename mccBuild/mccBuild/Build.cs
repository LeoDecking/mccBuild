using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace mccBuild
{
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public class Build
    {
        static string uuid = Guid.NewGuid().ToString();
        static int xOffset = 2;
        static int zOffset = 0;
        
        static List<Function> _functions = new List<Function>();
        static readonly List<String> _functionCalls = new List<String>();

        private static List<string> _initCommands;
        static List<Message> _messages;

        public static void Main(string[] args)
        {
            _functions.Add(new Function(new[] { "/execute @a ~ ~ ~ //Function(say)", "/execute @a ~ ~ ~ //Function(space)" }, true, "main", 1));
            _functions.Add(new Function(new[] { "/testfor @e[type=!Player,r=2]","//Space(10000)","c/say test" }, false, "say", 1));
            _functions.Add(new Function(new [] {"/testfor @a[r=5]","//Space(10000)"}, false, "space", 0));
        }

        public static string[] GetOneCommand(List<Function> functions)
        {
            _functions = functions;
            return GetOneCommand();
        }

        private static List<List<CommandBlock>> GetCommandBlocks()
        {
            List<List<CommandBlock>> rows = new List<List<CommandBlock>>();
            _initCommands = new List<string>();
            List<string> removeCommands = new List<string>();
            _messages = new List<Message>();
            
            #region normal
            List<Function> tempFunctions = _functions.OrderBy(x=>0-x.priority).ToList();
            foreach(Function function in tempFunctions)
            {
                List<CommandBlock> commandBlocks = new List<CommandBlock>();
                for(int i = 0; i<function.commands.Length; i++)
                {
                    string command = function.commands[i];

                    List<Message> cMessages;
                    foreach(string cmd in ParseCommand(command, out cMessages))
                    {
                        if (cmd.Length>1&&cmd.Trim().TrimStart('c')[0]=='/'&&function.priority>0)
                        {
                            if (function.name == "Init" && cmd.Trim().TrimStart('c').Length < 30000)
                                _initCommands.Add(cmd.Trim().TrimStart('c'));
                            else if (function.name == "Remove" && cmd.Trim().TrimStart('c').Length < 30000)
                                removeCommands.Add(cmd.Trim().TrimStart('c'));
                            else if(cmd.Trim().TrimStart('c').Length<30000)
                            {
                                commandBlocks.Add(new CommandBlock(cmd.Trim().TrimStart('c'), cmd.Trim()[0] == 'c' ? CommandBlockType.ChainConditional : CommandBlockType.Chain, CommandBlockOrientation.Up));
                            }
                            else
                            {
                                _messages.Add(new Message("Command is too long", MessageType.Error, 0, command.Length - 1, function.name + ".mcc", i + 1));
                            }
                        }
                    }
                    cMessages.ForEach(msg => msg.File = function.name + ".mcc");
                    cMessages.ForEach(msg => msg.Line = i+1);
                    cMessages.ForEach(msg => _messages.Add(msg));
                }
                if (commandBlocks.Count > 0)
                {
                    commandBlocks[0].CommandBlockType = function.loop ? CommandBlockType.Repeat : CommandBlockType.Impulse;
                    commandBlocks[0].Auto = function.loop;
                    rows.Add(commandBlocks);
                }
            }
            #endregion normal

            _initCommands.Add("/scoreboard objectives add "+uuid+"mccSuccessCount dummy");
            removeCommands.Add("/scoreboard objectives remove "+uuid+"mccSuccessCount");
            
            #region remove
            removeCommands.Add("/fill ~"+xOffset+" ~-$removeCount; ~"+zOffset+" ~" + rows.Count*xOffset + " ~" + (rows.Max(x=>x.Count)-2) + " ~" + rows.Count * zOffset + " air");
            List<CommandBlock> removeBlocks = new List<CommandBlock>();
            bool toLong = true;
            while(toLong)
            {
                toLong = false;
                string removeCommand = "/summon FallingSand ~ ~$removeY; ~ {Block:redstone_block,Data:0,Time:1,DropItem:0,Passengers:[{id:FallingSand,Block:activator_rail,Data:0,Time:1,DropItem:0},";

                List<string> forRemoveCommands = removeCommands.ToList();
                for (int i = 0; i < forRemoveCommands.Count; i++)
                {
                    string command = forRemoveCommands[i];
                    if ((removeCommand + "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:" + command + "},").Length < 30000)
                    {
                        removeCommand += "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:" + command + "},";
                        removeCommands.Remove(command);
                    }
                    else
                    {
                        toLong = true;
                        break;
                    }
                }
                if(!toLong)
                    removeCommand+= "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/setblock ~ ~1 ~ repeating_command_block 0 replace {auto:1b,Command:/fill ~ ~ ~ ~ ~-" + (3 + removeBlocks.Count) + " ~ air}},{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/kill @e[type=MinecartCommandBlock,tag=mccKill]},";
                removeBlocks.Add(new CommandBlock(removeCommand.Remove(removeCommand.Length-1) + "]}", CommandBlockType.Chain, CommandBlockOrientation.Up));
            }
            for(int i =0;i<removeBlocks.Count;i++)
            {
                removeBlocks[i].Command = removeBlocks[i].Command.Replace("$removeY;", removeBlocks.Count-i+"").Replace("$removeCount;",(removeBlocks.Count+1)+"");
            }
            removeBlocks.First().CommandBlockType = CommandBlockType.ImpulseConditional;
            removeBlocks.First().Auto = false;
            rows.Insert(0, removeBlocks);
            #endregion remove

            return rows;
        }

        private static string[] GetOneCommand()
        {
            List<string> commands = new List<string>();
            List<string> singleCommands = new List<string>();

            List<List<CommandBlock>> forCommandBlocks = GetCommandBlocks();
            for (int x = 0; x < forCommandBlocks.Count; x++)
            {
                for (int y = 0; y < forCommandBlocks[x].Count; y++)
                {
                    CommandBlock commandBlock = forCommandBlocks[x][y];
                    singleCommands.Add("/execute @e[c=1] ~ ~$offsetY; ~ /setblock ~" + x * xOffset + " ~" + y + " ~" + x * zOffset + " " + commandBlock.Name + " " + commandBlock.Meta + " replace {Command:" + commandBlock.Command + ",TrackOutput:0b,auto:" + (commandBlock.Auto ? 1 : 0) + "b}");
                }
            }
            _initCommands.ForEach(x => singleCommands.Add(x));

            bool toLong = true;
            while (toLong)
            {
                toLong = false;
                commands.Add("/summon FallingSand ~ ~$summonY; ~ {Block:minecraft:redstone_block,Data:0,Time:1,DropItem:0,Passengers:[{id:FallingSand,Block:activator_rail,Data:0,Time:1,DropItem:0},");

                List<string> forSingleCommands = singleCommands.ToList();
                foreach (string command in forSingleCommands)
                {
                    if ((commands.Last() + "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:" + command + "},").Length < 30000)
                    {
                        commands[commands.Count-1] += "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:" + command + "},";
                        singleCommands.Remove(command);
                    }
                    else
                    {
                        toLong = true;
                        break;
                    }
                }
                if (!toLong)
                    commands[commands.Count - 1] += "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/setblock ~ ~1 ~ repeating_command_block 0 replace {auto:1b,Command:/fill ~ ~ ~ ~ ~-"+(2+commands.Count-forCommandBlocks[0].Count)+" ~ air}},{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/kill @e[type=MinecartCommandBlock,tag=mccKill]},";
                commands[commands.Count - 1] = commands.Last().Remove(commands.Last().Length-1) + "]}";
            }
            for (int i = 0;i<commands.Count;i++)
            {
                commands[i] = commands[i].Replace("$offsetY;", (commands.Count>1?-i-3:-i-2)+"");
                commands[i] = commands[i].Replace("$summonY;", (commands.Count-i) + "");
            }
            if (commands.Count>1)
            {
                commands.Insert(0, "/summon FallingSand ~ ~1 ~ {Block:minecraft:chain_command_block,Data:1,Time:1,DropItem:0,TileEntityData:{Command:PASTE COMMAND 3},Passengers:[");
                for (int i = 3; i < commands.Count; i++)
                    commands[0] += "{id:\"FallingSand\",Block:chain_command_block,Data:1,Time:1,DropItem:0,TileEntityData:{Command:PASTE COMMAND " + (i + 1) + "}},";
                commands[0] += "{id:FallingSand,Block:minecraft:redstone_block,Data:0,Time:1,DropItem:0,Passengers:[{id:FallingSand,Block:activator_rail,Data:0,Time:1,DropItem:0},{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/setblock ~ ~-" + commands.Count + " ~ minecraft:command_block 9 replace {Command:PASTE COMMAND 2}},{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/setblock ~ ~1 ~ repeating_command_block 0 replace {auto:1b,Command:/fill ~ ~ ~ ~ ~-2 ~ air}},{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/kill @e[type=MinecartCommandBlock,tag=mccKill]}]}]}";
            }

            return commands.ToArray();
        }

        private static string[] ParseCommand(string command, out List<Message> messages)
        {
            List<String> commands = new List<string>();
            messages = new List<Message>();

            command = command.Replace("$uuid;", uuid);
            #region Function
            while (command.Contains("//Function("))
            {
                int index = command.IndexOf("//Function(", StringComparison.Ordinal);
                if (!command.Substring(index + 11).Contains(')'))
                {
                    messages.Add(new Message("//Function: ) expected", MessageType.Error, index, index+11));
                    return new[] { command };
                }
                
                if (_functions.FindAll(f => f.name==command.Substring(index + 11).Split(')')[0].Trim()).Count==0)
                {
                    messages.Add(new Message(command.Substring(index, 12 + command.Substring(index + 11).IndexOf(')'))+": valid Function expected", MessageType.Error, index, 12+command.Substring(index + 11).IndexOf(')')));
                    return new[] { command };
                }
                
                Function function = _functions.Find(f => f.name == command.Substring(index + 11).Split(')')[0].Trim());

                if (_functionCalls.Contains(function.name))
                {
                    messages.Add(new Message(command.Substring(index, 12 + command.Substring(index + 11).IndexOf(')')) + ": Infinity loop", MessageType.InfinityLoop, index, 12 + command.Substring(index + 11).IndexOf(')')));
                    return new[] { command };
                }

                _functionCalls.Add(function.name);
                string tag = uuid + "functionCall" + function.name;

                command = command.Replace(command.Substring(index, 12 + command.Substring(index + 11).IndexOf(')')), "/scoreboard players tag @e[c=1] add " + tag);
                commands.Add(command);
                commands.Add("/scoreboard players set @e[tag=" + tag + "] "+uuid+"mccSuccessCount 0");

                foreach (string cmd in function.commands)
                {
                    List<Message> msgs;
                    
                    string[] parsedCommands = ParseCommand(cmd, out msgs);

                    if (msgs.FindAll(x => x.MessageType == MessageType.InfinityLoop).Count>0)
                    {
                        messages.Add(msgs.Find(x => x.MessageType == MessageType.InfinityLoop));
                        return new[] { command };
                    }

                    foreach (string parsedCmd in parsedCommands)
                    {
                        if (parsedCmd.Length > 0 && parsedCmd.Trim().TrimStart('c')[0]=='/')
                        {
                            if(parsedCmd.Trim()[0]=='c')
                                commands.Add("/execute @e[tag=" + tag + ",score_"+uuid+"mccSuccessCount_min=1] ~ ~ ~ " + parsedCmd.Trim().TrimStart('c'));
                            else
                                commands.Add("/execute @e[tag=" + tag + "] ~ ~ ~ " + parsedCmd.Trim());
                        }
                    }
                }
                commands.Add("/scoreboard players tag @e[tag=" + tag + "] remove " + tag);
                _functionCalls.Remove(function.name);
            }
            #endregion Function
            #region Space
            while (command.Contains("//Space("))
            {
                int index = command.IndexOf("//Space(", StringComparison.Ordinal);
                if (!command.Substring(index + 8).Contains(')'))
                {
                    messages.Add(new Message("//Space: ) expected", MessageType.Error, index, index+8));
                    return new[] { command };
                }

                int count;
                if (!int.TryParse(command.Substring(index + 8).Split(')')[0].Trim(), out count) && count < 16384)
                {
                    messages.Add(new Message(command.Substring(index, 9 + command.Substring(index + 8).IndexOf(')'))+": number smaller than 16384 expected", MessageType.Error, index, 9 + command.Substring(index + 8).IndexOf(')')));
                    return new[] { command };
                }

                string whitespaces = "";
                for (int i = 0; i < count; i++)
                    whitespaces += " ";
                command = command.Replace(command.Substring(index, 9 + command.Substring(index + 8).IndexOf(')')), "/tellraw @p {\"text\":\"" + whitespaces + "\"}");
            }
            #endregion Space

            if (commands.Count == 0)
                commands.Add(command);
            return commands.ToArray();
        }
    }
}
