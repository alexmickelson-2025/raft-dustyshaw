using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NSubstitute;
using Raft;
using System.Xml.Serialization;
using Xunit;

namespace RaftTests
{
    public class ElectionTests
    {
        // Testing #1
        //[Fact]
        //public async Task TestCase1_ActiveLeadersSendHeartbeatsWithin50ms()
        //{
        //    // Arrange
        //    Node leaderNode = new Node([], null);
        //    leaderNode.BecomeLeader();

        //    var followerNode = Substitute.For<INode>();
        //    leaderNode.OtherNodes = [followerNode];

        //    // Act
        //    var atLeastTwoCyclesTime = 120;
        //    //Thread.Sleep(atLeastTwoCyclesTime);
        //    await Task.Delay(atLeastTwoCyclesTime); 

        //    // Assert
        //    await followerNode.Received(2).RecieveAppendEntriesRPC(leaderNode.NodeId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<List<Entry>>());
        //}

        // Testing #2
        [Fact]
        public void TestCase2_LeadersAreRememberedByFollowers()
        {
            // Arrange
            Node leaderNode = new Node([], null);
            leaderNode.BecomeLeader();

            var followerNode = new Node([leaderNode], null);
            leaderNode.OtherNodes = [followerNode];

            // Act
            leaderNode.SendAppendEntriesRPC();

            // Assert
            Assert.Equal(followerNode.LeaderId, leaderNode.NodeId);
        }

        // Testing #3
        [Fact]
        public void TestCase3_NodesStartAsFollowers()
        {
            // Arrange
            Node newNode = new Node([], null);

            // Act
            var currentNodeState = newNode.State;

            // Assert
            Assert.Equal(Node.NodeState.Follower, currentNodeState);
        }

        // Testing #4
        [Fact]
        public void TestCase4_IgnoredFollowersStartElectionAfter300ms()
        {
            // Arrange
            var followerNode = new Node([], null);

            // Act
            var BiggestElectionTimoutTime = 600;
            Thread.Sleep(BiggestElectionTimoutTime);

            // Assert
            Assert.Equal(Node.NodeState.Candidate, followerNode.State);
        }

        // Testing #5 (part 1)
        [Fact]
        public void TestCase5_ElectionTimesAreBetween150And300()
        {
            // Arrange
            int n = 10;
            List<int> electionTimeouts = new();

            // Act
            for (int i = 0; i < n; i++)
            {
                var node = new Node([], null);
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
                var node = new Node([], null);
                electionTimeouts.Add(node.ElectionTimeout);
            }

            var numberOfRepeats = n - electionTimeouts.Distinct().Count();

            // Assert
            Assert.True(numberOfRepeats <= threshold);
        }

        // Testing #6
        [Fact]
        public void TestCase6_NewElectionBeginsAndTermIsGreater()
        {
            // Arrange
            Node n = new Node([], null);

            // Act
            Thread.Sleep(600);

            // Assert
            Assert.True(n.TermNumber > 0);
        }

        // Testing #7
        [Fact]
        public async Task TestCase7_WhenLeadersSendMessagesToMeThenIStayFollower()
        {
            // Arrange
            var followerNode = new Node([], null);
            followerNode.State = Node.NodeState.Follower;

            var followerElectionTimeBefore = followerNode.ElectionTimeout;
            // Act
            // Leader sends messages to me
            await followerNode.RecieveAppendEntriesRPC(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<int>(), new List<Entry>(), Arg.Any<int>());
            Thread.Sleep(100);

            // Assert
            Assert.NotEqual(followerElectionTimeBefore, followerNode.ElectionTimeout);
            Assert.Equal(Node.NodeState.Follower, followerNode.State);
        }

