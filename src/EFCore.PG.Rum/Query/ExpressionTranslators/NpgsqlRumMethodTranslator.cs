using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

using NpgsqlTypes;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    internal class NpgsqlRumMethodTranslator : IMethodCallTranslator
    {
        static readonly Dictionary<MethodInfo, string> Functions = new Dictionary<MethodInfo, string>
        {
            [GetRuntimeMethod(nameof(NpgsqlRumLinqExtensions.Score), new[] { typeof(NpgsqlTsVector), typeof(NpgsqlTsQuery) })] = "rum_ts_score"
        };

        static readonly Dictionary<MethodInfo, string> DoubleReturningOperators = new Dictionary<MethodInfo, string>
        {
            [GetRuntimeMethod(nameof(NpgsqlRumLinqExtensions.Distance), new[] { typeof(NpgsqlTsVector), typeof(NpgsqlTsQuery) })] = "<=>"
        };

        static MethodInfo GetRuntimeMethod(string name, params Type[] parameters)
            => typeof(NpgsqlRumLinqExtensions).GetRuntimeMethod(name, parameters);

        readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;
        readonly RelationalTypeMapping _doubleMapping;

        public NpgsqlRumMethodTranslator(NpgsqlSqlExpressionFactory sqlExpressionFactory, IRelationalTypeMappingSource typeMappingSource)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
            _doubleMapping = typeMappingSource.FindMapping(typeof(double));
        }

        public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments)
        {
            if (Functions.TryGetValue(method, out var function))
                return _sqlExpressionFactory.Function(function, arguments, method.ReturnType);

            if (DoubleReturningOperators.TryGetValue(method, out var floatOperator))
                return new SqlCustomBinaryExpression(
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[0]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    floatOperator,
                    _doubleMapping.ClrType,
                    _doubleMapping);

            return null;
        }
    }
}
