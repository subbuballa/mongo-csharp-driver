﻿ /* Copyright 2010-2012 10gen Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDB.DriverUnitTests
{
    [TestFixture]
    public class MongoConnectionStringBuilderTests
    {
        private MongoServerAddress _localhost = new MongoServerAddress("localhost");

        [Test]
        public void TestAll()
        {
            var readPreference = new ReadPreference
            {
                ReadPreferenceMode = ReadPreferenceMode.Secondary,
                TagSets = new[] { new ReplicaSetTagSet { { "dc", "1" } } }
            };
            var built = new MongoConnectionStringBuilder()
            {
                ConnectionMode = ConnectionMode.ReplicaSet,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                DatabaseName = "database",
                FireAndForget = false,
                FSync = true,
                GuidRepresentation = GuidRepresentation.PythonLegacy,
                IPv6 = true,
                Journal = true,
                MaxConnectionIdleTime = TimeSpan.FromSeconds(2),
                MaxConnectionLifeTime = TimeSpan.FromSeconds(3),
                MaxConnectionPoolSize = 4,
                MinConnectionPoolSize = 5,
                Password = "password",
                ReadPreference = readPreference,
                ReplicaSetName = "name",
                SecondaryAcceptableLatency = TimeSpan.FromSeconds(6),
                Server = new MongoServerAddress("host"),
                SocketTimeout = TimeSpan.FromSeconds(7),
                Username = "username",
                UseSsl = true,
                VerifySslCertificate = false,
                W = 2,
                WaitQueueSize = 123,
                WaitQueueTimeout = TimeSpan.FromSeconds(8),
                WTimeout = TimeSpan.FromSeconds(9)
            };

            var connectionString = string.Join(";", new[] {
                "connect=replicaSet",
                "connectTimeout=1s",
                "database=database",
                "fireAndForget=false",
                "fsync=true",
                "uuidRepresentation=pythonLegacy",
                "ipv6=true",
                "journal=true",
                "maxIdleTime=2s",
                "maxLifeTime=3s",
                "maxPoolSize=4",
                "minPoolSize=5",
                "password=password",
                "readPreference=secondary;readPreferenceTags=dc:1",
                "replicaSet=name",
                "secondaryAcceptableLatency=6s",
                "server=host",
                "socketTimeout=7s",
                "username=username",
                "ssl=true", // UseSsl
                "sslVerifyCertificate=false", // VerifySslCertificate
                "w=2",
                "waitQueueSize=123",
                "waitQueueTimeout=8s",
                "wtimeout=9s"      
            });

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(123, builder.ComputedWaitQueueSize);
                Assert.AreEqual(ConnectionMode.ReplicaSet, builder.ConnectionMode);
                Assert.AreEqual(TimeSpan.FromSeconds(1), builder.ConnectTimeout);
                Assert.AreEqual("database", builder.DatabaseName);
                Assert.AreEqual(false, builder.FireAndForget);
                Assert.AreEqual(true, builder.FSync);
                Assert.AreEqual(GuidRepresentation.PythonLegacy, builder.GuidRepresentation);
                Assert.AreEqual(true, builder.IPv6);
                Assert.AreEqual(true, builder.Journal);
                Assert.AreEqual(TimeSpan.FromSeconds(2), builder.MaxConnectionIdleTime);
                Assert.AreEqual(TimeSpan.FromSeconds(3), builder.MaxConnectionLifeTime);
                Assert.AreEqual(4, builder.MaxConnectionPoolSize);
                Assert.AreEqual(5, builder.MinConnectionPoolSize);
                Assert.AreEqual("password", builder.Password);
                Assert.AreEqual(readPreference, builder.ReadPreference);
                Assert.AreEqual("name", builder.ReplicaSetName);
#pragma warning disable 618
                Assert.AreEqual(null, builder.Safe);
                Assert.AreEqual(new SafeMode(true) { FSync = true, Journal = true, W = 2, WTimeout = TimeSpan.FromSeconds(9) }, builder.SafeMode);
#pragma warning restore
                Assert.AreEqual(TimeSpan.FromSeconds(6), builder.SecondaryAcceptableLatency);
                Assert.AreEqual(new MongoServerAddress("host", 27017), builder.Server);
#pragma warning disable 618
                Assert.AreEqual(true, builder.SlaveOk);
#pragma warning restore
                Assert.AreEqual(TimeSpan.FromSeconds(7), builder.SocketTimeout);
                Assert.AreEqual("username", builder.Username);
                Assert.AreEqual(true, builder.UseSsl);
                Assert.AreEqual(false, builder.VerifySslCertificate);
                Assert.AreEqual(2, ((WriteConcern.WCount)builder.W).Value);
                Assert.AreEqual(0.0, builder.WaitQueueMultiple);
                Assert.AreEqual(123, builder.WaitQueueSize);
                Assert.AreEqual(TimeSpan.FromSeconds(8), builder.WaitQueueTimeout);
                Assert.AreEqual(TimeSpan.FromSeconds(9), builder.WTimeout);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestComputedWaitQueueSize_UsingMultiple()
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost, MaxConnectionPoolSize = 123, WaitQueueMultiple = 2.0 };
            var connectionString = "server=localhost;maxPoolSize=123;waitQueueMultiple=2";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(123, builder.MaxConnectionPoolSize);
                Assert.AreEqual(2.0, builder.WaitQueueMultiple);
                Assert.AreEqual(0, builder.WaitQueueSize);
                Assert.AreEqual(246, builder.ComputedWaitQueueSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestComputedWaitQueueSize_UsingSize()
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost, WaitQueueSize = 123 };
            var connectionString = "server=localhost;waitQueueSize=123";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(0.0, builder.WaitQueueMultiple);
                Assert.AreEqual(123, builder.WaitQueueSize);
                Assert.AreEqual(123, builder.ComputedWaitQueueSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(ConnectionMode.Automatic, "server=localhost;connect={0}", new[] { "automatic", "Automatic" })]
        [TestCase(ConnectionMode.Direct, "server=localhost;connect={0}", new[] { "direct", "Direct" })]
        [TestCase(ConnectionMode.ReplicaSet, "server=localhost;connect={0}", new[] { "replicaSet", "ReplicaSet" })]
        [TestCase(ConnectionMode.ShardRouter, "server=localhost;connect={0}", new[] { "shardRouter", "ShardRouter" })]
        public void TestConnectionMode(ConnectionMode? connectionMode, string formatString, string[] values)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (connectionMode != null) { built.ConnectionMode = connectionMode.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(connectionMode ?? ConnectionMode.Automatic, builder.ConnectionMode);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(500, "server=localhost;connectTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "server=localhost;connectTimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "server=localhost;connectTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "server=localhost;connectTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "server=localhost;connectTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestConnectTimeout(int? ms, string formatString, string[] values)
        {
            var connectTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (connectTimeout != null) { built.ConnectTimeout = connectTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(connectTimeout ?? MongoDefaults.ConnectTimeout, builder.ConnectTimeout);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestConnectTimeout_Range()
        {
            var builder = new MongoConnectionStringBuilder();
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.ConnectTimeout = TimeSpan.FromMilliseconds(-1); });
            builder.ConnectTimeout = TimeSpan.FromMilliseconds(0);
            builder.ConnectTimeout = TimeSpan.FromMilliseconds(1);
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase("database", "server=localhost;database=database")]
        public void TestDatabaseName(string databaseName, string connectionString)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost, DatabaseName = databaseName };
  
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(databaseName, builder.DatabaseName);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestDefaults()
        {
            var built = new MongoConnectionStringBuilder();
            var connectionString = "";
  
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(MongoDefaults.ComputedWaitQueueSize, builder.ComputedWaitQueueSize);
                Assert.AreEqual(ConnectionMode.Automatic, builder.ConnectionMode);
                Assert.AreEqual(MongoDefaults.ConnectTimeout, builder.ConnectTimeout);
                Assert.AreEqual(null, builder.DatabaseName);
                Assert.AreEqual(null, builder.FireAndForget);
                Assert.AreEqual(null, builder.FSync);
                Assert.AreEqual(MongoDefaults.GuidRepresentation, builder.GuidRepresentation);
                Assert.AreEqual(false, builder.IPv6);
                Assert.AreEqual(null, builder.Journal);
                Assert.AreEqual(MongoDefaults.MaxConnectionIdleTime, builder.MaxConnectionIdleTime);
                Assert.AreEqual(MongoDefaults.MaxConnectionLifeTime, builder.MaxConnectionLifeTime);
                Assert.AreEqual(MongoDefaults.MaxConnectionPoolSize, builder.MaxConnectionPoolSize);
                Assert.AreEqual(MongoDefaults.MinConnectionPoolSize, builder.MinConnectionPoolSize);
                Assert.AreEqual(null, builder.Password);
                Assert.AreEqual(null, builder.ReadPreference);
                Assert.AreEqual(null, builder.ReplicaSetName);
#pragma warning disable 618
                Assert.AreEqual(null, builder.Safe);
                Assert.AreEqual(null, builder.SafeMode);
#pragma warning restore
                Assert.AreEqual(MongoDefaults.SecondaryAcceptableLatency, builder.SecondaryAcceptableLatency);
                Assert.AreEqual(null, builder.Server);
                Assert.AreEqual(null, builder.Servers);
#pragma warning disable 618
                Assert.AreEqual(false, builder.SlaveOk);
#pragma warning restore
                Assert.AreEqual(MongoDefaults.SocketTimeout, builder.SocketTimeout);
                Assert.AreEqual(null, builder.Username);
                Assert.AreEqual(false, builder.UseSsl);
                Assert.AreEqual(true, builder.VerifySslCertificate);
                Assert.AreEqual(null, builder.W);
                Assert.AreEqual(MongoDefaults.WaitQueueMultiple, builder.WaitQueueMultiple);
                Assert.AreEqual(MongoDefaults.WaitQueueSize, builder.WaitQueueSize);
                Assert.AreEqual(MongoDefaults.WaitQueueTimeout, builder.WaitQueueTimeout);
                Assert.AreEqual(null, builder.WTimeout);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(false, "server=localhost;fireAndForget={0}", new[] { "false", "False" })]
        [TestCase(true, "server=localhost;fireAndForget={0}", new[] { "true", "True" })]
        public void TestFireAndForget(bool? fireAndForget, string formatString, string[] values)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost, FireAndForget = fireAndForget };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(fireAndForget, builder.FireAndForget);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestFireAndForget_AfterOtherSettings()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            builder.W = 2;
            builder.FireAndForget = null;
            builder.FireAndForget = false;
            Assert.Throws<InvalidOperationException>(() => { builder.FireAndForget = true; });
        }

        [Test]
        public void TestFireAndForget_AfterSafe()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
#pragma warning disable 618
            builder.Safe = false;
#pragma warning restore
            builder.FireAndForget = null;
            Assert.Throws<InvalidOperationException>(() => { builder.FireAndForget = false; });
            Assert.Throws<InvalidOperationException>(() => { builder.FireAndForget = true; });
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(false, "server=localhost;fsync={0}", new[] { "false", "False" })]
        [TestCase(true, "server=localhost;fsync={0}", new[] { "true", "True" })]
        public void TestFSync(bool? fsync, string formatString, string[] values)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost, FSync = fsync };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(fsync, builder.FSync);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestFSync_WhenFireAndForgetIsTrue()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            builder.FireAndForget = true;
            builder.FSync = null;
            Assert.Throws<InvalidOperationException>(() => { builder.FSync = false; });
            Assert.Throws<InvalidOperationException>(() => { builder.FSync = true; });

            builder = new MongoConnectionStringBuilder { Server = _localhost };
#pragma warning disable 618
            builder.Safe = false;
#pragma warning restore
            builder.FSync = null;
            Assert.Throws<InvalidOperationException>(() => { builder.FSync = false; });
            Assert.Throws<InvalidOperationException>(() => { builder.FSync = true; });
        }

        [Test]
        [TestCase(false, false, "server=localhost")]
        [TestCase(false, false, "server=localhost;fireAndForget=false")]
        [TestCase(false, true, "server=localhost;fireAndForget=true")]
        [TestCase(false, true, "server=localhost;safe=false")]
        [TestCase(false, false, "server=localhost;safe=true")]
        [TestCase(false, false, "server=localhost;w=2")]
        [TestCase(true, true, "server=localhost")]
        [TestCase(true, false, "server=localhost;fireAndForget=false")]
        [TestCase(true, true, "server=localhost;fireAndForget=true")]
        [TestCase(true, true, "server=localhost;safe=false")]
        [TestCase(true, false, "server=localhost;safe=true")]
        [TestCase(true, false, "server=localhost;w=2")]
        public void TestGetWriteConcern_FireAndForget(bool fireAndForgetDefault, bool fireAndForget, string connectionString)
        {
            var builder = new MongoConnectionStringBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(fireAndForgetDefault);
            Assert.AreEqual(fireAndForget, writeConcern.FireAndForget);
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase(false, "server=localhost;fsync=false")]
        [TestCase(true, "server=localhost;fsync=true")]
        public void TestGetWriteConcern_FSync(bool? fsync, string connectionString)
        {
            var builder = new MongoConnectionStringBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(false);
            Assert.AreEqual(fsync, writeConcern.FSync);
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" }, new[] { "" })]
        [TestCase(false, "server=localhost;{1}={0}", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(true, "server=localhost;{1}={0}", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestGetWriteConcern_Journal(bool? journal, string formatString, string[] values, string[] journalAliases)
        {
            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            foreach (var builder in EnumerateParsedBuilders(formatString, values, journalAliases))
            {
                var writeConcern = builder.GetWriteConcern(true);
                Assert.AreEqual(journal, writeConcern.Journal);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase(2, "server=localhost;w=2")]
        [TestCase("mode", "server=localhost;w=mode")]
        public void TestGetWriteConcern_W(object obj, string connectionString)
        {
            var w = (obj is int) ? (WriteConcern.WValue)(int)obj : (WriteConcern.WValue)(string)obj;
            var builder = new MongoConnectionStringBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(false);
            Assert.AreEqual(w, writeConcern.W);
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase(500, "server=localhost;wtimeout=500ms")]
        public void TestGetWriteConcern_WTimeout(int? ms, string connectionString)
        {
            var wtimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var builder = new MongoConnectionStringBuilder(connectionString);
            var writeConcern = builder.GetWriteConcern(false);
            Assert.AreEqual(wtimeout, writeConcern.WTimeout);
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" }, new[] { "" })]
        [TestCase(GuidRepresentation.CSharpLegacy, "server=localhost;{1}={0}", new[] { "csharpLegacy", "CSharpLegacy" }, new[] { "uuidRepresentation", "guids" })]
        [TestCase(GuidRepresentation.JavaLegacy, "server=localhost;{1}={0}", new[] { "javaLegacy", "JavaLegacy" }, new[] { "uuidRepresentation", "guids" })]
        [TestCase(GuidRepresentation.PythonLegacy, "server=localhost;{1}={0}", new[] { "pythonLegacy", "PythonLegacy" }, new[] { "uuidRepresentation", "guids" })]
        [TestCase(GuidRepresentation.Standard, "server=localhost;{1}={0}", new[] { "standard", "Standard" }, new[] { "uuidRepresentation", "guids" })]
        [TestCase(GuidRepresentation.Unspecified, "server=localhost;{1}={0}", new[] { "unspecified", "Unspecified" }, new[] { "uuidRepresentation", "guids" })]
        public void TestGuidRepresentation(GuidRepresentation? guidRepresentation, string formatString, string[] values, string[] uuidAliases)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (guidRepresentation != null) { built.GuidRepresentation = guidRepresentation.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0], "uuidRepresentation");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values, uuidAliases))
            {
                Assert.AreEqual(guidRepresentation ?? MongoDefaults.GuidRepresentation, builder.GuidRepresentation);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestIndexer_W_WithInteger()
        {
            var builder = new MongoConnectionStringBuilder();
            builder["w"] = 2;
            Assert.IsInstanceOf<WriteConcern.WCount>(builder.W);
            Assert.AreEqual(2, ((WriteConcern.WCount)builder.W).Value);
        }

        [Test]
        public void TestIndexer_W_WithString()
        {
            var builder = new MongoConnectionStringBuilder();
            builder["w"] = "mode";
            Assert.IsInstanceOf<WriteConcern.WMode>(builder.W);
            Assert.AreEqual("mode", ((WriteConcern.WMode)builder.W).Value);
        }

        [Test]
        public void TestIndexer_W_WithWValue()
        {
            var builder = new MongoConnectionStringBuilder();
            builder["w"] = new WriteConcern.WMode("mode");
            Assert.IsInstanceOf<WriteConcern.WMode>(builder.W);
            Assert.AreEqual("mode", ((WriteConcern.WMode)builder.W).Value);
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(false, "server=localhost;ipv6={0}", new[] { "false", "False" })]
        [TestCase(true, "server=localhost;ipv6={0}", new[] { "true", "True" })]
        public void TestIPv6(bool? ipv6, string formatString, string[] values)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (ipv6 != null) { built.IPv6 = ipv6.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(ipv6 ?? false, builder.IPv6);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" }, new[] { "" })]
        [TestCase(false, "server=localhost;{1}={0}", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(true, "server=localhost;{1}={0}", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestJournal(bool? journal, string formatString, string[] values, string[] journalAliases)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost, Journal = journal };

            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values, journalAliases))
            {
                Assert.AreEqual(journal, builder.Journal);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestJournal_WhenFireAndForgetIsTrue()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            builder.FireAndForget = true;
            builder.Journal = null;
            Assert.Throws<InvalidOperationException>(() => { builder.Journal = false; });
            Assert.Throws<InvalidOperationException>(() => { builder.Journal = true; });

            builder = new MongoConnectionStringBuilder { Server = _localhost };
#pragma warning disable 618
            builder.Safe = false;
#pragma warning restore
            builder.Journal = null;
            Assert.Throws<InvalidOperationException>(() => { builder.Journal = false; });
            Assert.Throws<InvalidOperationException>(() => { builder.Journal = true; });
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(500, "server=localhost;maxIdleTime{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "server=localhost;maxIdleTime{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "server=localhost;maxIdleTime{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "server=localhost;maxIdleTime{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "server=localhost;maxIdleTime{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestMaxConnectionIdleTime(int? ms, string formatString, string[] values)
        {
            var maxConnectionIdleTime = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (maxConnectionIdleTime != null) { built.MaxConnectionIdleTime = maxConnectionIdleTime.Value; };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(maxConnectionIdleTime ?? MongoDefaults.MaxConnectionIdleTime, builder.MaxConnectionIdleTime);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestMaxConnectionIdleTime_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionIdleTime = TimeSpan.FromSeconds(-1); });
            builder.MaxConnectionIdleTime = TimeSpan.Zero;
            builder.MaxConnectionIdleTime = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(500, "server=localhost;maxLifeTime{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "server=localhost;maxLifeTime{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "server=localhost;maxLifeTime{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "server=localhost;maxLifeTime{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "server=localhost;maxLifeTime{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestMaxConnectionLifeTime(int? ms, string formatString, string[] values)
        {
            var maxConnectionLifeTime = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (maxConnectionLifeTime != null) { built.MaxConnectionLifeTime = maxConnectionLifeTime.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(maxConnectionLifeTime ?? MongoDefaults.MaxConnectionLifeTime, builder.MaxConnectionLifeTime);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestMaxConnectionLifeTime_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionLifeTime = TimeSpan.FromSeconds(-1); });
            builder.MaxConnectionIdleTime = TimeSpan.Zero;
            builder.MaxConnectionIdleTime = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase(123, "server=localhost;maxPoolSize=123")]
        public void TestMaxConnectionPoolSize(int? maxConnectionPoolSize, string connectionString)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (maxConnectionPoolSize != null) { built.MaxConnectionPoolSize = maxConnectionPoolSize.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(maxConnectionPoolSize ?? MongoDefaults.MaxConnectionPoolSize, builder.MaxConnectionPoolSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestMaxConnectionPoolSize_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionPoolSize = -1; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MaxConnectionPoolSize = 0; });
            builder.MaxConnectionPoolSize = 1;
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase(123, "server=localhost;minPoolSize=123")]
        public void TestMinConnectionPoolSize(int? minConnectionPoolSize, string connectionString)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (minConnectionPoolSize != null) { built.MinConnectionPoolSize = minConnectionPoolSize.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(minConnectionPoolSize ?? MongoDefaults.MinConnectionPoolSize, builder.MinConnectionPoolSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestMinConnectionPoolSize_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.MinConnectionPoolSize = -1; });
            builder.MinConnectionPoolSize = 0;
            builder.MinConnectionPoolSize = 1;
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase("password", "server=localhost;password=password")]
        [TestCase("p;ssword", "server=localhost;password=\"p;ssword\"")]
        public void TestPassword(string password, string connectionString)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost, Password = password };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(password, builder.Password);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(ReadPreferenceMode.Primary, "server=localhost;readPreference={0}", new[] { "primary", "Primary" })]
        [TestCase(ReadPreferenceMode.PrimaryPreferred, "server=localhost;readPreference={0}", new[] { "primaryPreferred", "PrimaryPreferred" })]
        [TestCase(ReadPreferenceMode.Secondary, "server=localhost;readPreference={0}", new[] { "secondary", "Secondary" })]
        [TestCase(ReadPreferenceMode.SecondaryPreferred, "server=localhost;readPreference={0}", new[] { "secondaryPreferred", "SecondaryPreferred" })]
        [TestCase(ReadPreferenceMode.Nearest, "server=localhost;readPreference={0}", new[] { "nearest", "Nearest" })]
        public void TestReadPreference(ReadPreferenceMode? mode, string formatString, string[] values)
        {
            ReadPreference readPreference = null;
            if (mode != null) { readPreference = new ReadPreference { ReadPreferenceMode = mode.Value }; }
            var built = new MongoConnectionStringBuilder { Server = _localhost, ReadPreference = readPreference };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(readPreference, builder.ReadPreference);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(false, ReadPreferenceMode.Primary)]
        [TestCase(false, ReadPreferenceMode.Secondary)]
        [TestCase(true, ReadPreferenceMode.Primary)]
        [TestCase(true, ReadPreferenceMode.Secondary)]
        public void TestReadPreference_AfterSlaveOk(bool slaveOk, ReadPreferenceMode mode)
        {
            var readPreference = new ReadPreference { ReadPreferenceMode = mode };
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
#pragma warning disable 618
            builder.SlaveOk = slaveOk;
#pragma warning restore
            builder.ReadPreference = null;
            Assert.Throws<InvalidOperationException>(() => { builder.ReadPreference = readPreference; });
        }

        [Test]
        public void TestReadPreference_SecondaryWithOneTagSet()
        {
            var tagSets = new ReplicaSetTagSet[]
            {
                new ReplicaSetTagSet { { "dc", "ny" }, { "rack", "1" } }
            };
            var readPreference = new ReadPreference { ReadPreferenceMode = ReadPreferenceMode.Secondary, TagSets = tagSets };
            var built = new MongoConnectionStringBuilder { Server = _localhost, ReadPreference = readPreference };
            var connectionString = "server=localhost;readPreference=secondary;readPreferenceTags=dc:ny,rack:1";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(ReadPreferenceMode.Secondary, builder.ReadPreference.ReadPreferenceMode);
                var builderTagSets = builder.ReadPreference.TagSets.ToArray();
                Assert.AreEqual(1, builderTagSets.Length);
                var builderTagSet1Tags = builderTagSets[0].Tags.ToArray();
                Assert.AreEqual(2, builderTagSet1Tags.Length);
                Assert.AreEqual(new ReplicaSetTag("dc", "ny"), builderTagSet1Tags[0]);
                Assert.AreEqual(new ReplicaSetTag("rack", "1"), builderTagSet1Tags[1]);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestReadPreference_SecondaryWithTwoTagSets()
        {
            var tagSets = new ReplicaSetTagSet[]
            {
                new ReplicaSetTagSet { { "dc", "ny" }, { "rack", "1" } },
                new ReplicaSetTagSet { { "dc", "sf" } }
            };
            var readPreference = new ReadPreference { ReadPreferenceMode = ReadPreferenceMode.Secondary, TagSets = tagSets };
            var built = new MongoConnectionStringBuilder { Server = _localhost, ReadPreference = readPreference };
            var connectionString = "server=localhost;readPreference=secondary;readPreferenceTags=dc:ny,rack:1|dc:sf";

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(ReadPreferenceMode.Secondary, builder.ReadPreference.ReadPreferenceMode);
                var builderTagSets = builder.ReadPreference.TagSets.ToArray();
                Assert.AreEqual(2, builderTagSets.Length);
                var builderTagSet1Tags = builderTagSets[0].Tags.ToArray();
                var builderTagSet2Tags = builderTagSets[1].Tags.ToArray();
                Assert.AreEqual(2, builderTagSet1Tags.Length);
                Assert.AreEqual(new ReplicaSetTag("dc", "ny"), builderTagSet1Tags[0]);
                Assert.AreEqual(new ReplicaSetTag("rack", "1"), builderTagSet1Tags[1]);
                Assert.AreEqual(1, builderTagSet2Tags.Length);
                Assert.AreEqual(new ReplicaSetTag("dc", "sf"), builderTagSet2Tags[0]);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase("name", "server=localhost;replicaSet=name")]
        public void TestReplicaSetName(string name, string connectionString)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost, ReplicaSetName = name };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(name, builder.ReplicaSetName);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(false, "server=localhost;safe={0}", new[] { "false", "False" })]
        [TestCase(true, "server=localhost;safe={0}", new[] { "true", "True" })]
        public void TestSafe(bool? safe, string formatString, string[] values)
        {
#pragma warning disable 618
            var built = new MongoConnectionStringBuilder { Server = _localhost, Safe = safe };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(safe, builder.Safe);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
#pragma warning restore
        }

        [Test]
        public void TestSafe_AfterFireAndForget()
        {
#pragma warning disable 618
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            builder.FireAndForget = true;
            builder.Safe = null;
            Assert.Throws<InvalidOperationException>(() => { builder.Safe = false; });
            Assert.Throws<InvalidOperationException>(() => { builder.Safe = true; });
#pragma warning restore
        }

        [Test]
        public void TestSafe_AfterOtherSettings()
        {
#pragma warning disable 618
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            builder.W = 2;
            builder.Safe = null;
            builder.Safe = true;
            Assert.Throws<InvalidOperationException>(() => { builder.Safe = false; });
#pragma warning restore
        }

        [Test]
        [TestCase(false, "server=localhost;fsync={0};{1}={0};w=2;wtimeout=30s", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(false, "server=localhost;safe=true;fsync={0};{1}={0};w=2;wtimeout=30s", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(true, "server=localhost;fsync={0};{1}={0};w=2;wtimeout=30s", new[] { "true", "True" }, new[] { "journal", "j" })]
        [TestCase(true, "server=localhost;safe=true;fsync={0};{1}={0};w=2;wtimeout=30s", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestSafeMode_All(bool trueOrFalse, string formatString, string[] values, string[] journalAliases)
        {
#pragma warning disable 618
            var safeMode = new SafeMode(true) { FSync = trueOrFalse, Journal = trueOrFalse, W = 2, WTimeout = TimeSpan.FromSeconds(30) };
            var built = new MongoConnectionStringBuilder { Server = _localhost, SafeMode = safeMode };

            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            var isParsedBuilder = false;
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values, journalAliases))
            {
                Assert.AreEqual(true, builder.SafeMode.Enabled);
                Assert.AreEqual(trueOrFalse, builder.SafeMode.FSync);
                Assert.AreEqual(trueOrFalse, builder.SafeMode.Journal);
                Assert.AreEqual(2, builder.SafeMode.W);
                Assert.AreEqual(TimeSpan.FromSeconds(30), builder.SafeMode.WTimeout);
                if (canonicalConnectionString.Contains("safe=") || isParsedBuilder)
                {
                    Assert.AreEqual(canonicalConnectionString, builder.ToString());
                }
                isParsedBuilder = true;
            }
#pragma warning restore
        }

        [Test]
        [TestCase(null, "server=localhost;safe=true", new[] { "" })]
        [TestCase(false, "server=localhost;fsync={0}", new[] { "false", "False" })]
        [TestCase(false, "server=localhost;safe=true;fsync={0}", new[] { "false", "False" })]
        [TestCase(true, "server=localhost;fsync={0}", new[] { "true", "True" })]
        [TestCase(true, "server=localhost;safe=true;fsync={0}", new[] { "true", "True" })]
        public void TestSafeMode_FSync (bool? fsync, string formatString, string[] values)
        {
#pragma warning disable 618
            var safeMode = new SafeMode(true);
            if (fsync != null) { safeMode.FSync = fsync.Value; }
            var built = new MongoConnectionStringBuilder { Server = _localhost, SafeMode = safeMode };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            var isParsedBuilder = false;
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(true, builder.SafeMode.Enabled);
                Assert.AreEqual(fsync ?? false, builder.SafeMode.FSync);
                if (canonicalConnectionString.Contains("safe=") || isParsedBuilder)
                {
                    Assert.AreEqual(canonicalConnectionString, builder.ToString());
                }
                isParsedBuilder = true;
            }
#pragma warning restore
        }


        [Test]
        [TestCase(null, "server=localhost;safe=true", new[] { "" }, new[] { "" })]
        [TestCase(false, "server=localhost;{1}={0}", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(false, "server=localhost;safe=true;{1}={0}", new[] { "false", "False" }, new[] { "journal", "j" })]
        [TestCase(true, "server=localhost;{1}={0}", new[] { "true", "True" }, new[] { "journal", "j" })]
        [TestCase(true, "server=localhost;safe=true;{1}={0}", new[] { "true", "True" }, new[] { "journal", "j" })]
        public void TestSafeMode_Journal(bool? journal, string formatString, string[] values, string[] journalAliases)
        {
#pragma warning disable 618
            var safeMode = new SafeMode(true);
            if (journal != null) { safeMode.Journal = journal.Value; }
            var built = new MongoConnectionStringBuilder { Server = _localhost, SafeMode = safeMode };

            var canonicalConnectionString = string.Format(formatString, values[0], "journal");
            var isParsedBuilder = false;
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values, journalAliases))
            {
                Assert.AreEqual(true, builder.SafeMode.Enabled);
                Assert.AreEqual(journal ?? false, builder.SafeMode.Journal);
                if (canonicalConnectionString.Contains("safe=") || isParsedBuilder)
                {
                    Assert.AreEqual(canonicalConnectionString, builder.ToString());
                }
                isParsedBuilder = true;
            }
#pragma warning restore
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(false, "server=localhost;safe={0}", new[] { "false", "False" })]
        [TestCase(true, "server=localhost;safe={0}", new[] { "true", "True" })]
        public void TestSafeMode_Safe(bool? enabled, string formatString, string[] values)
        {
#pragma warning disable 618
            var safeMode = (enabled == null) ? null : new SafeMode(enabled.Value);
            var built = new MongoConnectionStringBuilder { Server = _localhost, SafeMode = safeMode };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(safeMode, builder.SafeMode);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
#pragma warning restore
        }

        [Test]
        [TestCase(null, "server=localhost;safe=true")]
        [TestCase(2, "server=localhost;w=2")]
        [TestCase(2, "server=localhost;safe=true;w=2")]
        public void TestSafeMode_W(int? w, string connectionString)
        {
#pragma warning disable 618
            var safeMode = new SafeMode(true);
            if (w != null) { safeMode.W = w.Value; }
            var built = new MongoConnectionStringBuilder { Server = _localhost, SafeMode = safeMode };

            var isParsedBuilder = false;
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(true, builder.SafeMode.Enabled);
                Assert.AreEqual(w ?? 0, builder.SafeMode.W);
                if (connectionString.Contains("safe=") || isParsedBuilder)
                {
                    Assert.AreEqual(connectionString, builder.ToString());
                }
                isParsedBuilder = true;
            }
#pragma warning restore
        }

        [Test]
        [TestCase(null, "server=localhost;safe=true")]
        [TestCase("mode", "server=localhost;w=mode")]
        [TestCase("mode", "server=localhost;safe=true;w=mode")]
        public void TestSafeMode_WMode(string wmode, string connectionString)
        {
#pragma warning disable 618
            var built = new MongoConnectionStringBuilder { Server = _localhost, SafeMode = new SafeMode(true) { WMode = wmode } };

            var isParsedBuilder = false;
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(true, builder.SafeMode.Enabled);
                Assert.AreEqual(wmode, builder.SafeMode.WMode);
                if (connectionString.Contains("safe=") || isParsedBuilder)
                {
                    Assert.AreEqual(connectionString, builder.ToString());
                }
                isParsedBuilder = true;
            }
#pragma warning restore
        }

        [Test]
        [TestCase(null, "server=localhost;safe=true", new[] { "" })]
        [TestCase(500, "server=localhost;wtimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(500, "server=localhost;safe=true;wtimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "server=localhost;wtimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(30000, "server=localhost;safe=true;wtimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "server=localhost;wtimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(1800000, "server=localhost;safe=true;wtimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "server=localhost;wtimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3600000, "server=localhost;safe=true;wtimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "server=localhost;wtimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        [TestCase(3723000, "server=localhost;safe=true;wtimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestSafeMode_WTimeout(int? ms, string formatString, string[] values)
        {
#pragma warning disable 618
            var wtimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var safeMode = new SafeMode(true);
            if (wtimeout != null) { safeMode.WTimeout = wtimeout.Value; }
            var built = new MongoConnectionStringBuilder { Server = _localhost, SafeMode = safeMode };

            var canonicalConnectionString = string.Format(formatString, values[0]);
            var isParsedBuilder = false;
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(true, builder.SafeMode.Enabled);
                Assert.AreEqual(wtimeout ?? TimeSpan.Zero, builder.SafeMode.WTimeout);
                if (canonicalConnectionString.Contains("safe=") || isParsedBuilder)
                {
                    Assert.AreEqual(canonicalConnectionString, builder.ToString());
                }
                isParsedBuilder = true;
            }
#pragma warning restore
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(500, "server=localhost;secondaryAcceptableLatency{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "server=localhost;secondaryAcceptableLatency{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "server=localhost;secondaryAcceptableLatency{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "server=localhost;secondaryAcceptableLatency{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "server=localhost;secondaryAcceptableLatency{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestSecondaryAcceptableLatency(int? ms, string formatString, string[] values)
        {
            var secondaryAcceptableLatency = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (secondaryAcceptableLatency != null) { built.SecondaryAcceptableLatency = secondaryAcceptableLatency.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(secondaryAcceptableLatency ?? MongoDefaults.SecondaryAcceptableLatency, builder.SecondaryAcceptableLatency);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestSecondaryAcceptableLatency_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.SecondaryAcceptableLatency = TimeSpan.FromSeconds(-1); });
            builder.SecondaryAcceptableLatency = TimeSpan.Zero;
            builder.SecondaryAcceptableLatency = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase(null, null, "", new[] { "" })]
        [TestCase("host", null, "{0}=host", new[] { "server", "servers" })]
        [TestCase("host", 27017, "{0}=host", new[] { "server", "servers" })]
        [TestCase("host", 27018, "{0}=host:27018", new[] { "server", "servers" })]
        public void TestServer(string host, int? port, string formatString, string[] serverAliases)
        {
            var server = (host == null) ? null : (port == null) ? new MongoServerAddress(host) : new MongoServerAddress(host, port.Value);
            var built = new MongoConnectionStringBuilder { Server = server };

            var canonicalConnectionString = string.Format(formatString, "server");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, serverAliases))
            {
                Assert.AreEqual(server, builder.Server);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, null, "", new[] { "" })]
        [TestCase(new string[] { "host" }, new object[] { null }, "{0}=host", new[] { "server", "servers" })]
        [TestCase(new string[] { "host" }, new object[] { 27017 }, "{0}=host", new[] { "server", "servers" })]
        [TestCase(new string[] { "host" }, new object[] { 27018 }, "{0}=host:27018", new[] { "server", "servers" })]
        [TestCase(new string[] { "host1", "host2" }, new object[] { null, null }, "{0}=host1,host2", new[] { "server", "servers" })]
        [TestCase(new string[] { "host1", "host2" }, new object[] { null, 27017 }, "{0}=host1,host2", new[] { "server", "servers" })]
        [TestCase(new string[] { "host1", "host2" }, new object[] { null, 27018 }, "{0}=host1,host2:27018", new[] { "server", "servers" })]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27017, null }, "{0}=host1,host2", new[] { "server", "servers" })]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27017, 27017 }, "{0}=host1,host2", new[] { "server", "servers" })]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27017, 27018 }, "{0}=host1,host2:27018", new[] { "server", "servers" })]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27018, null }, "{0}=host1:27018,host2", new[] { "server", "servers" })]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27018, 27017 }, "{0}=host1:27018,host2", new[] { "server", "servers" })]
        [TestCase(new string[] { "host1", "host2" }, new object[] { 27018, 27018 }, "{0}=host1:27018,host2:27018", new[] { "server", "servers" })]
        public void TestServers(string[] hosts, object[] ports, string formatString, string[] serverAliases)
        {
            var servers = (hosts == null) ? null : new List<MongoServerAddress>();
            if (hosts != null)
            {
                Assert.AreEqual(hosts.Length, ports.Length);
                for (var i = 0; i < hosts.Length; i++)
                {
                    var server = (hosts[i] == null) ? null : (ports[i] == null) ? new MongoServerAddress(hosts[i]) : new MongoServerAddress(hosts[i], (int)ports[i]);
                    servers.Add(server);
                }
            }
            var built = new MongoConnectionStringBuilder { Servers = servers };

            var canonicalConnectionString = string.Format(formatString, "server");
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, serverAliases))
            {
                Assert.AreEqual(servers, builder.Servers);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(false, "server=localhost;slaveOk={0}", new[] { "false", "False" })]
        [TestCase(true, "server=localhost;slaveOk={0}", new[] { "true", "True" })]
        public void TestSlaveOk(bool? slaveOk, string formatString, string[] values)
        {
#pragma warning disable 618
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (slaveOk != null) { built.SlaveOk = slaveOk.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(slaveOk ?? false, builder.SlaveOk);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
#pragma warning restore
        }

        [Test]
        [TestCase(ReadPreferenceMode.Primary, false)]
        [TestCase(ReadPreferenceMode.Primary, true)]
        [TestCase(ReadPreferenceMode.Secondary, false)]
        [TestCase(ReadPreferenceMode.Secondary, true)]
        public void TestSlaveOk_AfterReadPreference(ReadPreferenceMode mode, bool slaveOk)
        {
            var readPreference = new ReadPreference { ReadPreferenceMode = mode };
            var builder = new MongoConnectionStringBuilder { Server = _localhost, ReadPreference = readPreference };
#pragma warning disable 618
            Assert.Throws<InvalidOperationException>(() => { builder.SlaveOk = slaveOk; });
#pragma warning restore
        }

        [Test]
        [TestCase(null, false)]
        [TestCase(ReadPreferenceMode.Primary, false)]
        [TestCase(ReadPreferenceMode.PrimaryPreferred, false)]
        [TestCase(ReadPreferenceMode.Secondary, true)]
        [TestCase(ReadPreferenceMode.SecondaryPreferred, true)]
        [TestCase(ReadPreferenceMode.Nearest, true)]
        public void TestSlaveOk_ForReadPreference(ReadPreferenceMode? mode, bool slaveOk)
        {
            var readPreference = (mode == null) ? null : new ReadPreference { ReadPreferenceMode = mode.Value };
            var builder = new MongoConnectionStringBuilder { Server = _localhost, ReadPreference = readPreference };
#pragma warning disable 618
            Assert.AreEqual(slaveOk, builder.SlaveOk);
#pragma warning restore
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(500, "server=localhost;socketTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "server=localhost;socketTimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "server=localhost;socketTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "server=localhost;socketTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "server=localhost;socketTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestSocketTimeout(int? ms, string formatString, string[] values)
        {
            var socketTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (socketTimeout != null) { built.SocketTimeout = socketTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(socketTimeout ?? MongoDefaults.SocketTimeout, builder.SocketTimeout);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestSocketTimeout_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.SocketTimeout = TimeSpan.FromSeconds(-1); });
            builder.SocketTimeout = TimeSpan.Zero;
            builder.SocketTimeout = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase("username", "server=localhost;username=username")]
        [TestCase("usern;me", "server=localhost;username=\"usern;me\"")]
        public void TestUsername(string username, string connectionString)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost, Username = username };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(username, builder.Username);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(false, "server=localhost;ssl={0}", new[] { "false", "False" })]
        [TestCase(true, "server=localhost;ssl={0}", new[] { "true", "True" })]
        public void TestUseSsl(bool? useSsl, string formatString, string[] values)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (useSsl != null) { built.UseSsl = useSsl.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(useSsl ?? false, builder.UseSsl);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(false, "server=localhost;sslVerifyCertificate={0}", new[] { "false", "False" })]
        [TestCase(true, "server=localhost;sslVerifyCertificate={0}", new[] { "true", "True" })]
        public void TestVerifySslCertificate(bool? verifySslCertificate, string formatString, string[] values)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (verifySslCertificate != null) { built.VerifySslCertificate = verifySslCertificate.Value; }
            
            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(verifySslCertificate ?? true, builder.VerifySslCertificate);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase(1, "server=localhost;w=1")]
        [TestCase(2, "server=localhost;w=2")]
        [TestCase("mode", "server=localhost;w=mode")]
        public void TestW(object wobj, string connectionString)
        {
            var w = (wobj is int) ? (WriteConcern.WValue)(int)wobj : (WriteConcern.WValue)(string)wobj;
            var built = new MongoConnectionStringBuilder { Server = _localhost, W = w };

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(w, builder.W);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestW_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            builder.W = null;
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.W = -1; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.W = 0; });
            builder.W = 1;
            builder.W = "mode";
        }

        [Test]
        public void TestW_WhenFireAndForgetIsTrue()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            builder.FireAndForget = true;
            builder.W = null;
            Assert.Throws<InvalidOperationException>(() => { builder.W = 2; });
            Assert.Throws<InvalidOperationException>(() => { builder.W = "mode"; });

            builder = new MongoConnectionStringBuilder { Server = _localhost };
#pragma warning disable 618
            builder.Safe = false;
#pragma warning restore
            builder.W = null;
            Assert.Throws<InvalidOperationException>(() => { builder.W = 2; });
            Assert.Throws<InvalidOperationException>(() => { builder.W = "mode"; });
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase(2.0, "server=localhost;waitQueueMultiple=2")]
        public void TestWaitQueueMultiple(double? multiple, string connectionString)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (multiple != null) { built.WaitQueueMultiple = multiple.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual(multiple ?? MongoDefaults.WaitQueueMultiple, builder.WaitQueueMultiple);
                Assert.AreEqual((multiple == null) ? MongoDefaults.WaitQueueSize : 0, builder.WaitQueueSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestWaitQueueMultiple_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueMultiple = -1.0; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueMultiple = 0.0; });
            builder.WaitQueueMultiple = 1.0;
        }

        [Test]
        [TestCase(null, "server=localhost")]
        [TestCase(123, "server=localhost;waitQueueSize=123")]
        public void TestWaitQueueSize(int? size, string connectionString)
        {
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (size != null) { built.WaitQueueSize = size.Value; }

            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, connectionString))
            {
                Assert.AreEqual((size == null) ? MongoDefaults.WaitQueueMultiple : 0.0, builder.WaitQueueMultiple);
                Assert.AreEqual(size ?? MongoDefaults.WaitQueueSize, builder.WaitQueueSize);
                Assert.AreEqual(connectionString, builder.ToString());
            }
        }

        [Test]
        public void TestWaitQueueSize_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueSize = -1; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueSize = 0; });
            builder.WaitQueueSize = 1;
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(500, "server=localhost;waitQueueTimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "server=localhost;waitQueueTimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "server=localhost;waitQueueTimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "server=localhost;waitQueueTimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "server=localhost;waitQueueTimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestWaitQueueTimeout(int? ms, string formatString, string[] values)
        {
            var waitQueueTimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (waitQueueTimeout != null) { built.WaitQueueTimeout = waitQueueTimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(waitQueueTimeout ?? MongoDefaults.WaitQueueTimeout, builder.WaitQueueTimeout);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestWaitQueueTimeout_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WaitQueueTimeout = TimeSpan.FromSeconds(-1); });
            builder.WaitQueueTimeout = TimeSpan.Zero;
            builder.WaitQueueTimeout = TimeSpan.FromSeconds(1);
        }

        [Test]
        [TestCase(null, "server=localhost", new[] { "" })]
        [TestCase(500, "server=localhost;wtimeout{0}", new[] { "=500ms", "=0.5", "=0.5s", "=00:00:00.5", "MS=500" })]
        [TestCase(30000, "server=localhost;wtimeout{0}", new[] { "=30s", "=30000ms", "=30", "=0.5m", "=00:00:30", "MS=30000" })]
        [TestCase(1800000, "server=localhost;wtimeout{0}", new[] { "=30m", "=1800000ms", "=1800", "=1800s", "=0.5h", "=00:30:00", "MS=1800000" })]
        [TestCase(3600000, "server=localhost;wtimeout{0}", new[] { "=1h", "=3600000ms", "=3600", "=3600s", "=60m", "=01:00:00", "MS=3600000" })]
        [TestCase(3723000, "server=localhost;wtimeout{0}", new[] { "=01:02:03", "=3723000ms", "=3723", "=3723s", "MS=3723000" })]
        public void TestWTimeout(int? ms, string formatString, string[] values)
        {
            var wtimeout = (ms == null) ? (TimeSpan?)null : TimeSpan.FromMilliseconds(ms.Value);
            var built = new MongoConnectionStringBuilder { Server = _localhost };
            if (wtimeout != null) { built.WTimeout = wtimeout.Value; }

            var canonicalConnectionString = string.Format(formatString, values[0]);
            foreach (var builder in EnumerateBuiltAndParsedBuilders(built, formatString, values))
            {
                Assert.AreEqual(wtimeout, builder.WTimeout);
                Assert.AreEqual(canonicalConnectionString, builder.ToString());
            }
        }

        [Test]
        public void TestWTimeout_Range()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            Assert.Throws<ArgumentOutOfRangeException>(() => { builder.WTimeout = TimeSpan.FromSeconds(-1); });
            builder.WTimeout = TimeSpan.Zero;
            builder.WTimeout = TimeSpan.FromSeconds(1);
        }

        [Test]
        public void TestWTimeout_WhenFireAndForgetIsTrue()
        {
            var builder = new MongoConnectionStringBuilder { Server = _localhost };
            builder.FireAndForget = true;
            builder.WTimeout = null;
            Assert.Throws<InvalidOperationException>(() => { builder.WTimeout = TimeSpan.Zero; });

            builder = new MongoConnectionStringBuilder { Server = _localhost };
#pragma warning disable 618
            builder.Safe = false;
#pragma warning restore
            builder.WTimeout = null;
            Assert.Throws<InvalidOperationException>(() => { builder.WTimeout = TimeSpan.Zero; });
        }

        // private methods
        private IEnumerable<MongoConnectionStringBuilder> EnumerateBuiltAndParsedBuilders(
            MongoConnectionStringBuilder built,
            string connectionString)
        {
            yield return built;
            yield return new MongoConnectionStringBuilder(connectionString);
        }

        private IEnumerable<MongoConnectionStringBuilder> EnumerateBuiltAndParsedBuilders(
            MongoConnectionStringBuilder built,
            string formatString,
            string[] values)
        {
            yield return built;
            foreach (var parsed in EnumerateParsedBuilders(formatString, values))
            {
                yield return parsed;
            }
        }

        private IEnumerable<MongoConnectionStringBuilder> EnumerateBuiltAndParsedBuilders(
            MongoConnectionStringBuilder built,
            string formatString,
            string[] values1,
            string[] values2)
        {
            yield return built;
            foreach (var parsed in EnumerateParsedBuilders(formatString, values1, values2))
            {
                yield return parsed;
            }
        }

        private IEnumerable<MongoConnectionStringBuilder> EnumerateParsedBuilders(
            string formatString,
            string[] values)
        {
            foreach (var v in values)
            {
                var connectionString = string.Format(formatString, v);
                yield return new MongoConnectionStringBuilder(connectionString);
            }
        }

        private IEnumerable<MongoConnectionStringBuilder> EnumerateParsedBuilders(
            string formatString,
            string[] values1,
            string[] values2)
        {
            foreach (var v1 in values1)
            {
                foreach (var v2 in values2)
                {
                    var connectionString = string.Format(formatString, v1, v2);
                    yield return new MongoConnectionStringBuilder(connectionString);
                }
            }
        }
    }
}
