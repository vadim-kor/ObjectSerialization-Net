using System;
using ProtoBuf;

namespace ObjectSerialization.Performance.TestObjects
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(4, typeof(RegisteredSimpleClass))]
    class SimpleClass
    {
        [ProtoMember(3)]
        public double Double { get; set; }

        [ProtoMember(1)]
        public int Number { get; set; }
        [ProtoMember(2)]
        public string Text { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SimpleClass)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Number;
                hashCode = (hashCode * 397) ^ (Text != null ? Text.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Double.GetHashCode();
                return hashCode;
            }
        }

        protected bool Equals(SimpleClass other)
        {
            return Number == other.Number && string.Equals(Text, other.Text) && Double.Equals(other.Double);
        }
    }
}