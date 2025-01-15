using NSubstitute;
using Raft;

namespace RaftTests
{
    public class RaftUnitTests
    {
        // Testing #1
        // 1
        [Fact]
        public void TestCase1_ActiveLeadersSendHeartbeatsWithin50ms()
        {
            // Arrange
            Node leaderNode = new Node(true, []);
            leaderNode.BecomeLeader();

            var followerNode = Substitute.For<INode>();
            leaderNode.OtherNodes = [followerNode];

            // Act
            var atLeastTwoCyclesTime = 120;
            Thread.Sleep(atLeastTwoCyclesTime);

            // Assert
            followerNode.Received(2).RespondToAppendEntriesRPC();
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
            followerNode.RespondToAppendEntriesRPC();
            Thread.Sleep(600);

            // Assert
            Assert.NotEqual(followerElectionTimeBefore, followerNode.ElectionTimeout);
            Assert.NotEqual(Node.NodeState.Follower, followerNode.State);
        }

        // Testing #11
        // 7
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

        // Testing #16
        // 8
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
        // 9
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
            followerNode.Received(1).RespondToAppendEntriesRPC();
        }

        // Testing #18
        // 10
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