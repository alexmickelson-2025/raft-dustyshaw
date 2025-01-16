using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NSubstitute;
using Raft;
using Xunit;

namespace RaftTests
{
    public class RaftUnitTests
    {
        // Testing #1
        // 1
        [Fact]
        public async void TestCase1_ActiveLeadersSendHeartbeatsWithin50ms()
        {
            // Arrange
            Node leaderNode = new Node(true, []);
            leaderNode.BecomeLeader();

            var followerNode = Substitute.For<INode>();
            leaderNode.OtherNodes = [followerNode];

            // Act
            var atLeastTwoCyclesTime = 120;
            //Thread.Sleep(atLeastTwoCyclesTime);
            await Task.Delay(atLeastTwoCyclesTime); 

            // Assert
            followerNode.Received(2).RespondToAppendEntriesRPC(leaderNode.NodeId, Arg.Any<int>());
        }

        // Testing #2
        [Fact]
        public void TestCase2_LeadersAreRememberedByFollowers()
        {
            // Arrange
            Node leaderNode = new Node(true, []);
            leaderNode.BecomeLeader();

            var followerNode = new Node(true, [leaderNode]);
            leaderNode.OtherNodes = [followerNode];

            // Act
            leaderNode.SendAppendEntriesRPC();

            // Assert
            Assert.Equal(followerNode.LeaderId, leaderNode.NodeId);
        }

        // Testing #3
        // 2
        [Fact]
        public void TestCase3_NodesStartAsFollowers()
        {
            // Arrange
            Node newNode = new Node(true, []);

            // Act
            var currentNodeState = newNode.State;

            // Assert
            Assert.Equal(Node.NodeState.Follower, currentNodeState);
        }

        // Testing #4
        // 3
        [Fact]
        public void TestCase4_IgnoredFollowersStartElectionAfter300ms()
        {
            // Arrange
            var followerNode = new Node(true, []);

            // Act
            var BiggestElectionTimoutTime = 600;
            Thread.Sleep(BiggestElectionTimoutTime);

            // Assert
            Assert.Equal(Node.NodeState.Candidate, followerNode.State);
        }

        // Testing #5 (part 1)
        // 4
        [Fact]
        public void TestCase5_ElectionTimesAreBetween150And300()
        {
            // Arrange
            int n = 10;
            List<int> electionTimeouts = new();

            // Act
            for (int i = 0; i < n; i++)
            {
                var node = new Node(true, []);
                electionTimeouts.Add(node.ElectionTimeout);
            }

            // Assert
            electionTimeouts.ForEach(x => Assert.True(x >= 150));
            electionTimeouts.ForEach(x => Assert.True(x < 300));
        }

        // Testing #5 (part 2)
        [Fact]
        public void TestCase5_ElectionTimesAreRandom()
        {
            // Arrange
            int n = 10;
            int threshold = 3;
            List<int> electionTimeouts = new();

            // Act
            for (int i = 0; i < n; i++)
            {
                var node = new Node(true, []);
                electionTimeouts.Add(node.ElectionTimeout);
            }

            var numberOfRepeats = n - electionTimeouts.Distinct().Count();

            // Assert
            Assert.True(numberOfRepeats <= threshold);
        }

        // Testing #6
        // 5
        [Fact]
        public void TestCase6_NewElectionBeginsAndTermIsGreater()
        {
            // Arrange
            Node n = new Node(true, []);

            // Act
            Thread.Sleep(600);

            // Assert
            Assert.True(n.TermNumber > 0);
        }

        // Testing #7
        // 6
        [Fact]
        public void TestCase7_WhenLeadersSendMessagesToMeThenIStayFollower()
        {
            // Arrange
            var followerNode = new Node(true, []);
            followerNode.State = Node.NodeState.Follower;

            var followerElectionTimeBefore = followerNode.ElectionTimeout;

            // Act
            // Leader sends messages to me
            followerNode.RespondToAppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<int>());
            Thread.Sleep(100);

            // Assert
            Assert.NotEqual(followerElectionTimeBefore, followerNode.ElectionTimeout);
            Assert.Equal(Node.NodeState.Follower, followerNode.State);
        }

        // Testing #10
        // 7
        [Fact]
        public void TestCase10_FollowersRespondeYesToVotes()
        {
            // Arrange
            var node = new Node(true, []);
            node.VoteForId = Guid.Empty; // given a follower has not voted
            node.TermNumber = 0; // and is in an earlier term

            var candidateNode = new Node(true, [node]);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.TermNumber = 100;

            // Act
            candidateNode.SendVoteRequestRPCsToOtherNodes();

            // Assert
            Assert.Equal(node.VoteForId, candidateNode.NodeId); // Node recorded that they have voted for candidate
            Assert.Contains(true, candidateNode.votesRecieved); // Candidate has recieved a yes
        }

        // Testing #11
        // 8
        [Fact]
        public void TestCase11_NewCandidateNodesVoteForThemselves()
        {
            // Arrange
            Node n = new Node(true, []);
            Thread.Sleep(100);
            var thisNodesId = n.NodeId;

            // Act
            Thread.Sleep(375);

            // Assert
            Assert.Equal(n.VoteForId, thisNodesId); // It votes for itself
            Assert.Equal(Node.NodeState.Candidate, n.State);   // And it is a candidate now
        }

