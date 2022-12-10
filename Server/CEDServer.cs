﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using Cedserver;

namespace Server; 

public static class CEDServer {
#if DEBUG
    public static bool DEBUG = true;
#else
    public static bool DEBUG = false;
#endif
    
    public const int ProtocolVersion = 6;
    public static Landscape Landscape { get; }
    public static TcpListener TCPServer { get; }
    public static List<NetState> Clients { get; }
    public static bool Quit { get; set; }

    private static DateTime _lastFlush;
    private static bool _valid;

    static CEDServer() {
        LogInfo("Initialization started");
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;
        Landscape = new Landscape(Config.Map.MapPath, Config.Map.Statics, Config.Map.StaIdx, Config.Tiledata,
            Config.Radarcol, Config.Map.Width, Config.Map.Height, ref _valid);
        TCPServer = new TcpListener(IPAddress.Any, Config.Port);
        TCPServer.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        Quit = false;
        _lastFlush = DateTime.Now;
        Clients = new List<NetState>();
        LogInfo("Initialization done");
    }

    private static async void Listen() {
        while (true) {
            var ns = new NetState(await TCPServer.AcceptTcpClientAsync());
            Clients.Add(ns);
            SendPacket(ns, new ConnectionHandling.ProtocolVersionPacket(ProtocolVersion));
            new Task(() => Receive(ns)).Start();
        }
    }

    private static async void Receive(NetState ns) {
        byte[] buffer = new byte[4096];
        while(true) {
            int bytesRead = await ns.TcpClient.GetStream().ReadAsync(buffer);
            if (bytesRead > 0) {
                ns.ReceiveStream.Write(buffer, 0, bytesRead);
                buffer = new byte[4096];
            }
            ProcessBuffer(ns);
        }
    }

    private static void ProcessBuffer(NetState ns) {
        try {
            ns.ReceiveStream.Position = 0;
            while (ns.ReceiveStream.Length >= 1 && ns.TcpClient.Connected) {
                using var reader = new BinaryReader(ns.ReceiveStream, Encoding.UTF8, true);
                var packetId = reader.ReadByte();
                var packetHandler = PacketHandlers.GetHandler(packetId);
                if (packetHandler != null) {
                    ns.LastAction = DateTime.Now;
                    var size = packetHandler.Length;
                    if (size == 0) {
                        if (ns.ReceiveStream.Length > 5) {
                            size = reader.ReadUInt32();
                        }
                        else {
                            break; //wait for more data
                        }
                    }

                    if (ns.ReceiveStream.Length >= size) {
                        //buffer.Lock()
                        packetHandler.OnReceive(reader, ns);
                        //buffer.Unlock()/
                        ns.Dequeue(size);
                    }
                    else {
                        break; //wait for more data
                    }
                }
                else {
                    ns.LogError($"Dropping client due to unknown packet: {packetId}");
                    Disconnect(ns);
                }
            }

            ns.LastAction = DateTime.Now;
        }
        catch (Exception e) {
            Console.WriteLine(e);
            ns.LogError("Error processing buffer of client");
        }
    }

    private static void CheckNetStates() {
        foreach (var ns in Clients) {
            if (ns == null) return;
            if (ns.TcpClient.Connected) {
                if (DateTime.Now - TimeSpan.FromMinutes(2) > ns.LastAction) {
                    ns.LogInfo($"Timeout: {(ns.Account != null ? ns.Account.Name : string.Empty)}");
                    Disconnect(ns);
                }
            }
            else {
                OnDisconnect(ns);
            }
        }
    }

    private static void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
        LogInfo("Killed");
        Quit = true;
        e.Cancel = true;
    }

    public static void Run() {
        TCPServer.Start();
        new Task(Listen).Start();
        do {
            CheckNetStates();
            if (DateTime.Now - TimeSpan.FromMinutes(1) > _lastFlush) {
                Landscape.Flush();
                Config.Flush();
                _lastFlush = DateTime.Now;
            }
            Thread.Sleep(1);
        } while (!Quit);
    }

    public static void SendPacket(NetState? ns, Packet packet) {
        if (ns != null) {
            packet.Writer.Seek(0, SeekOrigin.Begin);
            packet.Write(ns.TcpClient.GetStream());
        }
        else { //broadcast
            foreach (var netState in Clients) {
                if (netState.TcpClient.Connected) {
                    SendPacket(netState, packet);
                }
            }
        }
    }

    private static void OnDisconnect(NetState? ns) {
        if (ns == null) return;
        ns.LogInfo("Disconnect");
        SendPacket(null, new ClientHandling.ClientDisconnectedPacket(ns.Account.Name));
    }

    public static void Disconnect(NetState ns) {
        if (ns.TcpClient.Connected) {
            ns.TcpClient.Close();
            Clients.Remove(ns);
        }
    }

    public static void LogInfo(string log) {
        Log("INFO", log);
    }

    public static void LogError(string log) {
        Log("ERROR", log);
    }

    public static void LogDebug(string log) {
        if (DEBUG) Log("DEBUG", log);
    }

    private static void Log(string level, string log) {
        Console.WriteLine($"[{level}]{DateTime.Now} {log}");
    }
}
