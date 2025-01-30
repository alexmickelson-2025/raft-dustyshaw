using NSubstitute;
using Raft;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RaftTests
{
	public class LogTests
	{
		// Testing Logs #1
		[Fact]
		public void TestCase01_LeadersSendRPCToFollowersWhenRecieveAnEntry()
		{
			// when a leader receives a client command, the leader sends the
			// log entry in the next appendentries RPC to all nodes

			// Arrange
			Node n = new Node([], null);
			Entry l = new Entry("1", "set a");
			n.Entries = [l];

			var follower = Substitute.For<INode>();

			n.OtherNodes = [follower];

			// Act
			n.RecieveClientCommand(l.Key, l.Command);

			// Assert
			follower.Received(1).RecieveAppendEntriesRPC(new AppendEntriesRPC(n));
		}


		// Testing Logs #2
		[Fact]
		public void TestCase02_NodesRecieveCommands()
		{
			Node n = new Node([], null);
			Entry l = new Entry("1", "set a", 0);

			n.RecieveClientCommand(l.Key, l.Command);

			Assert.True(n.Entries.Count() > 0);
			Assert.Contains(l.Command, n.Entries.First().Command);
		}

		// Testing Logs #3
		[Fact]
		public void TestCase03_NodesStartWithNoLogs()
		{
			// Arrange and Act
			Node n = new([], null);

			// Assert
			Assert.True(n.Entries.Count() == 0);
		}

		// Testing Logs #4
		[Fact]
		public void TestCase04_LeaderWinsElectionInitializesNextIndex()
		{
			//4. when a leader wins an election,
			//it initializes the nextIndex for each follower to the index just after the last one it its log

			var f1 = new Node([], null);	// will NOT work if these are substitutes and idk know why
			f1.NodeId = Guid.NewGuid();
			var f2 = new Node([], null);
			f2.NodeId = Guid.NewGuid();

			Node n = new Node([f1, f2], null);
			n.Entries = new List<Entry>() { new Entry("1", "set a")};
			int nextIndexesCountBeore = n.NextIndexes.Count();


			// Act
			n.BecomeLeader();

			// Assert
			Assert.True(n.NextIndexes[f1.NodeId] == 1);
			Assert.True(n.NextIndexes[f2.NodeId] == 1);
		}

		// Testing Logs #5
		[Fact]
		public void TestCase05p1_LeadersInitializeNextIndexForOtherNodes()
		{
			// leaders maintain an "nextIndex" for each follower that is the index
			// of the next log entry the leader will send to that follower

			// Leaders initialize their nextIndex[] array to have each node

			// arrange
			Node f1 = new Node([], null);
			Node f2 = new Node([], null);

			Node n = new Node([f1, f2], null);
			n.BecomeLeader();

			// act
			var result1 = n.NextIndexes.ContainsKey(f1.NodeId);
			var result2 = n.NextIndexes.ContainsKey(f2.NodeId);

			//assert
			// Make sure that the nextindexes contains the node ids
			Assert.True(result1);
			Assert.True(result2);
		}

		// Testing Logs #5 (part 2)
		[Fact]
		public void TestCase05p2_LeadersInitializeNextIndexForOtherNodesOneGreaterThanLastLogIndex()
		{
			// Leaders initialize their nextIndex[] array to have the lastLogTerm + 1 of leader

			// arrange
			Node f1 = new Node([], null);

			Node leader = new Node([f1], null);
			leader.Entries = new List<Entry> { new Entry("1", "set a"), new Entry("1", "set b") };
			leader.BecomeLeader();

			// act
			var indexValue = leader.NextIndexes[f1.NodeId];

			//assert
			Assert.Equal(2, indexValue);
		}

		// Testing Logs #5 (part 3)
		[Fact]
		public void TestCase05p3_LeadersInitializeNextIndexWhenLeaderHasNoLogs()
		{
			// arrange
			Node f1 = new Node([], null);

			Node leader = new Node([f1], null);
			leader.Entries = new List<Entry>();	// no logs yet
			leader.BecomeLeader();

			// act
			var indexValue = leader.NextIndexes[f1.NodeId];

			//assert
			Assert.Equal(0, indexValue);
		}

		// Testing Logs #6
		[Fact]
		public void TestCase06_CommittedIndexIsIncludedInAppendEntriesRPC()
		{
			// 6. Highest committed index from the leader is included in AppendEntries RPC's

			// Arrange
			var leader = new Node([], null);
			//leader.BecomeLeader();
			leader.TermNumber = 0;

			var follower = Substitute.For<INode>();
			leader.OtherNodes = [follower];

			int termBefore = leader.TermNumber;


			// Act
			leader.CommitIndex = 100;
			leader.SendAppendEntriesRPC();


			// assert
			// The follower should have recieved the leaders commit index (along with its id)
			AppendEntriesRPC rpc = new();
			follower.Received(1).RecieveAppendEntriesRPC(new AppendEntriesRPC(leader));
		}

		// Testing Logs #7
		[Fact]
		public async Task TestCase07_FollowersCommitEntriesToLocalStateMachine()
		{
			// 7. When a follower learns that a log entry is committed,
			// it applies the entry to its local state machine

			var f = new Node([], null);
			// follower has recieved 1 and 2, but hasn't committed 2 yet
			f.Entries = new List<Entry>() { new Entry("1", "set a"), new Entry("2", "set b") };
			f.CommitIndex = 0;

			// act
			// leader has committed to index 1
			int leadersCommitIndex = 1;
			List<Entry> leadersEntries = new List<Entry>();
			AppendEntriesRPC rpc = new();
			rpc.entries = leadersEntries;
			rpc.leaderCommit = leadersCommitIndex;
			await f.RecieveAppendEntriesRPC(rpc);
			Thread.Sleep(50);

			// assert
			Assert.Equal(2, f.StateMachine.Count);
			Assert.Equal("set b", f.StateMachine.Last().Command);
			Assert.Equal("2", f.StateMachine.Last().Key);
		}

		// Testing Logs #8
		[Fact]
		public void TestCase08_LeadersCommitEntriesWithMajorityConfirmation()
		{
			//  8. when the leader has received a majority confirmation of a log, it commits it
			var f1 = Substitute.For<INode>();
			var f2 = Substitute.For<INode>();

			Node leader = new Node([f1, f2], null);
			// leader has recieved
			leader.RecieveClientCommand("1", "2");

			// act
			leader.RespondBackToLeader(true, 1, 1, f1.NodeId);
			leader.RespondBackToLeader(true, 1, 1, f2.NodeId);

			// assert
			// make sure the leader adds the log to the state machine
			Assert.True(leader.StateMachine.Count() > 0);
		}

		// Testing Logs #9
		[Fact]
		public void TestCase09_LeadersCommitEntriesByIncreasingTheirIndex()
		{
			//  the leader commits logs by incrementing its committed log index

			// arrange
			Node leader = new Node([], null);
			int indexBefore = leader.CommitIndex;
			leader.Entries = new List<Entry> { new Entry("A", "B") };

			// act
			leader.CommitEntry();

			// assert
			Assert.True(leader.CommitIndex - 1 == indexBefore);
		}

		// Testing Logs #10
		[Fact]
		public async Task TestCase10_FollowersAddOneEntryToTheirLog()
		{
			// 10. given a follower receives an appendentries with log(s) it will add those entries to its personal log

			// arrange
			var f = new Node([], null);
			List<Entry> entries = new List<Entry>();
			Entry e = new Entry("1", "set a");
			entries.Add(e);

			// act
			AppendEntriesRPC rpc = new();
			rpc.entries = entries;
			await f.RecieveAppendEntriesRPC(rpc);

			//assert
			Assert.True(f.Entries.Count() > 0);
			Assert.Contains(e, entries);
		}

		// Testing Logs #10
		[Fact]
		public async Task TestCase10_FollowersAddMultipleEntriesToTheirLogInOrder()
		{
			// I want to make sure that the logs are appended in the order the follower recieved them.

			// arrange
			var f = new Node([], null);
			f.Entries = new List<Entry> { new Entry("1", "set a", 1) };

			List<Entry> entriesFromLeader = new List<Entry>();
			Entry e1 = new("1", "set a", 1);
			Entry e2 = new("1", "set b", 2);
			Entry e3 = new("1", "set c", 2);

			entriesFromLeader.Add(e1);
			entriesFromLeader.Add(e2);
			entriesFromLeader.Add(e3);

			// act
			// let's say the leader says to send a, b, and c...
			AppendEntriesRPC rpc = new();
			rpc.entries = entriesFromLeader;
			await f.RecieveAppendEntriesRPC(rpc);

			//assert
			// Check the order
			var entriesList = f.Entries.ToList();
			Assert.Equal(entriesList.Last(), e3);  // Ensure e2 is the last one in the list
			Assert.Equal(entriesList[entriesList.Count - 2], e2);  // Ensure e is the one before the last one
			Assert.Equal(entriesList[entriesList.Count - 3].TermReceived, e1.TermReceived);  // Ensure e is the one before the last one
			Assert.Equal(entriesList[entriesList.Count - 3].Command, e1.Command);  // Ensure e is the one before the last one

			Assert.Equal(3, entriesList.Count());
		}


		// Testing Logs #11
		[Fact]
		public async Task TestCase11_FollowersSendAResponseToLeaders()
		{
			//  a followers response to an appendentries includes the followers term number and log entry index

			// arrange
			var l = Substitute.For<INode>();
			l.Entries = new List<Entry>();

			Node  f = new([], null);
			f.OtherNodes = [l];

			// act
			AppendEntriesRPC rpc = new(l);
			await f.RecieveAppendEntriesRPC(rpc);

			// assert
			l.Received(1).RespondBackToLeader(Arg.Any<bool>(), f.TermNumber, f.CommitIndex, f.NodeId);
		}

		// Testing Logs #12
		[Fact]
		public void TestCase12_LeadersSendCLIENTConfirmation()
		{
			// 12. when a leader receives a majority responses from the clients after a log replication heartbeat,
			// the leader sends a confirmation

			var Client = Substitute.For<IClient>();

			var leader = new Node([], null);
			leader.RecieveClientCommand("A", "B");
			var leadersEntry = leader.Entries.First();
			leader.Client = Client;

			// Act
			leader.CommitEntry();

			// Assert
			// followers recieve an empty heartbeat with the new commit index
			List<Entry> emptyList = new List<Entry>();
			Client.Received(1).RecieveLogFromLeader(leadersEntry);
		}

		// Testing Logs #13
		[Fact]
		public void TestCase13_CommittingALogIncrementsCommitIndex()
		{
			Node l = new([], null);
			l.Entries = new List<Entry>() { new Entry("A", "B") };
			int indexBefore = l.CommitIndex;

			l.CommitEntry();

			Assert.Equal(l.CommitIndex - 1, indexBefore);	
		}

		// Testing Logs #14
		[Fact]
		public async Task TestCase14_FollwerIncreasesCommitIndexFromHeartbeats()
		{
			// 14.when a follower receives a heartbeat,
			// it increases its commitIndex to match the commit index of the heartbeat

			// arrange
			Node l = new([], null);
			l.CommitIndex = 100;
			Node f = new([], null);
			l.OtherNodes = [f];

			l.OtherNodes = [f];

			// act
			// follower recieves an empty heartbeat
			AppendEntriesRPC rpc = new AppendEntriesRPC();
			rpc.term = l.TermNumber;
			rpc.leaderId = l.NodeId;
			rpc.leaderCommit = l.CommitIndex;
			await f.RecieveAppendEntriesRPC(rpc);

			// assert
			Assert.True(f.CommitIndex == 100);	
		}

		// Testing Logs #15
		[Fact]
		public async Task TestCase15_LeadersIncludeTheirIndexAndPrevLogIndexWithNoEntries()
		{
			//  15. When sending an AppendEntries RPC,
			//  the leader includes the index
			//  and term of the entry in its log that immediately precedes the new entries
			var f1 = Substitute.For<INode>();


			var l = new Node([f1], null);
			// no logs 

			// act
			l.SendAppendEntriesRPC();

			// assert
			int term = 0;
			int prevLogIndex = -1;
			int commitIndex = -1;
			AppendEntriesRPC rpc = new(term, l.NodeId, prevLogIndex, new List<Entry>(), commitIndex);
			await f1.Received(1).RecieveAppendEntriesRPC(Arg.Is<AppendEntriesRPC>(actual => actual.term == rpc.term && actual.prevLogIndex == rpc.prevLogIndex && actual.leaderCommit == rpc.leaderCommit));
		}

		// Testing Logs #15
		[Fact]
		public async Task TestCase15_LeadersIncludeTheirIndexAndPrevLogIndexWithOneUncommittedEntry()
		{
			//  Same as test above, but including one uncommitted entry.

			// arrange
			var f1 = Substitute.For<INode>();
			f1.NodeId = Guid.NewGuid();

			var l = new Node([f1], null);
			var entries = new List<Entry>() { new Entry("a", "b", 0, 0) };
			l.Entries = entries;
			l.NextIndexes[f1.NodeId] = -1;  // and the leader needs to send a log to this behind follower
			l.ElectionTimeout = 100000; // this is just so it doesn't accidentally timeout

			// act
			l.SendAppendEntriesRPC();

			// assert
			int term = 0;
			int prevLogIndex = 0;	// prev log index is now the first one
			int commitIndex = -1;
			AppendEntriesRPC rpc = new(term, l.NodeId, prevLogIndex, entries, commitIndex);
			await f1.Received(1).RecieveAppendEntriesRPC(Arg.Is<AppendEntriesRPC>(actual => actual.term == rpc.term 
				&& actual.prevLogIndex == rpc.prevLogIndex 
				&& actual.leaderCommit == rpc.leaderCommit 
				&& actual.entries.Contains(entries.First())) );
		}

		// Testing Logs #15
		[Fact]
		public async Task TestCase15_NodesRejectRequestsIfTermsDiffer()
		{
			// If the follower does not find an entry in its log with the same index and term,
			// then it refuses the new entries.

			// arrange
			var f1 = new Node([], null);
			f1.Entries = new List<Entry> { new Entry("1", "set a", 1) };

			var l = Substitute.For<INode>();
			List<Entry> leadersEntries = new List<Entry> { new Entry("1", "set a", 2), new Entry("1", "set b", 2) };	// same command, but different term

			l.Entries = leadersEntries;
			f1.OtherNodes = [l];

			// act
			AppendEntriesRPC rpc = new(l);
			rpc.term = l.TermNumber;
			await f1.RecieveAppendEntriesRPC(rpc);

			// assert
			// Because the term the leader is trying to send 
			l.Received(1).RespondBackToLeader(false, f1.TermNumber, f1.CommitIndex, f1.NodeId);
		}

		[Fact]
		public void TestCase15_LeadersDecrimentNextIndexForThatFollowerAfterFalseResponse()
		{
			// if a follower rejects the AppendEntries RPC, the leader decrements nextIndex and retries the AppendEntries RPC
			Guid followersId = Guid.NewGuid();

			var leader = new Node([], null);
			leader.NextIndexes[followersId] = 1;

			// act
			leader.RespondBackToLeader(false, 1, 1, followersId);

			// assert
			leader.NextIndexes[followersId] = 0;
		}

		// Testing Logs #15
		[Fact]
		public void TestCase15_FollowersRecieveALog()
		{
			// Followers recieve a log successfully

			// arrange
			var f1 = Substitute.For<INode>();
			f1.Entries = new List<Entry> { new Entry("1", "set a", 1) };

			var l = new Node([], null);
			l.Entries = new List<Entry> { new Entry("1", "set a", 1) };  // same command, but different term
			f1.OtherNodes = [l];
			l.OtherNodes = [f1];
			l.BecomeLeader();


			l.RecieveClientCommand("1", "set b");
			List<Entry> logsToSend = l.CalculateEntriesToSend(f1.NodeId); // Should be the last one ("send b") one


			// act
			l.SendAppendEntriesRPC();

			// assert
			Assert.True(f1.Entries.Count == 1);
		}

		// Testing Logs #16
		[Fact]
		public void Testing16_NonMajorityConfirmationsDontGetCommitted()
		{
			// 16. when a leader sends a heartbeat with a log,
			// but does not receive responses from a majority of nodes,
			// the entry is uncommitted


			var f1 = Substitute.For<INode>();
			var f2 = Substitute.For<INode>();

			var leader = new Node([], null);
			leader.LogConfirmationsRecieved = new List<bool> { true };
			leader.OtherNodes = [f1, f2];
			int commitIndexBefore = leader.CommitIndex;

			// Act
			leader.RespondBackToLeader(false, 1, 1, f1.NodeId);

			// Assert
			// followers recieve an empty heartbeat with the new commit index
			Assert.True(commitIndexBefore == leader.CommitIndex);

		}

		// Testing Logs #17
		[Fact]
		public void Testing17_NoResponseFromFollowersLeaderContinuesToSendLogEntries()
		{
			// 17. if a leader does not response from a follower,
			// the leader continues to send the log entries in subsequent heartbeats
			
			// Substituted nodes won't actually send any methods in response, so its a good simulation for a "dead node"
			var deadFollower = Substitute.For<INode>();

			Node leaderNode = new Node([deadFollower], null);
			leaderNode.Entries = new List<Entry>() { new Entry("A", "B") };
			leaderNode.BecomeLeader();

			// act
			Thread.Sleep(200);

			// assert
			AppendEntriesRPC rpc = new AppendEntriesRPC(leaderNode);
			
			deadFollower.Received(4).RecieveAppendEntriesRPC(rpc);
		}

		// Testing Logs #18
		[Fact]
		public void TestCase18_IfLeadersDontCommitEntryThenTheyDontSendResponseToClient()
		{
			// 18. if a leader cannot commit an entry, it does not send a response to the client
			var Client = Substitute.For<IClient>();

			var leader = new Node([], null);
			leader.RecieveClientCommand("A", "B");
			var leadersEntry = leader.Entries.First();
			leader.Client = Client;

			// Act
			// leader can't commit entry if followers respond false
			leader.RespondBackToLeader(false, 0, 0, Guid.NewGuid());

			// Assert
			// followers recieve an empty heartbeat with the new commit index
			List<Entry> emptyList = new List<Entry>();
			Client.Received(0).RecieveLogFromLeader(leadersEntry);
		}

		// Testing Logs #19
		[Fact]
		public async Task TestCase19_LogsRejectAppendEntriesIfEntriesAreTooFarInFuture()
		{
			//19. if a node receives an appendentries
			// with a logs that are too far in the future from your local state,
			// you should reject the appendentries

			var f1 = new Node([], null);

			var leader = Substitute.For<INode>();
			leader.Entries = new List<Entry>() { new Entry("A", "B"), new Entry("C", "D") };

			f1.OtherNodes = [leader];

			// act
			AppendEntriesRPC rpc = new(leader);
			rpc.entries = new List<Entry>() { leader.Entries.Last() };
			await f1.RecieveAppendEntriesRPC(rpc);

			// assert
			leader.Received(1).RespondBackToLeader(false, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid>());
		}

		// Testing Logs #19 
		[Fact]
		public async Task TestCase19_NodesRejectFutureTerms()
		{
			// 19. if a node receives an appendentries with a logs that are too far in the future from your local state,
			// you should reject the appendentries

			// arrange
			var f1 = new Node([], null);
			f1.Entries = new List<Entry> { new Entry("1", "set a") };

			var l = Substitute.For<INode>();
			List<Entry> leadersEntries = new List<Entry> { new Entry("1", "set a"), new Entry("1", "set b"), new Entry("1", "set c") };
			l.Entries = leadersEntries;
			f1.OtherNodes = [l];

			// act
			AppendEntriesRPC rpc = new(l);
			rpc.term = 1;
			await f1.RecieveAppendEntriesRPC(rpc);

			// assert
			// Because f prevLogIndex is at 1, and l prevLogIndex is at 3, then 3 - 1 > 1, so we reject
			l.Received(1).RespondBackToLeader(false, f1.TermNumber, f1.CommitIndex, f1.NodeId);
		}

		// Testing Logs #19 - Same thing just opposite. Making sure they respond back with true
		[Fact]
		public async Task TestCase19_OppositeOf19()
		{
			//19. if a node receives an appendentries
			// with a logs that are NOT too far in the future from your local state,
			// you should ACCEPT the appendentries

			var f1 = new Node([], null);
			f1.TermNumber = 1;

			var leader = Substitute.For<INode>();
			leader.Entries = new List<Entry>() { new Entry("A", "B") }; // One log ahead is ok.
			leader.TermNumber = 1;
			
			f1.OtherNodes = [leader];
			f1.State = Node.NodeState.Follower;

			// act
			AppendEntriesRPC rpc = new(leader);
			rpc.entries = new List<Entry>() { leader.Entries.Last() };
			await f1.RecieveAppendEntriesRPC(rpc);

			// assert
			leader.Received(1).RespondBackToLeader(true, Arg.Any<int>(), Arg.Any<int>(), f1.NodeId);
		}


		// Testing Logs #20
		[Fact]
		public async Task TestCase20_NonMatchingTermsAndIndexGetRejected()
		{
			// 20. if a node receives and appendentries with a term and index that do not match,
			// you will reject the appendentry until you find a matching log

			var leader = Substitute.For<INode>();
			List<Entry> leadersLogs = new List<Entry>() { new Entry("set", "1", 1, 0), new Entry("set", "1", 2, 1), new Entry("set", "3", 2, 2) };
			leader.Entries = leadersLogs;
			leader.BecomeLeader();
			leader.TermNumber = 100;


			var f1 = new Node([], null);        // matching log				// this node has incorrect term
			f1.Entries = new List<Entry>() { new Entry("set", "1", 1, 0), new Entry("set", "incorrect", 1, 1) };
			f1.OtherNodes = [leader];
			f1.TermNumber = 2;
			f1.State = Node.NodeState.Follower;

			// act
			AppendEntriesRPC rpc = new(leader);
			rpc.entries = leadersLogs.TakeLast(2).ToList();
			var logsToSend = leadersLogs.TakeLast(2).ToList(); // let's say the leader is sending the last 2. The 
			await f1.RecieveAppendEntriesRPC(rpc);

			// assert
			// leader should say 
			leader.Received(1).RespondBackToLeader(false, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<Guid>());
		}


		// Testing logs #20 but using bad testing practices because I am evil >:)
		//[Fact]
		//public async Task TestCase20Evil_NonMatchingTermsAndIndexGetRejectedBadTestPractices()
		//{
		//	var leader = new Node([], null);
		//	leader.Entries = new List<Entry>() { new Entry("set", "1", 1), new Entry("set", "1", 2), new Entry("set", "3", 2) };
		//	leader.BecomeLeader();
		//	leader.TermNumber = 2;

		//	var f1 = new Node([], null);        // matching log				// this node has incorrect term
		//	f1.Entries = new List<Entry>() { new Entry("set", "1", 1), new Entry("set", "incorrect", 1) };
		//	f1.OtherNodes = [leader];
		//	f1.TermNumber = 2;
		//	f1.State = Node.NodeState.Follower;

		//	leader.OtherNodes = [f1];

		//	// act
		//	leader.SendAppendEntriesRPC();



		//	// assert
		//	// as a follower with no entries yet, sending in two should be rejected?
		//	//leader.Received(1).RespondBackToLeader(false, Arg.Any<int>(), Arg.Any<int>());
		//}

	}
}
