using Microsoft.Azure.Graphs;
using Microsoft.Azure.Graphs.Elements;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpectoLogic.Azure.Graph.Serializer
{
    public class GraphSerializer<T, TIn, TOut> : IGraphSerializer where T : new()
    {
        public void AddDefinedPropertyListItem(GraphDefinedPropertyType propertyType, object targetInstance, object value)
        {
            throw new NotImplementedException();
        }

        public JObject ConvertToDocDBJObject(object poco)
        {
            throw new NotImplementedException();
        }

        public IGraphSerializer CreateGraphSerializerForItem(GraphDefinedPropertyType propertyType)
        {
            throw new NotImplementedException();
        }

        public IGraphSerializer CreateGraphSerializerForListItem(GraphDefinedPropertyType propertyType)
        {
            throw new NotImplementedException();
        }

        public IGraphSerializer CreateGraphSerializerForType(Type itemType)
        {
            throw new NotImplementedException();
        }

        public object CreateItemInstanceObject(string id)
        {
            throw new NotImplementedException();
        }

        public object CreateListItemInstance(GraphDefinedPropertyType propertyType, string id)
        {
            throw new NotImplementedException();
        }

        public object GetCustomProperty(string propertyName, object targetInstance)
        {
            throw new NotImplementedException();
        }

        public object GetDefinedProperty(GraphDefinedPropertyType propertyType, object targetInstance)
        {
            throw new NotImplementedException();
        }

        public bool IsEdge()
        {
            throw new NotImplementedException();
        }

        public bool IsVertex()
        {
            throw new NotImplementedException();
        }

        public void SetDefinedProperty(GraphDefinedPropertyType propertyType, object targetInstance, object value)
        {
            throw new NotImplementedException();
        }
    }

    public class GraphSerializer<T> : IGraphSerializer where T : new()
    {
        /// <summary>
        /// Contains the Reflection PropertyInfo of all defined Properties
        /// </summary>
        Dictionary<GraphDefinedPropertyType, PropertyInfo> myPI_Defined = new Dictionary<GraphDefinedPropertyType, PropertyInfo>();
        /// <summary>
        /// Contains all Reflection PropertyInfo of all custom Properties detected during the instantiation
        /// </summary>
        Dictionary<string, PropertyInfo> myPI_Custom = new Dictionary<string, PropertyInfo>();
        /// <summary>
        /// Contains a reference to the GraphClass Attribute if provided. If there was none provided it gets created automatically
        /// based on the available defined properties.
        /// </summary>
        GraphClassAttribute myClassAttribute = null;
        /// <summary>
        /// Contains a reference to the used Context. All GraphElements are only instantiated once (detected via ID) and stored in the COntext for quicker retrieval,
        /// richer data availabilty and prevention of duplicated entries like Edges that contain only partial information although they have been retrieved earlier or later.
        /// </summary>
        IGraphContext myContext;
        Type myTIn = null;     // Type of In-Vertex or In-Edge
        Type myTOut = null;    // Type of Out-Vertex or Out-Edge

        internal GraphSerializer(IGraphContext context)
        {
            myContext = context;
            Type TType = typeof(T);

            Type[] interfaces = TType.GetInterfaces();
            var vInterface = interfaces.Where(i => i.Name == "IVertex`2").FirstOrDefault();
            var eInterface = interfaces.Where(i => i.Name == "IEdge`2").FirstOrDefault();
            myClassAttribute = TType.GetCustomAttribute<GraphClassAttribute>();
            if (vInterface != null)
            {
                myClassAttribute.ElementType = GraphElementType.Vertex;
                Type[] genericArgs = vInterface.GetGenericArguments();
                myTIn = genericArgs[0];
                myTOut = genericArgs[1];
            }
            if (eInterface != null)
            {
                myClassAttribute.ElementType = GraphElementType.Edge;
                Type[] genericArgs = eInterface.GetGenericArguments();
                myTIn = genericArgs[0];
                myTOut = genericArgs[1];
            }

            #region Evaluate Properties 
            PropertyInfo[] propertyInfos = TType.GetProperties();
            foreach (PropertyInfo pi in propertyInfos)
            {
                GraphPropertyAttribute gpa = pi.GetCustomAttribute<GraphPropertyAttribute>();
                if ((gpa != null) && (gpa.DefinedProperty != GraphDefinedPropertyType.None))
                {
                    #region Assign Defined Property to Member
                    if (myPI_Defined.ContainsKey(gpa.DefinedProperty))
                    {
                        myPI_Custom.Add(myPI_Defined[gpa.DefinedProperty].Name, myPI_Defined[gpa.DefinedProperty]); // If set before by name move to custom properties
                    }
                    myPI_Defined.Add(gpa.DefinedProperty, pi);
                    #endregion
                }
                else
                {
                    GraphDefinedPropertyType detectedDefinedType = GraphDefinedPropertyType.Id;
                    bool definedDetected = true;
                    switch (pi.Name.ToLower())
                    {
                        case "id": { detectedDefinedType = GraphDefinedPropertyType.Id; } break;
                        case "label": { detectedDefinedType = GraphDefinedPropertyType.Label; } break;
                        case "inv": { detectedDefinedType = GraphDefinedPropertyType.InV; } break;
                        case "outv": { detectedDefinedType = GraphDefinedPropertyType.OutV; } break;
                        case "ine": { detectedDefinedType = GraphDefinedPropertyType.InE; } break;
                        case "oute": {detectedDefinedType = GraphDefinedPropertyType.OutE;} break;
                        default:
                            {
                                definedDetected = false;
                                myPI_Custom.Add(pi.Name, pi);
                            }
                            break;
                    }
                    if (definedDetected)
                    {
                        if (!myPI_Defined.ContainsKey(detectedDefinedType))
                            myPI_Defined.Add(detectedDefinedType, pi);
                        else
                            myPI_Custom.Add(pi.Name, pi);
                    }
                }
            }
            #endregion

            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.InV))
                myTIn = myPI_Defined[GraphDefinedPropertyType.InV].PropertyType;
            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.OutV))
                myTOut = myPI_Defined[GraphDefinedPropertyType.OutV].PropertyType;
            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.InE))
                myTIn = myPI_Defined[GraphDefinedPropertyType.InE].PropertyType.GenericTypeArguments[0]; // Must be a generic List
            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.OutE))
                myTOut = myPI_Defined[GraphDefinedPropertyType.OutE].PropertyType.GenericTypeArguments[0]; // Must be a generic List

            #region Evaluate if given Type is Vertex or Edge
            if (myClassAttribute == null)
            {

                if ((myPI_Defined.ContainsKey(GraphDefinedPropertyType.InV)) || (myPI_Defined.ContainsKey(GraphDefinedPropertyType.OutV)))
                    myClassAttribute = new GraphClassAttribute() { ElementType = GraphElementType.Edge };
                else
                    myClassAttribute = new GraphClassAttribute() { ElementType = GraphElementType.Vertex };
            }
            #endregion
            if (myClassAttribute.TypeKey==null)
                myClassAttribute.TypeKey = TType.Assembly.FullName + "|" + TType.FullName;
        }

        public IGraphContext GraphContext
        {
            get
            {
                return myContext;
            }
        }

        #region Helpers to work with defined properties
        public void SetDefinedProperty(GraphDefinedPropertyType propertyType, object targetInstance, object value)
        {
            if (myPI_Defined.ContainsKey(propertyType))
                myPI_Defined[propertyType].SetValue(targetInstance, value);
        }
        public object GetDefinedProperty(GraphDefinedPropertyType propertyType, object targetInstance)
        {
            if (myPI_Defined.ContainsKey(propertyType))
                return myPI_Defined[propertyType].GetValue(targetInstance);
            else
                return null;
        }
        public void AddDefinedPropertyListItem(GraphDefinedPropertyType propertyType, object targetInstance, object value)
        {
            IList targetList = myPI_Defined[propertyType].GetValue(targetInstance) as IList;
            if (targetList == null) // If the List is not yet created we need to create it.
            {
                targetList = this.CreateListInstance(propertyType);
                this.SetDefinedProperty(propertyType, targetInstance, targetList);
            }
            if (!targetList.Contains(value)) targetList.Add(value); // Only add if not already there
        }
        #endregion

        #region Helpers to work with custom properties
        public void SetCustomProperty(string propertyName, object targetInstance, object value)
        {
            if (myPI_Custom.ContainsKey(propertyName))
                myPI_Custom[propertyName].SetValue(targetInstance, value);
        }
        public object GetCustomProperty(string propertyName, object targetInstance)
        {
            if (myPI_Custom.ContainsKey(propertyName))
                return myPI_Custom[propertyName].GetValue(targetInstance);
            else
                return null;
        }
        private GraphProperty GetOrCreateGraphProperty(string propertyName, object targetInstance)
        {
            GraphProperty graphProp = null;
            object untypedGraphPropertyValue = myPI_Custom[propertyName].GetValue(targetInstance);
            if (untypedGraphPropertyValue == null)
            {
                graphProp = new GraphProperty();
                myPI_Custom[propertyName].SetValue(targetInstance, graphProp);
            }
            else
                graphProp = (GraphProperty)untypedGraphPropertyValue;
            graphProp.Name = propertyName;
            return graphProp;
        }
        public void SetCustomVertexProperty(string propertyName, object targetInstance, VertexProperty value)
        {
            if (myPI_Custom.ContainsKey(propertyName))
            {
                if (myPI_Custom[propertyName].PropertyType == typeof(GraphProperty))
                {
                    GraphProperty gP = GetOrCreateGraphProperty(propertyName, targetInstance);
                    GraphProperty.GraphPropertyValue propValue = new GraphProperty.GraphPropertyValue() { Id = value.Id.ToString(), Value = value.Value };
                    if (gP.Values.ContainsKey(value.Id.ToString())) gP.Values.Remove(value.Id.ToString());
                    gP.Values.Add(value.Id.ToString(), propValue);
                    // Read MetaData and add those too
                    PropertyInfo metaProperties = typeof(VertexProperty).GetProperty("Properties", BindingFlags.Instance | BindingFlags.NonPublic);
                    KeyedCollection<string, Property> col = (KeyedCollection<string, Property>)metaProperties.GetValue(value);
                    foreach (Property metaProp in col)
                        propValue.Meta.Add(metaProp.Key, metaProp.Value);
                }
                else
                    myPI_Custom[propertyName].SetValue(targetInstance, value.Value);
            }
        }
        public void SetCustomEdgeProperty(string propertyName, object targetInstance, Microsoft.Azure.Graphs.Elements.Property value)
        {
            if (myPI_Custom.ContainsKey(propertyName))
            {
                if (myPI_Custom[propertyName].PropertyType == typeof(GraphProperty))
                {
                    GraphProperty gP = GetOrCreateGraphProperty(propertyName, targetInstance);
                    GraphProperty.GraphPropertyValue propValue = new GraphProperty.GraphPropertyValue() { Id = value.Key, Value = value.Value };
                    if (gP.Values.ContainsKey(value.Key)) gP.Values.Remove(value.Key);
                    gP.Values.Add(value.Key, propValue);
                }
                else
                    myPI_Custom[propertyName].SetValue(targetInstance, value.Value);
            }
        }
        #endregion

        public bool IsEdge()
        {
            if ((myClassAttribute != null) && (myClassAttribute.ElementType == GraphElementType.Edge)) return true;
            return false;
        }
        public bool IsVertex()
        {
            return !IsEdge();
        }

        public bool IsSerializeTypeInformation()
        {
            return myClassAttribute.SerializeTypeInformation;
        }

        /// <summary>
        /// Deserializes a given GraphSON to either a list of instances of
        ///     Microsoft.Azure.Graphs.Elements.Vertex or 
        ///     Microsoft.Azure.Graphs.Elements.Edge
        /// This method uses the internal GraphSON Vertex Converter of Microsoft.
        /// </summary>
        /// <param name="graphSON"></param>
        /// <returns></returns>
        public List<T> DeserializeGraphSON(string graphSON)
        {
            List<T> result = new List<T>();
            JsonConverter converter = CreateVertexConverter();
            if (IsVertex())
            {
                List<Vertex> vertices = JsonConvert.DeserializeObject<List<Vertex>>(graphSON, converter);
                foreach (Vertex v in vertices)
                    result.Add(this.Convert(v));
            }
            else
            {
                List<Edge> edges = JsonConvert.DeserializeObject<List<Edge>>(graphSON, converter);
                foreach (Edge e in edges)
                    result.Add(this.Convert(e));
            }
            return result;
        }

        /// <summary>
        /// Unfortunatly the VertexConverter which is used my Microsoft.Azure.Graphs is internal only.
        /// In order to avoid a reimplementation of a GraphSON Converter we access it with reflection.
        /// </summary>
        /// <returns></returns>
        private JsonConverter CreateVertexConverter()
        {
            Type graphCommandType = typeof(GraphCommand);
            Type vertexConverterType = graphCommandType.Assembly.GetType("Microsoft.Azure.Graphs.Elements.VertexConverter");
            JsonConverter converter = Activator.CreateInstance(vertexConverterType) as JsonConverter;
            return converter;
        }

        /// <summary>
        /// Constructs a JObject that reassembles the representation of a GraphElement (Vertex or Edge) in CosmosDB
        /// </summary>
        /// <param name="poco"></param>
        /// <returns></returns>
        private JObject ConvertToDocDBJObject(T poco)
        {
            JObject jOutput = new JObject(
                        new JProperty("id", this.GetDefinedProperty(GraphDefinedPropertyType.Id, poco).ToString()),
                        new JProperty("label", this.GetDefinedProperty(GraphDefinedPropertyType.Label, poco).ToString())
                    );
            if (IsVertex())
            {
                if (this.IsSerializeTypeInformation())
                {
                    jOutput.Add(new JProperty("_type", new[]{ new JObject(
                                new JProperty("id",Guid.NewGuid().ToString("D")),
                                new JProperty("_value",myClassAttribute.TypeKey)
                            ) }));
                }
                foreach (KeyValuePair<string, PropertyInfo> cp in this.myPI_Custom)
                {
                    PropertyInfo pi = cp.Value;
                    if (this.GetCustomProperty(pi.Name, poco) != null)
                    {
                        if (pi.PropertyType == typeof(GraphProperty))
                        {
                            GraphProperty gp = (GraphProperty)this.GetCustomProperty(pi.Name, poco);
                            jOutput.Add(new JProperty(gp.Name, JArray.FromObject(gp.Values.Values.ToList<GraphProperty.GraphPropertyValue>())));
                        }
                        else
                        {
                            jOutput.Add(new JProperty(pi.Name, new[]{ new JObject(
                                new JProperty("id",Guid.NewGuid().ToString("D")),
                                new JProperty("_value",this.GetCustomProperty(pi.Name, poco))
                            ) }));
                        }
                    }
                }
            }
            else
            {
                object outV = this.GetDefinedProperty(GraphDefinedPropertyType.OutV, poco);
                IGraphSerializer outVSerial = outV != null ? CreateGraphSerializerForType(outV.GetType()) : null;

                object inV = this.GetDefinedProperty(GraphDefinedPropertyType.InV, poco);
                IGraphSerializer inVSerial = inV != null ? CreateGraphSerializerForType(inV.GetType()) : null;

                jOutput.Add(new JProperty("_isEdge", "true"));
                if (inVSerial != null)
                {
                    jOutput.Add(new JProperty("_sink", inVSerial.GetDefinedProperty(GraphDefinedPropertyType.Id, inV)));
                    jOutput.Add(new JProperty("_sinkLabel", inVSerial.GetDefinedProperty(GraphDefinedPropertyType.Label, inV)));
                }
                if (outVSerial != null)
                {
                    jOutput.Add(new JProperty("_vertexId", outVSerial.GetDefinedProperty(GraphDefinedPropertyType.Id, outV)));
                    jOutput.Add(new JProperty("_vertexLabel", outVSerial.GetDefinedProperty(GraphDefinedPropertyType.Label, outV)));
                }

                if (this.IsSerializeTypeInformation())
                    jOutput.Add(new JProperty("_type", myClassAttribute.TypeKey));

                foreach (KeyValuePair<string, PropertyInfo> cp in this.myPI_Custom)
                {
                    PropertyInfo pi = cp.Value;
                    if (this.GetCustomProperty(pi.Name, poco) != null)
                    {
                        jOutput.Add(new JProperty(pi.Name, this.GetCustomProperty(pi.Name, poco)));
                    }
                }
            }
            return jOutput;
        }

        /// <summary>
        /// Untyped verion of JObject ConvertToDocDBJObject(T poco) to satisfy interface
        /// </summary>
        /// <param name="poco"></param>
        /// <returns></returns>
        public JObject ConvertToDocDBJObject(object poco)
        {
            JObject result = this.ConvertToDocDBJObject((T)poco);
            return result;
        }

        /// <summary>
        /// Converts a Vertex Object to a typed custom object
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public T Convert(Vertex v)
        {
            // Creates or fetches Vertex
            T resultVertex = CreateItemInstance(v.Id.ToString());
            // Propulate lable and custom properties
            this.SetDefinedProperty(GraphDefinedPropertyType.Label, resultVertex, v.Label);
            foreach (var vp in v.GetVertexProperties())
                SetCustomVertexProperty(vp.Key, resultVertex, vp);
            // If you used GraphSON instead of GraphSONCompact you might get additional references to InEdges
            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.InE))
            {
                foreach (var ve in v.GetInEdges())
                {
                    // Create or fetch the In-Edge 
                    IGraphSerializer inEdgeSerializer = CreateGraphSerializerForListItem(GraphDefinedPropertyType.InE);
                    object edge = CreateListItemInstance(GraphDefinedPropertyType.InE, ve.Id.ToString());
                    // Populate Label of In-Edge and set the In-Vertex of the Edge to the resultVertex
                    inEdgeSerializer.SetDefinedProperty(GraphDefinedPropertyType.Label, edge, ve.Label);
                    inEdgeSerializer.SetDefinedProperty(GraphDefinedPropertyType.InV, edge, resultVertex); // InV is the result

                    // ve also contains the id of the OutVertex
                    // Create or fetch the Out-Vertex
                    IGraphSerializer vertexSerializer = inEdgeSerializer.CreateGraphSerializerForItem(GraphDefinedPropertyType.OutV);
                    object outvertex = vertexSerializer.CreateItemInstanceObject(ve.OutVertexId.ToString()); // TODO: Why is this an object?
                    // Make sure the Out-Edge of the Vertex is set to the Edge
                    vertexSerializer.AddDefinedPropertyListItem(GraphDefinedPropertyType.OutE, outvertex, edge);
                    // Set the OutV property of the edge with the just fetched OutV
                    inEdgeSerializer.SetDefinedProperty(GraphDefinedPropertyType.OutV, edge, outvertex); // OUTV we just created

                    // Add the incomming Edge to the collection of incomming edges of the resultVertex
                    this.AddDefinedPropertyListItem(GraphDefinedPropertyType.InE, resultVertex, edge);
                }
            }
            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.OutE))
            {
                foreach (var ve in v.GetOutEdges())
                {

                    IGraphSerializer serializer = CreateGraphSerializerForListItem(GraphDefinedPropertyType.OutE);
                    object edge = CreateListItemInstance(GraphDefinedPropertyType.OutE, ve.Id.ToString()); // TODO: Why is this an object?
                    serializer.SetDefinedProperty(GraphDefinedPropertyType.Label, edge, ve.Label);
                    serializer.SetDefinedProperty(GraphDefinedPropertyType.OutV, edge, resultVertex); // outV is the result

                    IGraphSerializer vertexSerializer = serializer.CreateGraphSerializerForItem(GraphDefinedPropertyType.InV);
                    object inVertex = vertexSerializer.CreateItemInstanceObject(ve.InVertexId.ToString());// TODO: Why is this an object?
                    vertexSerializer.AddDefinedPropertyListItem(GraphDefinedPropertyType.InE, inVertex, edge);
                    serializer.SetDefinedProperty(GraphDefinedPropertyType.InV, edge, inVertex); // OUTV we just created

                    this.AddDefinedPropertyListItem(GraphDefinedPropertyType.OutE, resultVertex, edge);
                }
            }
            return resultVertex;
        }

        /// <summary>
        /// Converts an Edge Object to a typed custom object
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public T Convert(Edge e)
        {
            /// Create or fetch the Edge Instance
            T resultEdge = CreateItemInstance(e.Id.ToString());
            /// (Re)populate Lable and custom properties
            this.SetDefinedProperty(GraphDefinedPropertyType.Label, resultEdge, e.Label);
            foreach (var ep in e.GetProperties())
                SetCustomEdgeProperty(ep.Key, resultEdge, ep);

            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.InV))
            {
                /// Try to create/fetch the referenced In-Vertex
                IGraphSerializer serializer = CreateGraphSerializerForItem(GraphDefinedPropertyType.InV);
                object inVertex = serializer.CreateItemInstanceObject(e.InVertexId.ToString()); // TODO: Why is this an object?
                serializer.SetDefinedProperty(GraphDefinedPropertyType.Label, inVertex, e.InVertexLabel);
                serializer.AddDefinedPropertyListItem(GraphDefinedPropertyType.InE, inVertex, resultEdge);
                /// Set the created In-Vertex as InV-Property
                this.SetDefinedProperty(GraphDefinedPropertyType.InV, resultEdge, inVertex);
            }
            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.OutV))
            {
                /// Try to create/fetch the referenced Out-Vertex
                IGraphSerializer serializer = CreateGraphSerializerForItem(GraphDefinedPropertyType.OutV);
                object outVertex = serializer.CreateItemInstanceObject(e.OutVertexId.ToString()); // TODO: Why is this an object?
                serializer.SetDefinedProperty(GraphDefinedPropertyType.Label, outVertex, e.OutVertexLabel);
                serializer.AddDefinedPropertyListItem(GraphDefinedPropertyType.OutE, outVertex, resultEdge);
                /// Set the created Out-Vertex as OutV-Property
                this.SetDefinedProperty(GraphDefinedPropertyType.OutV, resultEdge, outVertex);
            }
            return resultEdge;
        }

        /// <summary>
        /// Identifies the type T of a List<T> property and creates an instance
        /// of T, sets the defined property id, if the instance is not found
        /// in the GraphContext. Otherwise it is fetched from the GraphContext.
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private object CreateListItemInstance(PropertyInfo propertyInfo, string id)
        {
            if (myContext[id] != null) return myContext[id];
            Type listElementType = propertyInfo.PropertyType.GenericTypeArguments[0];
            object item = Activator.CreateInstance(listElementType); // Create Edge
            myContext[id] = item;

            IGraphSerializer serializer = CreateGraphSerializerForListItem(listElementType);
            serializer.SetDefinedProperty(GraphDefinedPropertyType.Id, item, id);

            return item;
        }

        /// <summary>
        /// Shortcut for defined Properties of "object CreateListItemInstance(PropertyInfo propertyInfo, string id)"
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public object CreateListItemInstance(GraphDefinedPropertyType propertyInfo, string id)
        {
            return CreateListItemInstance(myPI_Defined[propertyInfo], id);
        }

        /// <summary>
        /// Creates a new instance of type T and sets the defined property ID if it cannot be already 
        /// found in the GraphContext. Otherwise it is fetched from the GraphContext. 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private T CreateItemInstance(string id)
        {
            if (myContext[id] != null) return (T)myContext[id];
            T instance = new T();
            this.SetDefinedProperty(GraphDefinedPropertyType.Id, instance, id);
            myContext[id] = instance;
            return instance;
        }

        /// <summary>
        /// Untyped version of T CreateItemInstance(string id) to satisfy Interface
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public object CreateItemInstanceObject(string id)
        {
            return CreateItemInstance(id);
        }

        /// <summary>
        /// See CreateListInstance(PropertyInfo propertyInfo) - Shortcut for Defined Properties
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        private IList CreateListInstance(GraphDefinedPropertyType propertyType)
        {
            return CreateListInstance(myPI_Defined[propertyType]);
        }

        /// <summary>
        /// Creates a generic List<T> instance by a given PropertyInfo. If a class has a property defined like this "List<Path> OutE {get; set;}"
        /// and PropertyInfo was retrieved via reflection we can create an instance of this exact generic List
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        private IList CreateListInstance(PropertyInfo propertyInfo)
        {
            Type listElementType = propertyInfo.PropertyType.GenericTypeArguments[0];
            var list = (IList)typeof(List<>)
              .MakeGenericType(listElementType)
              .GetConstructor(Type.EmptyTypes)
              .Invoke(null);
            return list;
        }

        /// <summary>
        /// Detects the type of an ListItem of a defined property (InE or OutE) and creates a GraphSerializer for that
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public IGraphSerializer CreateGraphSerializerForListItem(GraphDefinedPropertyType propertyType)
        {
            Type listElementType = myPI_Defined[propertyType].PropertyType.GenericTypeArguments[0];
            return GraphSerializerFactory.CreateGraphSerializer(myContext, listElementType);
        }
        /// <summary>
        /// Detects the type of the defined property (OutV or InV) and creates a GraphSerializer object
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public IGraphSerializer CreateGraphSerializerForItem(GraphDefinedPropertyType propertyType)
        {
            Type itemType = myPI_Defined[propertyType].PropertyType;
            return GraphSerializerFactory.CreateGraphSerializer(myContext, itemType);
        }
        /// <summary>
        /// Helper to create a GraphSerializer for a specific type passing the local GraphContext
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public IGraphSerializer CreateGraphSerializerForListItem(Type propertyType)
        {
            return GraphSerializerFactory.CreateGraphSerializer(myContext, propertyType);
        }
        public IGraphSerializer CreateGraphSerializerForType(Type itemType)
        {
            return GraphSerializerFactory.CreateGraphSerializer(myContext, itemType);
        }
    }
}
