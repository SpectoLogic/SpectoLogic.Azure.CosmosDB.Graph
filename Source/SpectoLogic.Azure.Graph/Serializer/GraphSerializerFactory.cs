using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.Azure.Graphs.Elements;

namespace SpectoLogic.Azure.Graph.Serializer
{
    /// <summary>
    /// The GraphSerializerFactory allows to create instances of GraphSerializer with various factory methods.
    /// </summary>
    public static class GraphSerializerFactory
    {
        private static IGraphContext myDefaultGraphContext = new MemoryGraph();
        private static Dictionary<IGraphContext, Dictionary<Type, object>> mySerializers = new Dictionary<IGraphContext, Dictionary<Type, object>>();
        private static Dictionary<string, Type> myTypes = new Dictionary<string, Type>();
        private static MethodInfo CreateGraphSerializerMethodInfo = null;

        internal static IGraphSerializer<T> CreateGraphSerializer<T>() where T : new()
        {
            return CreateGraphSerializer<T>(myDefaultGraphContext);
        }

        public static IGraphSerializer<T> CreateGraphSerializer<T>(IGraphContext context) where T : new()
        {
            if (context == null) context = myDefaultGraphContext;
            if (!mySerializers.ContainsKey(context)) mySerializers.Add(context, new Dictionary<Type, object>());

            GraphSerializer<T> result = null;
            if (mySerializers[context].ContainsKey(typeof(T)))
            {
                return mySerializers[context][typeof(T)] as GraphSerializer<T>;
            }
            else
            {
                result = new GraphSerializer<T>(context);
                mySerializers[context].Add(typeof(T), result);
            }
            return result;
        }

        internal static IGraphSerializer CreateGraphSerializer(IGraphContext context, Type itemType)
        {
            if (context == null) context = myDefaultGraphContext;
            if (CreateGraphSerializerMethodInfo == null)
            {
                MethodInfo[] methods = typeof(GraphSerializerFactory).GetMethods();
                foreach (MethodInfo mi in methods.Where(m => m.Name == "CreateGraphSerializer" && m.IsGenericMethod == true))
                {
                    var parameters = mi.GetParameters();
                    if (parameters.Length > 0)
                    {
                        CreateGraphSerializerMethodInfo = mi; break;
                    }
                }
            }
            if (!mySerializers.ContainsKey(context)) mySerializers.Add(context, new Dictionary<Type, object>());
            if (mySerializers[context].ContainsKey(itemType))
                return mySerializers[context][itemType] as IGraphSerializer;

            MethodInfo generic = CreateGraphSerializerMethodInfo.MakeGenericMethod(itemType);
            IGraphSerializer serializer = generic.Invoke(null, new object[] { context }) as IGraphSerializer;
            return serializer;
        }

        /// <summary>
        /// Allows to create a GraphSerializer for a Type that is defined as
        /// a string which consists out of AssemblyFullName and TypeFullname separated through an |-character
        /// </summary>
        /// <param name="context"></param>
        /// <param name="itemTypeString"></param>
        /// <returns></returns>
        internal static IGraphSerializer CreateGraphSerializer(IGraphContext context, string itemTypeString)
        {
            Type itemType = null;
            if (myTypes.ContainsKey(itemTypeString))
                itemType = myTypes[itemTypeString];
            else
            {
                string[] types = itemTypeString.Split('|');
                string fullAssemblyName = types[0];
                string fullTypeName = types[1];
                Assembly assembly = Assembly.Load(fullAssemblyName);
                itemType = assembly.GetType(fullTypeName);
                myTypes.Add(itemTypeString, itemType);
            }
            return CreateGraphSerializer(context, itemType);
        }
    }
}
