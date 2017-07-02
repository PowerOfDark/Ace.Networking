﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Ace.Networking
{
    public static class FastReflection
    {
        public static Action<T, object> BuildUntypedSetter<T>(PropertyInfo propertyInfo)
        {
            var targetType = propertyInfo.DeclaringType;
            var methodInfo = propertyInfo.GetSetMethod();
            var exTarget = Expression.Parameter(targetType, "t");
            var exValue = Expression.Parameter(typeof(object), "p");
            var exBody = Expression.Call(exTarget, methodInfo,
                Expression.Convert(exValue, propertyInfo.PropertyType));
            var lambda = Expression.Lambda<Action<T, object>>(exBody, exTarget, exValue);
            var action = lambda.Compile();
            return action;
        }

        public static Func<T, T2> BuildUntypedGetter<T, T2>(PropertyInfo propertyInfo)
        {
            var targetType = propertyInfo.DeclaringType;
            var methodInfo = propertyInfo.GetGetMethod();
            var returnType = methodInfo.ReturnType;
            var exTarget = Expression.Parameter(targetType, "t");
            var exBody = Expression.Call(exTarget, methodInfo);
            var exBody2 = Expression.Convert(exBody, typeof(T2));
            var lambda = Expression.Lambda<Func<T, T2>>(exBody2, exTarget);
            var action = lambda.Compile();
            return action;
        }
    }
}