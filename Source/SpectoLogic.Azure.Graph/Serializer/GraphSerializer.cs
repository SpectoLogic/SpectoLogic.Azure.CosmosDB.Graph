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
    internal class GraphSerializer
    {
        internal static string GetTypeKey(Type targetType)
        {
            return targetType.Assembly.FullName + "|" + targetType.FullName;
        }
        internal static string GetTypePropertyString(Edge e, out string invTypeString, out string outvTypeString)
        {
            Property _type = null;
            Property _inType = null;
            Property _outType = null;

            // Would be great if one could test if this property was there
            try { _type = e.GetProperty("_type"); } catch (Exception) { }
            try { _inType = e.GetProperty("_typeIn"); } catch (Exception) { }
            try { _outType = e.GetProperty("_typeOut"); } catch (Exception) { }
            if (_inType != null) invTypeString = _inType.Value.ToString(); else invTypeString = String.Empty;
            if (_outType != null) outvTypeString = _outType.Value.ToString(); else outvTypeString = String.Empty;

            if (_type == null) return String.Empty;
            return _type.Value.ToString();
        }
        internal static string GetTypePropertyString(Vertex v)
        {
            VertexProperty _type = null;
            // Would be great if one could test if this property was there
            try { _type = v.GetVertexProperties("_type").FirstOrDefault(); } catch (Exception) { }
            if (_type == null) return String.Empty;
            return _type.Value.ToString();
        }
        /// <summary>
        /// Try to detect if the Element is a Vertex or an Edge
        /// If we are not provided with interfaces, we need to have an 
        /// Type that has a default constructor, to use
        /// GraphSerializerFactory to create a Serializer which knows about this.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        internal static GraphElementType GetElementType(Type t)
        {
            Type[] interfaces = t.GetInterfaces();
            var vInterface = interfaces.Where(i => i.Name == typeof(IVertex<object, object>).Name).FirstOrDefault();
            if (vInterface != null) return GraphElementType.Vertex;
            var eInterface = interfaces.Where(i => i.Name == typeof(IEdge<object, object>).Name).FirstOrDefault();
            if (eInterface != null) return GraphElementType.Edge;

            if (t.GetConstructor(Type.EmptyTypes) != null)
            {
                IGraphSerializer serializer = GraphSerializerFactory.CreateGraphSerializer(null, t);
                if (serializer.IsEdge()) return GraphElementType.Edge; else return GraphElementType.Vertex;
            }
            throw new Exception("Could not determine element type!");
        }
    }

    /// <summary>
    /// GraphSerializer converts instances of Edge/Vertex from Microsoft.Azure.Graph.Elements into plain C# objects of Type T.
    /// GraphSerializer can also convert a JSON (Graphson Format) to vertices and edges. 
    /// 
    /// GraphSerializer also work with IGraphContex which can be a represenatation of a Graph Subset in Memory. Imagine quering
    /// multiple vertices and then some related edges. Ideally you do not want to end up with independent vertices and edges but
    /// with vertices and edges that reference each other accordingly. If you pass a IGraphContext the serializer will make sure
    /// that already created instances are updates and references are kept.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class GraphSerializer<T> : IGraphSerializer, IGraphSerializer<T> where T : new()
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
        /// <summary>
        /// Type of Edge/Vertex that reference to this GraphElement (Vertex/Edge)
        /// If this Item is an Edge there is only one incomming vertex.
        /// If this Item is a Vertex there can be multiple incomming edges
        /// </summary>
        public Type InType
        {
            get; private set;
        }
        /// <summary>
        /// Type of Edge/Vertex that this GraphElement references to (Vertex/Edge)
        /// If this Item is an Edge there is only one outgoing vertex.
        /// If this Item is a Vertex there can be multiple outgoing edges
        /// </summary>
        public Type OutType
        {
            get; private set;
        }

        /// <summary>
        /// Internal constructor to create a GraphSerializer for a graph element of type T
        /// GraphSerializers are created with the GraphSerializerFactory
        /// 
        /// This constructor extracts all relevant information of the generic Type like the
        /// implemented interfaces (IGraphElement,IVertex,IEdge), defined properties like (InE,OutE,...)
        /// and types of incomming/outgoing vertices/edges.
        /// 
        /// Since only one graph serializers is created by type this only occurs once which should improve performance.
        /// </summary>
        /// <param name="context"></param>
        internal GraphSerializer(IGraphContext context)
        {
            myContext = context;
            Type targetType = typeof(T);

            Type[] interfaces = targetType.GetInterfaces(); // Retrieve all interfaces of the GraphElementType
            // Find out if it implements IVertex<In,Out> or IEdge<In,Out> and store references to those interfaces
            var vInterface = interfaces.Where(i => i.Name == typeof(IVertex<object, object>).Name).FirstOrDefault();
            var eInterface = interfaces.Where(i => i.Name == typeof(IEdge<object, object>).Name).FirstOrDefault();
            var geInterface = interfaces.Where(i => i.Name == typeof(IGraphElement).Name).FirstOrDefault();
            // Evaluate if the GraphClassAttribute is set
            myClassAttribute = targetType.GetCustomAttribute<GraphClassAttribute>();

            if (geInterface != null)
            {
                PropertyInfo pi = geInterface.GetProperty("Id");
                myPI_Defined.Add(GraphDefinedPropertyType.Id, pi);
                pi = geInterface.GetProperty("Label");
                myPI_Defined.Add(GraphDefinedPropertyType.Label, pi);
                // Create a new class Attribute if there was none defined (is used to store info about the graph element type (Vertex or Edge))
                if (myClassAttribute == null) myClassAttribute = new GraphClassAttribute();
            }

            // If an interface has been defined we use that as reference if the GraphElement is an Vertex or Edge.
            // Also we extract the type of the incomming/outgoing vertices/edges and store this in InType/OutType.
            if (vInterface != null)
            {
                // Ensure consistency in case someone implements IVertex but declares the GraphElement as Edge
                myClassAttribute.ElementType = GraphElementType.Vertex;
                Type[] genericArgs = vInterface.GetGenericArguments();
                InType = genericArgs[0];
                OutType = genericArgs[1];

                PropertyInfo pi = vInterface.GetProperty("InE");
                myPI_Defined.Add(GraphDefinedPropertyType.InE, pi);
                pi = vInterface.GetProperty("OutE");
                myPI_Defined.Add(GraphDefinedPropertyType.OutE, pi);
            }
            if (eInterface != null)
            {
                // Ensure consistency in case someone implements IEdge but declares the GraphElement as Vertex
                myClassAttribute.ElementType = GraphElementType.Edge;
                Type[] genericArgs = eInterface.GetGenericArguments();
                InType = genericArgs[0];
                OutType = genericArgs[1];

                PropertyInfo pi = eInterface.GetProperty("InV");
                myPI_Defined.Add(GraphDefinedPropertyType.InV, pi);
                pi = eInterface.GetProperty("OutV");
                myPI_Defined.Add(GraphDefinedPropertyType.OutV, pi);
            }

            #region Evaluate Properties 
            PropertyInfo[] propertyInfos = targetType.GetProperties();
            foreach (PropertyInfo pi in propertyInfos)
            {
                // Defined Properties can be decorated with the GraphProperty Attribute to clearly define its purpose
                GraphPropertyAttribute gpa = pi.GetCustomAttribute<GraphPropertyAttribute>();
                if ((gpa != null) && (gpa.DefinedProperty != GraphDefinedPropertyType.None))
                {
                    #region Assign Defined Property to Member
                    if (myPI_Defined.ContainsKey(gpa.DefinedProperty))
                    {   // If we added a other property (found by name convention) earlier move the stored property
                        // to the custom properties and replace it with this declared defined graph property.
                        myPI_Custom.Add(myPI_Defined[gpa.DefinedProperty].Name, myPI_Defined[gpa.DefinedProperty]); // If set before by name move to custom properties
                    }
                    myPI_Defined.Add(gpa.DefinedProperty, pi);
                    #endregion
                }
                else
                {
                    // Detect Properties by name convention
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
                    // If an interface was defined ignore properties found by name convention as the interfaces adhere to those!
                    if (definedDetected && vInterface==null && eInterface==null)
                    {
                        // If we found a property by name convention ensure that no other defined property with the same purpose
                        // was defined earlier. If so we assume that this property is a custom property.
                        if (!myPI_Defined.ContainsKey(detectedDefinedType))
                            myPI_Defined.Add(detectedDefinedType, pi);
                        else
                            myPI_Custom.Add(pi.Name, pi);
                    }
                }
            }
            #endregion

            #region Ensure InType and OutType are defined (In case no interface was implemented)
            if (InType == null)
            {
                if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.InV))
                    InType = myPI_Defined[GraphDefinedPropertyType.InV].PropertyType;
                if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.InE))
                    InType = myPI_Defined[GraphDefinedPropertyType.InE].PropertyType.GenericTypeArguments[0]; // Must be a generic List
            }
            if (OutType == null)
            {
                if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.OutV))
                    OutType = myPI_Defined[GraphDefinedPropertyType.OutV].PropertyType;
                if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.OutE))
                    OutType = myPI_Defined[GraphDefinedPropertyType.OutE].PropertyType.GenericTypeArguments[0]; // Must be a generic List
            }
            #endregion

            #region If not yet defined evaluate if given Type is Vertex or Edge
            // If the classAttribute has not been defined yet (no Interface implemented and no ClassAttribute defined)
            if (myClassAttribute == null)
            {
                if ((myPI_Defined.ContainsKey(GraphDefinedPropertyType.InV)) || (myPI_Defined.ContainsKey(GraphDefinedPropertyType.OutV)))
                    myClassAttribute = new GraphClassAttribute() { ElementType = GraphElementType.Edge };
                else
                    myClassAttribute = new GraphClassAttribute() { ElementType = GraphElementType.Vertex };
            }
            #endregion

            // Store the type information in the internal classattribute
            if (myClassAttribute.TypeKey==null)
                myClassAttribute.TypeKey = GraphSerializer.GetTypeKey(targetType);
        }

        /// <summary>
        /// returns a reference to the GraphContext which can store a partial graph
        /// </summary>
        public IGraphContext GraphContext
        {
            get
            {
                return myContext;
            }
        }
        
        // Defined Properties are Id, Label, InE, OutE, InV, OutV
        #region Helpers to work with defined properties

        /// <summary>
        /// Sets the value of a defined property of an target instance. 
        /// OutE and InE are always Lists as one Vertex can refer to multiple edges.
        /// </summary>
        /// <param name="propertyType">the property that should be set: Id, Label, InV, InE, OutE, InE</param>
        /// <param name="targetInstance">target instance which property should be set</param>
        /// <param name="value">the value that should be set</param>
        public void SetDefinedProperty(GraphDefinedPropertyType propertyType, object targetInstance, object value)
        {
            if (myPI_Defined.ContainsKey(propertyType))
                myPI_Defined[propertyType].SetValue(targetInstance, value);
        }
        /// <summary>
        /// Retrieves a value from a defined property (like Id,Label,...) from a target instance
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="targetInstance"></param>
        /// <returns></returns>
        public object GetDefinedProperty(GraphDefinedPropertyType propertyType, object targetInstance)
        {
            if (myPI_Defined.ContainsKey(propertyType))
                return myPI_Defined[propertyType].GetValue(targetInstance);
            else
                return null;
        }
        /// <summary>
        /// In case the defined property is a List<> this method can be used to add an item to this list.
        /// If the List<> is not yet instantiated it is created!
        /// If the item is already part of the list it is NOT added again!
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="targetInstance"></param>
        /// <param name="value"></param>
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
        /// <summary>
        /// Sets any other property (other than defined) on the target instance
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="targetInstance"></param>
        /// <param name="value"></param>
        public void SetCustomProperty(string propertyName, object targetInstance, object value)
        {
            if (myPI_Custom.ContainsKey(propertyName))
                myPI_Custom[propertyName].SetValue(targetInstance, value);
        }
        /// <summary>
        /// Gets any other property (other than defined) from the target instance
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="targetInstance"></param>
        /// <returns></returns>
        public object GetCustomProperty(string propertyName, object targetInstance)
        {
            if (myPI_Custom.ContainsKey(propertyName))
                return myPI_Custom[propertyName].GetValue(targetInstance);
            else
                return null;
        }
        /// <summary>
        /// If the graph elements want to store additional meta information instead of simple data types
        /// they can use the GraphProperty-Type. 
        /// This method returns a reference to such graph property. If it was not set it is created!
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="targetInstance"></param>
        /// <returns></returns>
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
        /// <summary>
        /// This method is used to transfer the information of a CosmosDB-VertexProperty object into
        /// a GraphProperty instance.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="targetInstance"></param>
        /// <param name="value"></param>
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
        /// <summary>
        /// This method is used to transfer the information of a CosmosDB-EdgeProperty object into
        /// a GraphProperty instance.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="targetInstance"></param>
        /// <param name="value"></param>
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

        /// <summary>
        /// States if the Type from GraphElements should be stored as additional meta information in CosmosDB
        /// </summary>
        /// <returns></returns>
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
                    result.Add((T)this.Convert(v));
            }
            else
            {
                List<Edge> edges = JsonConvert.DeserializeObject<List<Edge>>(graphSON, converter);
                foreach (Edge e in edges)
                    result.Add((T)this.Convert(e));
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
                IGraphSerializer outVSerial = outV != null ? GraphSerializerFactory.CreateGraphSerializer(myContext, outV.GetType()) : null;

                object inV = this.GetDefinedProperty(GraphDefinedPropertyType.InV, poco);
                IGraphSerializer inVSerial = inV != null ? GraphSerializerFactory.CreateGraphSerializer(myContext,inV.GetType()) : null;

                jOutput.Add(new JProperty("_isEdge", true)); // Change from 2.2 to 2.4 --> in 2.2 it was required to store it as string!
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
                {
                    jOutput.Add(new JProperty("_type", myClassAttribute.TypeKey));
                    jOutput.Add(new JProperty("_typeIn", GraphSerializer.GetTypeKey(inV.GetType())));
                    jOutput.Add(new JProperty("_typeOut", GraphSerializer.GetTypeKey(outV.GetType())));
                }

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
            object resultVertex = CreateItemInstance(v.Id.ToString());
            // Propulate lable and custom properties
            this.SetDefinedProperty(GraphDefinedPropertyType.Label, resultVertex, v.Label);
            foreach (var vp in v.GetVertexProperties())
                SetCustomVertexProperty(vp.Key, resultVertex, vp);
            // If you used GraphSON instead of GraphSONCompact you might get additional references to InEdges
            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.InE))
            {
                foreach (var ve in v.GetInEdges())
                {
                    IGraphSerializer inEdgeSerializer = null;
                    string typeString = GraphSerializer.GetTypePropertyString(ve, out string inVTypeString, out string outVTypeString);
                    object edge = null;

                    if (String.IsNullOrEmpty(typeString))
                        inEdgeSerializer = GraphSerializerFactory.CreateGraphSerializer(myContext, this.InType);
                    else
                        inEdgeSerializer = GraphSerializerFactory.CreateGraphSerializer(myContext, typeString);
                    edge = inEdgeSerializer.CreateItemInstanceObject(ve.Id.ToString());
                    inEdgeSerializer.SetDefinedProperty(GraphDefinedPropertyType.Label, edge, ve.Label);
                    inEdgeSerializer.SetDefinedProperty(GraphDefinedPropertyType.InV, edge, resultVertex); // InV is the result


                    IGraphSerializer vertexSerializer = null;
                    object outvertex = null;

                    if (String.IsNullOrEmpty(outVTypeString))
                        vertexSerializer = GraphSerializerFactory.CreateGraphSerializer(myContext, typeof(T));
                    else
                        vertexSerializer = GraphSerializerFactory.CreateGraphSerializer(myContext, outVTypeString);

                    outvertex = vertexSerializer.CreateItemInstanceObject(ve.OutVertexId.ToString()); 
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
                    IGraphSerializer outEdgeSerializer = null;
                    string typeString = GraphSerializer.GetTypePropertyString(ve, out string inVTypeString, out string outVTypeString);
                    object edge = null;

                    if (String.IsNullOrEmpty(typeString))
                        outEdgeSerializer = GraphSerializerFactory.CreateGraphSerializer(myContext, this.OutType);
                    else
                        outEdgeSerializer = GraphSerializerFactory.CreateGraphSerializer(myContext, typeString);
                    edge = outEdgeSerializer.CreateItemInstanceObject(ve.Id.ToString());
                    outEdgeSerializer.SetDefinedProperty(GraphDefinedPropertyType.Label, edge, ve.Label);
                    outEdgeSerializer.SetDefinedProperty(GraphDefinedPropertyType.OutV, edge, resultVertex); // outV is the result

                    IGraphSerializer vertexSerializer = null;
                    object inVertex = null;

                    if (String.IsNullOrEmpty(inVTypeString))
                        vertexSerializer = GraphSerializerFactory.CreateGraphSerializer(myContext, typeof(T)); // typeof (T) instead?
                    else
                        vertexSerializer = GraphSerializerFactory.CreateGraphSerializer(myContext, inVTypeString);

                    inVertex = vertexSerializer.CreateItemInstanceObject(ve.InVertexId.ToString());

                    vertexSerializer.AddDefinedPropertyListItem(GraphDefinedPropertyType.InE, inVertex, edge);
                    outEdgeSerializer.SetDefinedProperty(GraphDefinedPropertyType.InV, edge, inVertex); // OUTV we just created

                    this.AddDefinedPropertyListItem(GraphDefinedPropertyType.OutE, resultVertex, edge);
                }
            }
            return (T)resultVertex;
        }

        /// <summary>
        /// Converts an Edge Object to a typed custom object
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public T Convert(Edge e)
        {
            /// Create or fetch the Edge Instance
            object resultEdge = CreateItemInstance(e.Id.ToString());
            /// (Re)populate Lable and custom properties
            this.SetDefinedProperty(GraphDefinedPropertyType.Label, resultEdge, e.Label);
            foreach (var ep in e.GetProperties())
                SetCustomEdgeProperty(ep.Key, resultEdge, ep);

            string typeString = GraphSerializer.GetTypePropertyString(e, out string inVTypeString, out string outVTypeString);

            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.InV))
            {
                IGraphSerializer serializer = null;
                object inVertex = null;

                if (String.IsNullOrEmpty(inVTypeString))
                    serializer = GraphSerializerFactory.CreateGraphSerializer(myContext, this.InType);
                else
                    serializer = GraphSerializerFactory.CreateGraphSerializer(myContext, inVTypeString);

                /// Try to create/fetch the referenced In-Vertex
                inVertex = serializer.CreateItemInstanceObject(e.InVertexId.ToString()); 
                
                serializer.SetDefinedProperty(GraphDefinedPropertyType.Label, inVertex, e.InVertexLabel);
                serializer.AddDefinedPropertyListItem(GraphDefinedPropertyType.InE, inVertex, resultEdge);
                
                /// Set the created In-Vertex as InV-Property
                this.SetDefinedProperty(GraphDefinedPropertyType.InV, resultEdge, inVertex);
            }
            if (myPI_Defined.ContainsKey(GraphDefinedPropertyType.OutV))
            {
                IGraphSerializer serializer = null;
                object outVertex = null;

                if (String.IsNullOrEmpty(outVTypeString))
                    serializer = GraphSerializerFactory.CreateGraphSerializer(myContext, this.OutType);
                else
                    serializer = GraphSerializerFactory.CreateGraphSerializer(myContext, outVTypeString);

                // Try to create/fetch the referenced Out-Vertex
                outVertex = serializer.CreateItemInstanceObject(e.OutVertexId.ToString());

                serializer.SetDefinedProperty(GraphDefinedPropertyType.Label, outVertex, e.OutVertexLabel);
                serializer.AddDefinedPropertyListItem(GraphDefinedPropertyType.OutE, outVertex, resultEdge);
                
                /// Set the created Out-Vertex as OutV-Property
                this.SetDefinedProperty(GraphDefinedPropertyType.OutV, resultEdge, outVertex);
            }
            return (T)resultEdge;
        }

        public void Convert(Vertex v, out object result)
        {
            result = Convert(v);
        }
        public void Convert(Edge e, out object result)
        {
            result = Convert(e);
        }

        /// <summary>
        /// Creates a new instance of type T and sets the defined property ID if it cannot be already 
        /// found in the GraphContext. Otherwise it is fetched from the GraphContext. 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private object CreateItemInstance(string id)
        {
            object tmpGraphItem = myContext[id];
            if (tmpGraphItem != null)
            {
                return myContext[id];
            }
            T instance = new T();
            this.SetDefinedProperty(GraphDefinedPropertyType.Id, instance, id);
            myContext[id] = instance;
            return instance;
        }

        public object CreateItemInstance(IGraphContext context, string id, out IGraphSerializer serializer)
        {
            object tmpGraphItem = context[id];
            if (tmpGraphItem != null)
            {
                serializer = GraphSerializerFactory.CreateGraphSerializer(context, tmpGraphItem.GetType());
                return tmpGraphItem;
            }
            T instance = new T();
            serializer = GraphSerializerFactory.CreateGraphSerializer(context,typeof(T));
            serializer.SetDefinedProperty(GraphDefinedPropertyType.Id, instance, id);
         
            context[id] = instance;
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

        ///// <summary>
        ///// Detects the type of an ListItem of a defined property (InE or OutE) and creates a GraphSerializer for that
        ///// </summary>
        ///// <param name="propertyType"></param>
        ///// <returns></returns>
        //public IGraphSerializer CreateGraphSerializerForListItem(GraphDefinedPropertyType propertyType)
        //{
        //    Type listElementType = myPI_Defined[propertyType].PropertyType.GenericTypeArguments[0];
        //    return GraphSerializerFactory.CreateGraphSerializer(myContext, listElementType);
        //}
        ///// <summary>
        ///// Detects the type of the defined property (OutV or InV) and creates a GraphSerializer object
        ///// </summary>
        ///// <param name="propertyType"></param>
        ///// <returns></returns>
        //public IGraphSerializer CreateGraphSerializerForItem(GraphDefinedPropertyType propertyType)
        //{
        //    Type itemType = myPI_Defined[propertyType].PropertyType;
        //    return GraphSerializerFactory.CreateGraphSerializer(myContext, itemType);
        //}
    }
}
