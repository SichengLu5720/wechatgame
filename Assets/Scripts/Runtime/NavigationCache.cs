using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using StairsCrowd.Core;

namespace StairsCrowd.Runtime
{
    public static class NavigationFactory
    {
        // Free editor drafts may intentionally have incomplete connections. Preview
        // and play keep that existing behavior; unsupported moves remain rejected.
        public static WalkSpace ForDraft(LevelSpec level,WalkGeometryConfig config=null)=>new WalkSpace(level,null,true,config);
        public static WalkSpace Create(LevelSpec level,WalkGeometryConfig config=null)
        {
            var space=new WalkSpace(level,null,true,config);space.Geometry.RequireComplete();return space;
        }
        public static WalkSpace ForOfflineValidation(LevelSpec level,byte[] optionalCache=null,WalkGeometryConfig config=null)
        {
            var space=new WalkSpace(level,optionalCache,false,config);space.Geometry.RequireComplete();return space;
        }
    }

    // Optional offline dense cache. Parsing is bounded, checksummed and transactional:
    // every rejection causes WalkSpace to regenerate every cell from current geometry.
    public static class NavigationCache
    {
        const int Magic=0x3441564e,Version=1,HeaderBytes=88;
        static byte[] Signature(string hex)
        {
            var bytes=new byte[32];for(int i=0;i<bytes.Length;i++)bytes[i]=Convert.ToByte(hex.Substring(i*2,2),16);return bytes;
        }
        public static byte[] Write(WalkSpace space)
        {
            if(space.Heights.Length==0)throw new InvalidOperationException("Dense cache export requires an offline dense space, not runtime sparse geometry.");
            using(var payload=new MemoryStream())using(var p=new BinaryWriter(payload)){
                for(int i=0;i<space.Heights.Length;i++){p.Write(space.Heights[i]);var mask=space.Owners[i].ToByteArray();p.Write(mask.Length);p.Write(mask);}
                p.Flush();var bytes=payload.ToArray();byte[] checksum;using(var sha=SHA256.Create())checksum=sha.ComputeHash(bytes);
                using(var output=new MemoryStream())using(var w=new BinaryWriter(output)){
                    w.Write(Magic);w.Write(Version);w.Write(Signature(space.GeometrySignature));w.Write(space.Width);w.Write(space.Depth);w.Write(space.Heights.Length);w.Write(bytes.Length);w.Write(checksum);w.Write(bytes);w.Flush();return output.ToArray();
                }
            }
        }
        public static bool TryRead(WalkSpace space,byte[] bytes,out string reason)
        {
            reason="Invalid cache";
            if(bytes==null){reason="Missing";return false;}
            try{
                using(var stream=new MemoryStream(bytes,false))using(var r=new BinaryReader(stream)){
                    if(bytes.Length<HeaderBytes){reason="Truncated header";return false;}
                    if(r.ReadInt32()!=Magic||r.ReadInt32()!=Version){reason="Unsupported version";return false;}
                    if(!r.ReadBytes(32).SequenceEqual(Signature(space.GeometrySignature))){reason="Geometry signature mismatch";return false;}
                    if(r.ReadInt32()!=space.Width||r.ReadInt32()!=space.Depth||r.ReadInt32()!=space.Heights.Length){reason="Grid dimensions mismatch";return false;}
                    int length=r.ReadInt32();if(length<0||length!=bytes.Length-HeaderBytes){reason="Payload length mismatch";return false;}
                    var checksum=r.ReadBytes(32);using(var sha=SHA256.Create())if(!checksum.SequenceEqual(sha.ComputeHash(bytes,HeaderBytes,length))){reason="Payload checksum mismatch";return false;}
                    int bits=space.Centers.Length+space.Level.edges.Length,maxMaskBytes=bits/8+1;
                    for(int i=0;i<space.Heights.Length;i++){
                        float height=r.ReadSingle();if(float.IsInfinity(height)){reason="Invalid height";return false;}
                        int count=r.ReadInt32();if(count<1||count>maxMaskBytes||count>stream.Length-stream.Position){reason="Invalid owner size";return false;}
                        var mask=r.ReadBytes(count);
                        // SurfaceSet accepts signed BigInteger input; caches must only contain
                        // nonnegative owners within this geometry's node/edge bit range.
                        if((mask[count-1]&128)!=0){reason="Negative owner mask";return false;}
                        for(int b=0;b<mask.Length;b++)if(b*8>=bits?mask[b]!=0:(bits-b*8<8&&(mask[b]>>(bits-b*8))!=0)){reason="Owner outside geometry";return false;}
                        space.Heights[i]=height;space.Owners[i]=new SurfaceSet(mask);
                    }
                    if(stream.Position!=stream.Length){reason="Trailing payload";return false;}
                    reason="Valid";return true;
                }
            }catch(EndOfStreamException){reason="Truncated payload";return false;}
            catch(IOException){reason="Unreadable cache";return false;}
            catch(ArgumentException){reason="Malformed cache";return false;}
        }
    }
}
