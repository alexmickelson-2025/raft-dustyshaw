using System.Timers;

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
        bool IsRunning { get; set; }

        void BecomeLeader();
		bool HasMajority(List<bool> Results);
		Task RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm);
		void RecieveVoteResults(bool result, int termNumber);
		Task RecieveAppendEntriesRPC(int LeadersTermNumber, Guid leaderId, int prevLogIndex, List<Entry> LeadersLog, int leaderCommit);
		void SendAppendEntriesRPC();
		List<Entry> CalculateEntriesToSend(INode node);
		Task SendMyVoteToCandidate(Guid candidateId, bool result);
		void TimeoutHasPassed();
		void SendVoteRequestRPCsToOtherNodes();
		void StartElection();
		void TimeoutHasPassedForLeaders();
		void RespondBackToLeader(bool response, int myTermNumber, int myCommitIndex);

		public void PauseNode();
		public void UnpauseNode();

    }
}