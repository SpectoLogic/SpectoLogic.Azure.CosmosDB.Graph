using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace SpectoLogic.Azure.Graph.Serializer
{
    public static class GraphSerializerFactory
    {
        private static IGraphContext myDefaultGraphContext = new MemoryGraph();
        private static Dictionary<IGraphContext, Dictionary<Type, object>> mySerializers = new Dictionary<IGraphContext, Dictionary<Type, object>>();
        private static MethodInfo CreateGraphSerializerMethodInfo = null;

        public static GraphSerializer<Y> CreateGraphSerializer<Y>() where Y : new()
        {
            return CreateGraphSerializer<Y>(myDefaultGraphContext);
        }

        public static GraphSerializer<Y> CreateGraphSerializer<Y>(IGraphContext context) where Y : new()
        {
            if (context == null) context = myDefaultGraphContext;
            if (!mySerializers.ContainsKey(context)) mySerializers.Add(context, new Dictionary<Type, object>());

            GraphSerializer<Y> result = null;
            if (mySerializers[context].ContainsKey(typeof(Y)))
            {
                return mySerializers[context][typeof(Y)] as GraphSerializer<Y>;
            }
            else
            {
                result = new GraphSerializer<Y>(context);
                mySerializers[context].Add(typeof(Y), result);
            }
            return result;
        }

        public static IGraphSerializer CreateGraphSerializer(IGraphContext context, Type itemType)
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
    }
}
