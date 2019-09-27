using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal
{
    internal class NpgsqlRumMethodTranslator : IMethodCallTranslator
    {
        readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;

        public NpgsqlRumMethodTranslator(NpgsqlSqlExpressionFactory sqlExpressionFactory, IRelationalTypeMappingSource typeMappingSource)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        public SqlExpression Translate(SqlExpression instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments)
        {
            if (method.DeclaringType == typeof(NpgsqlRumLinqExtensions))
            {
                if (method.Name == nameof(NpgsqlRumLinqExtensions.Distance))
                {
                    return BinaryOperator("<=>");
                }

                if (method.Name == nameof(NpgsqlRumLinqExtensions.Score))
                {
                    return arguments.Count switch
                    {
                        2 => _sqlExpressionFactory.Function("rum_ts_score", arguments, method.ReturnType, _sqlExpressionFactory.FindMapping(method.ReturnType)),

                        _ => throw new ArgumentException($"Invalid method overload for rum_ts_score")
                    };
                }
            }

            return null;

            #pragma warning disable EF1001
            SqlCustomBinaryExpression BinaryOperator(string @operator)
            {
                var inferredMapping = ExpressionExtensions.InferTypeMapping(arguments[0], arguments[1]);
                return new SqlCustomBinaryExpression(
                    _sqlExpressionFactory.ApplyTypeMapping(arguments[0], inferredMapping),
                    _sqlExpressionFactory.ApplyTypeMapping(arguments[1], inferredMapping),
                    @operator,
                    method.ReturnType,
                    inferredMapping);
            }
            #pragma warning restore EF1001
        }
    }
}