        // Testing #12
        [Fact]
        public void TestCase12_CandidatesBecomeFollowersWhenRecieveLaterTermRPC()
        {
            // 12.Given a candidate, when it receives an AppendEntries message from a node with a later term,
            // then the candidate loses and becomes a follower.

            // Arrange
            Node node1 = new Node(true, []);
            node1.TermNumber = 100;

            Node candidateNode = new Node(true, [node1]);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.TermNumber = 1;

            node1.OtherNodes = [candidateNode];

            // Act
            node1.SendAppendEntriesRPC();

            // Assert
            Assert.Equal(Node.NodeState.Follower, candidateNode.State); // Candidate reverts to follower
        }

        // Testing #12 (but opposite)
        [Fact]
        public void TestCase12_CandidatesStaysCandidateWhenRecieveLesserTermRPC()
        {
            // Arrange
            Node node1 = new Node(true, []);
            node1.TermNumber = 1;

            Node candidateNode = new Node(true, [node1]);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.TermNumber = 100;

            node1.OtherNodes = [candidateNode];

            // Act
            node1.SendAppendEntriesRPC();

            // Assert
            Assert.Equal(Node.NodeState.Candidate, candidateNode.State); // Candidate stays a candidate
        }

        // Testing #13
        [Fact]
        public void TestCase13_CandidatesBecomeFollowersWhenRecieveEqualTermRPC()
        {
            // Arrange
            Node node1 = new Node(true, []);
            node1.TermNumber = 100; 

            Node candidateNode = new Node(true, [node1]);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.TermNumber = 100;  // equal terms

            node1.OtherNodes = [candidateNode];

            // Act
            node1.SendAppendEntriesRPC();

            // Assert
            Assert.Equal(Node.NodeState.Follower, candidateNode.State); // Converts to Follower
        }


        // Testing #14
        [Fact]
        public void TestCase14_SecondVoteRequestInSameTermRespondNo()
        {
            // Arrange
            Node node = new Node(true, []);
            node.TermNumber = 1;

            Node candidateNode = new Node(true, [node]);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.TermNumber = 100;

            Node candidateNode2 = new Node(true, [node, candidateNode]);
            candidateNode2.State = Node.NodeState.Candidate;
            candidateNode2.TermNumber = 100;

            candidateNode.OtherNodes = [node, candidateNode2];

            // Act
            candidateNode.SendVoteRequestRPCsToOtherNodes();    // follower says yes in term 100
            candidateNode2.SendVoteRequestRPCsToOtherNodes();   // second vote request for term 100 is rejected

            // Assert
            Assert.Equal(node.VoteForId, candidateNode.NodeId);
            Assert.NotEqual(candidateNode2.NodeId, node.VoteForId);

            Assert.Equal(100, node.VotedForTermNumber);
            Assert.Contains(true, candidateNode.votesRecieved);
            Assert.Contains(false, candidateNode2.votesRecieved);
        }

        // Testing #15
        [Fact]
        public void TestCase15_SecondVoteRequestFromNodeInFutureTermVotesYes()
        {
            // 15.If a node receives a second request for a vote for a future term, it should vote for that node.
            // Arrange
            Node node = new Node(true, []);
            node.TermNumber = 1;

            Node candidateNode = new Node(true, [node]);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.TermNumber = 50;

            Node candidateNode2 = new Node(true, [node, candidateNode]);
            candidateNode2.State = Node.NodeState.Candidate;
            candidateNode2.TermNumber = 100;

            candidateNode.OtherNodes = [node, candidateNode2];

            // Act
            candidateNode.SendVoteRequestRPCsToOtherNodes();    // follower says yes
            candidateNode2.SendVoteRequestRPCsToOtherNodes();   // second vote request for greater term is accepted

            // Assert
            Assert.Equal(node.VoteForId, candidateNode2.NodeId);
            Assert.NotEqual(candidateNode.NodeId, node.VoteForId);

            Assert.Equal(100, node.VotedForTermNumber);
            Assert.Contains(true, candidateNode2.votesRecieved);
            Assert.Contains(false, candidateNode.votesRecieved);
        }

        // Testing #16
        // 9
        [Fact]
        public void TestCase16_ElectionTimersRestartDuringElection()
        {
            // Arrange
            Node n = new Node(true, []);
            n.State = Node.NodeState.Candidate;
            var termBefore = n.TermNumber;

            // Act
            Thread.Sleep(600); // When election timer runs out

            // Assert
            // In my eyes, these are indicators that a new election began
            Assert.True(termBefore < n.TermNumber);
            Assert.Equal(Node.NodeState.Candidate, n.State);
        }

        // Testing #17
        // 10
        [Fact]
        public void TestCase17_FollowersSendResponses()
        {
            // Arrange
            var followerNode = Substitute.For<INode>();
            followerNode.State = Node.NodeState.Follower;

            var leaderNode = new Node(true, []);
            leaderNode.BecomeLeader();

            leaderNode.OtherNodes = [followerNode];
            followerNode.OtherNodes = [leaderNode];

            // Act
            leaderNode.SendAppendEntriesRPC(); // Send heartbeat

            // Assert
            followerNode.Received(1).RespondToAppendEntriesRPC(leaderNode.NodeId, Arg.Any<int>());
        }

        // Testing #18
        // 11
        [Fact]
        public void TestCase18_AppendEntriesFromPreviousTermsAreRejected()
        {
            // Arrange
            var node = new Node(true, []);
            node.State = Node.NodeState.Follower;
            node.TermNumber = 2;

            // Act
            var result = node.RecieveAVoteRequestFromCandidate(Guid.NewGuid(), 1);

            // Assert
            Assert.False(result);
        }

    }
}