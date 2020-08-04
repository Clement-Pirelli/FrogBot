using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using Microsoft.VisualBasic.CompilerServices;

namespace DiscordBot
{
    public abstract class FileData 
    {
        public bool Loaded { get; private set; } = false;
        private string path = "";

        public FileData(string path) 
        {
            this.path = path;
            Reload();
        }

        public bool Reload() 
        {
            try
            {
                using (var reader = new StreamReader(path))
                {
                    Deserialize(reader);
                    Loaded = true;
                }
            }
            catch (FileNotFoundException exception)
            {
                Console.WriteLine(exception.Message);
                return false;
            }

            return true;
        }

        protected abstract void Deserialize(StreamReader reader);
    }


    public class FileString : FileData
    {
        public string Contents { get; private set; } = "";

        public FileString(string path) : base(path) { }
        protected override void Deserialize(StreamReader reader)
        {
            lock (Contents)
            {
                Contents = reader.ReadToEnd();
            }
        }
    }

    public class FileDictionary<TKey, TValue> : FileData//todo: something like where TKey : IXmlSerializable where TValue : IXmlSerializable
    {
        [Serializable]
        public struct Entry 
        {
            public TKey key;
            public TValue value;
        }


        public Dictionary<TKey, TValue> Data { get; private set; } = new Dictionary<TKey, TValue>();

        public TValue this[TKey key]
        {
            get
            {
                if (!Loaded)
                {
                    return default;
                }
                else
                {
                    return Data[key];
                }
            }
        }

        public FileDictionary(string path) : base(path) { }

        protected override void Deserialize(StreamReader reader)
        {
            var serializer = new XmlSerializer(typeof(List<Entry>));
            List<Entry> entries = (List<Entry>)serializer.Deserialize(reader);

            lock (Data)
            {
                Data.Clear();
                foreach (var entry in entries)
                {
                    Data[entry.key] = entry.value;
                }
            }
        }
    }
}
