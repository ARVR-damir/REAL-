using LiteNetLib;
public class InfoPacket
{
    public byte CodeCommand { get; set; }
    public UserInfo userInfo { get; set; }
    public NetPeer Peer { get; set; }
}