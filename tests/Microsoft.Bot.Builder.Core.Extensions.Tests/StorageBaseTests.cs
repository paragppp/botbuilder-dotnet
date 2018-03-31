﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Core.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Core.Extensions.Tests
{
    public class StorageBaseTests
    {
        private readonly string TestStateNamespace = $"test/state/namespace/{Guid.NewGuid().ToString("D")}";

        protected async Task _readUnknownTest(IStateStore stateStore)
        {
            var result = await stateStore.Load(TestStateNamespace, Guid.NewGuid().ToString("N"));
            Assert.IsNull(result);
        }

        protected async Task _createObjectTest(IStateStore stateStore)
        {
            var stateStoreEntryKey = Guid.NewGuid().ToString("D");
            var newStateStoreEntry = stateStore.CreateNewStateEntry(TestStateNamespace, stateStoreEntryKey);

            var newState = new StateTestsPocoState() { Id = Guid.NewGuid().ToString("D"), Nested = new StateTestsNestedPocoState { NestedId = Guid.NewGuid().ToString("D") } };
            newStateStoreEntry.SetValue(newState);

            await stateStore.Save(newStateStoreEntry);

            var loadedStateStoreEntry = await stateStore.Load(TestStateNamespace, stateStoreEntryKey);

            Assert.IsNotNull(loadedStateStoreEntry);

            var loadedState = newStateStoreEntry.GetValue<StateTestsPocoState>();

            Assert.IsNotNull(loadedState);

            Assert.AreEqual(newState.Id, loadedState.Id);

            Assert.IsNotNull(loadedState.Nested);

            Assert.AreEqual(newState.Nested.NestedId, loadedState.Nested.NestedId);
        }

        protected async Task _handleCrazyKeys(IStateStore stateStore)
        {
            var stateStoreEntryKey = $"{Guid.NewGuid().ToString("N")}-!@#$%^&*()~/\\><,.?';\"`~";
            var stateStoreEntry = stateStore.CreateNewStateEntry(TestStateNamespace, stateStoreEntryKey);

            var state = new StateTestsPocoState() { Id = Guid.NewGuid().ToString("D") };
            stateStoreEntry.SetValue(state);

            await stateStore.Save(stateStoreEntry);

            var loadedStateStoreEntry = await stateStore.Load(TestStateNamespace, stateStoreEntryKey);

            Assert.IsNotNull(loadedStateStoreEntry);

            var loadedState = loadedStateStoreEntry.GetValue<StateTestsPocoState>();

            Assert.IsNotNull(loadedState);

            Assert.AreEqual(state.Id, loadedState.Id);
        }

        protected async Task _updateObjectTest(IStateStore stateStore)
        {
            var stateStoreEntryKey = Guid.NewGuid().ToString("D");
            var stateStoreEntry = stateStore.CreateNewStateEntry(TestStateNamespace, stateStoreEntryKey);


            var originalState = new StateTestsPocoState() { Id = Guid.NewGuid().ToString("D"), Count = 1 };
            stateStoreEntry.SetValue(originalState);

            //first write should work
            await stateStore.Save(stateStoreEntry);

            var loadedStateStoreEntry = await stateStore.Load(TestStateNamespace, stateStoreEntryKey);

            Assert.IsNotNull(loadedStateStoreEntry);
            Assert.IsNotNull(loadedStateStoreEntry.ETag, "Loaded state store entry should have an ETag value assigned.");

            var loadedState = loadedStateStoreEntry.GetValue<StateTestsPocoState>();

            Assert.IsNotNull(loadedState);

            // 2nd write should work, because we have new etag, or no etag
            loadedState.Count++;

            await stateStore.Save(loadedStateStoreEntry);

            var reloadedStateStoreEntry = await stateStore.Load(TestStateNamespace, stateStoreEntryKey);

            Assert.IsNotNull(reloadedStateStoreEntry);
            Assert.IsNotNull(reloadedStateStoreEntry.ETag, "Reloaded state store entry should have an ETag value assigned.");
            Assert.AreNotEqual(loadedStateStoreEntry.ETag, reloadedStateStoreEntry.ETag);

            var reloadedState = reloadedStateStoreEntry.GetValue<StateTestsPocoState>();

            Assert.IsNotNull(reloadedState);

            Assert.AreEqual(reloadedState.Count, 2);

            // update latest state entry should succeed because we have latest ETag
            reloadedState.Count++;

            await stateStore.Save(reloadedStateStoreEntry);


            // update originally loaded state entry should fail because we have a stale ETag
            try
            {
                loadedState.Count++;

                await stateStore.Save(loadedStateStoreEntry);

                Assert.Fail("Should have thrown exception on write because of stale ETag");
            }
            catch
            {
            }

            var rereloadedStateStoreEntry = await stateStore.Load(TestStateNamespace, stateStoreEntryKey);
            var rereloadedState = rereloadedStateStoreEntry.GetValue<StateTestsPocoState>();

            Assert.AreEqual(rereloadedState.Count, 3);

            rereloadedState.Count = 100;

            //rereloadedStateStoreEntry.ETag = "*";

            //// write with wildcard etag should work
            //result3.updatePocoItem.Count = 100;
            //result3.updatePocoStoreItem.Count = 100;
            //result3.updatePocoStoreItem.eTag = "*";
            //result3.updateStoreItem.Count = 100;
            //result3.updateStoreItem.eTag = "*";
            //await stateStore.Write(result3);

            //dynamic result4 = await stateStore.Read(((StoreItems)storeItems).GetDynamicMemberNames().ToArray());
            //Assert.AreEqual(result3.updatePocoItem.Count, 100, "updatePocoItem.Count should be 100");
            //Assert.AreEqual(result3.updatePocoStoreItem.Count, 100, "updatePocoStoreItem.Count should be 100");
            //Assert.AreEqual(result3.updateStoreItem.Count, 100, "updateStoreItem.Count should be 100");

            //// write with empty etag should not work
            //try
            //{
            //    dynamic storeItemsUpdate = new StoreItems();
            //    storeItemsUpdate.updatePocoStoreItem = FlexObject.Clone(result4.updatePocoStoreItem);
            //    storeItemsUpdate.updatePocoStoreItem.eTag = "";
            //    await stateStore.Write(result);
            //    Assert.Fail("Should not throw exception on write with pocoStoreItem because of empty etag");
            //}
            //catch
            //{
            //}
            //try
            //{
            //    dynamic storeItemsUpdate = new StoreItems();
            //    storeItemsUpdate.updateStoreItem = FlexObject.Clone(result4.updateStoreItem);
            //    storeItemsUpdate.updateStoreItem.eTag = "";
            //    await stateStore.Write(result);
            //    Assert.Fail("Should not throw exception on write with storeItem because of empty etag");
            //}
            //catch
            //{
            //}

            //dynamic result5 = await stateStore.Read(((StoreItems)storeItems).GetDynamicMemberNames().ToArray());
            //Assert.AreEqual(result3.updatePocoItem.Count, 100, "updatePocoItem.Count should be 100");
            //Assert.AreEqual(result3.updatePocoStoreItem.Count, 100, "updatePocoStoreItem.Count should be 100");
            //Assert.AreEqual(result3.updateStoreItem.Count, 100, "updateStoreItem.Count should be 100");
        }

        protected async Task _deleteObjectTest(IStateStore stateStore)
        {
            var stateStoreEntryKey = Guid.NewGuid().ToString("D");

            var stateStoreEntry = stateStore.CreateNewStateEntry(TestStateNamespace, stateStoreEntryKey);
            var state = new StateTestsPocoState() { Id = "1", Count = 1 };

            stateStoreEntry.SetValue(state);

            //first write should work
            await stateStore.Save(stateStoreEntry);

            var loadedStateStoreEntry = await stateStore.Load(TestStateNamespace, stateStoreEntryKey);

            Assert.IsTrue(!String.IsNullOrEmpty(loadedStateStoreEntry.ETag), "Expected ETag to be set on a loaded state store entry.");


            var loadedState = loadedStateStoreEntry.GetValue<StateTestsPocoState>();
            Assert.AreEqual(state.Count, loadedState.Count);

            await stateStore.Delete(TestStateNamespace, stateStoreEntryKey);

            var reloadedStateStoreEntry = await stateStore.Load(TestStateNamespace, stateStoreEntryKey);
            Assert.IsNull(reloadedStateStoreEntry, "Did not expect to find the state store entry that was just deleted.");
        }

        public class StateTestsPocoState
        {
            public string Id { get; set; }

            public int Count { get; set; }

            public StateTestsNestedPocoState Nested { get; set; }
        }

        public class StateTestsNestedPocoState
        {
            public string NestedId { get; set; }
        }
    }   
}
