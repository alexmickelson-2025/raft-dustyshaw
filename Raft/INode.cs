using Raft.DTOs;
using System.Timers;

namespace Raft
{
	public interface INode
	{
		Guid NodeId { get; set; }


		Task RecieveAVoteRequestFromCandidate(VoteRequestFromCandidateRpc rpc);
		Task RecieveVoteResults(VoteFromFollowerRpc vote);
		Task SendMyVoteToCandidate(VoteRpc vote);
		Task RecieveAppendEntriesRPC(AppendEntriesRPC rpc);
		void RespondBackToLeader(bool response, int myTermNumber, int myCommitIndex, Guid fNodeId);
    }
}