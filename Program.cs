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
        private static int _counter = 0;

        private static void Main()
        {
            const string first = "https://att-dl.akamaized.net/att/cdn/att-";
            const string second = "201805142039-ad8c7bd10a";
            var arr = new[]
            {
                "b1af9759bc0b085c0db2334c8e97527f7937e4a6", "3a5fbc9adf8780d2a214af8c213246a6177abda9",
                "52ab1a6b65f99a1dce2eec6ae4e9df4d732b44fa", "69b0c1003c6537d54ed287bbe5901d119f94eb84",
                "d0f061ef7fedbbafab4bfc894c1811541ad40ca5", "b263ee8f4217bd71d96165d615e31c8678a68465",
                "44080ab9d73b43c3afeda998cdab8f9733b1452a", "008b7e0fedc0171a66e98363f6b88a1e87145126",
                "64a6a2a6bd995c773587e2b45b39925362db6961", "68a221d695e7f628190ce3bdaf881aa16f9639cc",
                "0716ddf7a98da1cf31cb0f5d94113462b5c0aa0d", "cf39e3c237e3fd8fa3b2f254d320baed9219944d",
                "fa552ef77b3f4cea17f076b5f04fec1a4b1fa4d5", "d337ad63887142f936b983a819fa2c0ce8b567bb",
                "72c97a11f9d57abf425ec87c06c224631d56dab1", "42ddc95f9312adecd603f17b3504d16870ffda57",
                "1af32dc45a31d7bf398bcd5808cf3859f6bdc9a2", "226f64998843a3003e757e04bea3f6ae0664073d",
                "a1e6eff5ee0f05d4a3264696949ef4944f562da0", "1d1a3b64ef2b201255edce106da964845406c3b1",
                "9b0b63eddad3b56718e8b582ae6bed1440cfbcb1", "095839e19a404b84dd36aef367e2350ca8300534",
                "72d9f08e5100fbd06edb231c5f486d07d7871fdb", "ba79f07045876fce6be53fb26d4b1ab60495245a",
                "b56d8a3e1dca9be9f79047c456264b34ac6b648c", "00d331af609041bb0dd906bd0b8565cc0927cf9e",
                "1f97390f2850d401f6845a2ef3f4c6e148f37792", "c46f20a839d6e81f1df8148475b392077647d053",
                "dc1cf88139b4607a75a5f3311effbc167e28b4db", "29d56da6bb590866aad27723cced0ded17c5e7f2",
                "f050b6d80cf285adc19b894912e1a30e2de7f151", "40709779bed0419843021489d7f19d23090077ff",
                "78325709ce355ef163938f1573f33fc4801bc873", "4e46e59223d84f188ad23bb12933e870aaf3857a",
                "79777f63d2169bcbd7f6f36727e9d65b588386b1", "8a9dbeeb8156d05a846419d2f3c8bf7232c7d8cc",
                "9854e2440970067eb5edb2b1acf4b6a49d4139ce", "37ec7bd3a369db9a8ce99c6920cef261b86a8686",
                "7fa1737469dabbaf4c01b42eb0bbd60b9f55ffb2", "56a24dd53b9f80cfb3f57ceb534da51d4999f762",
                "811c6b45052de974df348d59bb189463e2ded076", "f64c72889aa996eac5bbee4fb7a8fc0d6740593d",
                "be3d192493b820e03e2d6b12cfcc5b22bb8112af", "00053e4ea4985d16063754130e69dfb8b8b7fbbc",
                "69f149c5273bbf78d92c4eb0559a5fc5bb559425", "e61724ba9192d524a36881b9355662b3344205a8",
                "a28ea3549e4e20ad66d3eba59a3f5d8ca87033ce", "48cb9cfdfd06d487bcbff0a9f1f6a5fe7fa050aa",
                "caa388426269ca88b7ee7de19bffd9c7b05e0411", "89c6b6394fa349f561e203fe9c565ebeecd1f8da",
                "4930672c2554dbdc5a105eaa7e3d6c891f5ea00d", "b13e6444e8467eabd2df6bdb93016617dd9984be",
                "b7b5063ffc7278c47e702bd57a692ae9b2344102", "6874eb9efce459c21f16f05afdf9df3768ef2fec",
                "e2e7704505798daafde186d9658485542f98719e", "fa8442086696ff09853d8f8de40c464fa3c6f3e7",
                "8ac83b63569b3eed939dd40102a41bcfb0f10ba1", "d13080a4b122cc9ed6102032ff0ea5705ce48527",
                "04db9f6fd76a443f375f91163cb21d4110e36ce3", "19604da26531a2d7b4c3760487558831dcc72197",
                "61339d53873f8c2be00b0dac7899a418edcc97f1", "2a99b1a534f346b2bac14377d2a30d20f78f945a",
                "1d25030f38fa9254f6eb5b47ce7b028a76bdee56", "3cd6660c9963ed36b9a335855c4807344ea53792",
                "7463231564c31aeb3d325ed7defa3e1ea74978ce", "df0a3e59e1fa97ea89d2eaeb742b7cb07861eb27",
                "e991522b161d73af0d1fcf5ae3469fa52f07dcae", "a41148fa02b5bae68037c26deee44e73fba90e01",
                "f15eb3bd6f568db3771fb2536a9959e04067a6d2", "64298f26a85fcf6f16dc1ce7f7abcbea2f6e5f68",
                "e7756bd9794f37d69a86f0519b4f441c325408e8", "b895e76d37ab06eef0588242c71e5413344fea66",
                "5dd20609114d6c944021366fadfdbcc1acb53c64", "aadf120da8aeb09a08c3032ed52286922a2a528a",
                "a997f9a9beae3d0420e780a2a82a5994d957398c", "5ae071bfbf7e016243a87bcaca80ea7770351d1c",
                "8461386109bca2be4338af04c0dc51d4e111bdbe", "c28afbf34ad699af31cb19ae396633c4d40560f7",
                "cc7fe10eabc1721349fe8d037508beb34968a36a", "6739b67014ecc10babe98558e3db0b754617f1a7",
                "f9e800618ffaa33d99ec4449e27dc7e722688551", "890ab570821f5c2727de8eb68b79b009e99b3b1f",
                "297b99f11de2dee30ef928fe1b48bcf2741ab2b3", "7b64214bc8ac63b607609528bd801b6badcd80c8",
                "ae02b41d2ff25058d0b691b5c0ac8b15a2996a06", "094e4590ddd67cfd4ed4c5ce31c74304698303de",
                "e8ec2e20956edea095dfceafdcda0e5aac9ecf48", "87b047693dd10c63bf96c4ed3acae7ef11cdda49",
                "932ba8186792a052e8b462f5ba77a053f5ffa9b1", "60e8209cd303941b71d1cf29deb3045e9e7ad63d",
                "feda67f557762618467b6595bf8a778d4ea2aaa2", "c1bfbf9295a82650543ad0f96a29f65b6f693ebd",
                "ac2e3d489194249475b1bed65a1bcf3529e327ce", "49e9333e5ce28289970da9f807ab8c33e0025e73",
                "a715a81b64316c0df8567cd1563cd166853b2f34", "b8b7eea53cf65be5652bccad110e9d319ca90979",
                "66e4657798318e126eab7db37c40a44d89bca8e8", "1002363d86a606c7dfa6d14e99a5a598c2abb1a5",
                "877f0ea38a4e74d16f4b84523fec8a0e555b6c20", "40cd17f186c76a06375e5eaec6b86e3f5c70ec3a",
                "12f440f11f28732794037626f9a4b6fbdc4e4647", "3653f5c6631d11b447ea83895ad1a85080165d00",
                "0c9f0a6f1d51efd9f4e1894bd1047438de20bdec", "01df222e9c8567d6a506ed179bcdba23a914aa5e",
                "d9b62481e5d524da699ea822a5816e3b1afcd564", "1110010049abfda41d3ffbc24d7a94d6e64697d8",
                "9f56dc55ec44bc2cb0c79fcca657f52a01fea1a2", "c3078939f74479c57c3d5639c962b9fbb6209c71",
                "5ed2ce7e395094ab302c8747b4e929805822c553", "c42d927edfcecbc6268a5aaf9a9332879e4f19bd",
                "b26fbf032afe865058755f3f9e845cd5fd80abb2", "aa59f7b673fccca709a3c5d5be98c36d08758cfd",
                "8bac27b997de95e9b47f65b1b3926447e81fc498", "f4bbf298398297e7d7d155bb38ea3203d8ea9e34",
                "dac5db4196994b0cf30d562362c5472198944d0a", "933ae289d19ade225e40db7637bea59e0d02c891",
                "65f8d405181b32200954fc61980fd93557f434b5", "21efd1eab85d31b7d168450ccf099631253528cc",
                "4217c6805afeb9d4adc483ec8c7008aba6499e4a", "79ffe3546e7f95320bd7805b787a9a02b245e7ae",
                "fa1c15263e635eefcbc9fadeb9daecc66a6efa09", "f3cea5b8a61340fcbb351bc504779b178b8e6c97",
                "bfddd7745a7417ca7971128bd29be089a40ace9b", "15414cdf736d711a25c3b5054d41946c9b8656ff",
                "daa54573f4b2961f6ca0c401f33cc724b728691c", "908f48e2925b9f45d60118e034526ba8e8f22bec",
                "61308c085c667d4d6486fce2b380daa80610bf24", "ad42608feaa3f27b5eebb2e1168e0986d5f898c1",
                "1b3f477072f8b80c6bec1a963733ea34b5fb9ac1", "2f500b33c59191b4a18953391edc652b9c4fc559",
                "746b604753f002b75bcd9687d17bbcf5c935e9d4", "5438590d3a9427df866cc0384bd13b1a858fddae",
                "c4935dfb567bccf31a4d7e44d3da6748e1c5c310", "a35f9daa44a3f68eb2c4575dcd3805406688eb38",
                "ce1d09975b970b2abfe584b60321260f1ef04542", "024e2773d26041db56d52a2da590b92a9357f21d",
                "4a4ee0c26781a075bd8b12f3a3e3bf95172f1a51", "785d77628a181e91b1aac0ffc4872c0c08e8f33a",
                "7a050aad59249f43f8d829efde01e3d3daacb9d3", "bf0cb2c47569f2789b128e461a9ae7c5a7c1e343",
                "aa0ebcab935e5abfffe61e942a0e44a5b5042443", "6d629a1c3776478bfe241c2cb4c2104240b4b0f7",
                "e75f10fa8b977609fe43bfbafec75eaad91c5950", "2cd5b6c52383065e00676934019f1a2b3bf84422",
                "3506875926fa2876d1fab00cb06416cd6d8e26b1", "06d909a51e80555a7a52ecaf7d8438c80e89bb66",
                "602ff1d8cb61794d133730b1c06a601273b8eef1", "d9977cf193a555b663d460201120ef37e9745498",
                "cb9b10292ca13be0bd2061e2180d5d18075360a6", "60ffaeb44ea118a3e1402a7d7a9bd280c2538b23",
                "9c81764f24754e26603d465c5ce6cbc9c8185f6e", "7e197bc56c497704e95c14edda32299d9e0acb93",
                "41157fa82abbd22de0ff71d7b23ceadf96b400b0", "4945a64ec1edaf6b7af45ff23040ddf23ac0bcf1",
                "b333c7da54664aabc7a6d2e8c1faa822b29254e6", "b9e8119d27deef065cfec9e3c03567edb7fc823d",
                "6a5ad9db0a3953d3a3099f5ae92f096feefaed02", "cb4992e21b120c8ee6ec82976d534875d3bb6949",
                "4aefd988733225192e243af6bb1cac146f04e390", "26305c33c4c111e898d79efd3e8482a1a42e7698",
                "6af12337398867f0a3474bfae53fdfd86ab0b981", "bf5fc9c706f3ed2134bb3339e4008752398040bc",
                "a04132ef8533e60c1db9156ab15cc1c2c77806c6", "1f84be8467a11e0f4269b352a1903fb8b18b5056",
                "9a619530ce63ed5ac681d490ff263c96c7d1b745", "5d06cc14ff5a14beaef56513fdf8037f704bd88c",
                "7aeae253f5aa9eb37388d8c94586ff2649c08758", "823f39f4ec98ffac4fbc4a9880a0c97298c2ac9e",
                "fd920e3c92b55e4f782088d10109a970b1ac355b", "281f3ca3531e904162b645fe333f58835696345f",
                "750d1abbbb26ad21b3eba7da5cb7df36cf42d7ac", "6179d59d242214a681277f2732ec00e93c0f18cc",
                "c90defc4767ec03cfc979fcc63ed93f72c0b9536", "ba3622e14e2dd2bdb0dd07126a96b59e798ee989",
                "622fab0b73ee17443ba8aaf58fe24ef1756f5c88", "73b095734191271c12bf08be5274909430527863",
                "b9c7911a78638543e7a4df005e05bd769fb5ad27", "4cefb815219c388d1d9061e012cd1d978db245b5",
                "1075eb10e744c480afef14182a31b22b3add9fb5", "085e67a88fea267ddf4e2e9130a7821bba6250de",
                "fd9d7a9711a75a40091172c165d07903569be6c5", "ceda025c596c5fb0ba594c2b7491807cabf0a5f3",
                "a579c98df5ca683c4dc4ed6cf605d8965b4d71f1", "bc3222b5d5951d64a4dc1fb75a116ab9ce0cbd97",
                "0746028748818b6ec2fb392e71f370977941759f", "6dcf67c8f171f3b284e4beb683da962febb5eda4",
                "fea235fb8862aa03d80a3f6509f99c796ba53614", "90746f2fc3480a656b087d38ad4d0598462af3db",
                "2aa2f3f8ae171b30b4e8beb0f0626669251149e3", "81330b366b6a00de240b7d835b0bec4afc5e6b88",
                "b987528d2ca42da7a600610ccbe24113926bfaf2", "33af27cfda79d00808c6adfb2984dfe06735f507",
                "36a8e357dbe02e0675e70b1322b63a76d5be80d4", "f546f9197f2851364c39ae3f152586f76911b4c5",
                "4162b34cc78b1aba0aed4e1a56b466b49e197216", "908bea81e73a72d93f0e977d601533587629d8a4",
                "1e6ee48b9b269b1b09bf4147fa778c471b278458", "c3301bda485127a9ec0b9bd862915b78345336ce",
                "1328244a757339d6c730394540c99cd48bb6736c", "6c78c3c19364171a59635db32343cfe82d0fb23c",
                "be3860a12211b2be84882a859f2960748a36b713", "d40547b055e3d4aef662e3e5c56eb5d650f86e09",
                "eee4ead6aafbe7a8a2fd1cc259ba6ced38504e13", "6dea009cab968223ea8f6cf5480af91c720b99aa",
                "246268a575b72375e4411efbc009f854021298d6", "ab992ce05d6c69bb493652e5b6a69742505abca7",
                "7eb6f5473dbb41b8b1d334405e6751df15ead945", "61291a54cb6df749528cced87d95a01aab3bc752",
                "85fed7242c3c85eedba2075122b8c87e3207d2e8", "e5cf57b8d8690246360f62fea5d7c5997dd92ed1",
                "4bc8ce64fbe41d55ea7f45a6c74c944ffd13a719", "18615d39c1d7d341dc60ad64e4db7e94bd3e9d4b",
                "44af7bc4cd7bda3acfc7d53d2be29343f0d42451", "a08464a0d260c40a70a497ace9a98d30c111581a",
                "09126344b5b158faecf34be29da5175266c8b875", "81c8abc3bede486fef3a8986b8c2bb486e3b6904",
                "4d7b1c9afd2344ec19a4436e460c784f47faff6a", "9b5799adaaaf0cf6ce184933bcd2fa5e53b76d0a",
                "21a7ef410b843c20ccbf3e44d0f178ebfff6f21a", "a95d14e5a6ad67fe10ee0fa05b4197f019d71fa5",
                "89e3db42898f67694d1b1bf19ad2de59e4fa5d0b", "fa4f6e1ee28da7189e8062ba5a476dafd97d58ce",
                "b2d9891ca37c7997289c33c0c54c758dd2eac43e", "9823fd7d179392eddf0d576bcbecef2fd6b5491f",
                "bddc65e8bd0b8735e981c9f116353e736a12686a", "b213f4a7718583e08d37e6899fbe879fe97dec64",
                "3c0e52617a09a29d7884c297484e4a5c3cfc5588", "a632de136296663a591d7556ba02916c22ac3963",
                "534f0c78a27ed3cdcaa13d4569c8f6e64d78e814", "1cab3866c8319da40694bef2ec4558185c841c8c",
                "868279764be5ad32e2ef69e954431fdcdef1140c", "3a1749b8693a400eb905b8b8cd73a6df2a9cf22b",
                "8d627301ea2c9a4656c8aa9a86f1423011b7f248", "7148a28a1a77b3c1160f4ac2800b479bf3aaf427",
                "5252292d84fdcc00ff65b818d8205caaefcde7fb", "2f1a7a5290df0e2089f7112bf152ddbe77557a24",
                "c36b1b8c0b147c92608506b5835b425900a583c6", "0b67eea7a6148066e0388dec6809814e3e7564d8",
                "576922908408a39465fab8dd1931e5993a885e4f", "126b320be01e25acee2d34e24ebfaf6b966e6fab",
                "b723634d35013ac783e5eb68cb7b897b490cdf51", "db08173da0ac1d7decfff735b412ae2e1331756a",
                "41a3d6a15c64c0bbad56e9a77f58beb5c0dd4193", "23fcb2952dc5b673ee8db7c3850e358d8b3fbf51",
                "3744bbc8f160041d165abbb769e09c73d6cd9fea", "e01c56130cfaf5bfb0efb1c9079fc72a97f59547",
                "fafd86b354282fc6444d345751ca6c4e51dcb1bd", "e9e0704b22c1513156532a67f081d515882e27c4",
                "0a3343559de8d75ebffebf065b3131a9b457022b", "50648bb59578c7add29e2a4b41d6d3ee83acdc94",
                "d901c7e1dd94166c0e2b37cd536242e66f5dc8de", "ee6ad842e6ebc189cf53d459fa58856db6b1b166",
                "1ff0bf51f5cc307bcd7fa55c2c1f11b99d4f42cc", "6f7924e852dc20597e6dbbab852d2276f1928178",
                "4dc6a2a40362b699f6dcc85b037d7c6a16a37ecb", "88482f071fe1330962df15654bcf60cfac26105a",
                "93cc97cd5cab7505a00771898fe21acf5bc44ec0", "522ca5d5a02c80c1da89da73d68bd3bfbee6aa83",
                "b44a65d8626c4e1566636bbf337ee8ea4325d1d4", "4fbc68b69ce9c33d764c46e0ad8b2c80dc3cb227",
                "f47984da75c269b31c99854e99c9e9ecc0d12e43", "82ce762f5da27324e0e4a620a8d61dd82671ebfe",
                "373fdb24b59913d31917bafba6a474b4b649b85d", "9e906038f3134283861b41791ee59604be6d1461",
                "52075fb3a341453aca5a09d136ecb8aadc34e756", "caebf7f8a6db34a609bc864388572303bca87448",
                "d7e6e2a1787512d029d97350b6d2286127803379", "93c498a3e4629adbcd39cc8faac7001b6c0dcfad",
                "566221e4af4c0d7edbe4aedbf81330219da6ed3f", "0922b258f6398caa5b82a14965334842d7f5d175",
                "b14dd033f0e8551072865095dadb6a8caf3421bc", "36d75db45cea9297e4c1d9612d683714d9087510",
                "aa8621ad9132113edf8cb81b97a3c6b833b99638", "f033f60228187210adac4a9b43e43cc1798a2b6e",
                "849291512fafaf6628204e8e6fedff039f620dc8", "83e3e4a752d125160d1619303ca5975af33d8cef",
                "c26d24cf96e93063418b2829e5779f9baffbc1a8", "aa3a037ff107b4922bce68f972cf7eaecc893f6d",
                "e625eb6a49aeabaab3ab954fad0d434177555847", "5cb689d83280259ff8dc86e4bd70fc5293ac1cb4",
                "a15418dea97bceeedda38e1511cf99d9397d9754", "d35c144c657084c458b5657f887b39511e3a3970",
                "e54523b0aed4b1a0ba1ee61b1d66ccfe938f0e92", "2704225b17ccc7e724d9d6b3719c1b98392ac10f",
                "42834bfe6877703bfb038cce4fefa2ffe6ed88db", "42b3b9c2290c19ef1608437e1a558b2e7e885994",
                "3fc4641b1a18b1ddf0aca07c2f21b8940bbc3830", "a0b53ab78068a4dd9310d94776af371333b45515",
                "49d9aca0734618f6999f08c49ad69b04fe44b632", "94260bb17e4ee14cf6320af7c3458beaef428a18",
                "8c3f5a341f68dc2314b02921e886b493282279f3", "2297a17187a10b70da3583aae1e76a7c1f10b0df",
                "a2872692d08653d420704acb437982b97e5f1bcf", "6127ce281fba4036de530b8cbe55bff15993ea34",
                "565e28cc12653cf3f7ebe35589f9ebe0af0fd791", "55d275b6070ab974b98c103d8a8334045bd6fc87",
                "8f825d2ecfc6ce3e47ff8196caf924348ec64529", "89ceb6fc586569467fd078621cea6ea0bbd5fa31",
                "d573349ca9a68f615add1e64d5eafc5b0da5b8e7", "cb160d03461366b5ef0d1cea8fcaf4c6afe29784",
                "05fd974ba3f5e25581ad2a3c986eea50d1ffcb6b", "74e2fd30718a6e516933d553ae77f3b562015b7d",
                "47e4188660ce7b9b025fd573e24037242221fd48", "7509fcd99758a1a47ce2a78a06e34b0b9d765ee6",
                "fe4c6f49c07d17253d8042669697d77645222fa5", "be27a1fda3182135c34b5f4f8e841696cee8e065",
                "2bb59914bc0da70636e0ea4c8c950b1e339bd3c0", "9c457e5ef3f5c26e3f7f4bdc7f3e8f11d21852c9",
                "769b03a567e4930065fb30f47c7ff3afb4f78df1", "f70b8c6d66db036ca5a0c207ed43bbbabd79baed",
                "14caf440f30fc555190ec4b322f6e45edac901d0", "0ba54646c85984c0cc49b5bee4f66ba0a3b6803b",
                "54c149ab1a6648600a58e38a5e3156442cf8f051", "2f1d428e4542f9254836bb9c1625ea5e911101ed",
                "2f8756268d48572cc6c14c38de1fd408b7115597", "13974fa0b9e8829f7a018c96044bf7a0646250a4",
                "bf495789e2153fe19f5b820062515d800df25772", "8ebb594fc2c13987517476d1a99d2571c434784f",
                "02ade08215094e423eb48c5b4439849fe97cb261", "3226a41ff71b73038158ae72770aa872432f66cb",
                "5a758f382eac5daa15886cc1a97b22b591d3d3cc", "72bbfb3a41f479bfe5be004a48dadb9ff2aff7d4",
                "b0d71677f26e5662938d85271e6816be151f69cc", "ec7334f74ef693586a81ab19e7581829bed83349",
                "aa8e8b92f41a61470f8e43fce1a56a8dafe3c615", "ce6f409549844b1c44e4eecee4dba0bb59975287",
                "2b4c33f2d0aa6cb40590deb6965d198e7428e0d2", "79750f73f973fa422906fac6cf9bd08df0a6db64",
                "653b9c8dd3da5abe134a838a6a4ca8706021ab4e", "8d563c74335867d3f3c818f5c0cf231ee6c0eaa9",
                "4586f6cf2b48707c6c61595af7c7d97f7780c874", "5b56f1f758fbb74a3ce888c4814b7f680155b04a"
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
                "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Yu-Gi-Oh! Duel Links\\LocalData\\f884fafa\\";
            var builder = new StringBuilder(basepath.Length + 16) {Length = 0};
            builder.Append(basepath);
            builder.Append(((int) ((crc & 4278190080u) >> 24)).ToString("x2"));
            builder.Append("\\");
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
                                File.WriteAllBytes($"{resourceType}/{(list?[0] as string)?.Replace('/', '&')}-{_counter}",
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

                                File.WriteAllBytes($"{resourceType}/{((string) list?[0])?.Replace('/', '&')}-{_counter}",
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
            // Note: this type is marked as 'beforefieldinit'.
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