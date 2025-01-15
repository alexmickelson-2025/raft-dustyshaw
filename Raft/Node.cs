using System.Timers;

namespace Raft
{
    public class Node : INode
    {
        public enum NodeState
        {
            Follower,
            Candidate,
            Leader
        }
        public Guid NodeId { get; set; } = Guid.NewGuid();
        public Guid VoteForId { get; set; }

        public int ElectionTimeout { get; set; } // in ms
        public static System.Timers.Timer aTimer { get; set; }
        public int HeartbeatTimeout { get; } = 50; // in ms
        public int timeElapsedFromHearingFromLeader { get; set; } = 0; // in ms

        public int TermNumber { get; set; } = 0;
        public bool Vote { get; set; }
        public INode[] OtherNodes { get; set; }

        public NodeState State { get; set; } = NodeState.Follower; // nodes start as followers

        public Node(bool Vote, Node[] OtherNodes)
        {
            aTimer = new System.Timers.Timer(HeartbeatTimeout);
            aTimer.Elapsed += TimeoutHasPassed;
            aTimer.AutoReset = false;
            aTimer.Start();

            this.Vote = Vote;
            this.OtherNodes = OtherNodes;
            this.ElectionTimeout = Random.Shared.Next(150, 300);
        }

        public void TimeoutHasPassed(Object source, ElapsedEventArgs e)
        {
            timeElapsedFromHearingFromLeader += (int)aTimer.Interval;

            if (timeElapsedFromHearingFromLeader > ElectionTimeout)
            {
                StartElection();
            }

            SendAppendEntriesRPC();
            aTimer.Start();
        }

        public void SendAppendEntriesRPC()
        {
            // As the leader, I need to send an RPC to other nodes
            foreach (var node in OtherNodes)
            {
                node.RespondToAppendEntriesRPC();
            }
        }

        public void RespondToAppendEntriesRPC()
        {
            // As a follower, I have heard from the leader
            timeElapsedFromHearingFromLeader = 0;
        }

        public void AskForVotesFromOtherNodes()
        {
            // as the candidate, I am asking for votes from other nodes
            foreach (var node in OtherNodes)
            {
                node.RecieveAVoteRequestFromCandidate(this.NodeId, this.TermNumber);
            }
        }

        public bool RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm)
        {
            // as a server, I recieve a vote request from a candidate
            if (lastLogTerm < this.TermNumber)
            {
                return false;
            }
            return true;
        }

        public void StartElection()
        {
            this.State = NodeState.Candidate;
            this.VoteForId = this.NodeId;
            this.TermNumber = this.TermNumber + 1;
            this.ElectionTimeout = Random.Shared.Next(150, 300);
            aTimer = new System.Timers.Timer(ElectionTimeout);
        }

    }
}
