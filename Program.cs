using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using System.Net;
using System.Net.Sockets;
using LiteNetLib.Utils;
/*using UnityEngine;*/


public class InfoPacket
{
    public byte CodeCommand { get; set; }
    public float[] XYZ_ord { get; set; }
    public int PeerId { get; set; }
}



public class Server
{
    private static Dictionary<int, float[]> _users = new Dictionary<int, float[]>();
    EventBasedNetListener netListener;
    NetManager netManager;
    NetPacketProcessor netPacketProcessor;

    float[] Respawn;


    // Start is called before the first frame update
    private static string GetLocalIPAddress() // getting the Ip server
    {
        var host = Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                Console.WriteLine(ip.GetType());
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
    static void Main()
    {

        EventBasedNetListener netListener = new EventBasedNetListener();
        NetManager netManager = new NetManager(netListener);
        NetPacketProcessor netPacketProcessor = new NetPacketProcessor();
        
        string _serverIp = GetLocalIPAddress();
        Console.WriteLine("Server's Ip: {0}", _serverIp);
        netManager.Start(9078);

        netListener.ConnectionRequestEvent += (request) =>
        {
            if (netManager.ConnectedPeersCount < 6)
                request.AcceptIfKey("SECRET_KEY");
            else
                request.Reject();
        };

        netListener.NetworkReceiveEvent += (client, packet, DeliverMethod) =>
        {
            netPacketProcessor.Send<InfoPacket>(client, new InfoPacket()
            {
                CodeCommand = 0,
                PeerId = client.Id,
                XYZ_ord = new float[] { 0, 0, 0 }
            },DeliveryMethod.ReliableOrdered);
            netPacketProcessor.ReadAllPackets(packet, client);
        };

        


        netPacketProcessor.SubscribeReusable<InfoPacket>((packet) =>
        {
            //Console.WriteLine("Got a packet from client!");
            // 0 - INIT only FROM server once
            // 1 - MOVE every time from server
            // 2 - ACTION 2
            // ...
            // 255 - deleting such an object - THE SERVER DOES NOT ACCEPT - ONLY SENDS

            byte ReceivedCodeCommand = packet.CodeCommand;

            if (ReceivedCodeCommand == 1)
            {
                if (_users.ContainsKey(packet.PeerId))//  if  server already has this object
                {
                    //just resend to others
                    if (!Equals( (_users[packet.PeerId][0], _users[packet.PeerId][1], _users[packet.PeerId][2]),(packet.XYZ_ord[0], packet.XYZ_ord[1], packet.XYZ_ord[2])))
                    {
                        _users[packet.PeerId] = packet.XYZ_ord;
                    }
                }
                else 
                {
                    _users.Add(packet.PeerId, packet.XYZ_ord);
                }
                foreach (NetPeer VarPeer in netManager.ConnectedPeerList)
                {
                    if (packet.PeerId != VarPeer.Id)
                    {
                        netPacketProcessor.Send<InfoPacket>(VarPeer, packet,DeliveryMethod.ReliableUnordered);
                    }
                }
            }
            else if (ReceivedCodeCommand == 2)// just resend packet to others
            {
                foreach (NetPeer VarPeer in netManager.ConnectedPeerList)
                {
                    if (packet.PeerId != VarPeer.Id)
                    {
                        netPacketProcessor.Send<InfoPacket>(VarPeer, packet, DeliveryMethod.ReliableUnordered);
                    }
                }
            }
            //else if (ReceivedCodeCommand == 255) - эту команду отправляет только сервер


        });

        netListener.PeerConnectedEvent += (NetPeer peer) =>
        {
            Console.WriteLine($"{peer.Id} connect");
            netPacketProcessor.Send<InfoPacket>(peer, new InfoPacket() {CodeCommand = 0, PeerId = peer.Id, XYZ_ord = new float[] { 0, 0, 0 } },DeliveryMethod.ReliableOrdered);
            Console.WriteLine($"Server send to {peer.Id} packet");

            _users.Add(peer.Id, new float[] {0, 0, 0});//adding to the cache

            if (netManager.ConnectedPeersCount != 1)
            {
                Console.WriteLine($"{peer.Id} NOT FIRST");
                foreach (NetPeer VarPeer in netManager.ConnectedPeerList) //TO OLDER ABOUT YOUNG
                {
                    if (VarPeer != peer)
                        netPacketProcessor.Send<InfoPacket>(VarPeer, new InfoPacket() { CodeCommand = 1, PeerId = peer.Id, XYZ_ord = new float[] { 0, 0, 0 } }, DeliveryMethod.ReliableOrdered);
                }
                Console.WriteLine("OTHER PLAYERS HAVE BEEN NOTIFIED");
                foreach (NetPeer VarPeer in netManager.ConnectedPeerList) //TO YOUNG ABOUT OLDER
                {
                    if (VarPeer != peer)
                        netPacketProcessor.Send<InfoPacket>(peer, new InfoPacket() { CodeCommand = 1, PeerId = VarPeer.Id, XYZ_ord = _users[VarPeer.Id] }, DeliveryMethod.ReliableOrdered);
                }
                Console.WriteLine("YOUNG PLAYER HAS BEEN NOTIFIED");

            }
            else
                Console.WriteLine($"{peer.Id}  FIRST");
        };

        netListener.PeerDisconnectedEvent += (peer, _) =>
        {
            Console.WriteLine($"{peer} disconnect");
            netManager.ConnectedPeerList.Remove(peer);
            _users.Remove(peer.Id);
            foreach(NetPeer varPeer in netManager.ConnectedPeerList)
            {
                if(peer != varPeer)
                {
                    netPacketProcessor.Send<InfoPacket>(varPeer, new InfoPacket() { CodeCommand = 255, PeerId = peer.Id, XYZ_ord = new float[] { 0, 0, 0 } }, DeliveryMethod.ReliableOrdered);
                }
            }
        };

        Console.WriteLine("Server Started!");
        var stop = false;
        Console.CancelKeyPress += (a, b) => stop = true;
        while (!stop)
        {
            netManager.PollEvents();
            Thread.Sleep(10);
        }
        netManager.Stop();

    }
    





    // Update is called once per frame
    void Update()
    {
        netManager.PollEvents();/*
        Thread.Sleep(15);*/
    }
}