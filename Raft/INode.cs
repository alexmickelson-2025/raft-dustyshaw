namespace Raft
{
    public interface INode
    {
        int ElectionTimeout { get; set; }
        INode[] OtherNodes { get; set; }
        Node.NodeState State { get; set; }
		public Guid LeaderId { get; set; }
		int TermNumber { get; set; }
		public DateTime WhenTimerStarted { get; set; }


		void RespondToAppendEntriesRPC(Guid leaderId, int termNumber);
        void SendAppendEntriesRPC();
        void StartElection();
        void SendVoteRequestRPCsToOtherNodes();
        bool RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm);
    }
}