        // Testing #8
        [Fact]
        public void TestCase8_MajorityVotesWins()
        {
            // 8. Given an election begins, when the candidate gets a majority of votes,
            // it becomes a leader.

            // Arrange
            var followerNode = Substitute.For<INode>();
            followerNode.NodeId = Guid.NewGuid();
			var followerNode2 = Substitute.For<INode>();
            followerNode2.NodeId = Guid.NewGuid();

			Node candidateNode = new([], null);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.OtherNodes = [followerNode, followerNode2];
			followerNode.OtherNodes = [candidateNode, followerNode2];
			followerNode2.OtherNodes = [candidateNode, followerNode];

			// Act
			candidateNode.RecieveVoteResults(true, 100);
            candidateNode.RecieveVoteResults(true, 100);

            Thread.Sleep(300);

            // Assert
			Assert.Equal(Node.NodeState.Leader, candidateNode.State);   
        }

		// Testing #8
		[Fact]
		public void _MajorityVotesWinsTest()
		{
			// 8. Given an election begins, when the candidate gets a majority of votes,
			// it becomes a leader.

			// Arrange
			Node followerNode = new Node([], null);
			Node followerNode2 = new Node([], null);

			Node candidateNode = new([], null);
            candidateNode.TermNumber = 100;
			candidateNode.State = Node.NodeState.Candidate;

			candidateNode.OtherNodes = [followerNode, followerNode2];
            followerNode.OtherNodes = [candidateNode, followerNode2];
            followerNode2.OtherNodes = [candidateNode, followerNode];

            // Act
            candidateNode.SendVoteRequestRPCsToOtherNodes();
            Thread.Sleep(300);

			// Assert
			Assert.Equal(Node.NodeState.Leader, candidateNode.State);
            Assert.Contains(true, candidateNode.votesRecieved);
		}

		// Testing #9
		[Fact]
        public void TestCase9_MajorityVotesEvenWithUnresponsiveStillBecomeLeader()
        {
            Node followerNode1 = new([], null);
            Node followerNode2 = new([], null);
            Node followerNode3 = new([], null);
            Node followerNode4 = new([], null);

            Node candidateNode = new([], null);
            candidateNode.TermNumber = 100;
            candidateNode.ElectionTimeout = 9999999;
            candidateNode.State = Node.NodeState.Candidate;
            
            // 5 node system...
            candidateNode.OtherNodes = [followerNode1, followerNode2, followerNode3, followerNode4];

            // Act
            // Say in a 5 system I recieve only 3 votes, and have one server unresponsive
            candidateNode.votesRecieved = [true, true, true];
            candidateNode.DetermineElectionResults();

            // Assert
            Assert.Equal(Node.NodeState.Leader, candidateNode.State);
        }

        // Testing #10
  //      [Fact]
  //      public async Task TestCase10_FollowersRespondeYesToVotes()
  //      {
  //          // A follower that has not voted and is in an earlier term responds to a RequestForVoteRPC with "yes".

  //          // Arrange
  //          var follower = new Node([], null);
  //          follower.VoteForId = Guid.Empty; // given a follower has not voted
  //          follower.TermNumber = 0; // and is in an earlier term

  //          var candidateNode = Substitute.For<INode>();
  //          candidateNode.OtherNodes = [follower];
  //          candidateNode.State = Node.NodeState.Candidate;
  //          candidateNode.TermNumber = 100;

		//	// Act
  //          await follower.RecieveAVoteRequestFromCandidate(candidateNode.NodeId, candidateNode.TermNumber);

  //          // Assert
  //          Assert.Equal(follower.VoteForId, candidateNode.NodeId); // Node recorded that they have voted for candidate
  //          Assert.Equal(follower.VotedForTermNumber, candidateNode.TermNumber);
		//}

