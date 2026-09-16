using System;
using System.Numerics;

namespace StairsCrowd.Runtime
{
    // Common boards use a single machine word; larger boards extend without aliasing IDs.
    public readonly struct SurfaceSet : IEquatable<SurfaceSet>
    {
        readonly ulong low;readonly BigInteger high;
        SurfaceSet(ulong lower,BigInteger upper){low=lower;high=upper;}
        public SurfaceSet(byte[] bytes){var value=new BigInteger(bytes);low=(ulong)(value&ulong.MaxValue);high=value>>64;}
        public static SurfaceSet One {get{return new SurfaceSet(1,BigInteger.Zero);}}
        public static implicit operator SurfaceSet(int value){return new SurfaceSet((ulong)value,BigInteger.Zero);}
        public static SurfaceSet operator |(SurfaceSet a,SurfaceSet b){return new SurfaceSet(a.low|b.low,a.high.IsZero?b.high:b.high.IsZero?a.high:a.high|b.high);}
        public static SurfaceSet operator &(SurfaceSet a,SurfaceSet b){return new SurfaceSet(a.low&b.low,a.high.IsZero||b.high.IsZero?BigInteger.Zero:a.high&b.high);}
        public static SurfaceSet operator <<(SurfaceSet a,int bits){if(bits<64&&a.high.IsZero&&a.low==1)return new SurfaceSet(1UL<<bits,BigInteger.Zero);var value=a.ToInteger()<<bits;return new SurfaceSet((ulong)(value&ulong.MaxValue),value>>64);}
        BigInteger ToInteger(){return (high<<64)|low;}
        public byte[] ToByteArray(){return ToInteger().ToByteArray();}
        public bool Equals(SurfaceSet other){return low==other.low&&high==other.high;}
        public override bool Equals(object other){return other is SurfaceSet&&Equals((SurfaceSet)other);}
        public override int GetHashCode(){return low.GetHashCode()^high.GetHashCode();}
        public static bool operator ==(SurfaceSet a,SurfaceSet b){return a.Equals(b);}
        public static bool operator !=(SurfaceSet a,SurfaceSet b){return !a.Equals(b);}
        public override string ToString(){return ToInteger().ToString();}
    }
    sealed class SearchValues<T>
    {
        readonly T[] dense;readonly System.Collections.Generic.Dictionary<long,T> sparse;readonly T fallback;
        public SearchValues(int count,T initial){fallback=initial;if(count>0){dense=new T[count];for(int i=0;i<count;i++)dense[i]=initial;}else sparse=new System.Collections.Generic.Dictionary<long,T>();}
        public T this[long id]{get{if(dense!=null)return dense[(int)id];T value;return sparse.TryGetValue(id,out value)?value:fallback;}set{if(dense!=null)dense[(int)id]=value;else sparse[id]=value;}}
    }
}

