using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cypretex.Data.Filters.Utils
{
    public class InterfaceConverterFactory : JsonConverterFactory
    {
        public InterfaceConverterFactory(Type concrete, Type interfaceType)
        {
            this.ConcreteType = concrete;
            this.InterfaceType = interfaceType;
        }

        public Type ConcreteType { get; }
        public Type InterfaceType { get; }

        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == this.InterfaceType;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(InterfaceConverter<,>).MakeGenericType(this.ConcreteType, this.InterfaceType);

            return (JsonConverter)Activator.CreateInstance(converterType);
        }
    }

    public class InterfaceConverter<M, I> : JsonConverter<I> where M : class, I
    {
        public override I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<M>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, I value, JsonSerializerOptions options) {
            if(value==null){
                throw new Exception("Value cannot be null!");
            }
            JsonSerializer.Serialize<M>(writer, (M)value, options);
        }
    }
}
