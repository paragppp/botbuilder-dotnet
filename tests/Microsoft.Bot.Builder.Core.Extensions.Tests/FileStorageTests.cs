// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Core.State;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Core.Extensions.Tests
{
    [TestClass]
    public class FileStorageTests
    {
        [TestClass]
        [TestCategory("Storage")]
        [TestCategory("Storage - File")]
        public class Storage_FileTests : StorageBaseTests
        {
            private IStateStore stateStore;

            [TestInitialize]
            public void Initialize()
            {
                string path = Path.Combine(Path.GetTempPath(), "BotFramework/FileSystemStateStoreTests");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                foreach (var file in Directory.GetFiles(path))
                    File.Delete(file);
                stateStore = new FileSystemStateStore(path);
            }

            [TestMethod]
            public async Task FileStorage_CreateObjectTest()
            {
                await base._createObjectTest(this.stateStore);
            }

            [TestMethod]
            public async Task FileStorage_ReadUnknownTest()
            {
                await base._readUnknownTest(this.stateStore);
            }

            [TestMethod]
            public async Task FileStorage_UpdateObjectTest()
            {
                await base._updateObjectTest(this.stateStore);
            }

            [TestMethod]
            public async Task FileStorage_DeleteObjectTest()
            {
                await base._deleteObjectTest(this.stateStore);
            }

            [TestMethod]
            public async Task FileStorage_HandleCrazyKeys()
            {
                await base._handleCrazyKeys(this.stateStore);
            }
        }
    }
}
