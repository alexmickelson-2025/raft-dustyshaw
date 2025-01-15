namespace Raft
{
    public interface INode
    {
        int ElectionTimeout { get; set; }
        INode[] OtherNodes { get; set; }
        Node.NodeState State { get; set; }
        bool Vote { get; set; }

        void RespondToAppendEntriesRPC();
        void SendAppendEntriesRPC();
        void StartElection();
        void AskForVotesFromOtherNodes();
        bool RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm);
    }
}