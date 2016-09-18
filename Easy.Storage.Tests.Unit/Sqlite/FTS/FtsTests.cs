﻿namespace Easy.Storage.Tests.Unit.Sqlite.FTS
{
    using System.Data;
    using System.Data.SQLite;
    using System.Linq;
    using System.Threading.Tasks;
    using Easy.Storage.Common.Extensions;
    using Easy.Storage.Sqlite;
    using Easy.Storage.Sqlite.FTS;
    using NUnit.Framework;
    using Shouldly;
    using static FtsContext;

    [TestFixture]
    internal sealed class FtsTests
    {
        [Test]
        public void When_searching_for_non_existing_table()
        {
            using (var db = new SqliteDatabase("Data Source=:memory:"))
            {
                var selectAllTerm = Term<Log>.All;
                Should.Throw<SQLiteException>(async () => await db.SearchAsync(selectAllTerm))
                    .Message.ShouldBe("SQL logic error or missing database\r\nno such table: Log");
            }
        }

        [Test]
        public async Task When_searching_for_all_records_in_empty_table()
        {
            using (var db = new SqliteDatabase("Data Source=:memory:"))
            {
                await Given_a_logTtable_and_an_ftsTable(db.Connection);

                var selectAllTerm = Term<Log>.All;
                var result = await db.SearchAsync(selectAllTerm);
                result.ShouldBeEmpty();
            }
        }

        [Test]
        public async Task When_searching_for_all_records_in_a_table()
        {
            using (var db = new SqliteDatabase("Data Source=:memory:"))
            {
                await Given_a_logTtable_and_an_ftsTable(db.Connection);

                var logs = new[]
                {
                    new Log {Level = Level.Debug, Message = "There is a Cat"},
                    new Log {Level = Level.Debug, Message = "There is a Dog"},
                    new Log {Level = Level.Debug, Message = "There is a Cat and a Dog"}
                };

                var repo = db.GetRepository<Log>();
                await repo.InsertAsync(logs);

                var selectAllTerm = Term<Log>.All;
                var result = (await db.SearchAsync(selectAllTerm)).ToArray();
                result.Length.ShouldBe(3);
                result[0].Level.ShouldBe(logs[0].Level);
                result[0].Message.ShouldBe(logs[0].Message);

                result[1].Level.ShouldBe(logs[1].Level);
                result[1].Message.ShouldBe(logs[1].Message);
                       
                result[1].Level.ShouldBe(logs[1].Level);
                result[1].Message.ShouldBe(logs[1].Message);

                result[2].Level.ShouldBe(logs[2].Level);
                result[2].Message.ShouldBe(logs[2].Message);
            }
        }

        [Test]
        public async Task When_searching_for_all_records_in_a_table_and_matching_any_of_the_given_log_level()
        {
            using (var db = new SqliteDatabase("Data Source=:memory:"))
            {
                await Given_a_logTtable_and_an_ftsTable(db.Connection);

                var logs = new[]
                {
                    new Log {Level = Level.Debug, Message = "There is a Cat"},
                    new Log {Level = Level.Info, Message = "There is a Dog"},
                    new Log {Level = Level.Warn, Message = "There is a Cat and a Dog"}
                };

                var repo = db.GetRepository<Log>();
                await repo.InsertAsync(logs);

                var term = Term<Log>.All
                    .And(Match.Any, l => l.Level, Level.Debug, Level.Info);

                var result = (await db.SearchAsync(term)).ToArray();
                result.Length.ShouldBe(2);
                result[0].Level.ShouldBe(logs[0].Level);
                result[0].Message.ShouldBe(logs[0].Message);

                result[1].Level.ShouldBe(logs[1].Level);
                result[1].Message.ShouldBe(logs[1].Message);

                result[1].Level.ShouldBe(logs[1].Level);
                result[1].Message.ShouldBe(logs[1].Message);
            }
        }

        [Test]
        public async Task When_searching_for_all_records_in_a_table_and_matching_all_of_the_given_keywords()
        {
            using (var db = new SqliteDatabase("Data Source=:memory:"))
            {
                await Given_a_logTtable_and_an_ftsTable(db.Connection);

                var logs = new[]
                {
                    new Log {Level = Level.Debug, Message = "There is a Cat"},
                    new Log {Level = Level.Info, Message = "There is a Dog"},
                    new Log {Level = Level.Warn, Message = "There is a Cat and a Dog"}
                };

                var repo = db.GetRepository<Log>();
                await repo.InsertAsync(logs);

                var term = Term<Log>.All
                    .And(Match.All, l => l.Message, "Cat", "Dog");

                var result = (await db.SearchAsync(term)).ToArray();
                result.Length.ShouldBe(1);
                result[0].Level.ShouldBe(logs[2].Level);
                result[0].Message.ShouldBe(logs[2].Message);
            }
        }

        [Test]
        public async Task When_searching_for_all_records_in_a_table_and_matching_any_of_the_given_keywords()
        {
            using (var db = new SqliteDatabase("Data Source=:memory:"))
            {
                await Given_a_logTtable_and_an_ftsTable(db.Connection);

                var logs = new[]
                {
                    new Log {Level = Level.Debug, Message = "There is a Cat"},
                    new Log {Level = Level.Debug, Message = "There is a dog"},
                    new Log {Level = Level.Info, Message = "There is a big cat"},
                    new Log {Level = Level.Warn, Message = "There is a very big cat"}
                };

                var repo = db.GetRepository<Log>();
                await repo.InsertAsync(logs);

                var term = Term<Log>.All
                    .And(Match.Any, l => l.Message, "big", "Dog");

                var result = (await db.SearchAsync(term)).ToArray();
                result.Length.ShouldBe(3);
                result[0].Level.ShouldBe(logs[1].Level);
                result[0].Message.ShouldBe(logs[1].Message);

                result[1].Level.ShouldBe(logs[2].Level);
                result[1].Message.ShouldBe(logs[2].Message);

                result[2].Level.ShouldBe(logs[3].Level);
                result[2].Message.ShouldBe(logs[3].Message);
            }
        }

        [Test]
        public async Task When_searching_for_all_records_in_a_table_not_matching_all_of_the_given_keywords()
        {
            using (var db = new SqliteDatabase("Data Source=:memory:"))
            {
                await Given_a_logTtable_and_an_ftsTable(db.Connection);

                var logs = new[]
                {
                    new Log {Level = Level.Debug, Message = "There is a Cat"},
                    new Log {Level = Level.Info, Message = "There is a Dog"},
                    new Log {Level = Level.Warn, Message = "There is a Cat and a Dog"},
                    new Log {Level = Level.Warn, Message = "There is a Parrot"}
                };

                var repo = db.GetRepository<Log>();
                await repo.InsertAsync(logs);

                var term = Term<Log>.All
                    .AndNot(Match.All, l => l.Message, "Cat", "Dog");

                var result = (await db.SearchAsync(term)).ToArray();
                result.Length.ShouldBe(3);
                result[0].Level.ShouldBe(logs[0].Level);
                result[0].Message.ShouldBe(logs[0].Message);

                result[1].Level.ShouldBe(logs[1].Level);
                result[1].Message.ShouldBe(logs[1].Message);

                result[2].Level.ShouldBe(logs[3].Level);
                result[2].Message.ShouldBe(logs[3].Message);
            }
        }

        [Test]
        public async Task When_searching_for_all_records_in_a_table_not_matching_any_of_the_given_keywords()
        {
            using (var db = new SqliteDatabase("Data Source=:memory:"))
            {
                await Given_a_logTtable_and_an_ftsTable(db.Connection);

                var logs = new[]
                {
                    new Log {Level = Level.Debug, Message = "There is a Cat"},
                    new Log {Level = Level.Info, Message = "There is a Dog"},
                    new Log {Level = Level.Warn, Message = "There is a Cat and a Dog"},
                    new Log {Level = Level.Warn, Message = "There is a Parrot"}
                };

                var repo = db.GetRepository<Log>();
                await repo.InsertAsync(logs);

                var term = Term<Log>.All
                    .AndNot(Match.Any, l => l.Message, "Cat", "Dog");

                var result = (await db.SearchAsync(term)).ToArray();
                result.Length.ShouldBe(1);
                
                result[0].Level.ShouldBe(logs[3].Level);
                result[0].Message.ShouldBe(logs[3].Message);
            }
        }

        [Test]
        public async Task When_searching_for_all_records_in_a_table_matching_multiple_keywords()
        {
            using (var db = new SqliteDatabase("Data Source=:memory:"))
            {
                await Given_a_logTtable_and_an_ftsTable(db.Connection);

                var logs = new[]
                {
                    new Log {Level = Level.Debug, Message = "Linux can be fun"},
                    new Log {Level = Level.Info, Message = "Windows and linux are two operating systems"},
                    new Log {Level = Level.Warn, Message = "linux is a Unix like operating system"},
                    new Log {Level = Level.Fatal, Message = "software development in Mac OS operating system may be fun!"}
                };

                var repo = db.GetRepository<Log>();
                await repo.InsertAsync(logs);

                var term = Term<Log>.All;

                var result = (await db.SearchAsync(term)).ToArray();
                result.Length.ShouldBe(4);

                term.AndNot(Match.Any, l => l.Message, "Mac Os");

                result = (await db.SearchAsync(term)).ToArray();
                result.Length.ShouldBe(3);

                result[0].Level.ShouldBe(logs[0].Level);
                result[0].Message.ShouldBe(logs[0].Message);

                result[1].Level.ShouldBe(logs[1].Level);
                result[1].Message.ShouldBe(logs[1].Message);

                result[2].Level.ShouldBe(logs[2].Level);
                result[2].Message.ShouldBe(logs[2].Message);

                term.Clear();

                term
                    .AndNot(Match.Any, l => l.Message, "Mac Os")
                    .And(Match.Any, l => l.Message, "operating system", "fun");
                result = (await db.SearchAsync(term)).ToArray();
                result.Length.ShouldBe(2);

                result[0].Level.ShouldBe(logs[0].Level);
                result[0].Message.ShouldBe(logs[0].Message);

                result[1].Level.ShouldBe(logs[2].Level);
                result[1].Message.ShouldBe(logs[2].Message);

                term.Clear();

                term
                    .AndNot(Match.Any, l => l.Message, "Mac Os")
                    .And(Match.Any, l => l.Message, "operating system*", "fun");
                result = (await db.SearchAsync(term)).ToArray();
                result.Length.ShouldBe(3);

                result[0].Level.ShouldBe(logs[0].Level);
                result[0].Message.ShouldBe(logs[0].Message);

                result[1].Level.ShouldBe(logs[1].Level);
                result[1].Message.ShouldBe(logs[1].Message);

                result[2].Level.ShouldBe(logs[2].Level);
                result[2].Message.ShouldBe(logs[2].Message);
            }
        }

        private static async Task Given_a_logTtable_and_an_ftsTable(IDbConnection connection)
        {
            var tableSql = SqliteSqlGenerator.Table<Log>();
            await connection.ExecuteAsync(tableSql);
            var ftsTableSql = SqliteSqlGenerator.FtsTable<Log>();
            await connection.ExecuteAsync(ftsTableSql);
        }
    }
}