using Unity.NetCode;

public struct InterestInZoneRPC : IRpcCommand {
    public int ZoneId;
    public bool IsInterested;
}