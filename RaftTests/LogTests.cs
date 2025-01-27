using NSubstitute;
using Raft;
using System;
using System.Collections.Generic;
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
			Node n = new Node([], null, null);
			Entry l = new Entry("set a");
			n.Entries = [l];

			var follower = Substitute.For<INode>();

			n.OtherNodes = [follower];

			// Act
			n.RecieveClientCommand(l.Command);

			// Assert
			follower.Received(1).RecieveAppendEntriesRPC(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<List<Entry>>(), Arg.Any<int>());
		}


		// Testing Logs #2
		[Fact]
		public void TestCase02_NodesRecieveCommands()
		{
			Node n = new Node([], null, null);
			Entry l = new Entry("set a");

			n.RecieveClientCommand(l.Command);

			Assert.True(n.Entries.Count() > 0);
			Assert.Contains(l.Command, n.Entries.First().Command);
			//Assert.StrictEqual([l], n.Entries);	// node should contain the log
		}

		// Testing Logs #3
		[Fact]
		public void TestCase03_NodesStartWithNoLogs()
		{
			// Arrange and Act
			Node n = new([], null, null);

			// Assert
			Assert.True(n.Entries.Count() == 0);
		}

		// Testing Logs #5
		[Fact]
		public void TestCase05_LeadersInitializeNextIndexForOtherNodes()
		{
			// leaders maintain an "nextIndex" for each follower that is the index
			// of the next log entry the leader will send to that follower

			// Leaders initialize their nextIndex[] array to have each node

			// arrange
			Node f1 = new Node([], null, null);
			Node f2 = new Node([], null, null);

			Node n = new Node([f1, f2], null, null);
			n.BecomeLeader();

			// act
			var result1 = n.NextIndexes.ContainsKey(f1.NodeId);
			var result2 = n.NextIndexes.ContainsKey(f2.NodeId);

			//assert
			Assert.True(result1);
			Assert.True(result2);
		}

		// Testing Logs #5 (part 2)
		[Fact]
		public void TestCase05_LeadersInitializeNextIndexForOtherNodesOneGreaterThanLastLogIndex()
		{
			// Leaders initialize their nextIndex[] array to have the lastLogTerm + 1 of leader

			// arrange
			Node f1 = new Node([], null, null);

			Node n = new Node([f1], null, null);
			n.Entries = new List<Entry> { new Entry("set a"), new Entry("set b") };
			n.BecomeLeader();

			// act
			var result = n.NextIndexes.ContainsValue(2);	

			//assert
			Assert.True(result);
		}

		// Testing Logs #6
		[Fact]
		public void TestCase06_CommittedIndexIsIncludedInAppendEntriesRPC()
		{
			// 6. Highest committed index from the leader is included in AppendEntries RPC's
			// Arrange
			var leader = new Node([], null, null);
			leader.BecomeLeader();
			leader.TermNumber = 0;

			var follower = Substitute.For<INode>();
			leader.OtherNodes = [follower];

			int termBefore = leader.TermNumber;


			// Act
			leader.CommitIndex = 100;
			leader.SendAppendEntriesRPC();

			// assert
			// The follower should have recieved the leaders commit index (along with its id)
			follower.Received(1).RecieveAppendEntriesRPC(Arg.Any<int>(), leader.NodeId, Arg.Any<int>(), Arg.Any<List<Entry>>(), leader.CommitIndex);
		}

		// Testing Logs #9
		[Fact]
		public void TestCase09_LeadersCommitEntriesByIncreasingTheirIndex()
		{
			//  the leader commits logs by incrementing its committed log index

			// arrange
			Node leader = new Node([], null, null);
			int indexBefore = leader.CommitIndex;

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
			var f = new Node([], null, null);
			List<Entry> entries = new List<Entry>();
			Entry e = new Entry("set a");
			entries.Add(e);

			// act
			await f.RecieveAppendEntriesRPC(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<int>(), entries, Arg.Any<int>());

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
			var f = new Node([], null, null);
			f.Entries = new List<Entry> { new Entry("set a", 1) };

			List<Entry> entriesFromLeader = new List<Entry>();
			Entry e1 = new("set a", 1);
			Entry e2 = new("set b", 2);
			Entry e3 = new("set c", 2);

			entriesFromLeader.Add(e1);
			entriesFromLeader.Add(e2);
			entriesFromLeader.Add(e3);

			// act
			// let's say the leader says to send a, b, and c...
			await f.RecieveAppendEntriesRPC(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<int>(), entriesFromLeader, Arg.Any<int>());

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

			Node  f = new([], null, null);
			f.OtherNodes = [l];

			// act
			await f.RecieveAppendEntriesRPC(l.TermNumber, l.NodeId, (l.Entries.Count - 1), l.Entries, l.CommitIndex);

			// assert
			l.Received(1).RespondBackToLeader(Arg.Any<bool>(), f.TermNumber, f.CommitIndex);

		}

		// Testing Logs #13
		[Fact]
		public void TestCase13_CommittingALogIncrementsCommitIndex()
		{
			Node l = new([], null, null);
			int indexBefore = l.CommitIndex;

			l.CommitEntry();

			Assert.Equal(l.CommitIndex - 1, indexBefore);	
		}

		// Testing Logs #14
		[Fact]
		public async Task TestCase14_()
		{
			// 14.when a follower receives a heartbeat,
			// it increases its commitIndex to match the commit index of the heartbeat

			// arrange
			Node l = new([], null, null);
			l.CommitIndex = 100;
			Node f = new([], null, null);
			l.OtherNodes = [f];

			l.OtherNodes = [f];

			// act
			// follower recieves an empty heartbeat
			await f.RecieveAppendEntriesRPC(l.TermNumber, l.NodeId, Arg.Any<int>(),  new List<Entry>(), l.CommitIndex);

			// assert
			Assert.True(f.CommitIndex == 100);	
		}

		// Testing #19 
		[Fact]
		public async Task TestCase19_NodesRejectFutureTerms()
		{
            // 19. if a node receives an appendentries with a logs that are too far in the future from your local state,
			// you should reject the appendentries

			// arrange
			var f1 = new Node([], null, null);
            f1.Entries = new List<Entry> { new Entry("set a") };

			var l = Substitute.For<INode>();
			List<Entry> leadersEntries = new List<Entry> { new Entry("set a"), new Entry("set b"), new Entry("set c") };
			l.Entries = leadersEntries;
			f1.OtherNodes = [l];

			// act
			await f1.RecieveAppendEntriesRPC(1, l.NodeId, (l.Entries.Count - 1), leadersEntries, l.CommitIndex);

			// assert
			// Because f prevLogIndex is at 1, and l prevLogIndex is at 3, then 3 - 1 > 1, so we reject
			l.Received(1).RespondBackToLeader(false, f1.TermNumber, f1.CommitIndex);
        }


		// Testing 15
		[Fact]
		public async Task TestCase15_NodesRejectRequestsIfTermsDiffer()
		{
			// If the follower does not find an entry in its log with the same index and term,
			// then it refuses the new entries.

			// arrange
			var f1 = new Node([], null, null);
			f1.Entries = new List<Entry> { new Entry("set a", 1) };

			var l = Substitute.For<INode>();
			List<Entry> leadersEntries = new List<Entry> { new Entry("set a", 2), new Entry("set b", 2) };	// same command, but different term

			l.Entries = leadersEntries;
			f1.OtherNodes = [l];

			// act
			await f1.RecieveAppendEntriesRPC(1, l.NodeId, (l.Entries.Count - 1), leadersEntries, l.CommitIndex);

			// assert
			// Because the term the leader is trying to send 
			l.Received(1).RespondBackToLeader(false, f1.TermNumber, f1.CommitIndex);
		}

		// Testing 15
		[Fact]
		public void TestCase15_FollowersRecieveALog()
		{
			// Followers recieve a log

			// arrange
			var f1 = Substitute.For<INode>();
			f1.Entries = new List<Entry> { new Entry("set a", 1) };

			var l = new Node([], null, null);
			l.Entries = new List<Entry> { new Entry("set a", 1) };  // same command, but different term
			f1.OtherNodes = [l];
			l.OtherNodes = [f1];
			l.BecomeLeader();


			l.RecieveClientCommand("set b");
			List<Entry> logsToSend = l.CalculateEntriesToSend(f1); // Should be the last one ("send b") one


			// act
			l.SendAppendEntriesRPC();

			// assert
			Assert.True(f1.Entries.Count == 1);


		}


		//[Fact]
		//public void TestCase15_LeadersSendTheLastNumEntriesToAFollower()
		//{
		//	// Leaders keep a list of node ID's and the associated nodes prevLogIndex. 
		//	// When a followers prevLogIndex is 1 less than the leaders prevLogIndex, 
		//	// then the leader sends 1 entry

		//	// arrange
		//	var f1 = new Node([], null, null);
		//	f1.Entries = new List<Entry> { new Entry("set a", 1) };

		//	var l = new Node([f1], null, null);
		//	l.Entries = new List<Entry> { new Entry("set a", 1), new Entry("set b", 2) };
		//	l.BecomeLeader();

		//	var entriesToSend = l.CalculateEntriesToSend(f1);


		//	// act

		//	// assert
		//	Assert.True(entriesToSend.Count == 1);
		//}
	}
}
