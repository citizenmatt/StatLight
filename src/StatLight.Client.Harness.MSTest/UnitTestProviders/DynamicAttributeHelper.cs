﻿using Microsoft.Silverlight.Testing.UnitTesting.Metadata;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace StatLight.Client.Harness.Hosts.MSTest.UnitTestProviders
{
    public static class DynamicAttributeHelper
    {
        public static bool HasAttribute(this MemberInfo method, string attributeName)
        {
            return GetAttribute(method, attributeName) != null;
        }

        public static object GetAttribute(this MemberInfo method, string attributeName)
        {
            return GetAllAttributes(method, attributeName).FirstOrDefault();
        }

        public static object[] GetAllAttributes(this MemberInfo method, string attributeName)
        {
            return method.GetCustomAttributes(true)
                    .Where(w => w.ToString().Contains(attributeName)).ToArray();
        }

        public static object GetObjectPropertyValue(this object attributeInstance, string propertyName)
        {
            object value = null;

            if (attributeInstance != null)
            {
                var property = attributeInstance
                    .GetType()
                    .GetProperties()
                    .Where(f => f.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase))
                    .FirstOrDefault();

                if (property != null)
                    value = property.GetValue(attributeInstance, null);
            }

            return value;
        }

        public static bool TryGetTypes(this Assembly assembly, out IList<Type> types)
        {
            types = new List<Type>();
            try
            {
                types = assembly.GetTypes().ToList();
            }
            catch (ReflectionTypeLoadException rfex)
            {
                ReflectionInfoHelper.HandleReflectionTypeLoadException(rfex);
                return false;
            }

            return true;
        }

        public static ICollection<ITestClass> GetTestClasses(this
            Assembly assembly,
            Func<Type, bool> filter,
            Func<Type, ITestClass> createType)
        {
            IList<Type> types;
            if (assembly.TryGetTypes(out types))
            {
                //Server.Debug("Total Types Found [{0}] in Assembly [{1}]".FormatWith(types.Count, assembly.FullName));
                var tests = new List<ITestClass>();
                foreach (Type type in types.Where(filter))
                {
                    if (type.IsAbstract)
                        continue;

                    //Server.Debug("type={0}".FormatWith(type.FullName));
                    tests.Add(createType(type));
                }
                //Server.Debug("Total Types Found [{0}] in Assembly [{1}]".FormatWith(tests.Count, assembly.FullName));
                return tests;
            }
            return new List<ITestClass>();
        }

    }

}
