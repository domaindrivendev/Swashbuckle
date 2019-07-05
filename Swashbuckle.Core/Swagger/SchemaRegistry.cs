using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using System.Net.Http.Formatting;

namespace Swashbuckle.Swagger
{
    public class SchemaRegistry
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IDictionary<Type, Func<Schema>> _customSchemaMappings;
        private readonly IEnumerable<ISchemaFilter> _schemaFilters;
        private readonly IEnumerable<IModelFilter> _modelFilters;
        private readonly Func<Type, string> _schemaIdSelector;
        private readonly bool _ignoreObsoleteProperties;
        private readonly bool _describeAllEnumsAsStrings;
        private readonly bool _describeStringEnumsInCamelCase;
        private readonly bool _applyFiltersToAllSchemas;

        private readonly IContractResolver _contractResolver;

        private IDictionary<Type, WorkItem> _workItems;
        private class WorkItem
        {
            public string SchemaId;
            public bool InProgress;
            public Schema Schema;
        }

        public SchemaRegistry(
            JsonSerializerSettings jsonSerializerSettings,
            IDictionary<Type, Func<Schema>> customSchemaMappings,
            IEnumerable<ISchemaFilter> schemaFilters,
            IEnumerable<IModelFilter> modelFilters,
            bool ignoreObsoleteProperties,
            Func<Type, string> schemaIdSelector,
            bool describeAllEnumsAsStrings,
            bool describeStringEnumsInCamelCase,
            bool applyFiltersToAllSchemas)
        {
            _jsonSerializerSettings = jsonSerializerSettings;
            _customSchemaMappings = customSchemaMappings;
            _schemaFilters = schemaFilters;
            _modelFilters = modelFilters;
            _schemaIdSelector = schemaIdSelector;
            _ignoreObsoleteProperties = ignoreObsoleteProperties;
            _describeAllEnumsAsStrings = describeAllEnumsAsStrings;
            _describeStringEnumsInCamelCase = describeStringEnumsInCamelCase;
            _applyFiltersToAllSchemas = applyFiltersToAllSchemas;

            _contractResolver = jsonSerializerSettings.ContractResolver ?? new DefaultContractResolver();
            _workItems = new Dictionary<Type, WorkItem>();
            Definitions = new Dictionary<string, Schema>();
        }

        public Schema GetOrRegister(Type type, string typeName = null)
        {
            var schema = CreateInlineSchema(type, typeName );

            // Iterate outstanding work items (i.e. referenced types) and generate the corresponding definition
            while (_workItems.Any(entry => entry.Value.Schema == null && !entry.Value.InProgress))
            {
                var typeMapping = _workItems.First(entry => entry.Value.Schema == null && !entry.Value.InProgress);
                var workItem = typeMapping.Value;

                workItem.InProgress = true;
                workItem.Schema = CreateDefinitionSchema(typeMapping.Key);
                Definitions.Add(workItem.SchemaId, workItem.Schema);
                workItem.InProgress = false;
            }

            return schema;
        }

        public IDictionary<string, Schema> Definitions { get; private set; }

        private Schema CreateInlineSchema(Type type, string typeName = null)
        {
            var jsonContract = _contractResolver.ResolveContract(type);

            if (_customSchemaMappings.ContainsKey(type))
                return FilterSchema(_customSchemaMappings[type](), jsonContract);

            if (jsonContract is JsonPrimitiveContract)
                return FilterSchema(CreatePrimitiveSchema((JsonPrimitiveContract)jsonContract), jsonContract);

            var dictionaryContract = jsonContract as JsonDictionaryContract;
            if (dictionaryContract != null)
                return dictionaryContract.IsSelfReferencing()
                    ? CreateRefSchema(type)
                    : FilterSchema(CreateDictionarySchema(dictionaryContract), jsonContract);

            var arrayContract = jsonContract as JsonArrayContract;
            if (arrayContract != null)
                return arrayContract.IsSelfReferencing()
                    ? CreateRefSchema(type)
                    : FilterSchema(CreateArraySchema(arrayContract, true, typeName), jsonContract);

            var objectContract = jsonContract as JsonObjectContract;
            if (objectContract != null && !objectContract.IsAmbiguous())
                return CreateRefSchema(type);

            // Fallback to abstract "object"
            return FilterSchema(new Schema { type = "object" }, jsonContract);
        }

        private Schema CreateDefinitionSchema(Type type)
        {
            var jsonContract = _contractResolver.ResolveContract(type);

            if (jsonContract is JsonDictionaryContract)
                return FilterSchema(CreateDictionarySchema((JsonDictionaryContract)jsonContract), jsonContract);

            if (jsonContract is JsonArrayContract)
                return FilterSchema(CreateArraySchema((JsonArrayContract)jsonContract, false), jsonContract);

            if (jsonContract is JsonObjectContract)
                return FilterSchema(CreateObjectSchema((JsonObjectContract)jsonContract, true), jsonContract);

            throw new InvalidOperationException(
                String.Format("Unsupported type - {0} for Defintitions. Must be Dictionary, Array or Object", type));
        }

