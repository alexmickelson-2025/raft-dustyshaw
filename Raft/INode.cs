using Raft.DTOs;
using System.Timers;

namespace Raft
{
	public interface INode
	{
		Guid NodeId { get; set; }


		Task RecieveAVoteRequestFromCandidate(VoteRequestFromCandidateRpc rpc);
		void RecieveVoteResults(bool result, int termNumber);
		Task SendMyVoteToCandidate(Guid candidateId, bool result);
		Task RecieveAppendEntriesRPC(AppendEntriesRPC rpc);
		void RespondBackToLeader(bool response, int myTermNumber, int myCommitIndex, Guid fNodeId);
    }
}