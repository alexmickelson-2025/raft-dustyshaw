using System.Timers;

namespace Raft
{
	public interface INode
	{
		Guid NodeId { get; set; }


		Task RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm);
		void RecieveVoteResults(bool result, int termNumber);
		Task RecieveAppendEntriesRPC(AppendEntriesRPC rpc);
		public Task SendVoteRequestRPCsToOtherNodes();
		public void SendAppendEntriesRPC();
		Task SendMyVoteToCandidate(Guid candidateId, bool result);
		void RespondBackToLeader(bool response, int myTermNumber, int myCommitIndex, Guid fNodeId);

    }
}