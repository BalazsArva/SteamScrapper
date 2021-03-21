using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SteamScrapper.Infrastructure.Database.Context;

namespace SteamScrapper.Infrastructure.Database.Repositories
{
    public class EntityPatchOperation<TEntity, TEntityKey> where TEntity : class
    {
        private readonly TEntityKey key;
        private readonly SteamContext context;
        private readonly DbCommand dbCommand;
        private readonly DbType keyDbType;

        private readonly Dictionary<string, DbParameter> parameters = new();

        public EntityPatchOperation(TEntityKey key, SteamContext context, DbCommand dbCommand)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.dbCommand = dbCommand ?? throw new ArgumentNullException(nameof(dbCommand));

            if (typeof(TEntityKey) == typeof(long))
            {
                keyDbType = DbType.Int64;
            }
            else
            {
                throw new NotSupportedException("Currently only Int64 keys are supported.");
            }

            this.key = key;
        }

        public string ToPatchSqlCommandText()
        {
            var columns = parameters.Keys.ToList();

            if (columns.Count == 0)
            {
                return string.Empty;
            }

            var schema = GetSchemaName() ?? "dbo";
            var table = GetTableName();

            var commandTextBuilder = new StringBuilder($"UPDATE [{schema}].[{table}] SET ");

            for (var i = 0; i < columns.Count; ++i)
            {
                var columnName = columns[i];
                var parameter = parameters[columnName];

                commandTextBuilder.Append($" [{columnName}] = @{parameter.ParameterName}");

                if (i < columns.Count - 1)
                {
                    commandTextBuilder.Append(',');
                }

                // Add the parameter here and not in the Patch(...) methods, because it's easier to handle multiple patches for the same id and property (latest wins).
                // If we did that in the Patch(...) methods, we'd have to deal with possibly colliding parameter names.
                dbCommand.Parameters.Add(parameter);
            }

            var idParameter = dbCommand.CreateParameter();

            idParameter.ParameterName = $"{schema}_{table}_{key}_PK";
            idParameter.Value = key;
            idParameter.DbType = keyDbType;
            idParameter.Direction = ParameterDirection.Input;

            dbCommand.Parameters.Add(idParameter);

            commandTextBuilder.Append($" WHERE [{GetPrimaryKeyColumnName()}] = @{idParameter.ParameterName}");

            return commandTextBuilder.ToString();
        }

        public EntityPatchOperation<TEntity, TEntityKey> Patch(Expression<Func<TEntity, bool>> columnSelector, bool newValue)
        {
            if (columnSelector is null)
            {
                throw new ArgumentNullException(nameof(columnSelector));
            }

            Patch(columnSelector, newValue, DbType.Boolean);

            return this;
        }

        public EntityPatchOperation<TEntity, TEntityKey> Patch(Expression<Func<TEntity, string>> columnSelector, string newValue)
        {
            if (columnSelector is null)
            {
                throw new ArgumentNullException(nameof(columnSelector));
            }

            Patch(columnSelector, newValue, DbType.String);

            return this;
        }

        public EntityPatchOperation<TEntity, TEntityKey> Patch(Expression<Func<TEntity, DateTime>> columnSelector, DateTime newValue)
        {
            if (columnSelector is null)
            {
                throw new ArgumentNullException(nameof(columnSelector));
            }

            Patch(columnSelector, newValue, DbType.DateTime2);

            return this;
        }

        private EntityPatchOperation<TEntity, TEntityKey> Patch<TValue>(Expression<Func<TEntity, TValue>> columnSelector, TValue newValue, DbType dbType)
        {
            if (columnSelector is null)
            {
                throw new ArgumentNullException(nameof(columnSelector));
            }

            // We assume all expressions are one-step member access expressions.
            if (columnSelector.Body.NodeType != ExpressionType.MemberAccess)
            {
                throw new NotSupportedException("This method only support simple member access expressions on the expression's parameter.");
            }

            var memberExpression = (MemberExpression)columnSelector.Body;
            var entityType = context.Model.GetEntityTypes(typeof(TEntity)).Single();

            var tableName = GetTableName();
            var schemaName = GetSchemaName();

            var columnName = entityType
                .FindProperty(memberExpression.Member)
                .GetColumnName(StoreObjectIdentifier.Table(tableName, schemaName));

            var dbParameter = dbCommand.CreateParameter();

            dbParameter.ParameterName = $"{schemaName}_{tableName}_{key}_{memberExpression.Member.Name}";
            dbParameter.Value = newValue == null ? DBNull.Value : newValue;
            dbParameter.DbType = dbType;
            dbParameter.Direction = ParameterDirection.Input;

            parameters[columnName] = dbParameter;

            return this;
        }

        private string GetPrimaryKeyColumnName()
        {
            var entityType = context.Model.GetEntityTypes(typeof(TEntity)).Single();

            return entityType.FindPrimaryKey().Properties[0].Name;
        }

        private string GetTableName()
        {
            var entityType = context.Model.GetEntityTypes(typeof(TEntity)).Single();

            return entityType.GetTableName();
        }

        private string GetSchemaName()
        {
            var entityType = context.Model.GetEntityTypes(typeof(TEntity)).Single();
            var schema = entityType.GetSchema();

            if (string.IsNullOrWhiteSpace(schema))
            {
                return null;
            }

            return schema;
        }
    }
}