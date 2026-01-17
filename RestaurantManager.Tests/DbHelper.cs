using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RestaurantManager.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RestaurantManager.Tests
{
    public static class DbHelper
    {
        public static AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }
    }

    // Prosta implementacja sesji w pamięci
    public class MockSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new Dictionary<string, byte[]>();

        public string Id => "test-session-id";
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);
    }
}