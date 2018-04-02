using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Frame;
using BeardedManStudios.SimpleJSON;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace NatHolePunchServer
{
	class NatHolePunch
	{
		private static UDPServer server = null;
		private static Dictionary<string, List<Host>> hosts = new Dictionary<string, List<Host>>();

		static void Main(string[] args)
		{
			string read;
			bool quit = false;
            server = new UDPServer(2048);
			System.Console.Write("Hosting nat on: ");
			System.Console.Write(BeardedManStudios.Forge.Networking.Nat.NatHolePunch.DEFAULT_NAT_SERVER_PORT);
			System.Console.Write(System.Environment.NewLine);
			server.Connect("0.0.0.0", BeardedManStudios.Forge.Networking.Nat.NatHolePunch.DEFAULT_NAT_SERVER_PORT);

			server.textMessageReceived += TextMessageReceived;

			while (!quit)
			{
				read = System.Console.ReadLine().ToLower();
			}
		}

		private static void TextMessageReceived(NetworkingPlayer player, Text frame, NetWorker sender)
		{
			try
			{
				var json = JSON.Parse(frame.ToString());

				if (json["register"] != null)
				{
					string address = player.IPEndPointHandle.Address.ToString();
					ushort port = json["register"]["port"].AsUShort;

					if (!hosts.ContainsKey(address))
						hosts.Add(address, new List<Host>());

                    for (int i = hosts[address].Count - 1; i >= 0; i--)
					{
                        // This host is already registered. Let's remove it so it can be refreshed.
                        if (hosts[address][i].port == port)
                            hosts[address].Remove(hosts[address][i]);
                    }

					System.Console.Write("Hosted Server received: ");
					System.Console.Write(address);
					System.Console.Write(":");
					System.Console.Write(port);
					System.Console.Write(" received");
					System.Console.Write(System.Environment.NewLine);

					hosts[address].Add(new Host(player, address, port));
				}
				else if (json["host"] != null && json["port"] != null)
				{
					server.Disconnect(player, false);

					string addresss = json["host"];
					ushort port = json["port"].AsUShort;
					ushort listeningPort = json["clientPort"].AsUShort;

					addresss = NetWorker.ResolveHost(addresss, port).Address.ToString();

					if (!hosts.ContainsKey(addresss))
						return;

					Host foundHost = new Host();
					foreach (Host iHost in hosts[addresss])
					{
						if (iHost.port == port)
						{
							foundHost = iHost;
							break;
						}
					}


					if (string.IsNullOrEmpty(foundHost.host))
						return;

					JSONNode obj = JSONNode.Parse("{}");
					obj.Add("host", new JSONData(player.IPEndPointHandle.Address.ToString().Split(':')[0]));
					obj.Add("port", new JSONData(listeningPort));

					JSONClass sendObj = new JSONClass();
					sendObj.Add("nat", obj);

					Text notifyFrame = Text.CreateFromString(server.Time.Timestep, sendObj.ToString(), false, Receivers.Target, MessageGroupIds.NAT_ROUTE_REQUEST, false);

					server.Send(foundHost.player, notifyFrame, true);
				}
			}
			catch
			{
				server.Disconnect(player, true);
			}
		}
	}
}