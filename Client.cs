using System;
using System.Collections;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;



public class Client : MonoBehaviour
{
    EventBasedNetListener netListener;
    NetManager netManager;
    NetPacketProcessor netPacketProcessor;
    public GameObject OtherPlayerPrefab;

    private UserInfo selfStart;
    private UserInfo selfCurrent;

    [SerializeField]
    private Transform _userTransform;


    private static Dictionary<NetPeer, UserInfo> OtherUsersInfo = new Dictionary<NetPeer, UserInfo>();
    private static Dictionary<NetPeer, GameObject> OtherPrefabs = new Dictionary<NetPeer, GameObject>();

    void Start()
    {
        netListener = new EventBasedNetListener();
        netPacketProcessor = new NetPacketProcessor();

        netListener.PeerConnectedEvent += (server) => {
            Debug.LogError($"Connected to server: {server}");
            
        };

        netListener.NetworkReceiveEvent += (server, reader, deliveryMethod) => {
            netPacketProcessor.ReadAllPackets(reader, server);
        };

        netPacketProcessor.SubscribeReusable<InfoPacket>
       (
            (packet) => 
            {
                Debug.Log("Got a foo packet!");
                Debug.Log(packet.CodeCommand);
                Debug.Log(packet.userInfo);
                Debug.Log(packet.Peer);
            }
        );

        netManager = new NetManager(netListener);
        netManager.Start();
        netManager.Connect("192.168.168.109", 9060, "SECRET_KEY");
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Update");
        netManager.PollEvents();
        UserInfo PlayerPos = GetSerializedPosition();
        var oldPos = (selfCurrent.X_ord, selfCurrent.Y_ord, selfCurrent.Z_ord);
        var newPos = (PlayerPos.X_ord, PlayerPos.Y_ord, PlayerPos.Z_ord);
        if (Equals(oldPos, newPos))
        {
            
        }
    }
    private UserInfo GetSerializedPosition()
    {
        UserInfo PlayerPos = new UserInfo();

        PlayerPos.X_ord = _userTransform.position.x;
        PlayerPos.Y_ord = _userTransform.position.y;
        PlayerPos.Z_ord = _userTransform.position.z;


        return PlayerPos;
    }
}
