using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace mccBuild
{
    [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
    public class Build
    {
        static readonly string Uuid = Guid.NewGuid().ToString();
        static int xOffset = 2;
        static int zOffset = 0;
        
        static readonly List<string> FunctionCalls = new List<string>();

        private static List<string> _initCommands;
        private static List<string> _removeCommands;
        private static Dictionary<string, string[]> parsedFiles = new Dictionary<string, string[]>();

        static List<Message> _messages = new List<Message>();

        public static void Main(string[] args)
        {
            if (args.Length < 1) return;
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("No such file");
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(args[0]);
            Directory.SetCurrentDirectory(Directory.GetParent(args[0]).FullName);
            string dir = Directory.CreateDirectory("output").FullName;
            foreach (string file in Directory.GetFiles(dir))
            {
                File.Delete(file);
            }

            string[] commands = GetOneCommand(fileName);
            for (int i = 0; i < commands.Length; i++)
            {
                File.WriteAllText(dir + "\\output" + (i + 1) + ".txt", commands[i]);
            }
            _messages.ForEach(msg=>Console.WriteLine(msg.Text+" at "+msg.File));
        }

        private static List<List<CommandBlock>> GetCommandBlocks(string fileName)
        {
            List<List<CommandBlock>> rows = new List<List<CommandBlock>>();
            _initCommands = new List<string>();
            _removeCommands = new List<string>();
            _messages = new List<Message>();

            _initCommands.Add("/summon ArmorStand ~ ~-$offsetY; ~ {NoGravity:1b,Invisible:1b,Marker:1b,Small:1b,CustomName:@,Tags:[\""+Uuid+"\"]}");
            
            #region normal
            List<CommandBlock> commandBlocks = new List<CommandBlock>();
            
            foreach (string cmd in ParseFile(fileName))
            {
                commandBlocks.Add(new CommandBlock(cmd.Trim().TrimStart('c'),
                    cmd.Trim()[0] == 'c' ? CommandBlockType.ChainConditional : CommandBlockType.Chain,
                    CommandBlockOrientation.Up));
            }
            if (commandBlocks.Count > 0)
            {
                commandBlocks[0].CommandBlockType = CommandBlockType.Repeat;
                rows.Add(commandBlocks);
            }
            #endregion normal

            _initCommands.Add("/scoreboard objectives add mccSuccessCount dummy");
            _removeCommands.Add("/kill @e[tag=" + Uuid + "]");
            #region remove
            _removeCommands.Add("/fill ~" + xOffset + " ~-$removeCount; ~" + zOffset + " ~" + rows.Count * xOffset + " ~" + (rows.Max(x => x.Count) - 2) + " ~" + rows.Count * zOffset + " air");
            List<CommandBlock> removeBlocks = new List<CommandBlock>();
            bool toLong = true;
            while (toLong)
            {
                toLong = false;
                string removeCommand = "/summon FallingSand ~ ~$removeY; ~ {Block:redstone_block,Data:0,Time:1,DropItem:0,Passengers:[{id:FallingSand,Block:activator_rail,Data:0,Time:1,DropItem:0},";

                List<string> forRemoveCommands = _removeCommands.ToList();
                for (int i = 0; i < forRemoveCommands.Count; i++)
                {
                    string command = forRemoveCommands[i];
                    if (i != forRemoveCommands.Count-1)
                        command = "/execute @e[tag=" + Uuid + "] ~ ~ ~ " + command;
                    if ((removeCommand + "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:" + command + "},").Length < 30000)
                    {
                        removeCommand += "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:" + command + "},";
                        _removeCommands.Remove(command);
                    }
                    else
                    {
                        toLong = true;
                        break;
                    }
                }
                if (!toLong)
                    removeCommand += "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/setblock ~ ~1 ~ repeating_command_block 0 replace {auto:1b,Command:/fill ~ ~ ~ ~ ~-" + (3 + removeBlocks.Count) + " ~ air}},{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/kill @e[type=MinecartCommandBlock,tag=mccKill]},";
                removeBlocks.Add(new CommandBlock(removeCommand.Remove(removeCommand.Length - 1) + "]}", CommandBlockType.Chain, CommandBlockOrientation.Up));
            }
            for (int i = 0; i < removeBlocks.Count; i++)
            {
                removeBlocks[i].Command = removeBlocks[i].Command.Replace("$removeY;", removeBlocks.Count - i + "").Replace("$removeCount;", (removeBlocks.Count + 1) + "");
            }
            removeBlocks.First().CommandBlockType = CommandBlockType.ImpulseConditional;
            removeBlocks.First().Auto = false;
            rows.Insert(0, removeBlocks);
            #endregion remove

            return rows;
        }

        private static string[] GetOneCommand(string fileName)
        {
            List<string> commands = new List<string>();
            List<string> singleCommands = new List<string>();

            List<List<CommandBlock>> forCommandBlocks = GetCommandBlocks(fileName);
            for (int x = 0; x < forCommandBlocks.Count; x++)
            {
                for (int y = 0; y < forCommandBlocks[x].Count; y++)
                {
                    CommandBlock commandBlock = forCommandBlocks[x][y];
                    singleCommands.Add("/execute @e[c=1] ~ ~-$offsetY; ~ /setblock ~" + x * xOffset + " ~" + y + " ~" + x * zOffset + " " + commandBlock.Name + " " + commandBlock.Meta + " replace {Command:" + commandBlock.Command + ",TrackOutput:0b,auto:" + (commandBlock.Auto ? 1 : 0) + "b}");
                }
            }
            singleCommands.Add(_initCommands.First());
            _initCommands.Skip(1).ToList().ForEach(x => singleCommands.Add("/execute @e[tag="+Uuid+"] ~ ~ ~ "+x));

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
                        commands[commands.Count - 1] += "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:" + command + "},";
                        singleCommands.Remove(command);
                    }
                    else
                    {
                        toLong = true;
                        break;
                    }
                }
                if (!toLong)
                    commands[commands.Count - 1] += "{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/setblock ~ ~1 ~ repeating_command_block 0 replace {auto:1b,Command:/fill ~ ~ ~ ~ ~-" + (2 + commands.Count - forCommandBlocks[0].Count) + " ~ air}},{id:\"MinecartCommandBlock\",Tags:[\"mccKill\"],Command:/kill @e[type=MinecartCommandBlock,tag=mccKill]},";
                commands[commands.Count - 1] = commands.Last().Remove(commands.Last().Length - 1) + "]}";
            }
            for (int i = 0; i < commands.Count; i++)
            {
                commands[i] = commands[i].Replace("$offsetY;", (commands.Count + 1) + "");
                commands[i] = commands[i].Replace("$summonY;", (commands.Count - i) + "");
            }
            if (commands.Count > 1)
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

            command = command.Replace("$uuid;", Uuid);
            #region Function
            while (command.Contains("//"))
            {
                int index = command.IndexOf("//", StringComparison.Ordinal);
                if (!command.Substring(index).Contains('('))
                {
                    messages.Add(new Message("//: ( expected", MessageType.Error, index, index + 1));
                    return new[] { command };
                }

                int index2 = command.Substring(index).IndexOf('(');
                if (!command.Substring(index2).Contains(')'))
                {
                    messages.Add(new Message(command.Substring(index, index2-index)+": ) expected", MessageType.Error, index, index2));
                    return new[] { command };
                }

                string fileName = command.Substring(index + 2, index2 - index - 2).ToLower();
                
                if (!File.Exists(command.Substring(index+2, index2 - index-2)+".mcc")|| !new Regex("^[a-zA-Z0-9]+$").IsMatch(fileName.Replace('\\', 'a')))
                {
                    messages.Add(new Message(command.Substring(index, index2 - index) + ": valid File expected", MessageType.Error, index+2, index2-1));
                    return new[] { command };
                }
                
                if (FunctionCalls.Contains(fileName))
                {
                    messages.Add(new Message(command.Substring(index, fileName.Length+command.Substring(index2).IndexOf(')')) + ": Infinity loop", MessageType.InfinityLoop, index, index2+ command.Substring(index2).IndexOf(')')));
                    return new[] { command };
                }

                FunctionCalls.Add(fileName);
                string tag = Uuid + "functionCall" + fileName.Replace('\\','-');

                command = command.Replace(command.Substring(index, index2+1+command.Substring(index2).IndexOf(')')), "/scoreboard players tag @e[c=1] add " + tag);
                commands.Add(command);
                commands.Add("/stats entity @e[tag="+tag+"] set SuccessCount @e[c=1] mccSuccessCount");
                commands.Add("/scoreboard players set @e[tag=" + tag + "] mccSuccessCount 0");
                
                foreach (string cmd in ParseFile(fileName))
                {
                    List<Message> msgs;

                    string[] parsedCommands = ParseCommand(cmd, out msgs);

                    if (msgs.FindAll(x => x.MessageType == MessageType.InfinityLoop).Count > 0)
                    {
                        messages.Add(msgs.Find(x => x.MessageType == MessageType.InfinityLoop));
                        return new[] { command };
                    }

                    foreach (string parsedCmd in parsedCommands)
                    {
                        if (parsedCmd.Length > 0 && parsedCmd.Trim().TrimStart('c')[0] == '/')
                        {
                            if (parsedCmd.Trim()[0] == 'c')
                                commands.Add("/execute @e[tag=" + tag + ",score_mccSuccessCount_min=1] ~ ~ ~ " + parsedCmd.Trim().TrimStart('c'));
                            else
                                commands.Add("/execute @e[tag=" + tag + "] ~ ~ ~ " + parsedCmd.Trim());
                        }
                    }
                }
                commands.Add("/scoreboard players tag @e[tag=" + tag + "] remove " + tag);
                FunctionCalls.Remove(fileName);
            }
            #endregion Function
            #region Space
            while (command.Contains("//Space("))
            {
                int index = command.IndexOf("//Space(", StringComparison.Ordinal);
                if (!command.Substring(index + 8).Contains(')'))
                {
                    messages.Add(new Message("//Space: ) expected", MessageType.Error, index, index + 8));
                    return new[] { command };
                }

                int count;
                if (!int.TryParse(command.Substring(index + 8).Split(')')[0].Trim(), out count) && count < 16384)
                {
                    messages.Add(new Message(command.Substring(index, 9 + command.Substring(index + 8).IndexOf(')')) + ": number smaller than 16384 expected", MessageType.Error, index, 9 + command.Substring(index + 8).IndexOf(')')));
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
            Message msg = null;
            commands.FindAll(x=>x.Length>30000).ForEach(x=>msg=new Message("Command is too long", MessageType.Error));
            if (msg != null)
            {
                messages.Add(msg);
                return new[] {command};
            }
            return commands.ToArray();
        }

        private static string[] ParseFile(string fileName)
        {

            if (parsedFiles.ContainsKey(fileName))
                return parsedFiles[fileName];

            List<string> initCommands = new List<string>();
            List<string> mainCommands = new List<string>();
            List<string> removeCommands = new List<string>();

            string[] file = File.ReadAllLines(fileName + ".mcc");
            #region read
            ParseState parseState = ParseState.Nothing;
            foreach (string line in file)
            {
                switch (line)
                {
                    case "#INIT":
                    {
                        parseState=ParseState.Init;
                        break;
                    }
                    case "#MAIN":
                    {
                        parseState=ParseState.Main;
                        break;
                    }
                    case "#REMOVE":
                    {
                        parseState=ParseState.Remove;
                        break;
                    }
                    default:
                    {
                        if(line.StartsWith("#"))
                            break;
                        switch (parseState)
                        {
                                case ParseState.Init:
                                {
                                    initCommands.Add(line.Trim());
                                    break;
                                }
                                case ParseState.Main:
                                {
                                    mainCommands.Add(line.Trim());
                                    break;
                                }
                                case ParseState.Remove:
                                {
                                    removeCommands.Add(line.Trim());
                                    break;
                                }
                        }
                        break;
                    }

                }
            }
            if (parseState == ParseState.Nothing)
                foreach (string s in file)
                {
                    if (!s.StartsWith("#"))
                        mainCommands.Add(s.Trim());
                }
            #endregion read
            List<Message> messages;
            initCommands = string.Join("\n", initCommands).Replace("\n/", "\n//").Replace("\nc/", "\n//").Split(new[] { "\n/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            initCommands.RemoveAll(x => !x.TrimStart('c').StartsWith("/"));
            for(int i =0;i<initCommands.Count;i++)
            {
                ParseCommand(initCommands[i].Replace('\n', ' '), out messages)
                    .ToList()
                    .ForEach(y => _initCommands.Add(y));
                messages.ForEach(msg => msg.File = fileName+".mcc");
                messages.ForEach(msg => msg.Line = i + 1);
                messages.ForEach(msg => _messages.Add(msg));
            }

            removeCommands = string.Join("\n", removeCommands).Replace("\n/", "\n//").Replace("\nc/", "\n//").Split(new[] { "\n/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            removeCommands.RemoveAll(x => !x.TrimStart('c').StartsWith("/"));
            for (int i = 0; i < removeCommands.Count; i++)
            {
                ParseCommand(removeCommands[i].Replace('\n', ' '), out messages)
                    .ToList()
                    .ForEach(y => _removeCommands.Add(y));
                messages.ForEach(msg => msg.File = fileName + ".mcc");
                messages.ForEach(msg => msg.Line = i + 1);
                messages.ForEach(msg => _messages.Add(msg));
            }

            List<string> parsedCommands = new List<string>();
            mainCommands = string.Join("\n", mainCommands).Replace("\n/", "\n//").Replace("\nc/", "\n/c/").Split(new[] { "\n/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            mainCommands.RemoveAll(x => !x.TrimStart('c').StartsWith("/"));
            for (int i = 0; i < mainCommands.Count; i++)
            {
                ParseCommand(mainCommands[i].Replace('\n', ' '), out messages)
                    .ToList()
                    .ForEach(y => parsedCommands.Add(y));
                messages.ForEach(msg => msg.File = fileName + ".mcc");
                messages.ForEach(msg => msg.Line = i + 1);
                messages.ForEach(msg => _messages.Add(msg));
            }

            parsedFiles.Add(fileName, parsedCommands.ToArray());
            return parsedCommands.ToArray();
        }

        private enum ParseState
        {
            Nothing,Init,Main,Remove
        }
    }
}
