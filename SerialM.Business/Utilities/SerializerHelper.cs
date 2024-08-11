using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Text.Json;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Xml;

namespace SerialM.Business.Utilities
{
    // Enum to choose between serialization formats
    public enum SerializationFormat
    {
        Xml,
        Json,
        Binary
    }

    public static class PageStorage
    {
        public static void Save<T>(string filename, T obj, SerializationFormat format = SerializationFormat.Json)
        {
            switch (format)
            {
                case SerializationFormat.Xml:
                    SaveXml(filename, obj);
                    break;

                case SerializationFormat.Binary:
                    SaveBinary(filename, obj);
                    break;

                case SerializationFormat.Json:
                    SaveJson(filename, obj);
                    break;

                default:
                    throw new ArgumentException("Unsupported serialization format");
            }
        }

        public static T Load<T>(string filename, SerializationFormat format = SerializationFormat.Json)
        {
            switch (format)
            {
                case SerializationFormat.Xml:
                    return LoadXml<T>(filename);

                case SerializationFormat.Binary:
                    return LoadBinary<T>(filename);

                case SerializationFormat.Json:
                    return LoadJson<T>(filename);

                default:
                    throw new ArgumentException("Unsupported serialization format");
            }
        }

        private static void SaveXml<T>(string filename, T obj)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));

            using (FileStream fs = new FileStream(filename, FileMode.Create))
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateTextWriter(fs))
            {
                serializer.WriteObject(writer, obj);
            }
        }

        private static T LoadXml<T>(string filename)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));

            using (FileStream fs = new FileStream(filename, FileMode.Open))
            using (XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(fs, XmlDictionaryReaderQuotas.Max))
            {
                return (T)serializer.ReadObject(reader);
            }
        }

        private static void SaveBinary<T>(string filename, T obj)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));

            using (FileStream fs = new FileStream(filename, FileMode.Create))
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(fs))
            {
                serializer.WriteObject(writer, obj);
            }
        }

        private static T LoadBinary<T>(string filename)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));

            using (FileStream fs = new FileStream(filename, FileMode.Open))
            using (XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(fs, XmlDictionaryReaderQuotas.Max))
            {
                return (T)serializer.ReadObject(reader);
            }
        }

        private static void SaveJson<T>(string filename, T obj)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            using (Stream writer = fs)
            {
                JsonSerializer.Serialize(writer, value: obj);
            }
        }

        private static T LoadJson<T>(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            using (StreamReader reader = new StreamReader(fs))
            {
                return JsonSerializer.Deserialize<T>(reader.ReadToEnd());
            }
        }
    }
}