        // Testing #11
        [Fact]
        public void TestCase11_NewCandidateNodesVoteForThemselves()
        {
            // Arrange
            Node n = new Node([], null);
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
            Node node1 = new Node([], null);
            node1.TermNumber = 100;

            Node candidateNode = new Node([node1], null);
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
            Node node1 = new Node([], null);
            node1.TermNumber = 1;

            Node candidateNode = new Node([node1], null);
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
            Node node1 = new Node([], null);
            node1.TermNumber = 100; 

            Node candidateNode = new Node([node1], null);
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
		public async Task TestCase14_SecondVoteRequestInSameTermRespondNo()
		{
			// 14. If a node receives a second request for a vote for the same term, it should respond "no". 

			// Arrange
			Node node = new([], null);

			var c1 = Substitute.For<INode>();
            c1.TermNumber = 100;
            c1.NodeId = Guid.NewGuid();
			var c2 = Substitute.For<INode>();
            c2.TermNumber = 100;
            c2.NodeId = Guid.NewGuid();

            node.OtherNodes = [c1, c2];
            c1.OtherNodes = [node, c2];
            c2.OtherNodes = [node, c1];

            // Act
            await node.RecieveAVoteRequestFromCandidate(c1.NodeId, c1.TermNumber);
            await node.RecieveAVoteRequestFromCandidate(c2.NodeId, c2.TermNumber);

			// Assert
			c1.Received(1).RecieveVoteResults(true, Arg.Any<int>());
			c2.Received(1).RecieveVoteResults(false, Arg.Any<int>());
		}

		// Testing #15
		[Fact]
        public void TestCase15_SecondVoteRequestFromNodeInFutureTermVotesYes()
        {
            // 15.If a node receives a second request for a vote for a future term, it should vote for that node.
            // Arrange
            Node node = new Node([], null);
            node.TermNumber = 1;

            Node candidateNode = new Node([node], null);
            candidateNode.State = Node.NodeState.Candidate;
            candidateNode.TermNumber = 50;

            Node candidateNode2 = new Node( [node, candidateNode], null);
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
        [Fact]
        public void TestCase16_ElectionTimersRestartDuringElection()
        {
            // Arrange
            Node n = new Node([], null);
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
        [Fact]
        public void TestCase17_FollowersSendResponses()
        {
            // Arrange
            var followerNode = Substitute.For<INode>();
            followerNode.State = Node.NodeState.Follower;

            var leaderNode = new Node([], null);
            leaderNode.BecomeLeader();

            leaderNode.OtherNodes = [followerNode];
            followerNode.OtherNodes = [leaderNode];

            // Act
            leaderNode.SendAppendEntriesRPC(); // Send heartbeat

            // Assert
            followerNode.Received(1).RecieveAppendEntriesRPC(Arg.Any<int>(), leaderNode.NodeId, Arg.Any<int>(), Arg.Any<List<Entry>>(), Arg.Any<int>());
        }

        // Testing #18
        [Fact]
        public async Task TestCase18_AppendEntriesFromPreviousTermsAreRejected()
        {
			// Given a candidate receives an AppendEntries from a previous term, then it rejects.

			// Arrange
			var leader = Substitute.For<INode>();
			leader.TermNumber = 2;

			Node candidateNode = new([], null);
            candidateNode.TermNumber = 100;

            candidateNode.OtherNodes = [leader];
            leader.OtherNodes = [candidateNode];

			// Act
			await candidateNode.RecieveAppendEntriesRPC(leader.TermNumber, leader.NodeId, 2, new List<Entry>(), leader.CommitIndex);

			// Assert
			leader.Received(1).RespondBackToLeader(Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>());
			leader.Received(1).RespondBackToLeader(false, 100, Arg.Any<int>());

		}

		// Testing #19
		[Fact]
        public void TestCase19_NewLeadersSendRPC()
        {
            // 19. When a candidate wins an election, it immediately sends a heartbeat.
            var leaderNode = new Node([], null);
            var followerNode = Substitute.For<INode>();
            leaderNode.OtherNodes = [followerNode];

            // Act
            leaderNode.BecomeLeader();

            // Assert
            followerNode.Received(1).RecieveAppendEntriesRPC(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<List<Entry>>(), Arg.Any<int>());
        }


    }
}