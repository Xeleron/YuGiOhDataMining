using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zlib;

namespace ConsoleApp1
{
    public static class Program
    {
        private static readonly byte[] CompressedFileHeader =
        {
            89,
            68,
            76,
            90
        };

        private static readonly byte[] AssetBundleHeader =
        {
            85,
            110,
            105,
            116,
            121
        };

        private static readonly string[] DataPathForStream =
        {
            "Card/Data2/st_CARD_Indx",
            "Card/Data2/st_CARD_Name",
            "Card/Data2/st_CARD_Desc",
            "Card/Data2/st_WORD_Indx",
            "Card/Data2/st_WORD_Text",
            "Card/Data2/st_DLG_Indx",
            "Card/Data2/st_DLG_Text",
            "Card/Data2/st_CARD_RubyIndx",
            "Card/Data2/st_CARD_RubyName",
            "Card/Data2/st_CARD_SearchName"
        };

        private static Dictionary<int, int> _mrk2Idx;
        private static Dictionary<int, int> _mrk2Rare;

        private static Dictionary<int, int> _sortIdx;

        private static List<Property> _allCardList = new List<Property>();
        private static readonly MemoryStream[] CardStreams = new MemoryStream[Enum.GetValues(typeof(Streams)).Length];

        private static Dictionary<int, int> _limitList;
        private static int _counter;

        private static void Main()
        {
            const string first = "https://att-dl.akamaized.net/att/cdn/att-";
            const string second = "201805142039-ad8c7bd10a";
            var arr = new[]
            {
                ""
            };
            foreach (var s in arr)
            {
                var bytes = new WebClient().DownloadData($"{first}{second}/{s}");
                InstallData(bytes, "testp-asswd", "1023yrjf");
            }
            //var _test1 = CreateListAll(File.ReadAllBytes("1207_Prop"));
            //var _test2 = CreateListAll(File.ReadAllBytes("1219_Prop"));
            //foreach (var property in _test2.Except(_test1)) Console.WriteLine(property.Mrk);
            //LoadBinary();

            //for (var i = 0; i < 150; i++)
            //{
            //    var name = _texts.ContainsKey($"NAME{i:D3}") ? _texts[$"NAME{i:D3}"] : "NONAME";
            //    var text = _texts.ContainsKey($"TEXT{i:D3}") ? _texts[$"TEXT{i:D3}"] : "NODESC";
            //    File.AppendAllText("skills.txt", $"{name}:{text}{Environment.NewLine}");
            //}
            //var nearDict =
            //    new MiniMessagePacker().Unpack(GetBytes("Duel/VoiceText/st_TEXT_SN0102")) as
            //        Dictionary<string, object>;
            //foreach (var o in nearDict)
            //{
            //    File.AppendAllText($"voicetext102.txt", JsonConvert.SerializeObject(o, Formatting.Indented));
            //}
        }

