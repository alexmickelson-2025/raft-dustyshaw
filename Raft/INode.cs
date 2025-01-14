namespace Raft
{
    public interface INode
    {
        int ElectionTimeout { get; set; }
        INode[] OtherNodes { get; set; }
        Node.NodeState State { get; set; }
        bool Vote { get; set; }

        bool RespondToAppendEntriesRPC();
        bool SendAppendEntriesRPC();
    }
}