





namespace Raft
{
	public interface INode
	{
		System.Timers.Timer aTimer { get; set; }
		int ElectionTimeout { get; set; }
		int HeartbeatTimeout { get; }
		Guid LeaderId { get; set; }
		int NetworkRequestDelay { get; set; }
		Guid NodeId { get; set; }
		INode[] OtherNodes { get; set; }
		Node.NodeState State { get; set; }
		int TermNumber { get; set; }
		int VotedForTermNumber { get; set; }
		Guid VoteForId { get; set; }
		List<bool> votesRecieved { get; set; }
		DateTime WhenTimerStarted { get; set; }

		int LowerBoundElectionTime { get; set; }
		int UpperBoundElectionTime { get; set; }
		public List<Entry> Entries { get; set; } 
		int CommitIndex { get; set; }

		void BecomeLeader();
		void DetermineElectionResults();
		Task RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm);
		void RecieveVoteResults(bool result, int termNumber);
		Task RecieveAppendEntriesRPC(Guid leaderId, int TermNumber, int CommitIndex, List<Entry> LeadersLog);
		void SendAppendEntriesRPC();
		Task SendMyVoteToCandidate(Guid candidateId, bool result);
		void SendVoteRequestRPCsToOtherNodes();
		void StartElection();
		void TimeoutHasPassed();
		void TimeoutHasPassedForLeaders();
		void RespondBackToLeader(bool response, int myTermNumber, int myCommitIndex);
	}
}