        private static byte[] GetBytes(string s)
        {
            var crc = Crc32.GetStringCrc32(s);
            const string basepath =
                @"C:\Program Files (x86)\Steam\steamapps\common\Yu-Gi-Oh! Duel Links\LocalData\f884fafa\";
            var builder = new StringBuilder(basepath.Length + 16) {Length = 0};
            builder.Append(basepath);
            builder.Append(((int) ((crc & 4278190080u) >> 24)).ToString("x2"));
            builder.Append(@"\");
            builder.Append(crc.ToString("x8"));
            return DecompressedData(File.ReadAllBytes(builder.ToString()));
        }

        private static void InstallData(byte[] data, string password, string salt)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var br = new BinaryReader(ms))
                {
                    var headerSize = br.ReadInt32() - 2;
                    br.ReadBytes(2);
                    var header = Decompress(br.ReadBytes(headerSize));
                    var body = br.ReadBytes(data.Length - (4 + headerSize));
                    if (!(MessagePack.Unpack(header) is List<object> arcInfos)) return;
                    foreach (var arcInfo in arcInfos)
                    {
                        var list = arcInfo as List<object>;
                        var sourceIndex = Convert.ToInt32(list?[1]);
                        var num = Convert.ToInt32(list?[2]);
                        var crypto = Convert.ToInt32(list?[3]);
                        if (num == 0) continue;
                        var array = new byte[num];
                        Array.Copy(body, sourceIndex, array, 0, num);
                        var flag = CheckFileType(array);
                        var resourceType = flag ? Type.AssetBundle : Type.Binary;
                        switch (crypto)
                        {
                            case 0:
                                File.WriteAllBytes(
                                    $"{resourceType}/{(list?[0] as string)?.Replace('/', '&')}-{_counter}",
                                    Crypto.Decrypt(array, password, salt));
                                break;
                            case 1:
                                if (((string) list?[0]).Contains("Texts"))
                                {
                                    var test = ((string) list?[0])?.Remove(0, 1).Replace("IDS_", "st_IDS_");
                                    var texts = InitProc(test);
                                    foreach (var text in texts)
                                        File.AppendAllText($"{test?.Split('/')[1]}-{_counter}.txt",
                                            $"{text}{Environment.NewLine}");
                                }

                                File.WriteAllBytes(
                                    $"{resourceType}/{((string) list?[0])?.Replace('/', '&')}-{_counter}",
                                    DecompressedData(array));
                                break;
                        }

                        _counter++;
                    }
                }
            }
        }

        private static string GetName(int cardId)
        {
            var internalId = DLL_CardGetInternalID(cardId);
            CardStreams[0].Seek(internalId * 2 * 4, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(CardStreams[0]);
            var num = binaryReader.ReadUInt32();
            CardStreams[0].Seek((internalId * 2 + 2) * 4, SeekOrigin.Begin);
            var num2 = binaryReader.ReadUInt32() - num;
            var array = new byte[num2];
            CardStreams[1].Seek(num, SeekOrigin.Begin);
            CardStreams[1].Read(array, 0, (int) num2);
            var @string = Encoding.UTF8.GetString(array);
            return @string.Replace("\0", string.Empty);
        }

        private static void LoadBinary()
        {
            CreateCardStreams();
            _allCardList = CreateListAll(GetBytes("Card/Data2/st_CARD_Prop"));
            DLL_SetInternalID(GetBytes("Card/Data2/st_CARD_IntID"));
            var prop = GetBytes("Card/Data2/st_CARD_Prop");
            DLL_SetCardProperty(prop, prop.Length);
            var same = GetBytes("Card/Data2/st_CARD_Same");
            DLL_SetCardSame(same, same.Length);
            DLL_SetCardGenre(GetBytes("Card/Data2/st_CARD_Genre"));
            DLL_SetCardNamed(GetBytes("Card/Data2/st_CARD_Named"));
            var link = GetBytes("Card/Data2/st_CARD_Link");
            DLL_SetCardLink(link, link.Length);
            CreateSortIdxList(GetBytes("Card/Data2/st_CARD_Sort"));
            CreateIdxMrkList(GetBytes("Card/Data2/st_CARD_ReleaseHistory"));
            CreateLimitList(GetBytes("Card/Data2/st_limit_current"));
        }

        private static int ReadInt16(byte[] bytes, int pos)
        {
            return bytes[pos] + (bytes[pos + 1] << 8);
        }

        private static void CreateLimitList(byte[] bytes)
        {
            var num = ReadInt16(bytes, 0);
            if (num == 0)
            {
                _limitList = new Dictionary<int, int>();
                return;
            }

            _limitList = new Dictionary<int, int>(num);
            for (var i = 0; i < num; i++)
            {
                var pos = i * 2 + 8;
                var num3 = ReadInt16(bytes, pos);
                var value = (num3 >> 14) & 3;
                var num4 = num3 & 16383;
                if (!_limitList.ContainsKey(num4))
                    _limitList.Add(num4, value);
            }
        }

        private static Dictionary<string, string> InitProc(string resourcePath)
        {
            var texts = new Dictionary<string, string>();
            var bytes = GetBytes(resourcePath);
            if (bytes == null) return new Dictionary<string, string>();
            var miniMessagePacker = new MiniMessagePacker();
            var dictionary = miniMessagePacker.Unpack(bytes) as Dictionary<string, object>;
            foreach (var keyValuePair in dictionary)
            {
                if (keyValuePair.Key == "_GRP_" || keyValuePair.Key == "_LNG_") continue;
                if (keyValuePair.Value is string s)
                    texts[keyValuePair.Key] = s;
            }

            return texts;
        }

        [DllImport("duel")]
        private static extern int DLL_CardGetDef2(int cardId);

        [DllImport("duel")]
        private static extern int DLL_CardGetAtk2(int cardId);

        [DllImport("duel")]
        private static extern void DLL_SetCardLink(byte[] data, int size);

        [DllImport("duel")]
        private static extern void DLL_SetCardNamed(byte[] data);

        [DllImport("duel")]
        private static extern void DLL_SetCardGenre(byte[] data);

        [DllImport("duel")]
        private static extern void DLL_SetCardSame(byte[] data, int size);

        [DllImport("duel")]
        private static extern void DLL_SetInternalID(byte[] data);

        [DllImport("duel")]
        private static extern int DLL_SetCardProperty(byte[] data, int size);

        private static void CreateCardStreams()
        {
            for (var i = 0; i < DataPathForStream.Length; i++)
            {
                var data = GetBytes(DataPathForStream[i]);
                if (data != null)
                    CardStreams[i] = new MemoryStream(data);
            }
        }

        [DllImport("duel")]
        private static extern int DLL_CardGetInternalID(int cardId);

        private static string GetDesc(int cardId)
        {
            var internalId = DLL_CardGetInternalID(cardId);
            CardStreams[0].Seek((internalId * 2 + 1) * 4, SeekOrigin.Begin);
            var binaryReader = new BinaryReader(CardStreams[0]);
            var num = binaryReader.ReadUInt32();
            CardStreams[0].Seek((internalId * 2 + 1 + 2) * 4, SeekOrigin.Begin);
            var num2 = binaryReader.ReadUInt32() - num;
            var array = new byte[num2];
            CardStreams[2].Seek(num, SeekOrigin.Begin);
            CardStreams[2].Read(array, 0, (int) num2);
            var @string = Encoding.UTF8.GetString(array);
            return @string.Replace("\0", string.Empty);
        }

        private static List<Property> CreateListAll(byte[] bytes)
        {
            var list = new List<Property>();
            for (var i = 0; i < bytes.Length / 8; i++)
            {
                var p = bytes[i * 8] | (bytes[i * 8 + 1] << 8) | (bytes[i * 8 + 2] << 16) | (bytes[i * 8 + 3] << 24);
                var p2 = bytes[i * 8 + 4] | (bytes[i * 8 + 5] << 8) | (bytes[i * 8 + 6] << 16) |
                         (bytes[i * 8 + 7] << 24);
                var prop = new Property(p, p2);
                if (prop.Mrk > 0)
                    list.Add(prop);
            }

            return list;
        }

        private static bool CheckFileType(byte[] bytes)
        {
            return CompareHeader(bytes.Take(AssetBundleHeader.Length).ToArray(), AssetBundleHeader);
        }

        private static byte[] DecompressedData(byte[] data)
        {
            if (!CheckCompressed(data) || data.Length <= 10) return data;
            var array = new byte[data.Length - 10];
            Buffer.BlockCopy(data, 10, array, 0, data.Length - 10);
            return Decompress(array);
        }

        private static string BinaryToString(byte[] src)
        {
            if (src == null || src.Length == 0)
                return string.Empty;
            var stringBuilder = new StringBuilder(src.Length * 2);
            foreach (var t in src)
                stringBuilder.Append(t.ToString("x2"));
            return stringBuilder.ToString();
        }

        private static void CreateSortIdxList(byte[] bin)
        {
            if (bin == null)
                return;
            var num = bin.Length / 4;
            _sortIdx = new Dictionary<int, int>(num);
            using (var memoryStream = new MemoryStream(bin))
            {
                using (var binaryReader = new BinaryReader(memoryStream))
                {
                    for (var i = 0; i < num; i++)
                    {
                        var key = (int) binaryReader.ReadInt16();
                        var value = (int) binaryReader.ReadInt16();
                        _sortIdx[key] = value;
                    }
                }
            }
        }

        private static void CreateIdxMrkList(byte[] bin)
        {
            var num = bin.Length / 6;
            _mrk2Idx = new Dictionary<int, int>(num);
            _mrk2Rare = new Dictionary<int, int>(num);
            using (var memoryStream = new MemoryStream(bin))
            {
                using (var binaryReader = new BinaryReader(memoryStream))
                {
                    for (var i = 0; i < num; i++)
                    {
                        var key = (int) binaryReader.ReadInt16();
                        var value = (int) binaryReader.ReadInt16();
                        var value2 = (int) binaryReader.ReadInt16();
                        _mrk2Idx[key] = value;
                        _mrk2Rare[key] = value2;
                    }
                }
            }
        }

        private static byte[] Decompress(byte[] src)
        {
            return DeflateStream.UncompressBuffer(src);
        }

        private static bool CheckCompressed(byte[] data)
        {
            if (data.Length <= 4) return false;
            var array = new byte[4];
            Buffer.BlockCopy(data, 0, array, 0, 4);
            return CompareHeader(array, CompressedFileHeader);
        }

        private static bool CompareHeader(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            return !a.Where((t, i) => t != b[i]).Any();
        }

        private enum Rarity
        {
            None,
            Normal,
            Rare,
            SuperRare,
            UltraRare
        }

        private enum Streams
        {
            CardIndex,
            CardName,
            CardDesc,
            WordIndex,
            WordText,
            DialogIndex,
            DialogText,
            RubyIndex,
            RubyName,
            SearchName
        }

        private enum Type
        {
            BuiltIn,
            AssetBundle,
            Binary,
            Network,
            StreamingAssets,
            StreamingBinary
        }
    }

    public static class Crc32
    {
        private const int CharBit = 8;

        private static readonly uint[] Crc32Table =
        {
            0u,
            1996959894u,
            3993919788u,
            2567524794u,
            124634137u,
            1886057615u,
            3915621685u,
            2657392035u,
            249268274u,
            2044508324u,
            3772115230u,
            2547177864u,
            162941995u,
            2125561021u,
            3887607047u,
            2428444049u,
            498536548u,
            1789927666u,
            4089016648u,
            2227061214u,
            450548861u,
            1843258603u,
            4107580753u,
            2211677639u,
            325883990u,
            1684777152u,
            4251122042u,
            2321926636u,
            335633487u,
            1661365465u,
            4195302755u,
            2366115317u,
            997073096u,
            1281953886u,
            3579855332u,
            2724688242u,
            1006888145u,
            1258607687u,
            3524101629u,
            2768942443u,
            901097722u,
            1119000684u,
            3686517206u,
            2898065728u,
            853044451u,
            1172266101u,
            3705015759u,
            2882616665u,
            651767980u,
            1373503546u,
            3369554304u,
            3218104598u,
            565507253u,
            1454621731u,
            3485111705u,
            3099436303u,
            671266974u,
            1594198024u,
            3322730930u,
            2970347812u,
            795835527u,
            1483230225u,
            3244367275u,
            3060149565u,
            1994146192u,
            31158534u,
            2563907772u,
            4023717930u,
            1907459465u,
            112637215u,
            2680153253u,
            3904427059u,
            2013776290u,
            251722036u,
            2517215374u,
            3775830040u,
            2137656763u,
            141376813u,
            2439277719u,
            3865271297u,
            1802195444u,
            476864866u,
            2238001368u,
            4066508878u,
            1812370925u,
            453092731u,
            2181625025u,
            4111451223u,
            1706088902u,
            314042704u,
            2344532202u,
            4240017532u,
            1658658271u,
            366619977u,
            2362670323u,
            4224994405u,
            1303535960u,
            984961486u,
            2747007092u,
            3569037538u,
            1256170817u,
            1037604311u,
            2765210733u,
            3554079995u,
            1131014506u,
            879679996u,
            2909243462u,
            3663771856u,
            1141124467u,
            855842277u,
            2852801631u,
            3708648649u,
            1342533948u,
            654459306u,
            3188396048u,
            3373015174u,
            1466479909u,
            544179635u,
            3110523913u,
            3462522015u,
            1591671054u,
            702138776u,
            2966460450u,
            3352799412u,
            1504918807u,
            783551873u,
            3082640443u,
            3233442989u,
            3988292384u,
            2596254646u,
            62317068u,
            1957810842u,
            3939845945u,
            2647816111u,
            81470997u,
            1943803523u,
            3814918930u,
            2489596804u,
            225274430u,
            2053790376u,
            3826175755u,
            2466906013u,
            167816743u,
            2097651377u,
            4027552580u,
            2265490386u,
            503444072u,
            1762050814u,
            4150417245u,
            2154129355u,
            426522225u,
            1852507879u,
            4275313526u,
            2312317920u,
            282753626u,
            1742555852u,
            4189708143u,
            2394877945u,
            397917763u,
            1622183637u,
            3604390888u,
            2714866558u,
            953729732u,
            1340076626u,
            3518719985u,
            2797360999u,
            1068828381u,
            1219638859u,
            3624741850u,
            2936675148u,
            906185462u,
            1090812512u,
            3747672003u,
            2825379669u,
            829329135u,
            1181335161u,
            3412177804u,
            3160834842u,
            628085408u,
            1382605366u,
            3423369109u,
            3138078467u,
            570562233u,
            1426400815u,
            3317316542u,
            2998733608u,
            733239954u,
            1555261956u,
            3268935591u,
            3050360625u,
            752459403u,
            1541320221u,
            2607071920u,
            3965973030u,
            1969922972u,
            40735498u,
            2617837225u,
            3943577151u,
            1913087877u,
            83908371u,
            2512341634u,
            3803740692u,
            2075208622u,
            213261112u,
            2463272603u,
            3855990285u,
            2094854071u,
            198958881u,
            2262029012u,
            4057260610u,
            1759359992u,
            534414190u,
            2176718541u,
            4139329115u,
            1873836001u,
            414664567u,
            2282248934u,
            4279200368u,
            1711684554u,
            285281116u,
            2405801727u,
            4167216745u,
            1634467795u,
            376229701u,
            2685067896u,
            3608007406u,
            1308918612u,
            956543938u,
            2808555105u,
            3495958263u,
            1231636301u,
            1047427035u,
            2932959818u,
            3654703836u,
            1088359270u,
            936918000u,
            2847714899u,
            3736837829u,
            1202900863u,
            817233897u,
            3183342108u,
            3401237130u,
            1404277552u,
            615818150u,
            3134207493u,
            3453421203u,
            1423857449u,
            601450431u,
            3009837614u,
            3294710456u,
            1567103746u,
            711928724u,
            3020668471u,
            3272380065u,
            1510334235u,
            755167117u
        };

        private static uint GetMemCrc32(uint crc32, byte[] data, int size)
        {
            if (data == null) return crc32;
            var num = 0;
            while (size != 0)
            {
                crc32 = (crc32 >> CharBit) ^ Crc32Table[(byte) crc32 ^ data[num]];
                num++;
                size--;
            }

            return crc32;
        }

        public static uint GetStringCrc32(string str)
        {
            var num = uint.MaxValue;
            if (string.IsNullOrEmpty(str)) return num ^ uint.MaxValue;
            var bytes = Encoding.UTF8.GetBytes(str);
            num = GetMemCrc32(num, bytes, bytes.Length);
            return num ^ uint.MaxValue;
        }
    }

    public struct Property
    {
        public override string ToString()
        {
            return Mrk.ToString();
        }

        public Property(int param1, int param2)
        {
            _bit1 = new BitVector32(param1);
            _mrk = BitVector32.CreateSection(16383);
            _attack = BitVector32.CreateSection(511, _mrk);
            _defence = BitVector32.CreateSection(511, _attack);
            _bit2 = new BitVector32(param2);
            _exist = BitVector32.CreateSection(1);
            _kind = BitVector32.CreateSection(63, _exist);
            _attr = BitVector32.CreateSection(15, _kind);
            _level = BitVector32.CreateSection(15, _attr);
            _icon = BitVector32.CreateSection(7, _level);
            _type = BitVector32.CreateSection(31, _icon);
            _scaleL = BitVector32.CreateSection(15, _type);
            _scaleR = BitVector32.CreateSection(15, _scaleL);
        }

        public int Mrk => _bit1[_mrk];

        public int Atk => _bit1[_attack];

        public int Def => _bit1[_defence];

        public bool Exist => _bit2[_exist] > 0;

        public Kind Kind => (Kind) _bit2[_kind];

        public Attribute Attr => (Attribute) _bit2[_attr];

        public int Level => _bit2[_attr];

        public Icon Icon => (Icon) _bit2[_icon];

        public Type Type => (Type) _bit2[_type];

        public int ScaleL => _bit2[_scaleL];

        public int ScaleR => _bit2[_scaleR];

        private readonly BitVector32.Section _mrk;

        private readonly BitVector32.Section _attack;

        private readonly BitVector32.Section _defence;

        private readonly BitVector32.Section _exist;

        private readonly BitVector32.Section _kind;

        private readonly BitVector32.Section _attr;

        private readonly BitVector32.Section _level;

        private readonly BitVector32.Section _icon;

        private readonly BitVector32.Section _type;

        private readonly BitVector32.Section _scaleL;

        private readonly BitVector32.Section _scaleR;

        private readonly BitVector32 _bit1;

        private readonly BitVector32 _bit2;
    }

    public enum Attribute
    {
        Null,
        Light,
        Dark,
        Water,
        Fire,
        Earth,
        Wind,
        God,
        Magic,
        Trap
    }

    public enum Type
    {
        Null,
        Dragon,
        Undead,
        Devil,
        Flame,
        Poseidon,
        Sandrock,
        Machine,
        Fish,
        Dinosaurs,
        Insect,
        Beast,
        BeastBtl,
        Botanical,
        Aquarius,
        Soldier,
        Bird,
        Angel,
        Wizard,
        Thunder,
        Reptiles,
        Psychic,
        Mystdragon,
        God,
        Creator,
        Magic,
        Trap
    }

    public enum Icon
    {
        Null,
        Counter,
        Field,
        Equip,
        Continuous,
        QuickPlay,
        Ritual
    }

    public enum Kind
    {
        Normal,
        Effect,
        Fusion,
        FusionFx,
        Ritual,
        RitualFx,
        Toon,
        Spirit,
        Union,
        Dual,
        Token,
        God,
        Dummy,
        Magic,
        Trap,
        Tuner,
        TunerFx,
        Sync,
        SyncFx,
        SyncTuner,
        Dtuner,
        Dsync,
        Xyz,
        XyzFx,
        Flip,
        Pend,
        PendFx,
        SpEffect,
        SpToon,
        SpSpirit,
        SpTuner,
        SpDtuner,
        FlipTuner,
        PendTuner,
        XyzPend
    }

    public static class Crypto
    {
        public static byte[] Decrypt(byte[] src, string pass, string salt)
        {
            byte[] result;
            var utf = Encoding.UTF8;
            var array = new byte[4096];
            var rijndaelManaged = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                KeySize = 256,
                BlockSize = 128
            };
            GenerateKeyAndIv(utf.GetBytes(pass), utf.GetBytes(salt), 2048, out var rgbKey, out var rgbIv);
            var array2 = new byte[src.Length];
            Array.Copy(src, array2, src.Length);
            using (var memoryStream = new MemoryStream())
            {
                using (var memoryStream2 = new MemoryStream(array2, false))
                {
                    using (var cryptoTransform = rijndaelManaged.CreateDecryptor(rgbKey, rgbIv))
                    {
                        using (var cryptoStream =
                            new CryptoStream(memoryStream2, cryptoTransform, CryptoStreamMode.Read))
                        {
                            int count;
                            while ((count = cryptoStream.Read(array, 0, array.Length)) > 0)
                                memoryStream.Write(array, 0, count);
                            memoryStream.Flush();
                        }
                    }
                }

                memoryStream.Position = 0L;
                result = memoryStream.ToArray();
            }

            return result;
        }

        private static void GenerateKeyAndIv(byte[] pass, byte[] salt, int count, out byte[] key, out byte[] iv)
        {
            var list = new List<byte>();
            var num = pass.Length + (salt?.Length ?? 0);
            var array2 = new byte[num];
            Buffer.BlockCopy(pass, 0, array2, 0, pass.Length);
            if (salt != null)
                Buffer.BlockCopy(salt, 0, array2, pass.Length, salt.Length);
            var array = ComputeHash(array2);
            for (var i = 1; i < count; i++)
                array = ComputeHash(array);
            list.AddRange(array);
            while (list.Count < 48)
            {
                num = array.Length + pass.Length + (salt?.Length ?? 0);
                array2 = new byte[num];
                Buffer.BlockCopy(array, 0, array2, 0, array.Length);
                Buffer.BlockCopy(pass, 0, array2, array.Length, pass.Length);
                if (salt != null)
                    Buffer.BlockCopy(salt, 0, array2, array.Length + pass.Length, salt.Length);
                array = ComputeHash(array2);
                for (var j = 1; j < count; j++)
                    array = ComputeHash(array);
                list.AddRange(array);
            }

            key = new byte[32];
            iv = new byte[16];
            list.CopyTo(0, key, 0, 32);
            list.CopyTo(32, iv, 0, 16);
        }

        private static byte[] ComputeHash(byte[] data)
        {
            byte[] hash;
            using (var memoryStream = new MemoryStream(data))
            {
                memoryStream.Position = 0L;
                var array = new byte[4096];
                var sha1Managed = new SHA1Managed();
                while (memoryStream.Position + array.Length < memoryStream.Length)
                {
                    memoryStream.Read(array, 0, array.Length);
                    sha1Managed.TransformBlock(array, 0, array.Length, array, 0);
                }

                var num = (int) (memoryStream.Length - memoryStream.Position);
                memoryStream.Read(array, 0, num);
                sha1Managed.TransformFinalBlock(array, 0, num);
                hash = sha1Managed.Hash;
            }

            return hash;
        }
    }

    internal static class MessagePack
    {
        private static readonly MiniMessagePacker Packer = new MiniMessagePacker();

        static MessagePack()
        {
        }

        public static object Unpack(byte[] buf)
        {
            return Packer.Unpack(buf);
        }
    }

    internal class MiniMessagePacker
    {
        private readonly Encoding _encoder = Encoding.UTF8;

        private readonly byte[] _tmp0 = new byte[8];

        private readonly byte[] _tmp1 = new byte[8];

        private byte[] _stringBuf = new byte[128];

        private void Pack(Stream s, object o)
        {
            string val;
            IList list;
            IDictionary dict;
            if (o == null)
                PackNull(s);
            else if ((val = o as string) != null)
                Pack(s, val);
            else if ((list = o as IList) != null)
                Pack(s, list);
            else if ((dict = o as IDictionary) != null)
                Pack(s, dict);
            else
                switch (o)
                {
                    case bool _:
                        Pack(s, (bool) o);
                        break;
                    case sbyte _:
                        Pack(s, (sbyte) o);
                        break;
                    case byte _:
                        Pack(s, (byte) o);
                        break;
                    case short _:
                        Pack(s, (short) o);
                        break;
                    case ushort _:
                        Pack(s, (ushort) o);
                        break;
                    case int _:
                        Pack(s, (int) o);
                        break;
                    case uint _:
                        Pack(s, (uint) o);
                        break;
                    case long _:
                        Pack(s, (long) o);
                        break;
                    case ulong _:
                        Pack(s, (ulong) o);
                        break;
                    case float _:
                        Pack(s, (float) o);
                        break;
                    case double _:
                        Pack(s, (double) o);
                        break;
                    default:
                        Pack(s, o.ToString());
                        break;
                }
        }

        private static void PackNull(Stream s)
        {
            s.WriteByte(192);
        }

        private void Pack(Stream s, IList list)
        {
            var count = list.Count;
            if (count < 16)
            {
                s.WriteByte((byte) (144 + count));
            }
            else if (count < 65536)
            {
                s.WriteByte(220);
                Write(s, (ushort) count);
            }
            else
            {
                s.WriteByte(221);
                Write(s, (uint) count);
            }

            var enumerator = list.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var o = enumerator.Current;
                    Pack(s, o);
                }
            }
            finally
            {
                IDisposable disposable;
                if ((disposable = enumerator as IDisposable) != null)
                    disposable.Dispose();
            }
        }

        private void Pack(Stream s, IDictionary dict)
        {
            var count = dict.Count;
            if (count < 16)
            {
                s.WriteByte((byte) (128 + count));
            }
            else if (count < 65536)
            {
                s.WriteByte(222);
                Write(s, (ushort) count);
            }
            else
            {
                s.WriteByte(223);
                Write(s, (uint) count);
            }

            var enumerator = dict.Keys.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    var obj = enumerator.Current;
                    Pack(s, obj);
                    Pack(s, dict[obj]);
                }
            }
            finally
            {
                IDisposable disposable;
                if ((disposable = enumerator as IDisposable) != null)
                    disposable.Dispose();
            }
        }

        private static void Pack(Stream s, bool val)
        {
            s.WriteByte((byte) (!val ? 194 : 195));
        }

        private void Pack(Stream s, sbyte val)
        {
            if (val >= -32)
            {
                s.WriteByte((byte) val);
            }
            else
            {
                _tmp0[0] = 208;
                _tmp0[1] = (byte) val;
                s.Write(_tmp0, 0, 2);
            }
        }

        private void Pack(Stream s, byte val)
        {
            if (val <= 127)
            {
                s.WriteByte(val);
            }
            else
            {
                _tmp0[0] = 204;
                _tmp0[1] = val;
                s.Write(_tmp0, 0, 2);
            }
        }

        private void Pack(Stream s, short val)
        {
            if (val >= 0)
            {
                Pack(s, (ushort) val);
            }
            else if (val >= -128)
            {
                Pack(s, (sbyte) val);
            }
            else
            {
                s.WriteByte(209);
                Write(s, (ushort) val);
            }
        }

        private void Pack(Stream s, ushort val)
        {
            if (val < 256)
            {
                Pack(s, (byte) val);
            }
            else
            {
                s.WriteByte(205);
                Write(s, val);
            }
        }

        private void Pack(Stream s, int val)
        {
            if (val >= 0)
            {
                Pack(s, (uint) val);
            }
            else if (val >= -128)
            {
                Pack(s, (sbyte) val);
            }
            else if (val >= -32768)
            {
                s.WriteByte(209);
                Write(s, (ushort) val);
            }
            else
            {
                s.WriteByte(210);
                Write(s, (uint) val);
            }
        }

        private void Pack(Stream s, uint val)
        {
            if (val < 256u)
            {
                Pack(s, (byte) val);
            }
            else if (val < 65536u)
            {
                s.WriteByte(205);
                Write(s, (ushort) val);
            }
            else
            {
                s.WriteByte(206);
                Write(s, val);
            }
        }

        private void Pack(Stream s, long val)
        {
            if (val >= 0L)
            {
                Pack(s, (ulong) val);
            }
            else if (val >= -128L)
            {
                Pack(s, (sbyte) val);
            }
            else if (val >= -32768L)
            {
                s.WriteByte(209);
                Write(s, (ushort) val);
            }
            else if (val >= -2147483648L)
            {
                s.WriteByte(210);
                Write(s, (uint) val);
            }
            else
            {
                s.WriteByte(211);
                Write(s, (ulong) val);
            }
        }

        private void Pack(Stream s, ulong val)
        {
            if (val < 256UL)
            {
                Pack(s, (byte) val);
            }
            else if (val < 65536UL)
            {
                s.WriteByte(205);
                Write(s, (ushort) val);
            }
            else if (val < 4294967296UL)
            {
                s.WriteByte(206);
                Write(s, (uint) val);
            }
            else
            {
                s.WriteByte(207);
                Write(s, val);
            }
        }

        private void Pack(Stream s, float val)
        {
            var bytes = BitConverter.GetBytes(val);
            s.WriteByte(202);
            if (BitConverter.IsLittleEndian)
            {
                _tmp0[0] = bytes[3];
                _tmp0[1] = bytes[2];
                _tmp0[2] = bytes[1];
                _tmp0[3] = bytes[0];
                s.Write(_tmp0, 0, 4);
            }
            else
            {
                s.Write(bytes, 0, 4);
            }
        }

        private void Pack(Stream s, double val)
        {
            var bytes = BitConverter.GetBytes(val);
            s.WriteByte(203);
            if (BitConverter.IsLittleEndian)
            {
                _tmp0[0] = bytes[7];
                _tmp0[1] = bytes[6];
                _tmp0[2] = bytes[5];
                _tmp0[3] = bytes[4];
                _tmp0[4] = bytes[3];
                _tmp0[5] = bytes[2];
                _tmp0[6] = bytes[1];
                _tmp0[7] = bytes[0];
                s.Write(_tmp0, 0, 8);
            }
            else
            {
                s.Write(bytes, 0, 8);
            }
        }

        private void Pack(Stream s, string val)
        {
            var bytes = _encoder.GetBytes(val);
            if (bytes.Length < 32)
            {
                s.WriteByte((byte) (160 + bytes.Length));
            }
            else if (bytes.Length < 256)
            {
                s.WriteByte(217);
                s.WriteByte((byte) bytes.Length);
            }
            else if (bytes.Length < 65536)
            {
                s.WriteByte(218);
                Write(s, (ushort) bytes.Length);
            }
            else
            {
                s.WriteByte(219);
                Write(s, (uint) bytes.Length);
            }

            s.Write(bytes, 0, bytes.Length);
        }

        private void Write(Stream s, ushort val)
        {
            _tmp0[0] = (byte) (val >> 8);
            _tmp0[1] = (byte) val;
            s.Write(_tmp0, 0, 2);
        }

        private void Write(Stream s, uint val)
        {
            _tmp0[0] = (byte) (val >> 24);
            _tmp0[1] = (byte) (val >> 16);
            _tmp0[2] = (byte) (val >> 8);
            _tmp0[3] = (byte) val;
            s.Write(_tmp0, 0, 4);
        }

        private void Write(Stream s, ulong val)
        {
            _tmp0[0] = (byte) (val >> 56);
            _tmp0[1] = (byte) (val >> 48);
            _tmp0[2] = (byte) (val >> 40);
            _tmp0[3] = (byte) (val >> 32);
            _tmp0[4] = (byte) (val >> 24);
            _tmp0[5] = (byte) (val >> 16);
            _tmp0[6] = (byte) (val >> 8);
            _tmp0[7] = (byte) val;
            s.Write(_tmp0, 0, 8);
        }

        private object Unpack(byte[] buf, int offset, int size)
        {
            object result;
            using (var memoryStream = new MemoryStream(buf, offset, size))
            {
                result = Unpack(memoryStream);
            }

            return result;
        }

        public object Unpack(byte[] buf)
        {
            return Unpack(buf, 0, buf.Length);
        }

        private object Unpack(Stream s)
        {
            var num = s.ReadByte();
            if (num < 0)
                throw new FormatException();
            if (num <= 127)
                return (long) num;
            if (num <= 143)
                return UnpackMap(s, num & 15);
            if (num <= 159)
                return UnpackArray(s, num & 15);
            if (num <= 191)
                return UnpackString(s, num & 31);
            if (num >= 224)
                return (long) (sbyte) num;
            switch (num)
            {
                case 192:
                    return null;
                case 194:
                    return false;
                case 195:
                    return true;
                case 196:
                    return UnpackBinary(s, s.ReadByte());
                case 197:
                    return UnpackBinary(s, UnpackUint16(s));
                case 198:
                    return UnpackBinary(s, UnpackUint32(s));
                case 202:
                    s.Read(_tmp0, 0, 4);
                    if (!BitConverter.IsLittleEndian) return (double) BitConverter.ToSingle(_tmp0, 0);
                    _tmp1[0] = _tmp0[3];
                    _tmp1[1] = _tmp0[2];
                    _tmp1[2] = _tmp0[1];
                    _tmp1[3] = _tmp0[0];
                    return (double) BitConverter.ToSingle(_tmp1, 0);
                case 203:
                    s.Read(_tmp0, 0, 8);
                    if (!BitConverter.IsLittleEndian) return BitConverter.ToDouble(_tmp0, 0);
                    _tmp1[0] = _tmp0[7];
                    _tmp1[1] = _tmp0[6];
                    _tmp1[2] = _tmp0[5];
                    _tmp1[3] = _tmp0[4];
                    _tmp1[4] = _tmp0[3];
                    _tmp1[5] = _tmp0[2];
                    _tmp1[6] = _tmp0[1];
                    _tmp1[7] = _tmp0[0];
                    return BitConverter.ToDouble(_tmp1, 0);
                case 204:
                    return (long) s.ReadByte();
                case 205:
                    return UnpackUint16(s);
                case 206:
                    return UnpackUint32(s);
                case 207:
                    if (s.Read(_tmp0, 0, 8) != 8)
                        throw new FormatException();
                    return ((long) _tmp0[0] << 56) | ((long) _tmp0[1] << 48) | ((long) _tmp0[2] << 40) |
                           (((long) _tmp0[3] << 32) + ((long) _tmp0[4] << 24)) | ((long) _tmp0[5] << 16) |
                           ((long) _tmp0[6] << 8) | _tmp0[7];
                case 208:
                    return (long) (sbyte) s.ReadByte();
                case 209:
                    if (s.Read(_tmp0, 0, 2) != 2)
                        throw new FormatException();
                    return ((long) (sbyte) _tmp0[0] << 8) | _tmp0[1];
                case 210:
                    if (s.Read(_tmp0, 0, 4) != 4)
                        throw new FormatException();
                    return ((long) (sbyte) _tmp0[0] << 24) | ((long) _tmp0[1] << 16) | ((long) _tmp0[2] << 8) |
                           _tmp0[3];
                case 211:
                    if (s.Read(_tmp0, 0, 8) != 8)
                        throw new FormatException();
                    return ((long) (sbyte) _tmp0[0] << 56) | ((long) _tmp0[1] << 48) | ((long) _tmp0[2] << 40) |
                           (((long) _tmp0[3] << 32) + ((long) _tmp0[4] << 24)) | ((long) _tmp0[5] << 16) |
                           ((long) _tmp0[6] << 8) | _tmp0[7];
                case 217:
                    return UnpackString(s, s.ReadByte());
                case 218:
                    return UnpackString(s, UnpackUint16(s));
                case 219:
                    return UnpackString(s, UnpackUint32(s));
                case 220:
                    return UnpackArray(s, UnpackUint16(s));
                case 221:
                    return UnpackArray(s, UnpackUint32(s));
                case 222:
                    return UnpackMap(s, UnpackUint16(s));
                case 223:
                    return UnpackMap(s, UnpackUint32(s));
            }

            return null;
        }

        private long UnpackUint16(Stream s)
        {
            if (s.Read(_tmp0, 0, 2) != 2)
                throw new FormatException();
            return (_tmp0[0] << 8) | _tmp0[1];
        }

        private long UnpackUint32(Stream s)
        {
            if (s.Read(_tmp0, 0, 4) != 4)
                throw new FormatException();
            return ((long) _tmp0[0] << 24) | ((long) _tmp0[1] << 16) | ((long) _tmp0[2] << 8) | _tmp0[3];
        }

        private string UnpackString(Stream s, long len)
        {
            if (_stringBuf.Length < len)
                _stringBuf = new byte[len];
            s.Read(_stringBuf, 0, (int) len);
            return _encoder.GetString(_stringBuf, 0, (int) len);
        }

        private byte[] UnpackBinary(Stream s, long len)
        {
            var array = new byte[len];
            s.Read(array, 0, (int) len);
            return array;
        }

        private List<object> UnpackArray(Stream s, long len)
        {
            var list = new List<object>((int) len);
            for (var num = 0L; num < len; num += 1L)
                list.Add(Unpack(s));
            return list;
        }

        private Dictionary<string, object> UnpackMap(Stream s, long len)
        {
            var dictionary = new Dictionary<string, object>((int) len);
            for (var num = 0L; num < len; num += 1L)
            {
                var text = Unpack(s) as string;
                var value = Unpack(s);
                if (text != null)
                    dictionary.Add(text, value);
            }

            return dictionary;
        }
    }
}