        private Schema CreatePrimitiveSchema(JsonPrimitiveContract primitiveContract)
        {
            var type = Nullable.GetUnderlyingType(primitiveContract.UnderlyingType) ?? primitiveContract.UnderlyingType;

            if (type.IsEnum)
                return CreateEnumSchema(primitiveContract, type);

            switch (type.FullName)
            {
                case "System.Boolean":
                    return new Schema { type = "boolean" };
                case "System.Byte":
                case "System.SByte":
                case "System.Int16":
                case "System.UInt16":
                case "System.Int32":
                case "System.UInt32":
                    return new Schema { type = "integer", format = "int32" };
                case "System.Int64":
                case "System.UInt64":
                    return new Schema { type = "integer", format = "int64" };
                case "System.Single":
                    return new Schema { type = "number", format = "float" };
                case "System.Double":
                case "System.Decimal":
                    return new Schema { type = "number", format = "double" };
                case "System.Byte[]":
                    return new Schema { type = "string", format = "byte" };
                case "System.DateTime":
                case "System.DateTimeOffset":
                    return new Schema { type = "string", format = "date-time" };
                case "System.Guid":
                    return new Schema { type = "string", format = "uuid", example = Guid.Empty };
                default:
                    return new Schema { type = "string" };
            }
        }

        private Schema CreateEnumSchema(JsonPrimitiveContract primitiveContract, Type type)
        {
            var stringEnumConverter = primitiveContract.Converter as StringEnumConverter
                ?? _jsonSerializerSettings.Converters.OfType<StringEnumConverter>().FirstOrDefault();

            if (_describeAllEnumsAsStrings || stringEnumConverter != null)
            {
                var camelCase = _describeStringEnumsInCamelCase
                    || (stringEnumConverter != null && stringEnumConverter.CamelCaseText);

                return new Schema
                {
                    type = "string",
                    @enum = camelCase
                        ? type.GetEnumNamesForSerialization().Select(name => name.ToCamelCase()).ToArray()
                        : type.GetEnumNamesForSerialization()
                };
            }

            return new Schema
            {
                type = "integer",
                format = "int32",
                @enum = type.GetEnumValues().Cast<object>().ToArray()
            };
        }

        private Schema CreateDictionarySchema(JsonDictionaryContract dictionaryContract)
        {
            var keyType = dictionaryContract.DictionaryKeyType ?? typeof(object);
            var valueType = dictionaryContract.DictionaryValueType ?? typeof(object);

            if (keyType.IsEnum)
            {
                return new Schema
                {
                    type = "object",
                    properties = Enum.GetNames(keyType).ToDictionary(
                        (name) => dictionaryContract.DictionaryKeyResolver(name),
                        (name) => CreateInlineSchema(valueType)
                    )
                };
            }
            else
            {
                return new Schema
                {
                    type = "object",
                    additionalProperties = CreateInlineSchema(valueType)
                };
            }
        }

        private Schema CreateArraySchema(JsonArrayContract arrayContract, bool isWrapped = false, string typeName = null)
        {
            var itemType = arrayContract.CollectionItemType ?? typeof(object);
            var s = new Schema
            {
                type = "array",
                items = CreateInlineSchema(itemType)
            };
            if( itemType.Namespace != "System" && itemType.Namespace != "Enum" )
            {
                s.xml = new Xml { name = typeName ?? itemType.Name, wrapped = isWrapped };
            }
            return s;
        }

        private Schema CreateObjectSchema(JsonObjectContract jsonContract, bool addXmlName = false )
        {
            var properties = jsonContract.Properties
                .Where(p => !p.Ignored)
                .Where(p => !(_ignoreObsoleteProperties && p.IsObsolete()))
                .ToDictionary(
                    prop => prop.PropertyName,
                    prop => CreateInlineSchema(prop.PropertyType).WithValidationProperties(prop)
                );

            var required = jsonContract.Properties.Where(prop => prop.IsRequired())
                .Select(propInfo => propInfo.PropertyName)
                .ToList();

            var s = new Schema
            {
                required = required.Any() ? required : null, // required can be null but not empty
                properties = properties,
                type = "object"
            };
            if( addXmlName )
            {
                s.xml = new Xml { name = jsonContract.UnderlyingType.Name };
            }
            return s;
        }

        private Schema CreateRefSchema(Type type)
        {
            if (!_workItems.ContainsKey(type))
            {
                var schemaId = _schemaIdSelector(type);
                if (_workItems.Any(entry => entry.Value.SchemaId == schemaId))
                {
                    var conflictingType = _workItems.First(entry => entry.Value.SchemaId == schemaId).Key;
                    throw new InvalidOperationException(String.Format(
                        "Conflicting schemaIds: Duplicate schemaIds detected for types {0} and {1}. " +
                        "See the config setting - \"UseFullTypeNameInSchemaIds\" for a potential workaround",
                        type.FullName, conflictingType.FullName));
                }

                _workItems.Add(type, new WorkItem { SchemaId = schemaId });
            }

            return new Schema { @ref = "#/definitions/" + _workItems[type].SchemaId };
        }

        private Schema FilterSchema(Schema schema, JsonContract jsonContract)
        {
            if (schema.type == "object" || _applyFiltersToAllSchemas)
            {
                var jsonObjectContract = jsonContract as JsonObjectContract;
                if (jsonObjectContract != null)
                {
                    // NOTE: In next major version, _modelFilters will completely replace _schemaFilters
                    var modelFilterContext = new ModelFilterContext(jsonObjectContract.UnderlyingType, jsonObjectContract, this);
                    foreach (var filter in _modelFilters)
                    {
                        filter.Apply(schema, modelFilterContext);
                    }
                }

                foreach (var filter in _schemaFilters)
                {
                    filter.Apply(schema, this, jsonContract.UnderlyingType);
                }
            }

            return schema;
        }
    }
}