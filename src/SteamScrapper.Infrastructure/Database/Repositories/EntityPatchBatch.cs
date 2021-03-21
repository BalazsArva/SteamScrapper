using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SteamScrapper.Infrastructure.Database.Context;

namespace SteamScrapper.Infrastructure.Database.Repositories
{
    public class EntityPatchBatch<TEntity, TEntityKey> : IDisposable where TEntity : class
    {
        private readonly SteamContext context;
        private readonly DbCommand dbCommand;

        private readonly Dictionary<TEntityKey, EntityPatchOperation<TEntity, TEntityKey>> entityPatchOperations = new();

        private bool disposedValue;

        public EntityPatchBatch(SteamContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));

            if (typeof(TEntityKey) != typeof(long))
            {
                throw new NotSupportedException("Currently only Int64 keys are supported.");
            }

            dbCommand = context.Set<TEntity>().CreateDbCommand();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public EntityPatchOperation<TEntity, TEntityKey> ForEntity(TEntityKey key)
        {
            if (!entityPatchOperations.ContainsKey(key))
            {
                entityPatchOperations[key] = new EntityPatchOperation<TEntity, TEntityKey>(key, context, dbCommand);
            }

            return entityPatchOperations[key];
        }

        public string ToPatchSqlCommandText()
        {
            var commandTextsPerId = entityPatchOperations.Values.Select(x => x.ToPatchSqlCommandText()).Where(x => !string.IsNullOrWhiteSpace(x));

            return string.Join('\n', commandTextsPerId);
        }

        public async Task<int> ExecuteAsync()
        {
            var completeCommandText = ToPatchSqlCommandText();

            if (string.IsNullOrWhiteSpace(completeCommandText))
            {
                return 0;
            }

            dbCommand.CommandText = completeCommandText;

            if (dbCommand.Connection.State == ConnectionState.Closed || dbCommand.Connection.State == ConnectionState.Broken)
            {
                await dbCommand.Connection.OpenAsync();
            }

            return await dbCommand.ExecuteNonQueryAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    dbCommand.Dispose();
                }

                disposedValue = true;
            }
        }
    }
}