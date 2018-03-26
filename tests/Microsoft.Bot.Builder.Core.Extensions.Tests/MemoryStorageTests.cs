// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Core.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Core.Extensions.Tests
{
    [TestClass]
    [TestCategory("Storage")]
    [TestCategory("Storage - Memory")]
    public class MemoryStorageTests : StorageBaseTests
    {
        private IStateStore stateStore;

        public MemoryStorageTests() { }

        [TestInitialize]
        public void initialize()
        {
            stateStore = new MemoryStateStore();
        }

        [TestMethod]
        public async Task MemoryStorage_CreateObjectTest()
        {
            await base._createObjectTest(stateStore);
        }

        [TestMethod]
        public async Task MemoryStorage_ReadUnknownTest()
        {
            await base._readUnknownTest(stateStore);
        }

        [TestMethod]
        public async Task MemoryStorage_UpdateObjectTest()
        {
            await base._updateObjectTest(stateStore);
        }

        [TestMethod]
        public async Task MemoryStorage_DeleteObjectTest()
        {
            await base._deleteObjectTest(stateStore);
        }

        [TestMethod]
        public async Task MemoryStorage_HandleCrazyKeys()
        {
            await base._handleCrazyKeys(stateStore);
        }
    }
}
