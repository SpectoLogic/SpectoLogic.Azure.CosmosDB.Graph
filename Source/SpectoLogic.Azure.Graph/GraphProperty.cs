using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SpectoLogic.Azure.Graph
{
    /// <summary>
    /// Helper Class to store all data from GraphDB in a Property
    /// </summary>
    public class GraphProperty
    {
        public class GraphPropertyValue
        {
            public GraphPropertyValue()
            {
                this.Id = Guid.NewGuid().ToString("D");
            }
            [JsonProperty(PropertyName = "_value")]
            public object Value { get; set; }
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            private Dictionary<string, object> myMeta;

            [JsonProperty(PropertyName = "_meta")]
            public Dictionary<string, object> Meta
            {
                get { if (myMeta == null) myMeta = new Dictionary<string, object>(); return myMeta; }
                set { myMeta = value; }
            }
        }

        [JsonIgnore]
        public string Name { get; set; }
        private Dictionary<string, GraphPropertyValue> myValues;
        [JsonIgnore]
        public Dictionary<string, GraphPropertyValue> Values
        {
            get { if (myValues == null) myValues = new Dictionary<string, GraphPropertyValue>(); return myValues; }
            set { myValues = value; }
        }

        public void Add(GraphPropertyValue value)
        {
            this.Values.Add(value.Id, value);
        }

        /// <summary>
        /// Helper to create a new instance of GraphProperty more efficiently
        /// </summary>
        /// <example>
        /// To create an instance for the Country property with multiple values and additional meta informations:
        /// 
        /// person.Country = GraphProperty.Create("Country","AT","Meta1","Austria").AddValue("DE","Meta1","Germany");
        /// 
        /// </example>
        /// <param name="propertyName">Name of the property as stored in the GraphDB</param>
        /// <param name="properties">first element represents the value, then Tuples-2 follow for MetaInformation (Key/Value) </param>
        /// <returns></returns>
        public static GraphProperty Create(string propertyName, params object[] properties)
        {
            if (properties.Length < 1) throw new Exception("At least a value is required!");
            if ((properties.Length > 1) && ((properties.Length - 1) % 2 != 0)) throw new Exception("Some meta properties are lacking its value!");
            GraphProperty newProperty = new GraphProperty();

            GraphPropertyValue newPropertyValue = new GraphPropertyValue();
            newProperty.Name = propertyName;
            newPropertyValue.Value=properties[0];
            if (properties.Length > 1)
            {
                for (int i = 1; i < properties.Length; i += 2)
                {
                    newPropertyValue.Meta.Add(properties[i].ToString(), properties[i + 1].ToString());
                }
            }
            newProperty.Values.Add(newPropertyValue.Id, newPropertyValue);
            return newProperty;
        }

        public GraphProperty AddValue(params object[] properties)
        {
            if (properties.Length < 1) throw new Exception("At least a Value is required!");
            if ((properties.Length > 1) && ((properties.Length - 1) % 2 != 0)) throw new Exception("Some meta properties are lacking its value!");

            GraphPropertyValue newPropertyValue = new GraphPropertyValue() { Value = properties[0] };
            if (properties.Length > 1)
            {
                for (int i = 1; i < properties.Length; i += 2)
                {
                    newPropertyValue.Meta.Add(properties[i].ToString(), properties[i + 1].ToString());
                }
            }
            this.Values.Add(newPropertyValue.Id, newPropertyValue);
            return this;
        }
    }
}
