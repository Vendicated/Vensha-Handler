using System.Threading.Tasks;
using System;
using System.Reflection;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using msg = Discord.LogMessage;

namespace CommandHandler
{
    public class CommandHandler
    {
        private IEnumerable<CommandConstructor> commands = new List<CommandConstructor>();
        private DiscordSocketClient client;
        private Func<msg, Task> Log;
        private Config config;

        public CommandHandler(DiscordSocketClient client, Config config, Func<msg, Task> log)
        {
            this.client = client;
            this.config = config;
            this.Log = log;
        }

        public void InitCommands()
        {
            var types = from t in Assembly.GetEntryAssembly().GetTypes()
                        where t.IsSubclassOf(typeof(VenshaCommand))
                        select t;

            foreach (var t in types)
            {
                var methods = from m in t.GetMethods()
                              where m.GetCustomAttributes(typeof(Command)).Count() > 0
                              select m;

                foreach (var method in methods)
                {
                    var command = new CommandConstructor(method, t);
                    commands = commands.Append(command);
                }
            }

            this.Log(new msg(Discord.LogSeverity.Info, "CommandHandler", "Commands successfully initiated!"));
            this.client.MessageReceived += OnMessage;
        }

        private async Task OnMessage(SocketMessage msgRaw)
        {
            var msg = msgRaw as SocketUserMessage;
            if (msg == null) return;

            if (!msg.Content.StartsWith(this.config.prefix)) return;
            string[] args = msg.Content
                                .Trim()
                                .Substring(this.config.prefix.Length)
                                .Split(' ', int.MaxValue, StringSplitOptions.RemoveEmptyEntries);

            string commandName = args.FirstOrDefault()?.ToLower();
            if (commandName == null) return;

            var command = this.commands.FirstOrDefault(c => c.name == commandName || c.aliases.Contains(commandName));
            if (command == null) return;

            await command.callback(client, msgRaw, args.Skip(1).ToArray());
        }
    }
}