using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;


public class InfoPacket
{
    public byte CodeCommand { get; set; }
    public float[] XYZ_ord { get; set; }
    public int PeerId { get; set; }
}
public class Client : MonoBehaviour
{

    EventBasedNetListener netListener;
    NetManager netManager;
    NetPacketProcessor netPacketProcessor;

    NetPeer Server;
    NetPeer ClientpPeer;

    int SelfId;
    float[] StartPosition;
    float[] CurrentPosition;

    public bool BottonAction2;
    public bool BottonAction3;
    public bool BottonAction4;

    public GameObject GameObjectToSpawn;


    [SerializeField]
    private Transform _userTransform;


    private static Dictionary<int, float[]> OtherUsersInfo = new Dictionary<int, float[]>();
    private static Dictionary<int, GameObject> OtherPrefabs = new Dictionary<int, GameObject>();

    void Start()
    {
        netListener = new EventBasedNetListener();
        netPacketProcessor = new NetPacketProcessor();
        netManager = new NetManager(netListener);
            
        //netManager.Connect("192.168.168.109" /* host ip or name */, 9060 /* port */);


        netListener.PeerConnectedEvent += (client) =>
        {
            Debug.Log($"We got connection: {client.EndPoint}");
        };


        netListener.NetworkReceiveEvent += (client, reader, deliveryMethod) =>
        {
            netPacketProcessor.ReadAllPackets(reader, client);
        };


        netPacketProcessor.SubscribeReusable<InfoPacket>((packet) =>
        {
            //Console.WriteLine("Got a packet from client!");
            // 0 - INIT only from server once
            // 1 - MOVE every time from server
            // 2 - ACTION
            // ...
            // 255 - deleting such an object - THE SERVER DOES NOT ACCEPT - ONLY SENDS

            byte ReceivedCodeCommand = packet.CodeCommand;


            if (ReceivedCodeCommand == 0) 
            {
                SelfId = packet.PeerId;
            }

            else if (ReceivedCodeCommand == 1)// someone move
            {
                if (!(packet.PeerId == SelfId))
                {
                    if (OtherPrefabs.ContainsKey(packet.PeerId)) // if we have this peer in our memory
                    {
                        Debug.Log($"Player <<{packet.PeerId}>> is moving ");
                        OtherUsersInfo[packet.PeerId] = packet.XYZ_ord;
                        Vector3 position = new Vector3(OtherUsersInfo[packet.PeerId][0], OtherUsersInfo[packet.PeerId][1], OtherUsersInfo[packet.PeerId][2]);
                        // Quaternion.Euler rotation = new Quaternion.Euler() { };
                        OtherPrefabs[packet.PeerId].transform.position = position;
                        Debug.Log($"Player <<{packet.PeerId}>> is moving  - FINISHED");
                    }
                    else // we will remember
                    {
                        Debug.Log("OTHER PLAYER OLD");
                        Debug.Log($"Player <<{packet.PeerId}>> is already here ");
                        OtherUsersInfo.Add(packet.PeerId, packet.XYZ_ord);

                        Vector3 position = new Vector3(OtherUsersInfo[packet.PeerId][0], OtherUsersInfo[packet.PeerId][1], OtherUsersInfo[packet.PeerId][2]);

                        GameObject obj = Instantiate(GameObjectToSpawn, position, Quaternion.identity);
                        OtherPrefabs.Add(packet.PeerId, obj);

                        Debug.Log($"Player <<{packet.PeerId}>> is already here - FINISHED");
                    }
                }
                   
            }
            else if (ReceivedCodeCommand == 255) 
            {
                Debug.Log($"Player <<{packet.PeerId}>> left us");
                Destroy(OtherPrefabs[packet.PeerId]);
                OtherPrefabs.Remove(packet.PeerId);
                OtherUsersInfo.Remove(packet.PeerId);
                Debug.Log($"Player <<{packet.PeerId}>> left us - FINISHED");
            }

        });

        StartPosition = GetSerializedPosition();

        Debug.Log($"Start Pos: {StartPosition[0]},{StartPosition[1]},{StartPosition[2]}");

        CurrentPosition = StartPosition;

        netManager = new NetManager(netListener);
        netManager.Start();
        netManager.Connect("192.168.168.109", 9078, "SECRET_KEY");
        

    }
    void FixedUpdate()
    {
        Debug.Log("FixedUpdate");
        float[] NewPos = GetSerializedPosition();

        if (!Equals( (NewPos[0],NewPos[1],NewPos[2]),(CurrentPosition[0],CurrentPosition[1],CurrentPosition[2])))
        {
            CurrentPosition = NewPos;
        }
        // EVERY TIME WE SEND OUR POSITION WITH CODE = "1"
        netPacketProcessor.Send<InfoPacket>(netManager.FirstPeer, new InfoPacket() { PeerId = SelfId, CodeCommand = 1, XYZ_ord = CurrentPosition }, DeliveryMethod.ReliableUnordered);
        netManager.PollEvents();
    }

    void OnDestroy() 
    {
        netManager.Stop();
    }

    float[] GetSerializedPosition()
    {
        return new float[] { _userTransform.position.x, _userTransform.position.y, _userTransform.position.z };
    }

} 