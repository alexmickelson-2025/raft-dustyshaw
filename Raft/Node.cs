using System.Timers;
using Xunit.Sdk;

namespace Raft
{
    public partial class Node : INode
    {
        public Guid NodeId { get; set; } = Guid.NewGuid();
        public Guid VoteForId { get; set; }
        public int VotedForTermNumber { get; set; }
        public Guid LeaderId { get; set; }

        public int ElectionTimeout { get; set; } // in ms
        public System.Timers.Timer aTimer { get; set; }
        public int HeartbeatTimeout { get; } = 50; // in ms

        public int TermNumber { get; set; } = 0;
        public bool Vote { get; set; }
        public INode[] OtherNodes { get; set; }
        public List<bool> votesRecieved { get; set; } = new();

        public NodeState State { get; set; } = NodeState.Follower; // nodes start as followers

        public Node(bool Vote, Node[] OtherNodes)
        {
            this.ElectionTimeout = Random.Shared.Next(150, 300);
            aTimer = new System.Timers.Timer(ElectionTimeout);
            aTimer.Elapsed += (s, e) => { TimeoutHasPassed(); };
            aTimer.AutoReset = false;
            aTimer.Start();

            this.Vote = Vote;
            this.OtherNodes = OtherNodes;
        }

        public void BecomeLeader()
        {
            aTimer.Stop();  
            this.State = Node.NodeState.Leader;
            aTimer = new System.Timers.Timer(HeartbeatTimeout);
            aTimer.Elapsed += (s, e) => { TimeoutHasPassedForLeaders(); };
            aTimer.AutoReset = false;
            aTimer.Start();
        }

        public void TimeoutHasPassed()
        {
            StartElection();
        }

        public void TimeoutHasPassedForLeaders()
        {
            SendAppendEntriesRPC();
            aTimer.Start();
        }

        public void SendAppendEntriesRPC()
        {
            // As the leader, I need to send an RPC to other nodes
            foreach (var node in OtherNodes)
            {
                node.RespondToAppendEntriesRPC(this.NodeId);
            }
        }

        public void RespondToAppendEntriesRPC(Guid leaderId)
        {
            // As a follower, I have heard from the leader
            this.ElectionTimeout = Random.Shared.Next(150, 300);
            this.LeaderId = leaderId;
        }

        public void SendVoteRequestRPCsToOtherNodes()
        {
            // as the candidate, I am asking for votes from other nodes
            foreach (var node in OtherNodes)
            {
                votesRecieved.Add(node.RecieveAVoteRequestFromCandidate(this.NodeId, this.TermNumber));
            }
        }

        public bool RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm)
        {
            // as a server, I recieve a vote request from a candidate
            if (lastLogTerm < this.TermNumber || lastLogTerm == this.VotedForTermNumber)
            {
                return false;
            }
            this.VoteForId = candidateId;   
            this.VotedForTermNumber = lastLogTerm;
            return true;
        }

        public void StartElection()
        {
            this.State = NodeState.Candidate;
            this.VoteForId = this.NodeId;
            this.TermNumber = this.TermNumber + 1;
            this.ElectionTimeout = Random.Shared.Next(150, 300);
            aTimer = new System.Timers.Timer(ElectionTimeout);
            aTimer.Start();
        }
    }
}
