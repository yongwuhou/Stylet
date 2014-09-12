﻿using StyletIoC.Creation;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace StyletIoC.Internal.Registrations
{
    /// <summary>
    /// Knows how to generate an IEnumerable{T}, which contains all implementations of T
    /// </summary>
    internal class GetAllRegistration : IRegistration
    {
        private readonly IRegistrationContext parentContext;

        public string Key { get; private set; }
        private readonly Type _type;
        public Type Type
        {
            get { return this._type; }
        }

        private Expression expression;
        private readonly object generatorLock = new object();
        private Func<IRegistrationContext, object> generator;

        public GetAllRegistration(Type type, IRegistrationContext parentContext, string key)
        {
            this.Key = key;
            this._type = type;
            this.parentContext = parentContext;
        }

        public Func<IRegistrationContext, object> GetGenerator()
        {
            if (this.generator != null)
                return this.generator;

            lock (this.generatorLock)
            {
                if (this.generator == null)
                {
                    var registrationContext = Expression.Parameter(typeof(IRegistrationContext), "registrationContext");
                    this.generator = Expression.Lambda<Func<IRegistrationContext, object>>(this.GetInstanceExpression(registrationContext), registrationContext).Compile();
                }
                return this.generator;
            }
        }

        public Expression GetInstanceExpression(ParameterExpression registrationContext)
        {
            if (this.expression != null)
                return this.expression;

            var listNew = Expression.New(this.Type);
            var instanceExpressions = this.parentContext.GetAllRegistrations(this.Type.GenericTypeArguments[0], this.Key, false).Select(x => x.GetInstanceExpression(registrationContext));
            Expression list = instanceExpressions.Any() ? (Expression)Expression.ListInit(listNew, instanceExpressions) : listNew;

            Interlocked.CompareExchange(ref this.expression, list, null);
            return this.expression;
        }

        public IRegistration CloneToContext(IRegistrationContext context)
        {
            throw new InvalidOperationException("should not be cloned");
        }
    }
}
